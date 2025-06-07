using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        public string ConnectionString => _connectionString;
        
        #region Extended Estimate Methods
        
        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.* 
                    FROM Estimates e
                    JOIN Customers c ON e.customer_id = c.customer_id
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
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? null : (int?)reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? null : (int?)reader.GetInt32("num_floors"),
                                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                TotalLaborHours = reader.IsDBNull(reader.GetOrdinal("total_labor_hours")) ? null : (decimal?)reader.GetDecimal("total_labor_hours"),
                                TotalMaterialCost = reader.IsDBNull(reader.GetOrdinal("total_material_cost")) ? null : (decimal?)reader.GetDecimal("total_material_cost"),
                                TotalCost = reader.IsDBNull(reader.GetOrdinal("total_cost")) ? null : (decimal?)reader.GetDecimal("total_cost"),
                                CreatedDate = reader.GetDateTime("created_date"),
                                ConvertedToJobId = reader.IsDBNull(reader.GetOrdinal("converted_to_job_id")) ? null : (int?)reader.GetInt32("converted_to_job_id"),
                                ConvertedDate = reader.IsDBNull(reader.GetOrdinal("converted_date")) ? null : (DateTime?)reader.GetDateTime("converted_date"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal(reader.GetOrdinal("address") + 14)) ? null : reader.GetString(reader.GetOrdinal("address") + 14),
                                    City = reader.IsDBNull(reader.GetOrdinal(reader.GetOrdinal("city") + 14)) ? null : reader.GetString(reader.GetOrdinal("city") + 14),
                                    State = reader.IsDBNull(reader.GetOrdinal(reader.GetOrdinal("state") + 14)) ? null : reader.GetString(reader.GetOrdinal("state") + 14),
                                    Zip = reader.IsDBNull(reader.GetOrdinal(reader.GetOrdinal("zip") + 14)) ? null : reader.GetString(reader.GetOrdinal("zip") + 14)
                                }
                            };
                            
                            estimates.Add(estimate);
                        }
                    }
                }
            }
            
            return estimates;
        }
        
        public Estimate GetEstimateById(int estimateId)
        {
            Estimate estimate = null;
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get estimate with customer
                var query = @"
                    SELECT e.*, c.* 
                    FROM Estimates e
                    JOIN Customers c ON e.customer_id = c.customer_id
                    WHERE e.estimate_id = @estimate_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estimate = new Estimate
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
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? null : (int?)reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? null : (int?)reader.GetInt32("num_floors"),
                                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                TotalLaborHours = reader.IsDBNull(reader.GetOrdinal("total_labor_hours")) ? null : (decimal?)reader.GetDecimal("total_labor_hours"),
                                TotalMaterialCost = reader.IsDBNull(reader.GetOrdinal("total_material_cost")) ? null : (decimal?)reader.GetDecimal("total_material_cost"),
                                TotalCost = reader.IsDBNull(reader.GetOrdinal("total_cost")) ? null : (decimal?)reader.GetDecimal("total_cost"),
                                CreatedDate = reader.GetDateTime("created_date"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("name")
                                }
                            };
                        }
                    }
                }
                
                if (estimate != null)
                {
                    // Load rooms
                    LoadEstimateRooms(estimate, connection);
                    
                    // Load line items
                    LoadEstimateLineItems(estimate, connection);
                    
                    // Load stage summaries
                    LoadEstimateStageSummaries(estimate, connection);
                }
            }
            
            return estimate;
        }
        
        private void LoadEstimateRooms(Estimate estimate, MySqlConnection connection)
        {
            var query = @"
                SELECT * FROM EstimateRooms 
                WHERE estimate_id = @estimate_id 
                ORDER BY room_order";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var room = new EstimateRoom
                        {
                            RoomId = reader.GetInt32("room_id"),
                            EstimateId = reader.GetInt32("estimate_id"),
                            RoomName = reader.GetString("room_name"),
                            RoomOrder = reader.GetInt32("room_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        };
                        
                        estimate.Rooms.Add(room);
                    }
                }
            }
        }
        
        private void LoadEstimateLineItems(Estimate estimate, MySqlConnection connection)
        {
            var query = @"
                SELECT eli.*, pli.*
                FROM EstimateLineItems eli
                LEFT JOIN PriceListItems pli ON eli.item_id = pli.item_id
                WHERE eli.estimate_id = @estimate_id
                ORDER BY eli.room_id, eli.line_order";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var lineItem = new EstimateLineItem
                        {
                            LineItemId = reader.GetInt32("line_item_id"),
                            EstimateId = reader.GetInt32("estimate_id"),
                            RoomId = reader.GetInt32("room_id"),
                            ItemId = reader.GetInt32("item_id"),
                            Quantity = reader.GetInt32("quantity"),
                            ItemCode = reader.GetString("item_code"),
                            Description = reader.GetString("description"),
                            UnitPrice = reader.GetDecimal("unit_price"),
                            LineOrder = reader.GetInt32("line_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        };
                        
                        // Add to estimate and room
                        estimate.LineItems.Add(lineItem);
                        var room = estimate.Rooms.FirstOrDefault(r => r.RoomId == lineItem.RoomId);
                        room?.LineItems.Add(lineItem);
                        
                        // Load price list item if exists
                        if (!reader.IsDBNull(reader.GetOrdinal("item_code")))
                        {
                            lineItem.PriceListItem = new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name")
                            };
                        }
                    }
                }
            }
            
            // Load labor minutes for price list items
            foreach (var item in estimate.LineItems.Where(li => li.PriceListItem != null).Select(li => li.PriceListItem).Distinct())
            {
                LoadLaborMinutesForItem(item, connection);
            }
        }
        
        private void LoadLaborMinutesForItem(PriceListItem item, MySqlConnection connection)
        {
            var query = "SELECT * FROM LaborMinutes WHERE item_id = @item_id";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@item_id", item.ItemId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        item.LaborMinutes.Add(new LaborMinute
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Stage = reader.GetString("stage"),
                            Minutes = reader.GetInt32("minutes")
                        });
                    }
                }
            }
        }
        
        private void LoadEstimateStageSummaries(Estimate estimate, MySqlConnection connection)
        {
            var query = @"
                SELECT * FROM EstimateStageSummary
                WHERE estimate_id = @estimate_id";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        estimate.StageSummaries.Add(new EstimateStageSummary
                        {
                            SummaryId = reader.GetInt32("summary_id"),
                            EstimateId = reader.GetInt32("estimate_id"),
                            Stage = reader.GetString("stage"),
                            EstimatedHours = reader.GetDecimal("estimated_hours"),
                            EstimatedMaterial = reader.GetDecimal("estimated_material")
                        });
                    }
                }
            }
        }
        
        #endregion
    }
}