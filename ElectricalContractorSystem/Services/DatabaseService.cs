using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"].ConnectionString;
        }

        #region Price List Methods

        public List<PriceListItem> GetActivePriceListItems()
        {
            var items = new List<PriceListItem>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT p.*, 
                           l.stage as labor_stage, l.minutes,
                           m.stage as material_stage, m.material_cost
                    FROM PriceListItems p
                    LEFT JOIN LaborMinutes l ON p.item_id = l.item_id
                    LEFT JOIN MaterialStages m ON p.item_id = m.item_id
                    WHERE p.is_active = 1
                    ORDER BY p.category, p.name";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        var itemDict = new Dictionary<int, PriceListItem>();
                        
                        while (reader.Read())
                        {
                            var itemId = reader.GetInt32("item_id");
                            
                            if (!itemDict.ContainsKey(itemId))
                            {
                                var item = new PriceListItem
                                {
                                    ItemId = itemId,
                                    ItemCode = reader.GetString("item_code"),
                                    Name = reader.GetString("name"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                    BasePrice = reader.GetDecimal("base_price"),
                                    TaxRate = reader.GetDecimal("tax_rate"),
                                    Unit = reader.GetString("unit"),
                                    Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                    IsActive = reader.GetBoolean("is_active")
                                };
                                itemDict[itemId] = item;
                                items.Add(item);
                            }
                            
                            // Add labor minutes if present
                            if (!reader.IsDBNull(reader.GetOrdinal("labor_stage")))
                            {
                                itemDict[itemId].LaborMinutes.Add(new LaborMinute
                                {
                                    ItemId = itemId,
                                    Stage = reader.GetString("labor_stage"),
                                    Minutes = reader.GetInt32("minutes")
                                });
                            }
                            
                            // Add material stages if present
                            if (!reader.IsDBNull(reader.GetOrdinal("material_stage")))
                            {
                                itemDict[itemId].MaterialStages.Add(new MaterialStage
                                {
                                    ItemId = itemId,
                                    Stage = reader.GetString("material_stage"),
                                    MaterialCost = reader.GetDecimal("material_cost")
                                });
                            }
                        }
                    }
                }
            }
            
            return items;
        }

        #endregion

        #region Estimate Methods

        public string GetLastEstimateNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT estimate_number FROM Estimates ORDER BY estimate_id DESC LIMIT 1";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        public void SaveEstimate(Estimate estimate)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (estimate.EstimateId == 0)
                        {
                            // Insert new estimate
                            InsertEstimate(estimate, connection, transaction);
                        }
                        else
                        {
                            // Update existing estimate
                            UpdateEstimate(estimate, connection, transaction);
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void InsertEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO Estimates (
                    estimate_number, version, customer_id, job_name, 
                    address, city, state, zip, square_footage, num_floors,
                    status, labor_rate, material_markup, total_labor_hours,
                    total_material_cost, total_cost, notes, created_by
                ) VALUES (
                    @estimate_number, @version, @customer_id, @job_name,
                    @address, @city, @state, @zip, @square_footage, @num_floors,
                    @status, @labor_rate, @material_markup, @total_labor_hours,
                    @total_material_cost, @total_cost, @notes, @created_by
                )";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_number", estimate.EstimateNumber);
                cmd.Parameters.AddWithValue("@version", estimate.Version);
                cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@address", estimate.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", estimate.City ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@state", estimate.State ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zip", estimate.Zip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status.ToString());
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@total_labor_hours", estimate.TotalLaborHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total_cost", estimate.TotalCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", estimate.CreatedBy ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
                estimate.EstimateId = (int)cmd.LastInsertedId;
            }

            // Save rooms and line items
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
            }

            // Save stage summaries
            foreach (var summary in estimate.StageSummaries)
            {
                SaveStageSummary(summary, estimate.EstimateId, connection, transaction);
            }
        }

        private void UpdateEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                UPDATE Estimates SET
                    job_name = @job_name,
                    address = @address,
                    city = @city,
                    state = @state,
                    zip = @zip,
                    square_footage = @square_footage,
                    num_floors = @num_floors,
                    status = @status,
                    labor_rate = @labor_rate,
                    material_markup = @material_markup,
                    total_labor_hours = @total_labor_hours,
                    total_material_cost = @total_material_cost,
                    total_cost = @total_cost,
                    notes = @notes
                WHERE estimate_id = @estimate_id";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@address", estimate.Address ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", estimate.City ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@state", estimate.State ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zip", estimate.Zip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status.ToString());
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@total_labor_hours", estimate.TotalLaborHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total_cost", estimate.TotalCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
            }

            // Delete existing rooms and line items (they'll be re-inserted)
            using (var cmd = new MySqlCommand("DELETE FROM EstimateRooms WHERE estimate_id = @estimate_id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.ExecuteNonQuery();
            }

            // Save rooms and line items
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
            }

            // Update stage summaries
            using (var cmd = new MySqlCommand("DELETE FROM EstimateStageSummary WHERE estimate_id = @estimate_id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.ExecuteNonQuery();
            }

            foreach (var summary in estimate.StageSummaries)
            {
                SaveStageSummary(summary, estimate.EstimateId, connection, transaction);
            }
        }

        private void SaveRoom(EstimateRoom room, int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateRooms (estimate_id, room_name, room_order, notes)
                VALUES (@estimate_id, @room_name, @room_order, @notes)";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                cmd.Parameters.AddWithValue("@room_name", room.RoomName);
                cmd.Parameters.AddWithValue("@room_order", room.RoomOrder);
                cmd.Parameters.AddWithValue("@notes", room.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
                room.RoomId = (int)cmd.LastInsertedId;
            }

            // Save line items
            foreach (var item in room.LineItems)
            {
                SaveLineItem(item, estimateId, room.RoomId, connection, transaction);
            }
        }

        private void SaveLineItem(EstimateLineItem item, int estimateId, int roomId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateLineItems (
                    estimate_id, room_id, item_id, quantity, 
                    item_code, description, unit_price, line_order, notes
                ) VALUES (
                    @estimate_id, @room_id, @item_id, @quantity,
                    @item_code, @description, @unit_price, @line_order, @notes
                )";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                cmd.Parameters.AddWithValue("@room_id", roomId);
                cmd.Parameters.AddWithValue("@item_id", item.ItemId);
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@item_code", item.ItemCode);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@unit_price", item.UnitPrice);
                cmd.Parameters.AddWithValue("@line_order", item.LineOrder);
                cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
            }
        }

        private void SaveStageSummary(EstimateStageSummary summary, int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateStageSummary (estimate_id, stage, estimated_hours, estimated_material)
                VALUES (@estimate_id, @stage, @estimated_hours, @estimated_material)";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                cmd.Parameters.AddWithValue("@stage", summary.Stage);
                cmd.Parameters.AddWithValue("@estimated_hours", summary.EstimatedHours);
                cmd.Parameters.AddWithValue("@estimated_material", summary.EstimatedMaterial);
                
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Customer Methods

        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Customers ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            customers.Add(new Customer
                            {
                                CustomerId = reader.GetInt32("customer_id"),
                                Name = reader.GetString("name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
                            });
                        }
                    }
                }
            }
            
            return customers;
        }

        public void SaveCustomer(Customer customer)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                if (customer.CustomerId == 0)
                {
                    var query = @"
                        INSERT INTO Customers (name, address, city, state, zip, email, phone, notes)
                        VALUES (@name, @address, @city, @state, @zip, @email, @phone, @notes)";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", customer.Name);
                        cmd.Parameters.AddWithValue("@address", customer.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@city", customer.City ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@state", customer.State ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@zip", customer.Zip ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", customer.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", customer.Notes ?? (object)DBNull.Value);
                        
                        cmd.ExecuteNonQuery();
                        customer.CustomerId = (int)cmd.LastInsertedId;
                    }
                }
            }
        }

        #endregion
    }
}