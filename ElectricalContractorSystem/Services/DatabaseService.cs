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
                    SELECT item_id, category, item_code, name, description, 
                           base_cost, tax_rate, labor_minutes, markup_percentage, 
                           is_active, notes
                    FROM PriceList
                    WHERE is_active = 1
                    ORDER BY category, name";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                BaseCost = reader.GetDecimal("base_cost"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborMinutes = reader.GetInt32("labor_minutes"),
                                MarkupPercentage = reader.GetDecimal("markup_percentage"),
                                IsActive = reader.GetBoolean("is_active"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            };
                            items.Add(item);
                        }
                    }
                }
            }
            
            return items;
        }

        public PriceListItem GetPriceListItemByCode(string itemCode)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT item_id, category, item_code, name, description, 
                           base_cost, tax_rate, labor_minutes, markup_percentage, 
                           is_active, notes
                    FROM PriceList
                    WHERE item_code = @itemCode AND is_active = 1";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@itemCode", itemCode);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PriceListItem
                            {
                                ItemId = reader.GetInt32("item_id"),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString("category"),
                                ItemCode = reader.GetString("item_code"),
                                Name = reader.GetString("name"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                BaseCost = reader.GetDecimal("base_cost"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                LaborMinutes = reader.GetInt32("labor_minutes"),
                                MarkupPercentage = reader.GetDecimal("markup_percentage"),
                                IsActive = reader.GetBoolean("is_active"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            };
                        }
                    }
                }
            }
            
            return null;
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

        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name, c.address, c.city, c.state, c.zip, c.email, c.phone
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
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
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                JobAddress = reader.IsDBNull(reader.GetOrdinal("job_address")) ? null : reader.GetString("job_address"),
                                JobCity = reader.IsDBNull(reader.GetOrdinal("job_city")) ? null : reader.GetString("job_city"),
                                JobState = reader.IsDBNull(reader.GetOrdinal("job_state")) ? null : reader.GetString("job_state"),
                                JobZip = reader.IsDBNull(reader.GetOrdinal("job_zip")) ? null : reader.GetString("job_zip"),
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                                Status = reader.GetString("status"),
                                CreatedDate = reader.GetDateTime("created_date"),
                                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (DateTime?)null : reader.GetDateTime("expiration_date"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                                TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                                TotalPrice = reader.GetDecimal("total_price"),
                                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? (int?)null : reader.GetInt32("job_id"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
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
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get estimate details
                var query = @"
                    SELECT e.*, c.name as customer_name, c.address, c.city, c.state, c.zip, c.email, c.phone
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
                    WHERE e.estimate_id = @estimateId";
                
                Estimate estimate = null;
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estimate = new Estimate
                            {
                                EstimateId = reader.GetInt32("estimate_id"),
                                EstimateNumber = reader.GetString("estimate_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                JobAddress = reader.IsDBNull(reader.GetOrdinal("job_address")) ? null : reader.GetString("job_address"),
                                JobCity = reader.IsDBNull(reader.GetOrdinal("job_city")) ? null : reader.GetString("job_city"),
                                JobState = reader.IsDBNull(reader.GetOrdinal("job_state")) ? null : reader.GetString("job_state"),
                                JobZip = reader.IsDBNull(reader.GetOrdinal("job_zip")) ? null : reader.GetString("job_zip"),
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                                Status = reader.GetString("status"),
                                CreatedDate = reader.GetDateTime("created_date"),
                                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (DateTime?)null : reader.GetDateTime("expiration_date"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                                TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                                TotalPrice = reader.GetDecimal("total_price"),
                                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? (int?)null : reader.GetInt32("job_id"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
                                }
                            };
                        }
                    }
                }
                
                if (estimate != null)
                {
                    // Load rooms
                    LoadEstimateRooms(estimate, connection);
                }
                
                return estimate;
            }
        }

        private void LoadEstimateRooms(Estimate estimate, MySqlConnection connection)
        {
            var query = @"
                SELECT room_id, room_name, room_order, notes
                FROM EstimateRooms
                WHERE estimate_id = @estimateId
                ORDER BY room_order";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var room = new EstimateRoom
                        {
                            RoomId = reader.GetInt32("room_id"),
                            EstimateId = estimate.EstimateId,
                            RoomName = reader.GetString("room_name"),
                            RoomOrder = reader.GetInt32("room_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        };
                        estimate.Rooms.Add(room);
                    }
                }
            }
            
            // Load line items for each room
            foreach (var room in estimate.Rooms)
            {
                LoadRoomLineItems(room, connection);
            }
        }

        private void LoadRoomLineItems(EstimateRoom room, MySqlConnection connection)
        {
            var query = @"
                SELECT line_id, item_id, quantity, item_code, description, 
                       unit_price, material_cost, labor_minutes, line_order, notes
                FROM EstimateLineItems
                WHERE room_id = @roomId
                ORDER BY line_order";
            
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@roomId", room.RoomId);
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new EstimateLineItem
                        {
                            LineId = reader.GetInt32("line_id"),
                            RoomId = room.RoomId,
                            ItemId = reader.IsDBNull(reader.GetOrdinal("item_id")) ? (int?)null : reader.GetInt32("item_id"),
                            Quantity = reader.GetInt32("quantity"),
                            ItemCode = reader.IsDBNull(reader.GetOrdinal("item_code")) ? null : reader.GetString("item_code"),
                            Description = reader.GetString("description"),
                            UnitPrice = reader.GetDecimal("unit_price"),
                            MaterialCost = reader.GetDecimal("material_cost"),
                            LaborMinutes = reader.GetInt32("labor_minutes"),
                            LineOrder = reader.GetInt32("line_order"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                        };
                        room.Items.Add(item);
                    }
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
                    estimate_number, customer_id, job_name, job_address, job_city, 
                    job_state, job_zip, square_footage, num_floors, status, 
                    created_date, expiration_date, notes, tax_rate, material_markup, 
                    labor_rate, total_material_cost, total_labor_minutes, total_price
                ) VALUES (
                    @estimate_number, @customer_id, @job_name, @job_address, @job_city,
                    @job_state, @job_zip, @square_footage, @num_floors, @status,
                    @created_date, @expiration_date, @notes, @tax_rate, @material_markup,
                    @labor_rate, @total_material_cost, @total_labor_minutes, @total_price
                )";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_number", estimate.EstimateNumber);
                cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@job_address", estimate.JobAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_city", estimate.JobCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_state", estimate.JobState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_zip", estimate.JobZip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status);
                cmd.Parameters.AddWithValue("@created_date", estimate.CreatedDate);
                cmd.Parameters.AddWithValue("@expiration_date", estimate.ExpirationDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@tax_rate", estimate.TaxRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost);
                cmd.Parameters.AddWithValue("@total_labor_minutes", estimate.TotalLaborMinutes);
                cmd.Parameters.AddWithValue("@total_price", estimate.TotalPrice);
                
                cmd.ExecuteNonQuery();
                estimate.EstimateId = (int)cmd.LastInsertedId;
            }

            // Save rooms
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
            }
        }

        private void UpdateEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                UPDATE Estimates SET
                    job_name = @job_name,
                    job_address = @job_address,
                    job_city = @job_city,
                    job_state = @job_state,
                    job_zip = @job_zip,
                    square_footage = @square_footage,
                    num_floors = @num_floors,
                    status = @status,
                    expiration_date = @expiration_date,
                    notes = @notes,
                    tax_rate = @tax_rate,
                    material_markup = @material_markup,
                    labor_rate = @labor_rate,
                    total_material_cost = @total_material_cost,
                    total_labor_minutes = @total_labor_minutes,
                    total_price = @total_price
                WHERE estimate_id = @estimate_id";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@job_address", estimate.JobAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_city", estimate.JobCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_state", estimate.JobState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_zip", estimate.JobZip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status);
                cmd.Parameters.AddWithValue("@expiration_date", estimate.ExpirationDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@tax_rate", estimate.TaxRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost);
                cmd.Parameters.AddWithValue("@total_labor_minutes", estimate.TotalLaborMinutes);
                cmd.Parameters.AddWithValue("@total_price", estimate.TotalPrice);
                
                cmd.ExecuteNonQuery();
            }

            // Delete existing rooms (they'll be re-inserted)
            using (var cmd = new MySqlCommand("DELETE FROM EstimateRooms WHERE estimate_id = @estimate_id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.ExecuteNonQuery();
            }

            // Save rooms
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
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
            foreach (var item in room.Items)
            {
                SaveLineItem(item, room.RoomId, connection, transaction);
            }
        }

        private void SaveLineItem(EstimateLineItem item, int roomId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateLineItems (
                    room_id, item_id, quantity, item_code, description, 
                    unit_price, material_cost, labor_minutes, line_order, notes
                ) VALUES (
                    @room_id, @item_id, @quantity, @item_code, @description,
                    @unit_price, @material_cost, @labor_minutes, @line_order, @notes
                )";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@room_id", roomId);
                cmd.Parameters.AddWithValue("@item_id", item.ItemId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@item_code", item.ItemCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", item.Description);
                cmd.Parameters.AddWithValue("@unit_price", item.UnitPrice);
                cmd.Parameters.AddWithValue("@material_cost", item.MaterialCost);
                cmd.Parameters.AddWithValue("@labor_minutes", item.LaborMinutes);
                cmd.Parameters.AddWithValue("@line_order", item.LineOrder);
                cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveStageSummary(EstimateStageSummary summary, int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateStageSummary (estimate_id, stage, labor_hours, material_cost)
                VALUES (@estimate_id, @stage, @labor_hours, @material_cost)
                ON DUPLICATE KEY UPDATE
                    labor_hours = @labor_hours,
                    material_cost = @material_cost";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimateId);
                cmd.Parameters.AddWithValue("@stage", summary.Stage);
                cmd.Parameters.AddWithValue("@labor_hours", summary.LaborHours);
                cmd.Parameters.AddWithValue("@material_cost", summary.MaterialCost);
                
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
