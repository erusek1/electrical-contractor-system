using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Estimate Methods
        
        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name
                    FROM Estimates e
                    LEFT JOIN Customers c ON e.customer_id = c.customer_id
                    ORDER BY e.created_date DESC";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var estimate = new Estimate
                            {
                                EstimateId = reader.GetInt32("estimate_id"),
                                EstimateNumber = reader.GetString("estimate_number"),
                                Version = reader.GetInt32("version"),
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                TotalLaborHours = reader.IsDBNull(reader.GetOrdinal("total_labor_hours")) ? null : (decimal?)reader.GetDecimal("total_labor_hours"),
                                TotalMaterialCost = reader.IsDBNull(reader.GetOrdinal("total_material_cost")) ? null : (decimal?)reader.GetDecimal("total_material_cost"),
                                TotalCost = reader.IsDBNull(reader.GetOrdinal("total_cost")) ? null : (decimal?)reader.GetDecimal("total_cost"),
                                CreatedDate = reader.GetDateTime("created_date")
                            };
                            
                            // Add basic customer info if available
                            if (!reader.IsDBNull(reader.GetOrdinal("customer_name")))
                            {
                                estimate.Customer = new Customer 
                                { 
                                    CustomerId = estimate.CustomerId,
                                    Name = reader.GetString("customer_name") 
                                };
                            }
                            
                            estimates.Add(estimate);
                        }
                    }
                }
            }
            
            return estimates;
        }
        
        public Customer GetCustomerById(int customerId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Customers WHERE customer_id = @customer_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@customer_id", customerId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Customer
                            {
                                CustomerId = reader.GetInt32("customer_id"),
                                Name = reader.GetString("name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
                            };
                        }
                    }
                }
            }
            
            return null;
        }
        
        public void DeleteEstimate(int estimateId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // Cascading delete should handle related records
                var query = "DELETE FROM Estimates WHERE estimate_id = @estimate_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        #endregion
        
        #region Job Methods Extension
        
        public string GetLastJobNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT job_number FROM Jobs ORDER BY CAST(job_number AS UNSIGNED) DESC LIMIT 1";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }
        
        #endregion
        
        #region Estimate Conversion
        
        public async Task<Job> ConvertEstimateToJobAsync(Estimate estimate, ConversionOptions options)
        {
            return await Task.Run(() =>
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Create the job
                            var job = new Job
                            {
                                JobNumber = options.JobNumber,
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
                                Notes = options.Notes
                            };
                            
                            // Insert job
                            var jobQuery = @"
                                INSERT INTO Jobs (
                                    job_number, customer_id, job_name, address, city, state, zip,
                                    square_footage, num_floors, status, create_date, total_estimate,
                                    estimate_id, notes
                                ) VALUES (
                                    @job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                                    @square_footage, @num_floors, @status, @create_date, @total_estimate,
                                    @estimate_id, @notes
                                )";
                            
                            using (var cmd = new MySqlCommand(jobQuery, connection, transaction))
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
                                job.JobId = (int)cmd.LastInsertedId;
                            }
                            
                            // Create job stages if requested
                            if (options.IncludeAllStages && estimate.StageSummaries != null)
                            {
                                foreach (var stageSummary in estimate.StageSummaries)
                                {
                                    var stageQuery = @"
                                        INSERT INTO JobStages (
                                            job_id, stage_name, estimated_hours, estimated_material_cost
                                        ) VALUES (
                                            @job_id, @stage_name, @estimated_hours, @estimated_material_cost
                                        )";
                                    
                                    using (var cmd = new MySqlCommand(stageQuery, connection, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@job_id", job.JobId);
                                        cmd.Parameters.AddWithValue("@stage_name", stageSummary.Stage);
                                        cmd.Parameters.AddWithValue("@estimated_hours", stageSummary.EstimatedHours);
                                        cmd.Parameters.AddWithValue("@estimated_material_cost", 
                                            options.IncludeMaterialCosts ? stageSummary.EstimatedMaterial : 0);
                                        
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            // Copy room specifications if requested
                            if (options.IncludeRoomSpecifications && estimate.Rooms != null)
                            {
                                foreach (var room in estimate.Rooms)
                                {
                                    foreach (var item in room.LineItems)
                                    {
                                        var specQuery = @"
                                            INSERT INTO RoomSpecifications (
                                                job_id, room_name, item_description, quantity,
                                                item_code, unit_price, total_price
                                            ) VALUES (
                                                @job_id, @room_name, @item_description, @quantity,
                                                @item_code, @unit_price, @total_price
                                            )";
                                        
                                        using (var cmd = new MySqlCommand(specQuery, connection, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@job_id", job.JobId);
                                            cmd.Parameters.AddWithValue("@room_name", room.RoomName);
                                            cmd.Parameters.AddWithValue("@item_description", item.Description);
                                            cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                            cmd.Parameters.AddWithValue("@item_code", item.ItemCode);
                                            cmd.Parameters.AddWithValue("@unit_price", item.UnitPrice);
                                            cmd.Parameters.AddWithValue("@total_price", item.TotalPrice);
                                            
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                            
                            transaction.Commit();
                            return job;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            });
        }
        
        #endregion
    }
}
