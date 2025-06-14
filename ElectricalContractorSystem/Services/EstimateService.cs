using System;
using System.Collections.Generic;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public partial class EstimateService
    {
        private readonly DatabaseService _databaseService;

        public EstimateService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public string GenerateEstimateNumber()
        {
            var query = @"SELECT MAX(CAST(SUBSTRING(estimate_number, 5) AS UNSIGNED)) 
                         FROM Estimates 
                         WHERE estimate_number LIKE 'EST-%'";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    var lastNumber = result != DBNull.Value && result != null ? Convert.ToInt32(result) : 1000;
                    return $"EST-{lastNumber + 1}";
                }
            }
        }

        public Estimate GetEstimateById(int estimateId)
        {
            return GetEstimateWithDetails(estimateId);
        }

        public Estimate GetEstimateWithDetails(int estimateId)
        {
            Estimate estimate = null;

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();

                // Get estimate
                var estimateQuery = @"SELECT e.*, c.name as customer_name, c.address as customer_address, 
                                     c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                                     c.email as customer_email, c.phone as customer_phone
                                     FROM Estimates e
                                     JOIN Customers c ON e.customer_id = c.customer_id
                                     WHERE e.estimate_id = @estimateId";

                using (var command = new MySqlCommand(estimateQuery, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estimate = new Estimate
                            {
                                EstimateId = reader.GetInt32("estimate_id"),
                                EstimateNumber = reader.GetString("estimate_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                                CreateDate = reader.GetDateTime("create_date"),
                                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (DateTime?)null : reader.GetDateTime("expiration_date"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                Version = reader.IsDBNull(reader.GetOrdinal("version")) ? 1 : reader.GetInt32("version"),
                                ConvertedToJobId = reader.IsDBNull(reader.GetOrdinal("converted_to_job_id")) ? (int?)null : reader.GetInt32("converted_to_job_id"),
                                ConvertedDate = reader.IsDBNull(reader.GetOrdinal("converted_date")) ? (DateTime?)null : reader.GetDateTime("converted_date"),
                                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetString("created_by"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("customer_address")) ? null : reader.GetString("customer_address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("customer_city")) ? null : reader.GetString("customer_city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("customer_state")) ? null : reader.GetString("customer_state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("customer_zip")) ? null : reader.GetString("customer_zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("customer_email")) ? null : reader.GetString("customer_email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? null : reader.GetString("customer_phone")
                                }
                            };
                        }
                    }
                }

                if (estimate != null)
                {
                    // Get rooms
                    estimate.Rooms = GetEstimateRooms(connection, estimateId);

                    // Get line items for each room and add to estimate's LineItems collection
                    estimate.LineItems = new List<EstimateLineItem>();
                    foreach (var room in estimate.Rooms)
                    {
                        room.Items = GetRoomLineItems(connection, room.RoomId);
                        estimate.LineItems.AddRange(room.Items);
                    }

                    // Get stage summaries
                    estimate.StageSummaries = GetEstimateStageSummaries(connection, estimateId);

                    // Get permit items
                    estimate.PermitItems = GetEstimatePermitItems(connection, estimateId);

                    // Calculate totals
                    estimate.CalculateTotals();
                }

                return estimate;
            }
        }

        public List<EstimateStageSummary> GetEstimateStageSummaries(MySqlConnection connection, int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            var query = @"SELECT summary_id, stage, labor_hours, material_cost, labor_cost
                         FROM EstimateStageSummaries
                         WHERE estimate_id = @estimateId
                         ORDER BY FIELD(stage, 'Demo', 'Rough', 'Service', 'Finish', 'Extra', 'Temp Service', 'Inspection', 'Other')";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summaries.Add(new EstimateStageSummary
                        {
                            SummaryId = reader.GetInt32("summary_id"),
                            EstimateId = estimateId,
                            Stage = reader.GetString("stage"),
                            LaborHours = reader.GetDecimal("labor_hours"),
                            MaterialCost = reader.GetDecimal("material_cost"),
                            LaborCost = reader.GetDecimal("labor_cost")
                        });
                    }
                }
            }

            return summaries;
        }

        public List<EstimateStageSummary> GetEstimateStageSummaries(int estimateId)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                return GetEstimateStageSummaries(connection, estimateId);
            }
        }

        public List<EstimateRoom> GetEstimateRooms(MySqlConnection connection, int estimateId)
        {
            var rooms = new List<EstimateRoom>();
            var query = @"SELECT room_id, room_name, room_order, notes
                         FROM EstimateRooms
                         WHERE estimate_id = @estimateId
                         ORDER BY room_order";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new EstimateRoom
                        {
                            RoomId = reader.GetInt32("room_id"),
                            EstimateId = estimateId,
                            RoomName = reader.GetString("room_name"),
                            RoomOrder = reader.GetInt32("room_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                            Items = new List<EstimateLineItem>()
                        });
                    }
                }
            }

            return rooms;
        }

        public List<EstimateLineItem> GetRoomLineItems(MySqlConnection connection, int roomId)
        {
            var items = new List<EstimateLineItem>();
            var query = @"SELECT line_id, item_id, quantity, item_code, description, 
                                unit_price, material_cost, labor_minutes, line_order, notes
                         FROM EstimateLineItems
                         WHERE room_id = @roomId
                         ORDER BY line_order";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@roomId", roomId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new EstimateLineItem
                        {
                            LineId = reader.GetInt32("line_id"),
                            RoomId = roomId,
                            ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? (int?)null : reader.GetInt32("item_id"),
                            Quantity = reader.GetInt32("quantity"),
                            ItemCode = reader.IsDBNull(reader.GetOrdinal("item_code")) ? null : reader.GetString("item_code"),
                            ItemDescription = reader.GetString("description"),
                            UnitPrice = reader.GetDecimal("unit_price"),
                            MaterialCost = reader.GetDecimal("material_cost"),
                            LaborMinutes = reader.GetInt32("labor_minutes"),
                            LineOrder = reader.GetInt32("line_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        });
                    }
                }
            }

            return items;
        }

        public List<EstimatePermitItem> GetEstimatePermitItems(MySqlConnection connection, int estimateId)
        {
            var items = new List<EstimatePermitItem>();
            var query = @"SELECT permit_id, category, quantity, description
                         FROM EstimatePermitItems
                         WHERE estimate_id = @estimateId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new EstimatePermitItem
                        {
                            PermitItemId = reader.GetInt32("permit_id"),
                            EstimateId = estimateId,
                            Category = reader.GetString("category"),
                            Quantity = reader.GetInt32("quantity"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description")
                        });
                    }
                }
            }

            return items;
        }

        public void UpdateEstimateStatus(int estimateId, EstimateStatus status)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                var query = "UPDATE Estimates SET status = @status WHERE estimate_id = @estimateId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimateId);
                    command.Parameters.AddWithValue("@status", status.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }

        public int ConvertEstimateToJob(int estimateId, string jobNumber)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get the estimate
                        var estimate = GetEstimateWithDetails(estimateId);
                        if (estimate == null)
                        {
                            throw new InvalidOperationException("Estimate not found");
                        }

                        // Create the job
                        var job = new Job
                        {
                            JobNumber = jobNumber,
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
                            TotalEstimate = estimate.TotalPrice,
                            Notes = $"Created from estimate {estimate.EstimateNumber}"
                        };

                        // Insert the job
                        var jobId = InsertJob(connection, transaction, job);

                        // Create job stages from estimate stage summaries
                        foreach (var stageSummary in estimate.StageSummaries)
                        {
                            var stage = new JobStage
                            {
                                JobId = jobId,
                                StageName = stageSummary.Stage,
                                EstimatedHours = stageSummary.LaborHours,
                                EstimatedMaterialCost = stageSummary.MaterialCost,
                                ActualHours = 0,
                                ActualMaterialCost = 0
                            };
                            InsertJobStage(connection, transaction, stage);
                        }

                        // Copy room specifications
                        foreach (var room in estimate.Rooms)
                        {
                            foreach (var item in room.Items)
                            {
                                var spec = new RoomSpecification
                                {
                                    JobId = jobId,
                                    RoomName = room.RoomName,
                                    ItemDescription = item.ItemDescription,
                                    Quantity = item.Quantity,
                                    ItemCode = item.ItemCode,
                                    UnitPrice = item.UnitPrice,
                                    TotalPrice = item.UnitPrice * item.Quantity
                                };
                                InsertRoomSpecification(connection, transaction, spec);
                            }
                        }

                        // Copy permit items
                        foreach (var permitItem in estimate.PermitItems)
                        {
                            var item = new PermitItem
                            {
                                JobId = jobId,
                                Category = permitItem.Category,
                                Quantity = permitItem.Quantity,
                                Description = permitItem.Description
                            };
                            InsertPermitItem(connection, transaction, item);
                        }

                        // Update estimate with job reference
                        UpdateEstimateJobId(connection, transaction, estimateId, jobId);

                        // Update estimate status to Approved
                        UpdateEstimateStatusInTransaction(connection, transaction, estimateId, EstimateStatus.Approved);

                        transaction.Commit();
                        return jobId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private int InsertJob(MySqlConnection connection, MySqlTransaction transaction, Job job)
        {
            var query = @"INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                          square_footage, num_floors, status, create_date, total_estimate, notes)
                         VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                                @square_footage, @num_floors, @status, @create_date, @total_estimate, @notes)";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@job_number", job.JobNumber);
                command.Parameters.AddWithValue("@customer_id", job.CustomerId);
                command.Parameters.AddWithValue("@job_name", job.JobName);
                command.Parameters.AddWithValue("@address", job.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@city", job.City ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@state", job.State ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@zip", job.Zip ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@square_footage", job.SquareFootage ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@num_floors", job.NumFloors ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@status", job.Status);
                command.Parameters.AddWithValue("@create_date", job.CreateDate);
                command.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
                return (int)command.LastInsertedId;
            }
        }

        private void InsertJobStage(MySqlConnection connection, MySqlTransaction transaction, JobStage stage)
        {
            var query = @"INSERT INTO JobStages (job_id, stage_name, estimated_hours, estimated_material_cost, 
                                               actual_hours, actual_material_cost)
                         VALUES (@job_id, @stage_name, @estimated_hours, @estimated_material_cost,
                                @actual_hours, @actual_material_cost)";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@job_id", stage.JobId);
                command.Parameters.AddWithValue("@stage_name", stage.StageName);
                command.Parameters.AddWithValue("@estimated_hours", stage.EstimatedHours);
                command.Parameters.AddWithValue("@estimated_material_cost", stage.EstimatedMaterialCost);
                command.Parameters.AddWithValue("@actual_hours", stage.ActualHours);
                command.Parameters.AddWithValue("@actual_material_cost", stage.ActualMaterialCost);

                command.ExecuteNonQuery();
            }
        }

        private void InsertRoomSpecification(MySqlConnection connection, MySqlTransaction transaction, RoomSpecification spec)
        {
            var query = @"INSERT INTO RoomSpecifications (job_id, room_name, item_description, quantity, 
                                                        item_code, unit_price, total_price)
                         VALUES (@job_id, @room_name, @item_description, @quantity,
                                @item_code, @unit_price, @total_price)";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@job_id", spec.JobId);
                command.Parameters.AddWithValue("@room_name", spec.RoomName);
                command.Parameters.AddWithValue("@item_description", spec.ItemDescription);
                command.Parameters.AddWithValue("@quantity", spec.Quantity);
                command.Parameters.AddWithValue("@item_code", spec.ItemCode ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@unit_price", spec.UnitPrice);
                command.Parameters.AddWithValue("@total_price", spec.TotalPrice);

                command.ExecuteNonQuery();
            }
        }

        private void InsertPermitItem(MySqlConnection connection, MySqlTransaction transaction, PermitItem item)
        {
            var query = @"INSERT INTO PermitItems (job_id, category, quantity, description)
                         VALUES (@job_id, @category, @quantity, @description)";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@job_id", item.JobId);
                command.Parameters.AddWithValue("@category", item.Category);
                command.Parameters.AddWithValue("@quantity", item.Quantity);
                command.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
            }
        }

        private void UpdateEstimateJobId(MySqlConnection connection, MySqlTransaction transaction, int estimateId, int jobId)
        {
            var query = "UPDATE Estimates SET job_id = @jobId WHERE estimate_id = @estimateId";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@jobId", jobId);
                command.Parameters.AddWithValue("@estimateId", estimateId);
                command.ExecuteNonQuery();
            }
        }

        private void UpdateEstimateStatusInTransaction(MySqlConnection connection, MySqlTransaction transaction, int estimateId, EstimateStatus status)
        {
            var query = "UPDATE Estimates SET status = @status WHERE estimate_id = @estimateId";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@estimateId", estimateId);
                command.Parameters.AddWithValue("@status", status.ToString());
                command.ExecuteNonQuery();
            }
        }
    }
}
