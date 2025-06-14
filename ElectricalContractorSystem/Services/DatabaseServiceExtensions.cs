using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        // Add missing methods that are referenced in other services
        
        /// <summary>
        /// Get all jobs for a specific customer
        /// </summary>
        public List<Job> GetJobsByCustomer(int customerId)
        {
            var jobs = new List<Job>();
            
            using (var connection = GetConnection())
            {
                connection.Open();
                var query = @"SELECT * FROM Jobs WHERE customer_id = @customerId ORDER BY create_date DESC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jobs.Add(MapReaderToJob(reader));
                        }
                    }
                }
            }
            
            return jobs;
        }
        
        /// <summary>
        /// Read a single estimate by ID (used in DatabaseServicePricingExtensions)
        /// </summary>
        public Estimate ReadEstimate(int estimateId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var query = @"SELECT e.*, c.name as customer_name 
                             FROM Estimates e
                             JOIN Customers c ON e.customer_id = c.customer_id
                             WHERE e.estimate_id = @estimateId";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToEstimate(reader);
                        }
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Load estimate details including rooms, line items, and summaries
        /// </summary>
        public void LoadEstimateDetails(Estimate estimate)
        {
            if (estimate == null) return;
            
            using (var connection = GetConnection())
            {
                connection.Open();
                
                // Load rooms
                var roomQuery = @"SELECT * FROM EstimateRooms WHERE estimate_id = @estimateId ORDER BY room_order";
                using (var command = new MySqlCommand(roomQuery, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        estimate.Rooms = new List<EstimateRoom>();
                        while (reader.Read())
                        {
                            estimate.Rooms.Add(MapReaderToEstimateRoom(reader));
                        }
                    }
                }
                
                // Load line items for each room
                foreach (var room in estimate.Rooms)
                {
                    var itemQuery = @"SELECT * FROM EstimateLineItems WHERE room_id = @roomId ORDER BY line_order";
                    using (var command = new MySqlCommand(itemQuery, connection))
                    {
                        command.Parameters.AddWithValue("@roomId", room.RoomId);
                        using (var reader = command.ExecuteReader())
                        {
                            room.Items = new List<EstimateLineItem>();
                            while (reader.Read())
                            {
                                room.Items.Add(MapReaderToEstimateLineItem(reader));
                            }
                        }
                    }
                }
                
                // Load stage summaries
                var summaryQuery = @"SELECT * FROM EstimateStageSummaries WHERE estimate_id = @estimateId";
                using (var command = new MySqlCommand(summaryQuery, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        estimate.StageSummaries = new List<EstimateStageSummary>();
                        while (reader.Read())
                        {
                            estimate.StageSummaries.Add(MapReaderToEstimateStageSummary(reader));
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Map a database reader to a Job object
        /// </summary>
        private Job MapReaderToJob(MySqlDataReader reader)
        {
            return new Job
            {
                JobId = reader.GetInt32("job_id"),
                JobNumber = reader.GetString("job_number"),
                CustomerId = reader.GetInt32("customer_id"),
                JobName = reader.GetString("job_name"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                Status = reader.GetString("status"),
                CreateDate = reader.GetDateTime("create_date"),
                CompletionDate = reader.IsDBNull(reader.GetOrdinal("completion_date")) ? (System.DateTime?)null : reader.GetDateTime("completion_date"),
                TotalEstimate = reader.IsDBNull(reader.GetOrdinal("total_estimate")) ? (decimal?)null : reader.GetDecimal("total_estimate"),
                TotalActual = reader.IsDBNull(reader.GetOrdinal("total_actual")) ? (decimal?)null : reader.GetDecimal("total_actual"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }
        
        /// <summary>
        /// Map a database reader to an Estimate object
        /// </summary>
        private Estimate MapReaderToEstimate(MySqlDataReader reader)
        {
            return new Estimate
            {
                EstimateId = reader.GetInt32("estimate_id"),
                EstimateNumber = reader.GetString("estimate_number"),
                CustomerId = reader.GetInt32("customer_id"),
                JobName = reader.GetString("job_name"),
                Status = (EstimateStatus)System.Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                CreateDate = reader.GetDateTime("create_date"),
                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (System.DateTime?)null : reader.GetDateTime("expiration_date"),
                MaterialMarkup = reader.GetDecimal("material_markup"),
                TaxRate = reader.GetDecimal("tax_rate"),
                LaborRate = reader.GetDecimal("labor_rate"),
                Customer = new Customer
                {
                    CustomerId = reader.GetInt32("customer_id"),
                    Name = reader.GetString("customer_name")
                }
            };
        }
        
        /// <summary>
        /// Map a database reader to an EstimateRoom object
        /// </summary>
        private EstimateRoom MapReaderToEstimateRoom(MySqlDataReader reader)
        {
            return new EstimateRoom
            {
                RoomId = reader.GetInt32("room_id"),
                EstimateId = reader.GetInt32("estimate_id"),
                RoomName = reader.GetString("room_name"),
                RoomOrder = reader.GetInt32("room_order"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                Items = new List<EstimateLineItem>()
            };
        }
        
        /// <summary>
        /// Map a database reader to an EstimateLineItem object
        /// </summary>
        private EstimateLineItem MapReaderToEstimateLineItem(MySqlDataReader reader)
        {
            return new EstimateLineItem
            {
                LineId = reader.GetInt32("line_id"),
                RoomId = reader.GetInt32("room_id"),
                ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? (int?)null : reader.GetInt32("item_id"),
                Quantity = reader.GetInt32("quantity"),
                ItemCode = reader.IsDBNull(reader.GetOrdinal("item_code")) ? null : reader.GetString("item_code"),
                ItemDescription = reader.GetString("description"),
                UnitPrice = reader.GetDecimal("unit_price"),
                MaterialCost = reader.GetDecimal("material_cost"),
                LaborMinutes = reader.GetInt32("labor_minutes"),
                LineOrder = reader.GetInt32("line_order"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }
        
        /// <summary>
        /// Map a database reader to an EstimateStageSummary object
        /// </summary>
        private EstimateStageSummary MapReaderToEstimateStageSummary(MySqlDataReader reader)
        {
            return new EstimateStageSummary
            {
                SummaryId = reader.GetInt32("summary_id"),
                EstimateId = reader.GetInt32("estimate_id"),
                Stage = reader.GetString("stage"),
                LaborHours = reader.GetDecimal("labor_hours"),
                MaterialCost = reader.GetDecimal("material_cost"),
                LaborCost = reader.GetDecimal("labor_cost")
            };
        }
    }
}
