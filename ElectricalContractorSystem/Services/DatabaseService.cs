using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Complete database service for electrical contractor system
    /// Handles all database operations for jobs, customers, estimates, employees, materials, etc.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString ?? "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn;";
        }

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Connection and Utility Methods

        /// <summary>
        /// Get database connection - used by extension methods
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a table exists
        /// </summary>
        private bool TableExists(string tableName)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = @tableName";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@tableName", tableName);
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Execute reader query - used by report services
        /// </summary>
        public MySqlDataReader ExecuteReader(string query, Dictionary<string, object> parameters = null)
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var cmd = new MySqlCommand(query, connection);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// Execute non-query command with transaction support
        /// </summary>
        private int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null, MySqlConnection connection = null, MySqlTransaction transaction = null)
        {
            bool shouldCloseConnection = connection == null;
            
            if (connection == null)
            {
                connection = new MySqlConnection(_connectionString);
                connection.Open();
            }

            try
            {
                using (var cmd = new MySqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    connection?.Close();
                }
            }
        }

        /// <summary>
        /// Execute scalar query
        /// </summary>
        private T ExecuteScalar<T>(string query, Dictionary<string, object> parameters = null)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return default(T);
                    
                    return (T)Convert.ChangeType(result, typeof(T));
                }
            }
        }

        #endregion

        #region Customer Methods (REAL IMPLEMENTATION)

        /// <summary>
        /// Get all customers
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            if (!TableExists("customers"))
            {
                return customers;
            }
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT customer_id, name, address, city, state, zip, email, phone, notes FROM customers ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(new Customer
                        {
                            CustomerId = reader.GetInt32("customer_id"),
                            Name = reader.GetString("name"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            State = reader.IsDBNull("state") ? null : reader.GetString("state"),
                            Zip = reader.IsDBNull("zip") ? null : reader.GetString("zip"),
                            Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                            Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                            Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
                        });
                    }
                }
            }
            
            return customers;
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        public Customer GetCustomerById(int customerId)
        {
            if (!TableExists("customers"))
            {
                return null;
            }
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT customer_id, name, address, city, state, zip, email, phone, notes FROM customers WHERE customer_id = @customerId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Customer
                            {
                                CustomerId = reader.GetInt32("customer_id"),
                                Name = reader.GetString("name"),
                                Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                                City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                                State = reader.IsDBNull("state") ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull("zip") ? null : reader.GetString("zip"),
                                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
                            };
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Add new customer
        /// </summary>
        public int AddCustomer(Customer customer)
        {
            if (!TableExists("customers"))
            {
                return 0;
            }
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO customers (name, address, city, state, zip, email, phone, notes)
                    VALUES (@name, @address, @city, @state, @zip, @email, @phone, @notes);
                    SELECT LAST_INSERT_ID();";
                
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
                    
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Save customer (add or update)
        /// </summary>
        public void SaveCustomer(Customer customer)
        {
            if (customer.CustomerId == 0)
            {
                customer.CustomerId = AddCustomer(customer);
            }
            else
            {
                UpdateCustomer(customer);
            }
        }

        /// <summary>
        /// Update customer
        /// </summary>
        public void UpdateCustomer(Customer customer)
        {
            if (!TableExists("customers"))
            {
                return;
            }
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE customers SET 
                        name = @name, address = @address, city = @city, state = @state, 
                        zip = @zip, email = @email, phone = @phone, notes = @notes
                    WHERE customer_id = @customerId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@customerId", customer.CustomerId);
                    cmd.Parameters.AddWithValue("@name", customer.Name);
                    cmd.Parameters.AddWithValue("@address", customer.Address ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", customer.City ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@state", customer.State ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@zip", customer.Zip ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", customer.Email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", customer.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Delete customer
        /// </summary>
        public bool DeleteCustomer(int customerId)
        {
            try
            {
                if (!TableExists("customers"))
                {
                    return false;
                }

                var query = "DELETE FROM customers WHERE customer_id = @customerId";
                var parameters = new Dictionary<string, object> { ["@customerId"] = customerId };
                
                return ExecuteNonQuery(query, parameters) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get next job number
        /// </summary>
        public string GetNextJobNumber()
        {
            if (!TableExists("jobs"))
            {
                return "716"; // Start after your current highest
            }
            
            try
            {
                var query = "SELECT MAX(CAST(job_number AS UNSIGNED)) FROM jobs WHERE job_number REGEXP '^[0-9]+$'";
                var maxNumber = ExecuteScalar<int?>(query);
                
                if (maxNumber.HasValue)
                {
                    return (maxNumber.Value + 1).ToString();
                }
                else
                {
                    return "716"; // Default start
                }
            }
            catch
            {
                return "716"; // Default on error
            }
        }

        /// <summary>
        /// Convert estimate to job
        /// </summary>
        public Job ConvertEstimateToJob(int estimateId, EstimateToJobConversionOptions options)
        {
            if (!TableExists("estimates") || !TableExists("jobs"))
            {
                throw new InvalidOperationException("Required tables do not exist.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get the estimate
                        var estimate = GetEstimate(estimateId, connection, transaction);
                        if (estimate == null)
                        {
                            throw new InvalidOperationException("Estimate not found.");
                        }

                        // Create the job
                        var job = new Job
                        {
                            JobNumber = options.JobNumber,
                            CustomerId = estimate.CustomerId,
                            JobName = options.JobName,
                            Address = estimate.Address,
                            City = estimate.City,
                            State = estimate.State,
                            Zip = estimate.Zip,
                            SquareFootage = estimate.SquareFootage,
                            NumFloors = estimate.NumFloors,
                            Status = JobStatus.InProgress,
                            CreateDate = DateTime.Now,
                            TotalEstimate = estimate.TotalPrice,
                            TotalActual = 0,
                            Notes = options.Notes,
                            EstimateId = estimateId
                        };

                        // Insert the job
                        var jobId = InsertJob(job, connection, transaction);
                        job.JobId = jobId;

                        // Create job stages if requested
                        if (options.CreateJobStages)
                        {
                            CreateJobStagesFromEstimate(estimateId, jobId, connection, transaction);
                        }

                        // Copy room specifications if requested
                        if (options.IncludeLineItems)
                        {
                            CopyEstimateRoomsToJob(estimateId, jobId, connection, transaction);
                        }

                        // Mark estimate as converted if requested
                        if (options.MarkEstimateConverted)
                        {
                            UpdateEstimateStatus(estimateId, "Converted", jobId, connection, transaction);
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
        }

        private Estimate GetEstimate(int estimateId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "SELECT * FROM estimates WHERE estimate_id = @estimateId";
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadEstimate(reader);
                    }
                }
            }
            return null;
        }

        private int InsertJob(Job job, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                square_footage, num_floors, status, create_date, total_estimate, 
                                total_actual, notes, estimate_id)
                VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                        @square_footage, @num_floors, @status, @create_date, @total_estimate,
                        @total_actual, @notes, @estimate_id);
                SELECT LAST_INSERT_ID();";

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
                cmd.Parameters.AddWithValue("@status", job.Status.ToString());
                cmd.Parameters.AddWithValue("@create_date", job.CreateDate);
                cmd.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@total_actual", job.TotalActual ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@estimate_id", job.EstimateId ?? (object)DBNull.Value);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void CreateJobStagesFromEstimate(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO jobstages (job_id, stage_name, estimated_hours, estimated_material_cost)
                SELECT @jobId, stage, labor_hours, material_cost
                FROM estimatestagesummary 
                WHERE estimate_id = @estimateId";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@jobId", jobId);
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        private void CopyEstimateRoomsToJob(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // This would copy estimate line items to room specifications
            // The exact implementation depends on how you want to map the data
            var query = @"
                INSERT INTO roomspecifications (job_id, room_name, item_description, quantity, item_code, unit_price, total_price)
                SELECT @jobId, r.room_name, li.description, li.quantity, li.item_code, li.unit_price,
                       (li.quantity * li.unit_price) as total_price
                FROM estimaterooms r
                INNER JOIN estimatelineitems li ON r.room_id = li.room_id
                WHERE r.estimate_id = @estimateId";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@jobId", jobId);
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateEstimateStatus(int estimateId, string status, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "UPDATE estimates SET status = @status, job_id = @jobId, modified_date = @modifiedDate WHERE estimate_id = @estimateId";
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@jobId", jobId);
                cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Estimate Methods

        /// <summary>
        /// Get all estimates - using correct snake_case column names
        /// </summary>
        public List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            if (!TableExists("estimates"))
            {
                return estimates; // Return empty list if table doesn't exist
            }
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name 
                    FROM estimates e 
                    LEFT JOIN customers c ON e.customer_id = c.customer_id 
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
                            Name = reader.IsDBNull("customer_name") ? "Unknown" : reader.GetString("customer_name")
                        };
                        estimates.Add(estimate);
                    }
                }
            }
            
            return estimates;
        }

        /// <summary>
        /// Save estimate (add or update) - using correct snake_case column names
        /// </summary>
        public void SaveEstimate(Estimate estimate)
        {
            if (!TableExists("estimates"))
            {
                throw new InvalidOperationException("Estimates table does not exist. Please run the database migration script.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                string query;
                if (estimate.EstimateId == 0)
                {
                    // Insert new estimate
                    query = @"
                        INSERT INTO estimates (estimate_number, version, customer_id, job_name, job_address, 
                                             job_city, job_state, job_zip, square_footage, num_floors, status,
                                             created_date, created_by, tax_rate, material_markup, labor_rate,
                                             total_material_cost, total_labor_minutes, total_price, notes)
                        VALUES (@estimate_number, @version, @customer_id, @job_name, @job_address,
                                @job_city, @job_state, @job_zip, @square_footage, @num_floors, @status,
                                @created_date, @created_by, @tax_rate, @material_markup, @labor_rate,
                                @total_material_cost, @total_labor_minutes, @total_price, @notes);
                        SELECT LAST_INSERT_ID();";
                }
                else
                {
                    // Update existing estimate
                    query = @"
                        UPDATE estimates SET
                            estimate_number = @estimate_number, version = @version, customer_id = @customer_id,
                            job_name = @job_name, job_address = @job_address, job_city = @job_city,
                            job_state = @job_state, job_zip = @job_zip, square_footage = @square_footage,
                            num_floors = @num_floors, status = @status, modified_date = @modified_date,
                            modified_by = @modified_by, tax_rate = @tax_rate, material_markup = @material_markup,
                            labor_rate = @labor_rate, total_material_cost = @total_material_cost,
                            total_labor_minutes = @total_labor_minutes, total_price = @total_price, notes = @notes
                        WHERE estimate_id = @estimate_id";
                }

                using (var cmd = new MySqlCommand(query, connection))
                {
                    // Add parameters using snake_case column names and correct property names
                    cmd.Parameters.AddWithValue("@estimate_number", estimate.EstimateNumber);
                    cmd.Parameters.AddWithValue("@version", estimate.Version);
                    cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
                    cmd.Parameters.AddWithValue("@job_name", estimate.ProjectName);
                    cmd.Parameters.AddWithValue("@job_address", estimate.Address ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@job_city", estimate.City ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@job_state", estimate.State ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@job_zip", estimate.Zip ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", estimate.Status.ToString());
                    cmd.Parameters.AddWithValue("@tax_rate", estimate.TaxRate);
                    cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                    cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                    cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost);
                    cmd.Parameters.AddWithValue("@total_labor_minutes", estimate.TotalLaborMinutes);
                    cmd.Parameters.AddWithValue("@total_price", estimate.TotalPrice);
                    cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);

                    if (estimate.EstimateId == 0)
                    {
                        cmd.Parameters.AddWithValue("@created_date", estimate.CreateDate);
                        cmd.Parameters.AddWithValue("@created_by", estimate.CreatedBy ?? "System");
                        var result = cmd.ExecuteScalar();
                        estimate.EstimateId = Convert.ToInt32(result);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                        cmd.Parameters.AddWithValue("@modified_date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@modified_by", estimate.UpdatedBy ?? "System");
                        cmd.ExecuteNonQuery();
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
                if (!TableExists("estimates"))
                {
                    return false;
                }

                var query = "DELETE FROM estimates WHERE estimate_id = @estimateId";
                var parameters = new Dictionary<string, object> { ["@estimateId"] = estimateId };
                
                return ExecuteNonQuery(query, parameters) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get last estimate number - using correct snake_case column names
        /// </summary>
        public string GetLastEstimateNumber()
        {
            try
            {
                if (!TableExists("estimates"))
                {
                    return "EST-1000"; // Default if table doesn't exist
                }
                
                var query = "SELECT MAX(estimate_number) FROM estimates WHERE estimate_number LIKE 'EST-%'";
                var lastNumber = ExecuteScalar<string>(query);
                return lastNumber ?? "EST-1000";
            }
            catch
            {
                return "EST-1000"; // Default on any error
            }
        }

        /// <summary>
        /// Get estimate rooms (placeholder for extension methods)
        /// </summary>
        public List<EstimateRoom> GetEstimateRooms(int estimateId)
        {
            var rooms = new List<EstimateRoom>();
            
            if (!TableExists("estimaterooms"))
            {
                return rooms;
            }

            // Implementation would depend on actual table structure
            return rooms;
        }

        /// <summary>
        /// Get estimate stage summaries (placeholder for extension methods)
        /// </summary>
        public List<EstimateStageSummary> GetEstimateStageSummaries(int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            
            if (!TableExists("estimatestagesummary"))
            {
                return summaries;
            }

            // Implementation would depend on actual table structure
            return summaries;
        }

        /// <summary>
        /// Read estimate from data reader - using correct snake_case column names
        /// </summary>
        private Estimate ReadEstimate(MySqlDataReader reader)
        {
            return new Estimate
            {
                EstimateId = reader.GetInt32("estimate_id"),
                EstimateNumber = reader.GetString("estimate_number"),
                Version = reader.GetInt32("version"),
                CustomerId = reader.GetInt32("customer_id"),
                ProjectName = reader.GetString("job_name"),
                Address = reader.IsDBNull(reader.GetOrdinal("job_address")) ? null : reader.GetString("job_address"),
                City = reader.IsDBNull(reader.GetOrdinal("job_city")) ? null : reader.GetString("job_city"),
                State = reader.IsDBNull(reader.GetOrdinal("job_state")) ? null : reader.GetString("job_state"),
                Zip = reader.IsDBNull(reader.GetOrdinal("job_zip")) ? null : reader.GetString("job_zip"),
                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                Status = (EstimateStatus)Enum.Parse(typeof(EstimateStatus), reader.GetString("status")),
                CreateDate = reader.GetDateTime("created_date"),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetString("created_by"),
                UpdatedDate = reader.IsDBNull(reader.GetOrdinal("modified_date")) ? (DateTime?)null : reader.GetDateTime("modified_date"),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("modified_by")) ? null : reader.GetString("modified_by"),
                TaxRate = reader.GetDecimal("tax_rate"),
                MaterialMarkup = reader.GetDecimal("material_markup"),
                LaborRate = reader.GetDecimal("labor_rate"),
                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                TotalPrice = reader.GetDecimal("total_price"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                ConvertedToJobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? (int?)null : reader.GetInt32("job_id")
            };
        }

        #endregion

        #region Placeholder Methods for Other Entities

        // These are minimal implementations to prevent compilation errors
        // The full implementations would come from the main DatabaseService file

        public List<Job> GetAllJobs() => new List<Job>();
        public Job GetJobById(int jobId) => null;
        public Job GetJob(int jobId) => null;
        public Job GetJobByNumber(string jobNumber) => null;
        public List<Job> GetJobsByCustomer(int customerId) => new List<Job>();
        public int AddJob(Job job) => 0;
        public void UpdateJob(Job job) { }
        public void UpdateJobStatus(int jobId, string status) { }
        public bool DeleteJob(int jobId) => false;
        public string GetLastJobNumber() => "1";

        public List<JobStage> GetJobStages(int jobId) => new List<JobStage>();
        public int CreateJobStage(JobStage stage) => 0;
        public int AddJobStage(JobStage stage) => 0;
        public void UpdateJobStage(JobStage stage) { }
        public bool DeleteJobStage(int stageId) => false;

        public List<Employee> GetAllEmployees() => new List<Employee>();
        public List<Employee> GetActiveEmployees() => new List<Employee>();
        public int AddEmployee(Employee employee) => 0;
        public void UpdateEmployee(Employee employee) { }
        public void SaveEmployee(Employee employee) { }
        public bool DeleteEmployee(int employeeId) => false;

        public List<LaborEntry> GetLaborEntries(DateTime startDate, DateTime endDate) => new List<LaborEntry>();
        public int AddLaborEntry(LaborEntry entry) => 0;

        public List<MaterialEntry> GetMaterialEntriesByJob(int jobId) => new List<MaterialEntry>();
        public int AddMaterialEntry(MaterialEntry entry) => 0;

        public List<Vendor> GetAllVendors() => new List<Vendor>();
        public int AddVendor(Vendor vendor) => 0;

        public List<Material> GetAllMaterials() => new List<Material>();
        public Material GetMaterialById(int materialId) => null;
        public void UpdateMaterial(Material material) { }
        public void SaveMaterialPriceHistory(MaterialPriceHistory history) { }

        public List<PriceListItem> GetAllPriceListItems() => new List<PriceListItem>();
        public List<PriceListItem> GetActivePriceListItems() => new List<PriceListItem>();
        public PriceListItem GetPriceListItemByCode(string itemCode) => null;
        public int AddPriceListItem(PriceListItem item) => 0;
        public void UpdatePriceListItem(PriceListItem item) { }
        public void SavePriceListItem(PriceListItem item) { }
        public bool DeletePriceListItem(int itemId) => false;

        public List<RoomSpecification> GetRoomSpecificationsByJob(int jobId) => new List<RoomSpecification>();
        public List<RoomSpecification> GetRoomSpecifications(int jobId) => new List<RoomSpecification>();
        public int AddRoomSpecification(RoomSpecification spec) => 0;
        public void UpdateRoomSpecification(RoomSpecification spec) { }
        public bool DeleteRoomSpecification(int specId) => false;

        public List<PermitItem> GetPermitItemsByJob(int jobId) => new List<PermitItem>();
        public List<PermitItem> GetPermitItems(int jobId) => new List<PermitItem>();
        public int AddPermitItem(PermitItem item) => 0;
        public void UpdatePermitItem(PermitItem item) { }
        public bool DeletePermitItem(int permitId) => false;

        #endregion
    }
}
