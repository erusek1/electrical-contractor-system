using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace ElectricalContractorSystem.Services
{
    public class EstimateService
    {
        private readonly DatabaseService _databaseService;

        public EstimateService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        #region Estimate Management

        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            string query = @"
                SELECT e.*, c.name as customer_name, c.address as customer_address, 
                       c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                       c.email as customer_email, c.phone as customer_phone
                FROM Estimates e
                JOIN Customers c ON e.customer_id = c.customer_id
                ORDER BY e.created_date DESC";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                while (reader.Read())
                {
                    var estimate = MapEstimateFromReader(reader);
                    estimates.Add(estimate);
                }
            }

            // Load rooms and items for each estimate
            foreach (var estimate in estimates)
            {
                LoadEstimateDetails(estimate);
            }

            return estimates;
        }

        public Estimate GetEstimateById(int estimateId)
        {
            string query = @"
                SELECT e.*, c.name as customer_name, c.address as customer_address, 
                       c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                       c.email as customer_email, c.phone as customer_phone
                FROM Estimates e
                JOIN Customers c ON e.customer_id = c.customer_id
                WHERE e.estimate_id = @estimateId";

            var parameters = new Dictionary<string, object>
            {
                ["@estimateId"] = estimateId
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                if (reader.Read())
                {
                    var estimate = MapEstimateFromReader(reader);
                    LoadEstimateDetails(estimate);
                    return estimate;
                }
            }

            return null;
        }

        public Estimate GetEstimateByNumber(string estimateNumber)
        {
            string query = @"
                SELECT e.*, c.name as customer_name, c.address as customer_address, 
                       c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                       c.email as customer_email, c.phone as customer_phone
                FROM Estimates e
                JOIN Customers c ON e.customer_id = c.customer_id
                WHERE e.estimate_number = @estimateNumber";

            var parameters = new Dictionary<string, object>
            {
                ["@estimateNumber"] = estimateNumber
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                if (reader.Read())
                {
                    var estimate = MapEstimateFromReader(reader);
                    LoadEstimateDetails(estimate);
                    return estimate;
                }
            }

            return null;
        }

        public void SaveEstimate(Estimate estimate)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (estimate.EstimateId == 0)
                        {
                            InsertEstimate(estimate, connection, transaction);
                        }
                        else
                        {
                            UpdateEstimate(estimate, connection, transaction);
                        }

                        // Delete and re-insert rooms and items
                        DeleteEstimateDetails(estimate.EstimateId, connection, transaction);
                        InsertEstimateDetails(estimate, connection, transaction);

                        // Update stage summaries
                        UpdateStageSummaries(estimate, connection, transaction);

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

        public void DeleteEstimate(int estimateId)
        {
            string query = "DELETE FROM Estimates WHERE estimate_id = @estimateId";
            var parameters = new Dictionary<string, object>
            {
                ["@estimateId"] = estimateId
            };

            _databaseService.ExecuteNonQuery(query, parameters);
        }

        #endregion

        #region Estimate Numbering

        public string GenerateNextEstimateNumber()
        {
            string query = @"
                SELECT estimate_number 
                FROM Estimates 
                WHERE estimate_number LIKE 'EST-%' 
                ORDER BY estimate_id DESC 
                LIMIT 1";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                if (reader.Read())
                {
                    string lastNumber = reader.GetString("estimate_number");
                    if (lastNumber.StartsWith("EST-") && int.TryParse(lastNumber.Substring(4), out int number))
                    {
                        return $"EST-{(number + 1):D4}";
                    }
                }
            }

            return "EST-1001"; // Starting number
        }

        #endregion

        #region Estimate to Job Conversion

        public int ConvertEstimateToJob(int estimateId, string jobNumber, DateTime startDate, string notes)
        {
            var estimate = GetEstimateById(estimateId);
            if (estimate == null)
            {
                throw new InvalidOperationException("Estimate not found");
            }

            if (estimate.Status != "Approved")
            {
                throw new InvalidOperationException("Only approved estimates can be converted to jobs");
            }

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Create the job
                        string jobQuery = @"
                            INSERT INTO Jobs (
                                job_number, customer_id, job_name, address, city, state, zip,
                                square_footage, num_floors, status, create_date, 
                                total_estimate, notes, estimate_id
                            ) VALUES (
                                @jobNumber, @customerId, @jobName, @address, @city, @state, @zip,
                                @squareFootage, @numFloors, 'In Progress', @createDate,
                                @totalEstimate, @notes, @estimateId
                            )";

                        var jobParameters = new Dictionary<string, object>
                        {
                            ["@jobNumber"] = jobNumber,
                            ["@customerId"] = estimate.Customer.CustomerId,
                            ["@jobName"] = estimate.JobName,
                            ["@address"] = estimate.JobAddress,
                            ["@city"] = estimate.JobCity,
                            ["@state"] = estimate.JobState,
                            ["@zip"] = estimate.JobZip,
                            ["@squareFootage"] = estimate.SquareFootage,
                            ["@numFloors"] = estimate.NumFloors,
                            ["@createDate"] = startDate,
                            ["@totalEstimate"] = estimate.TotalPrice,
                            ["@notes"] = notes,
                            ["@estimateId"] = estimateId
                        };

                        var jobCmd = new MySqlCommand(jobQuery, connection, transaction);
                        foreach (var param in jobParameters)
                        {
                            jobCmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                        jobCmd.ExecuteNonQuery();

                        int jobId = (int)jobCmd.LastInsertedId;

                        // Create job stages based on estimate stage summaries
                        var stageSummaries = GetEstimateStageSummaries(estimateId);
                        foreach (var summary in stageSummaries)
                        {
                            string stageQuery = @"
                                INSERT INTO JobStages (
                                    job_id, stage_name, estimated_hours, estimated_material_cost
                                ) VALUES (
                                    @jobId, @stageName, @estimatedHours, @estimatedMaterial
                                )";

                            var stageCmd = new MySqlCommand(stageQuery, connection, transaction);
                            stageCmd.Parameters.AddWithValue("@jobId", jobId);
                            stageCmd.Parameters.AddWithValue("@stageName", summary.StageName);
                            stageCmd.Parameters.AddWithValue("@estimatedHours", summary.TotalLaborHours);
                            stageCmd.Parameters.AddWithValue("@estimatedMaterial", summary.TotalMaterialCost);
                            stageCmd.ExecuteNonQuery();
                        }

                        // Create room specifications
                        foreach (var room in estimate.Rooms)
                        {
                            foreach (var item in room.Items)
                            {
                                string specQuery = @"
                                    INSERT INTO RoomSpecifications (
                                        job_id, room_name, item_description, quantity,
                                        item_code, unit_price, total_price
                                    ) VALUES (
                                        @jobId, @roomName, @itemDescription, @quantity,
                                        @itemCode, @unitPrice, @totalPrice
                                    )";

                                var specCmd = new MySqlCommand(specQuery, connection, transaction);
                                specCmd.Parameters.AddWithValue("@jobId", jobId);
                                specCmd.Parameters.AddWithValue("@roomName", room.RoomName);
                                specCmd.Parameters.AddWithValue("@itemDescription", item.ItemName);
                                specCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                specCmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                specCmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                                specCmd.Parameters.AddWithValue("@totalPrice", item.TotalPrice);
                                specCmd.ExecuteNonQuery();
                            }
                        }

                        // Update estimate status
                        string updateQuery = "UPDATE Estimates SET status = 'Converted', job_id = @jobId WHERE estimate_id = @estimateId";
                        var updateCmd = new MySqlCommand(updateQuery, connection, transaction);
                        updateCmd.Parameters.AddWithValue("@jobId", jobId);
                        updateCmd.Parameters.AddWithValue("@estimateId", estimateId);
                        updateCmd.ExecuteNonQuery();

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

        #endregion

        #region Stage Summaries

        public List<EstimateStageSummary> GetEstimateStageSummaries(int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            string query = @"
                SELECT * FROM EstimateStageSummary 
                WHERE estimate_id = @estimateId 
                ORDER BY stage_order";

            var parameters = new Dictionary<string, object>
            {
                ["@estimateId"] = estimateId
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    summaries.Add(new EstimateStageSummary
                    {
                        SummaryId = reader.GetInt32("summary_id"),
                        EstimateId = reader.GetInt32("estimate_id"),
                        StageName = reader.GetString("stage_name"),
                        TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                        TotalLaborHours = reader.GetDecimal("total_labor_hours"),
                        TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                        StageOrder = reader.GetInt32("stage_order")
                    });
                }
            }

            return summaries;
        }

        private void UpdateStageSummaries(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Delete existing summaries
            string deleteQuery = "DELETE FROM EstimateStageSummary WHERE estimate_id = @estimateId";
            var deleteCmd = new MySqlCommand(deleteQuery, connection, transaction);
            deleteCmd.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
            deleteCmd.ExecuteNonQuery();

            // Calculate summaries by stage
            var stageSummaries = new Dictionary<string, (int minutes, decimal material)>();
            
            foreach (var room in estimate.Rooms)
            {
                foreach (var item in room.Items)
                {
                    // Default to "Finish" if no stage mapping exists
                    string stage = DetermineStageForItem(item.ItemCode, item.ItemName);
                    
                    if (!stageSummaries.ContainsKey(stage))
                    {
                        stageSummaries[stage] = (0, 0m);
                    }

                    var current = stageSummaries[stage];
                    stageSummaries[stage] = (
                        current.minutes + (item.LaborMinutes * item.Quantity),
                        current.material + item.MaterialCost
                    );
                }
            }

            // Insert new summaries
            var stageOrder = new Dictionary<string, int>
            {
                ["Demo"] = 1,
                ["Rough"] = 2,
                ["Service"] = 3,
                ["Finish"] = 4,
                ["Extra"] = 5,
                ["Temp Service"] = 6,
                ["Inspection"] = 7,
                ["Other"] = 8
            };

            foreach (var stage in stageSummaries)
            {
                string insertQuery = @"
                    INSERT INTO EstimateStageSummary (
                        estimate_id, stage_name, total_labor_minutes, 
                        total_labor_hours, total_material_cost, stage_order
                    ) VALUES (
                        @estimateId, @stageName, @totalMinutes,
                        @totalHours, @totalMaterial, @stageOrder
                    )";

                var cmd = new MySqlCommand(insertQuery, connection, transaction);
                cmd.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
                cmd.Parameters.AddWithValue("@stageName", stage.Key);
                cmd.Parameters.AddWithValue("@totalMinutes", stage.Value.minutes);
                cmd.Parameters.AddWithValue("@totalHours", Math.Round(stage.Value.minutes / 60m, 2));
                cmd.Parameters.AddWithValue("@totalMaterial", stage.Value.material);
                cmd.Parameters.AddWithValue("@stageOrder", stageOrder.ContainsKey(stage.Key) ? stageOrder[stage.Key] : 999);
                cmd.ExecuteNonQuery();
            }
        }

        private string DetermineStageForItem(string itemCode, string itemName)
        {
            // Map items to stages based on your business rules
            // This is a simplified version - you may want to make this configurable
            
            if (string.IsNullOrEmpty(itemCode))
                return "Finish";

            var code = itemCode.ToLower();
            var name = itemName.ToLower();

            // Service items
            if (code.Contains("panel") || code.Contains("meter") || code.Contains("service") ||
                name.Contains("panel") || name.Contains("meter") || name.Contains("service"))
                return "Service";

            // Rough items
            if (code.Contains("wire") || code.Contains("12/2") || code.Contains("14/2") || 
                code.Contains("pipe") || code.Contains("conduit") || code.Contains("box") ||
                name.Contains("wire") || name.Contains("romex") || name.Contains("conduit"))
                return "Rough";

            // Demo items
            if (code.Contains("demo") || name.Contains("demo") || name.Contains("remove"))
                return "Demo";

            // Default to Finish for most items (switches, outlets, fixtures, etc.)
            return "Finish";
        }

        #endregion

        #region Template Management

        public List<RoomTemplate> GetRoomTemplates()
        {
            var templates = new List<RoomTemplate>();
            string query = "SELECT * FROM RoomTemplates ORDER BY template_name";

            using (var reader = _databaseService.ExecuteReader(query))
            {
                while (reader.Read())
                {
                    var template = new RoomTemplate
                    {
                        TemplateId = reader.GetInt32("template_id"),
                        TemplateName = reader.GetString("template_name"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        IsActive = reader.GetBoolean("is_active"),
                        Items = new List<RoomTemplateItem>()
                    };

                    // Load template items
                    LoadTemplateItems(template);
                    templates.Add(template);
                }
            }

            return templates;
        }

        private void LoadTemplateItems(RoomTemplate template)
        {
            string query = @"
                SELECT rti.*, pl.name, pl.base_cost, pl.labor_minutes 
                FROM RoomTemplateItems rti
                JOIN PriceList pl ON rti.item_code = pl.item_code
                WHERE rti.template_id = @templateId
                ORDER BY rti.display_order";

            var parameters = new Dictionary<string, object>
            {
                ["@templateId"] = template.TemplateId
            };

            using (var reader = _databaseService.ExecuteReader(query, parameters))
            {
                while (reader.Read())
                {
                    template.Items.Add(new RoomTemplateItem
                    {
                        TemplateItemId = reader.GetInt32("template_item_id"),
                        TemplateId = reader.GetInt32("template_id"),
                        ItemCode = reader.GetString("item_code"),
                        ItemName = reader.GetString("name"),
                        DefaultQuantity = reader.GetInt32("default_quantity"),
                        DisplayOrder = reader.GetInt32("display_order"),
                        UnitPrice = reader.GetDecimal("base_cost"),
                        LaborMinutes = reader.GetInt32("labor_minutes")
                    });
                }
            }
        }

        public void SaveRoomTemplate(RoomTemplate template)
        {
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (template.TemplateId == 0)
                        {
                            // Insert new template
                            string insertQuery = @"
                                INSERT INTO RoomTemplates (template_name, description, is_active)
                                VALUES (@name, @description, @isActive)";

                            var cmd = new MySqlCommand(insertQuery, connection, transaction);
                            cmd.Parameters.AddWithValue("@name", template.TemplateName);
                            cmd.Parameters.AddWithValue("@description", template.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@isActive", template.IsActive);
                            cmd.ExecuteNonQuery();

                            template.TemplateId = (int)cmd.LastInsertedId;
                        }
                        else
                        {
                            // Update existing template
                            string updateQuery = @"
                                UPDATE RoomTemplates 
                                SET template_name = @name, description = @description, is_active = @isActive
                                WHERE template_id = @templateId";

                            var cmd = new MySqlCommand(updateQuery, connection, transaction);
                            cmd.Parameters.AddWithValue("@name", template.TemplateName);
                            cmd.Parameters.AddWithValue("@description", template.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@isActive", template.IsActive);
                            cmd.Parameters.AddWithValue("@templateId", template.TemplateId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete and re-insert template items
                        string deleteQuery = "DELETE FROM RoomTemplateItems WHERE template_id = @templateId";
                        var deleteCmd = new MySqlCommand(deleteQuery, connection, transaction);
                        deleteCmd.Parameters.AddWithValue("@templateId", template.TemplateId);
                        deleteCmd.ExecuteNonQuery();

                        // Insert template items
                        int order = 0;
                        foreach (var item in template.Items)
                        {
                            string insertItemQuery = @"
                                INSERT INTO RoomTemplateItems (
                                    template_id, item_code, default_quantity, display_order
                                ) VALUES (
                                    @templateId, @itemCode, @quantity, @order
                                )";

                            var itemCmd = new MySqlCommand(insertItemQuery, connection, transaction);
                            itemCmd.Parameters.AddWithValue("@templateId", template.TemplateId);
                            itemCmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            itemCmd.Parameters.AddWithValue("@quantity", item.DefaultQuantity);
                            itemCmd.Parameters.AddWithValue("@order", order++);
                            itemCmd.ExecuteNonQuery();
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

        #endregion

        #region Private Helper Methods

        private Estimate MapEstimateFromReader(MySqlDataReader reader)
        {
            var estimate = new Estimate
            {
                EstimateId = reader.GetInt32("estimate_id"),
                EstimateNumber = reader.GetString("estimate_number"),
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
                },
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
                Rooms = new List<EstimateRoom>()
            };

            return estimate;
        }

        private void LoadEstimateDetails(Estimate estimate)
        {
            // Load rooms
            string roomQuery = @"
                SELECT * FROM EstimateRooms 
                WHERE estimate_id = @estimateId 
                ORDER BY room_order";

            var parameters = new Dictionary<string, object>
            {
                ["@estimateId"] = estimate.EstimateId
            };

            using (var reader = _databaseService.ExecuteReader(roomQuery, parameters))
            {
                while (reader.Read())
                {
                    var room = new EstimateRoom
                    {
                        RoomId = reader.GetInt32("room_id"),
                        EstimateId = reader.GetInt32("estimate_id"),
                        RoomName = reader.GetString("room_name"),
                        RoomOrder = reader.GetInt32("room_order"),
                        Items = new List<EstimateItem>()
                    };

                    // Load items for this room
                    LoadRoomItems(room);
                    estimate.Rooms.Add(room);
                }
            }
        }

        private void LoadRoomItems(EstimateRoom room)
        {
            string itemQuery = @"
                SELECT * FROM EstimateItems 
                WHERE room_id = @roomId 
                ORDER BY line_order";

            var parameters = new Dictionary<string, object>
            {
                ["@roomId"] = room.RoomId
            };

            using (var reader = _databaseService.ExecuteReader(itemQuery, parameters))
            {
                while (reader.Read())
                {
                    room.Items.Add(new EstimateItem
                    {
                        ItemId = reader.GetInt32("item_id"),
                        RoomId = reader.GetInt32("room_id"),
                        ItemCode = reader.IsDBNull(reader.GetOrdinal("item_code")) ? null : reader.GetString("item_code"),
                        ItemName = reader.GetString("item_name"),
                        Quantity = reader.GetInt32("quantity"),
                        UnitPrice = reader.GetDecimal("unit_price"),
                        MaterialCost = reader.GetDecimal("material_cost"),
                        LaborMinutes = reader.GetInt32("labor_minutes"),
                        TotalPrice = reader.GetDecimal("total_price"),
                        LineOrder = reader.GetInt32("line_order")
                    });
                }
            }
        }

        private void InsertEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = @"
                INSERT INTO Estimates (
                    estimate_number, customer_id, job_name, job_address, job_city, job_state, job_zip,
                    square_footage, num_floors, status, created_date, expiration_date, notes,
                    tax_rate, material_markup, labor_rate, total_material_cost, total_labor_minutes, total_price
                ) VALUES (
                    @estimateNumber, @customerId, @jobName, @jobAddress, @jobCity, @jobState, @jobZip,
                    @squareFootage, @numFloors, @status, @createdDate, @expirationDate, @notes,
                    @taxRate, @materialMarkup, @laborRate, @totalMaterialCost, @totalLaborMinutes, @totalPrice
                )";

            var cmd = new MySqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@estimateNumber", estimate.EstimateNumber);
            cmd.Parameters.AddWithValue("@customerId", estimate.Customer.CustomerId);
            cmd.Parameters.AddWithValue("@jobName", estimate.JobName);
            cmd.Parameters.AddWithValue("@jobAddress", estimate.JobAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobCity", estimate.JobCity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobState", estimate.JobState ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobZip", estimate.JobZip ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@squareFootage", estimate.SquareFootage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@numFloors", estimate.NumFloors ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", estimate.Status);
            cmd.Parameters.AddWithValue("@createdDate", estimate.CreatedDate);
            cmd.Parameters.AddWithValue("@expirationDate", estimate.ExpirationDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@taxRate", estimate.TaxRate);
            cmd.Parameters.AddWithValue("@materialMarkup", estimate.MaterialMarkup);
            cmd.Parameters.AddWithValue("@laborRate", estimate.LaborRate);
            cmd.Parameters.AddWithValue("@totalMaterialCost", estimate.TotalMaterialCost);
            cmd.Parameters.AddWithValue("@totalLaborMinutes", estimate.TotalLaborMinutes);
            cmd.Parameters.AddWithValue("@totalPrice", estimate.TotalPrice);

            cmd.ExecuteNonQuery();
            estimate.EstimateId = (int)cmd.LastInsertedId;
        }

        private void UpdateEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            string query = @"
                UPDATE Estimates SET
                    customer_id = @customerId, job_name = @jobName, job_address = @jobAddress,
                    job_city = @jobCity, job_state = @jobState, job_zip = @jobZip,
                    square_footage = @squareFootage, num_floors = @numFloors, status = @status,
                    expiration_date = @expirationDate, notes = @notes,
                    tax_rate = @taxRate, material_markup = @materialMarkup, labor_rate = @laborRate,
                    total_material_cost = @totalMaterialCost, total_labor_minutes = @totalLaborMinutes, 
                    total_price = @totalPrice
                WHERE estimate_id = @estimateId";

            var cmd = new MySqlCommand(query, connection, transaction);
            cmd.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
            cmd.Parameters.AddWithValue("@customerId", estimate.Customer.CustomerId);
            cmd.Parameters.AddWithValue("@jobName", estimate.JobName);
            cmd.Parameters.AddWithValue("@jobAddress", estimate.JobAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobCity", estimate.JobCity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobState", estimate.JobState ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@jobZip", estimate.JobZip ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@squareFootage", estimate.SquareFootage ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@numFloors", estimate.NumFloors ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", estimate.Status);
            cmd.Parameters.AddWithValue("@expirationDate", estimate.ExpirationDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@taxRate", estimate.TaxRate);
            cmd.Parameters.AddWithValue("@materialMarkup", estimate.MaterialMarkup);
            cmd.Parameters.AddWithValue("@laborRate", estimate.LaborRate);
            cmd.Parameters.AddWithValue("@totalMaterialCost", estimate.TotalMaterialCost);
            cmd.Parameters.AddWithValue("@totalLaborMinutes", estimate.TotalLaborMinutes);
            cmd.Parameters.AddWithValue("@totalPrice", estimate.TotalPrice);

            cmd.ExecuteNonQuery();
        }

        private void DeleteEstimateDetails(int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Delete all rooms (items will be cascade deleted)
            string deleteRoomsQuery = "DELETE FROM EstimateRooms WHERE estimate_id = @estimateId";
            var cmd = new MySqlCommand(deleteRoomsQuery, connection, transaction);
            cmd.Parameters.AddWithValue("@estimateId", estimateId);
            cmd.ExecuteNonQuery();
        }

        private void InsertEstimateDetails(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            int roomOrder = 0;
            foreach (var room in estimate.Rooms)
            {
                // Insert room
                string roomQuery = @"
                    INSERT INTO EstimateRooms (estimate_id, room_name, room_order)
                    VALUES (@estimateId, @roomName, @roomOrder)";

                var roomCmd = new MySqlCommand(roomQuery, connection, transaction);
                roomCmd.Parameters.AddWithValue("@estimateId", estimate.EstimateId);
                roomCmd.Parameters.AddWithValue("@roomName", room.RoomName);
                roomCmd.Parameters.AddWithValue("@roomOrder", roomOrder++);
                roomCmd.ExecuteNonQuery();

                int roomId = (int)roomCmd.LastInsertedId;

                // Insert items
                int lineOrder = 0;
                foreach (var item in room.Items)
                {
                    string itemQuery = @"
                        INSERT INTO EstimateItems (
                            room_id, item_code, item_name, quantity, unit_price,
                            material_cost, labor_minutes, total_price, line_order
                        ) VALUES (
                            @roomId, @itemCode, @itemName, @quantity, @unitPrice,
                            @materialCost, @laborMinutes, @totalPrice, @lineOrder
                        )";

                    var itemCmd = new MySqlCommand(itemQuery, connection, transaction);
                    itemCmd.Parameters.AddWithValue("@roomId", roomId);
                    itemCmd.Parameters.AddWithValue("@itemCode", item.ItemCode ?? (object)DBNull.Value);
                    itemCmd.Parameters.AddWithValue("@itemName", item.ItemName);
                    itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@unitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@materialCost", item.MaterialCost);
                    itemCmd.Parameters.AddWithValue("@laborMinutes", item.LaborMinutes);
                    itemCmd.Parameters.AddWithValue("@totalPrice", item.TotalPrice);
                    itemCmd.Parameters.AddWithValue("@lineOrder", lineOrder++);
                    itemCmd.ExecuteNonQuery();
                }
            }
        }

        #endregion
    }
}
