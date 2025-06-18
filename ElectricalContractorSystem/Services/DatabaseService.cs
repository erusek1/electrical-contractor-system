        /// <summary>
        /// Helper method to add price list item parameters
        /// </summary>
        private void AddPriceListItemParameters(MySqlCommand cmd, PriceListItem item)
        {
            cmd.Parameters.AddWithValue("@category", item.Category ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@item_code", item.ItemCode);
            cmd.Parameters.AddWithValue("@name", item.Name);
            cmd.Parameters.AddWithValue("@description", item.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@base_cost", item.BaseCost);
            cmd.Parameters.AddWithValue("@tax_rate", item.TaxRate);
            cmd.Parameters.AddWithValue("@labor_minutes", item.LaborMinutes);
            cmd.Parameters.AddWithValue("@markup_percentage", item.MarkupPercentage);
            cmd.Parameters.AddWithValue("@is_active", item.IsActive);
            cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
        }

        /// <summary>
        /// Read price list item from data reader
        /// </summary>
        private PriceListItem ReadPriceListItem(MySqlDataReader reader)
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

        #endregion

        #region Estimate Methods

        /// <summary>
        /// Get all estimates
        /// </summary>
        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
                    ORDER BY e.estimate_number DESC";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var estimate = ReadEstimate(reader);
                        estimate.Customer = new Customer
                        {
                            CustomerId = estimate.CustomerId,
                            Name = reader.GetString("customer_name")
                        };
                        estimates.Add(estimate);
                    }
                }
            }
            
            return estimates;
        }

        /// <summary>
        /// Get estimate by ID
        /// </summary>
        public Estimate GetEstimateById(int estimateId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name, c.address as customer_address,
                           c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                           c.email as customer_email, c.phone as customer_phone
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
                    WHERE e.estimate_id = @estimateId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var estimate = ReadEstimate(reader);
                            estimate.Customer = new Customer
                            {
                                CustomerId = estimate.CustomerId,
                                Name = reader.GetString("customer_name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("customer_address")) ? null : reader.GetString("customer_address"),
                                City = reader.IsDBNull(reader.GetOrdinal("customer_city")) ? null : reader.GetString("customer_city"),
                                State = reader.IsDBNull(reader.GetOrdinal("customer_state")) ? null : reader.GetString("customer_state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("customer_zip")) ? null : reader.GetString("customer_zip"),
                                Email = reader.IsDBNull(reader.GetOrdinal("customer_email")) ? null : reader.GetString("customer_email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? null : reader.GetString("customer_phone")
                            };
                            
                            // Load estimate rooms and stage summaries
                            estimate.Rooms = GetEstimateRooms(estimateId);
                            estimate.StageSummaries = GetEstimateStageSummaries(estimateId);
                            
                            return estimate;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Save estimate
        /// </summary>
        public int SaveEstimate(Estimate estimate)
        {
            if (estimate.EstimateId == 0)
            {
                return AddEstimate(estimate);
            }
            else
            {
                UpdateEstimate(estimate);
                return estimate.EstimateId;
            }
        }

        /// <summary>
        /// Add new estimate
        /// </summary>
        public int AddEstimate(Estimate estimate)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert main estimate
                        var query = @"
                            INSERT INTO Estimates (estimate_number, customer_id, project_name, project_address, 
                                                  project_city, project_state, project_zip, square_footage, 
                                                  num_floors, status, create_date, valid_until_date, 
                                                  subtotal, material_markup, labor_total, grand_total, notes)
                            VALUES (@estimate_number, @customer_id, @project_name, @project_address,
                                    @project_city, @project_state, @project_zip, @square_footage,
                                    @num_floors, @status, @create_date, @valid_until_date,
                                    @subtotal, @material_markup, @labor_total, @grand_total, @notes)";
                        
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            AddEstimateParameters(cmd, estimate);
                            cmd.ExecuteNonQuery();
                            estimate.EstimateId = (int)cmd.LastInsertedId;
                        }
                        
                        // Insert rooms and items
                        if (estimate.Rooms != null)
                        {
                            foreach (var room in estimate.Rooms)
                            {
                                room.EstimateId = estimate.EstimateId;
                                SaveEstimateRoom(room, connection, transaction);
                            }
                        }
                        
                        // Insert stage summaries
                        if (estimate.StageSummaries != null)
                        {
                            foreach (var summary in estimate.StageSummaries)
                            {
                                summary.EstimateId = estimate.EstimateId;
                                SaveEstimateStageSummary(summary, connection, transaction);
                            }
                        }
                        
                        transaction.Commit();
                        return estimate.EstimateId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Update existing estimate
        /// </summary>
        public void UpdateEstimate(Estimate estimate)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update main estimate
                        var query = @"
                            UPDATE Estimates SET
                                customer_id = @customer_id, project_name = @project_name,
                                project_address = @project_address, project_city = @project_city,
                                project_state = @project_state, project_zip = @project_zip,
                                square_footage = @square_footage, num_floors = @num_floors,
                                status = @status, valid_until_date = @valid_until_date,
                                subtotal = @subtotal, material_markup = @material_markup,
                                labor_total = @labor_total, grand_total = @grand_total, notes = @notes
                            WHERE estimate_id = @estimate_id";
                        
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                            AddEstimateParameters(cmd, estimate);
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Delete existing rooms and stage summaries
                        DeleteEstimateRooms(estimate.EstimateId, connection, transaction);
                        DeleteEstimateStageSummaries(estimate.EstimateId, connection, transaction);
                        
                        // Insert updated rooms and items
                        if (estimate.Rooms != null)
                        {
                            foreach (var room in estimate.Rooms)
                            {
                                room.EstimateId = estimate.EstimateId;
                                SaveEstimateRoom(room, connection, transaction);
                            }
                        }
                        
                        // Insert updated stage summaries
                        if (estimate.StageSummaries != null)
                        {
                            foreach (var summary in estimate.StageSummaries)
                            {
                                summary.EstimateId = estimate.EstimateId;
                                SaveEstimateStageSummary(summary, connection, transaction);
                            }
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

        /// <summary>
        /// Delete estimate
        /// </summary>
        public bool DeleteEstimate(int estimateId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Delete rooms and items (cascade)
                            DeleteEstimateRooms(estimateId, connection, transaction);
                            
                            // Delete stage summaries
                            DeleteEstimateStageSummaries(estimateId, connection, transaction);
                            
                            // Delete main estimate
                            var query = "DELETE FROM Estimates WHERE estimate_id = @estimateId";
                            using (var cmd = new MySqlCommand(query, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                                cmd.ExecuteNonQuery();
                            }
                            
                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get last estimate number
        /// </summary>
        public string GetLastEstimateNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT MAX(estimate_number) FROM Estimates";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "EST-1000";
                }
            }
        }

        /// <summary>
        /// Get estimate rooms
        /// </summary>
        public List<EstimateRoom> GetEstimateRooms(int estimateId)
        {
            var rooms = new List<EstimateRoom>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM EstimateRooms WHERE estimate_id = @estimateId ORDER BY room_order, room_name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var room = ReadEstimateRoom(reader);
                            rooms.Add(room);
                        }
                    }
                }
                
                // Load items for each room
                foreach (var room in rooms)
                {
                    room.Items = GetEstimateRoomItems(room.RoomId);
                }
            }
            
            return rooms;
        }

        /// <summary>
        /// Get estimate stage summaries
        /// </summary>
        public List<EstimateStageSummary> GetEstimateStageSummaries(int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM EstimateStageSummaries WHERE estimate_id = @estimateId ORDER BY stage_name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            summaries.Add(ReadEstimateStageSummary(reader));
                        }
                    }
                }
            }
            
            return summaries;
        }

        /// <summary>
        /// Helper method to add estimate parameters
        /// </summary>
        private void AddEstimateParameters(MySqlCommand cmd, Estimate estimate)
        {
            cmd.Parameters.AddWithValue("@estimate_number", estimate.EstimateNumber);
            cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
            cmd.Parameters.AddWithValue("@project_name", estimate.ProjectName);
            cmd.Parameters.AddWithValue("@project_address", estimate.ProjectAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@project_city", estimate.ProjectCity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@project_state", estimate.ProjectState ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@project_zip", estimate.ProjectZip ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", estimate.Status);
            cmd.Parameters.AddWithValue("@create_date", estimate.CreateDate);
            cmd.Parameters.AddWithValue("@valid_until_date", estimate.ValidUntilDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@subtotal", estimate.Subtotal);
            cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
            cmd.Parameters.AddWithValue("@labor_total", estimate.LaborTotal);
            cmd.Parameters.AddWithValue("@grand_total", estimate.GrandTotal);
            cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
        }

        /// <summary>
        /// Read estimate from data reader
        /// </summary>
        private Estimate ReadEstimate(MySqlDataReader reader)
        {
            return new Estimate
            {
                EstimateId = reader.GetInt32("estimate_id"),
                EstimateNumber = reader.GetString("estimate_number"),
                CustomerId = reader.GetInt32("customer_id"),
                ProjectName = reader.GetString("project_name"),
                ProjectAddress = reader.IsDBNull(reader.GetOrdinal("project_address")) ? null : reader.GetString("project_address"),
                ProjectCity = reader.IsDBNull(reader.GetOrdinal("project_city")) ? null : reader.GetString("project_city"),
                ProjectState = reader.IsDBNull(reader.GetOrdinal("project_state")) ? null : reader.GetString("project_state"),
                ProjectZip = reader.IsDBNull(reader.GetOrdinal("project_zip")) ? null : reader.GetString("project_zip"),
                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                Status = reader.GetString("status"),
                CreateDate = reader.GetDateTime("create_date"),
                ValidUntilDate = reader.IsDBNull(reader.GetOrdinal("valid_until_date")) ? (DateTime?)null : reader.GetDateTime("valid_until_date"),
                Subtotal = reader.GetDecimal("subtotal"),
                MaterialMarkup = reader.GetDecimal("material_markup"),
                LaborTotal = reader.GetDecimal("labor_total"),
                GrandTotal = reader.GetDecimal("grand_total"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        /// <summary>
        /// Save estimate room
        /// </summary>
        private void SaveEstimateRoom(EstimateRoom room, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateRooms (estimate_id, room_name, room_order, notes)
                VALUES (@estimate_id, @room_name, @room_order, @notes)";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", room.EstimateId);
                cmd.Parameters.AddWithValue("@room_name", room.RoomName);
                cmd.Parameters.AddWithValue("@room_order", room.RoomOrder);
                cmd.Parameters.AddWithValue("@notes", room.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
                room.RoomId = (int)cmd.LastInsertedId;
            }
            
            // Save room items
            if (room.Items != null)
            {
                foreach (var item in room.Items)
                {
                    item.RoomId = room.RoomId;
                    SaveEstimateRoomItem(item, connection, transaction);
                }
            }
        }

        /// <summary>
        /// Save estimate room item
        /// </summary>
        private void SaveEstimateRoomItem(EstimateRoomItem item, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateRoomItems (room_id, item_description, quantity, unit_price, 
                                              total_price, labor_minutes, stage_name, line_order, notes)
                VALUES (@room_id, @item_description, @quantity, @unit_price,
                        @total_price, @labor_minutes, @stage_name, @line_order, @notes)";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@room_id", item.RoomId);
                cmd.Parameters.AddWithValue("@item_description", item.ItemDescription);
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@unit_price", item.UnitPrice);
                cmd.Parameters.AddWithValue("@total_price", item.TotalPrice);
                cmd.Parameters.AddWithValue("@labor_minutes", item.LaborMinutes);
                cmd.Parameters.AddWithValue("@stage_name", item.StageName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@line_order", item.LineOrder);
                cmd.Parameters.AddWithValue("@notes", item.Notes ?? (object)DBNull.Value);
                
                cmd.ExecuteNonQuery();
                item.ItemId = (int)cmd.LastInsertedId;
            }
        }

        /// <summary>
        /// Save estimate stage summary
        /// </summary>
        private void SaveEstimateStageSummary(EstimateStageSummary summary, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO EstimateStageSummaries (estimate_id, stage_name, total_hours, total_labor_cost, 
                                                   total_material_cost, total_stage_cost)
                VALUES (@estimate_id, @stage_name, @total_hours, @total_labor_cost,
                        @total_material_cost, @total_stage_cost)";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", summary.EstimateId);
                cmd.Parameters.AddWithValue("@stage_name", summary.StageName);
                cmd.Parameters.AddWithValue("@total_hours", summary.TotalHours);
                cmd.Parameters.AddWithValue("@total_labor_cost", summary.TotalLaborCost);
                cmd.Parameters.AddWithValue("@total_material_cost", summary.TotalMaterialCost);
                cmd.Parameters.AddWithValue("@total_stage_cost", summary.TotalStageCost);
                
                cmd.ExecuteNonQuery();
                summary.SummaryId = (int)cmd.LastInsertedId;
            }
        }

        /// <summary>
        /// Get estimate room items
        /// </summary>
        private List<EstimateRoomItem> GetEstimateRoomItems(int roomId)
        {
            var items = new List<EstimateRoomItem>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM EstimateRoomItems WHERE room_id = @roomId ORDER BY line_order";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@roomId", roomId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(ReadEstimateRoomItem(reader));
                        }
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// Delete estimate rooms
        /// </summary>
        private void DeleteEstimateRooms(int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Delete items first (foreign key constraint)
            var deleteItemsQuery = @"
                DELETE eri FROM EstimateRoomItems eri
                INNER JOIN EstimateRooms er ON eri.room_id = er.room_id
                WHERE er.estimate_id = @estimateId";
            
            using (var cmd = new MySqlCommand(deleteItemsQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
            
            // Delete rooms
            var deleteRoomsQuery = "DELETE FROM EstimateRooms WHERE estimate_id = @estimateId";
            using (var cmd = new MySqlCommand(deleteRoomsQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Delete estimate stage summaries
        /// </summary>
        private void DeleteEstimateStageSummaries(int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "DELETE FROM EstimateStageSummaries WHERE estimate_id = @estimateId";
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Read estimate room from data reader
        /// </summary>
        private EstimateRoom ReadEstimateRoom(MySqlDataReader reader)
        {
            return new EstimateRoom
            {
                RoomId = reader.GetInt32("room_id"),
                EstimateId = reader.GetInt32("estimate_id"),
                RoomName = reader.GetString("room_name"),
                RoomOrder = reader.GetInt32("room_order"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        /// <summary>
        /// Read estimate room item from data reader
        /// </summary>
        private EstimateRoomItem ReadEstimateRoomItem(MySqlDataReader reader)
        {
            return new EstimateRoomItem
            {
                ItemId = reader.GetInt32("item_id"),
                RoomId = reader.GetInt32("room_id"),
                ItemDescription = reader.GetString("item_description"),
                Quantity = reader.GetInt32("quantity"),
                UnitPrice = reader.GetDecimal("unit_price"),
                TotalPrice = reader.GetDecimal("total_price"),
                LaborMinutes = reader.GetInt32("labor_minutes"),
                StageName = reader.IsDBNull(reader.GetOrdinal("stage_name")) ? null : reader.GetString("stage_name"),
                LineOrder = reader.GetInt32("line_order"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        /// <summary>
        /// Read estimate stage summary from data reader
        /// </summary>
        private EstimateStageSummary ReadEstimateStageSummary(MySqlDataReader reader)
        {
            return new EstimateStageSummary
            {
                SummaryId = reader.GetInt32("summary_id"),
                EstimateId = reader.GetInt32("estimate_id"),
                StageName = reader.GetString("stage_name"),
                TotalHours = reader.GetDecimal("total_hours"),
                TotalLaborCost = reader.GetDecimal("total_labor_cost"),
                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                TotalStageCost = reader.GetDecimal("total_stage_cost")
            };
        }

        #endregion
    }
}