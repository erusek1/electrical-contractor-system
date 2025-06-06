using System;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public class EstimateConversionService
    {
        private readonly DatabaseService _databaseService;
        
        public EstimateConversionService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        
        /// <summary>
        /// Converts an approved estimate into a job with all related data
        /// </summary>
        public Job ConvertEstimateToJob(Estimate estimate)
        {
            if (estimate.Status != EstimateStatus.Approved)
            {
                throw new InvalidOperationException("Only approved estimates can be converted to jobs.");
            }
            
            if (estimate.ConvertedToJobId.HasValue)
            {
                throw new InvalidOperationException("This estimate has already been converted to a job.");
            }
            
            using (var connection = new MySqlConnection(_databaseService.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Create the job
                        var job = new Job
                        {
                            JobNumber = GenerateNextJobNumber(connection, transaction),
                            CustomerId = estimate.CustomerId,
                            JobName = estimate.JobName,
                            Address = estimate.Address,
                            City = estimate.City,
                            State = estimate.State,
                            Zip = estimate.Zip,
                            SquareFootage = estimate.SquareFootage,
                            NumFloors = estimate.NumFloors,
                            Status = "In Progress",
                            CreateDate = DateTime.Now,
                            TotalEstimate = estimate.TotalCost,
                            EstimateId = estimate.EstimateId,
                            Notes = $"Created from Estimate #{estimate.EstimateNumber} v{estimate.Version}"
                        };
                        
                        // Insert the job
                        var jobId = InsertJob(job, connection, transaction);
                        job.JobId = jobId;
                        
                        // Create job stages from estimate summary
                        foreach (var summary in estimate.StageSummaries)
                        {
                            var stage = new JobStage
                            {
                                JobId = jobId,
                                StageName = summary.Stage,
                                EstimatedHours = summary.EstimatedHours,
                                EstimatedMaterialCost = summary.EstimatedMaterial,
                                ActualHours = 0,
                                ActualMaterialCost = 0
                            };
                            
                            InsertJobStage(stage, connection, transaction);
                        }
                        
                        // Create initial material entries from estimate line items
                        CreateInitialMaterialEntries(estimate, jobId, connection, transaction);
                        
                        // Update estimate to mark as converted
                        UpdateEstimateAsConverted(estimate.EstimateId, jobId, connection, transaction);
                        
                        // Create permit items from estimate
                        CreatePermitItems(estimate, jobId, connection, transaction);
                        
                        transaction.Commit();
                        
                        // Update the estimate object
                        estimate.ConvertedToJobId = jobId;
                        estimate.ConvertedDate = DateTime.Now;
                        estimate.Status = EstimateStatus.Converted;
                        
                        return job;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        
        private string GenerateNextJobNumber(MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "SELECT MAX(CAST(job_number AS UNSIGNED)) FROM Jobs WHERE job_number REGEXP '^[0-9]+$'";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                var result = cmd.ExecuteScalar();
                var lastNumber = result == DBNull.Value ? 400 : Convert.ToInt32(result);
                return (lastNumber + 1).ToString();
            }
        }
        
        private int InsertJob(Job job, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO Jobs (
                    job_number, customer_id, job_name, address, city, state, zip,
                    square_footage, num_floors, status, create_date, total_estimate,
                    estimate_id, notes
                ) VALUES (
                    @job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                    @square_footage, @num_floors, @status, @create_date, @total_estimate,
                    @estimate_id, @notes
                )";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@job_number", job.JobNumber);
                cmd.Parameters.AddWithValue("@customer_id", job.CustomerId);
                cmd.Parameters.AddWithValue("@job_name", job.JobName);
                cmd.Parameters.AddWithValue("@address", job.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", job.City ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@state", job.State ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zip", job.Zip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", job.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", job.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", job.Status);
                cmd.Parameters.AddWithValue("@create_date", job.CreateDate);
                cmd.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estimate_id", job.EstimateId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
                return (int)cmd.LastInsertedId;
            }
        }
        
        private void InsertJobStage(JobStage stage, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO JobStages (
                    job_id, stage_name, estimated_hours, estimated_material_cost,
                    actual_hours, actual_material_cost
                ) VALUES (
                    @job_id, @stage_name, @estimated_hours, @estimated_material_cost,
                    @actual_hours, @actual_material_cost
                )";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@job_id", stage.JobId);
                cmd.Parameters.AddWithValue("@stage_name", stage.StageName);
                cmd.Parameters.AddWithValue("@estimated_hours", stage.EstimatedHours);
                cmd.Parameters.AddWithValue("@estimated_material_cost", stage.EstimatedMaterialCost);
                cmd.Parameters.AddWithValue("@actual_hours", stage.ActualHours);
                cmd.Parameters.AddWithValue("@actual_material_cost", stage.ActualMaterialCost);
                
                cmd.ExecuteNonQuery();
                stage.StageId = (int)cmd.LastInsertedId;
            }
        }
        
        private void CreateInitialMaterialEntries(Estimate estimate, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Group line items by stage based on the price list item's labor minutes
            var itemsByStage = estimate.LineItems
                .Where(li => li.PriceListItem != null)
                .SelectMany(li => 
                {
                    // For each line item, determine which stages it belongs to based on labor minutes
                    var stages = li.PriceListItem.LaborMinutes
                        .Where(lm => lm.Minutes > 0)
                        .Select(lm => new { Stage = lm.Stage, LineItem = li })
                        .ToList();
                    
                    // If no labor minutes defined, put in "Finish" by default
                    if (!stages.Any())
                    {
                        stages.Add(new { Stage = "Finish", LineItem = li });
                    }
                    
                    return stages;
                })
                .GroupBy(x => x.Stage);
            
            foreach (var stageGroup in itemsByStage)
            {
                // Get the stage ID
                var getStageQuery = "SELECT stage_id FROM JobStages WHERE job_id = @job_id AND stage_name = @stage_name";
                int stageId;
                
                using (var cmd = new MySqlCommand(getStageQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    cmd.Parameters.AddWithValue("@stage_name", stageGroup.Key);
                    
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        // Create the stage if it doesn't exist
                        var stage = new JobStage
                        {
                            JobId = jobId,
                            StageName = stageGroup.Key,
                            EstimatedHours = 0,
                            EstimatedMaterialCost = 0,
                            ActualHours = 0,
                            ActualMaterialCost = 0
                        };
                        InsertJobStage(stage, connection, transaction);
                        stageId = stage.StageId;
                    }
                    else
                    {
                        stageId = Convert.ToInt32(result);
                    }
                }
                
                // Create material entries for this stage
                var totalCost = stageGroup.Sum(x => x.LineItem.TotalPrice);
                
                if (totalCost > 0)
                {
                    var materialQuery = @"
                        INSERT INTO MaterialEntries (
                            job_id, stage_id, vendor_id, date, cost, notes
                        ) VALUES (
                            @job_id, @stage_id, @vendor_id, @date, @cost, @notes
                        )";
                    
                    using (var cmd = new MySqlCommand(materialQuery, connection, transaction))
                    {
                        // Use a default vendor (you might want to make this configurable)
                        var defaultVendorId = GetDefaultVendorId(connection, transaction);
                        
                        cmd.Parameters.AddWithValue("@job_id", jobId);
                        cmd.Parameters.AddWithValue("@stage_id", stageId);
                        cmd.Parameters.AddWithValue("@vendor_id", defaultVendorId);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@cost", totalCost);
                        cmd.Parameters.AddWithValue("@notes", $"Initial material allocation from estimate - {stageGroup.Key} stage");
                        
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        
        private int GetDefaultVendorId(MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "SELECT vendor_id FROM Vendors WHERE name = 'Stock/Estimate' LIMIT 1";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                var result = cmd.ExecuteScalar();
                
                if (result == null || result == DBNull.Value)
                {
                    // Create default vendor
                    var insertQuery = "INSERT INTO Vendors (name) VALUES ('Stock/Estimate')";
                    using (var insertCmd = new MySqlCommand(insertQuery, connection, transaction))
                    {
                        insertCmd.ExecuteNonQuery();
                        return (int)insertCmd.LastInsertedId;
                    }
                }
                
                return Convert.ToInt32(result);
            }
        }
        
        private void UpdateEstimateAsConverted(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                UPDATE Estimates SET
                    status = 'Converted',
                    converted_to_job_id = @job_id,
                    converted_date = @converted_date
                WHERE estimate_id = @estimate_id";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@job_id", jobId);
                cmd.Parameters.AddWithValue("@converted_date", DateTime.Now);
                cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                
                cmd.ExecuteNonQuery();
            }
        }
        
        private void CreatePermitItems(Estimate estimate, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Count permit items from estimate line items
            var permitCounts = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Switches", 0 },
                { "Receptacles", 0 },
                { "Lights", 0 },
                { "Fans", 0 },
                { "240V Circuits", 0 },
                { "GFCI", 0 }
            };
            
            foreach (var item in estimate.LineItems)
            {
                var code = item.ItemCode.ToLower();
                var qty = item.Quantity;
                
                // Count based on item codes
                if (code == "s" || code == "3w" || code == "dim")
                    permitCounts["Switches"] += qty;
                else if (code == "o" || code == "fridge" || code == "micro" || code == "dw" || code == "hood")
                    permitCounts["Receptacles"] += qty;
                else if (code == "hh" || code == "pend" || code == "sc" || code == "van")
                    permitCounts["Lights"] += qty;
                else if (code == "ex-l")
                    permitCounts["Fans"] += qty;
                else if (code == "oven" || code == "cook")
                    permitCounts["240V Circuits"] += qty;
                else if (code == "gfi" || code == "arl")
                    permitCounts["GFCI"] += qty;
            }
            
            // Insert permit items
            foreach (var permit in permitCounts.Where(p => p.Value > 0))
            {
                var query = @"
                    INSERT INTO PermitItems (job_id, category, quantity, description)
                    VALUES (@job_id, @category, @quantity, @description)";
                
                using (var cmd = new MySqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    cmd.Parameters.AddWithValue("@category", permit.Key);
                    cmd.Parameters.AddWithValue("@quantity", permit.Value);
                    cmd.Parameters.AddWithValue("@description", $"Auto-generated from estimate conversion");
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}