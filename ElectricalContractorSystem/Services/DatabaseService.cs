using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

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
            _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"].ConnectionString;
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

        #region Customer Methods

        /// <summary>
        /// Get all customers
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Customers ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(ReadCustomer(reader));
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
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Customers WHERE customer_id = @customerId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@customerId", customerId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadCustomer(reader);
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
            var query = @"
                INSERT INTO Customers (name, address, city, state, zip, email, phone, notes)
                VALUES (@name, @address, @city, @state, @zip, @email, @phone, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@name"] = customer.Name,
                ["@address"] = customer.Address,
                ["@city"] = customer.City,
                ["@state"] = customer.State,
                ["@zip"] = customer.Zip,
                ["@email"] = customer.Email,
                ["@phone"] = customer.Phone,
                ["@notes"] = customer.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Update customer
        /// </summary>
        public void UpdateCustomer(Customer customer)
        {
            var query = @"
                UPDATE Customers SET
                    name = @name, address = @address, city = @city, state = @state,
                    zip = @zip, email = @email, phone = @phone, notes = @notes
                WHERE customer_id = @customerId";
            
            var parameters = new Dictionary<string, object>
            {
                ["@customerId"] = customer.CustomerId,
                ["@name"] = customer.Name,
                ["@address"] = customer.Address,
                ["@city"] = customer.City,
                ["@state"] = customer.State,
                ["@zip"] = customer.Zip,
                ["@email"] = customer.Email,
                ["@phone"] = customer.Phone,
                ["@notes"] = customer.Notes
            };

            ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Delete customer
        /// </summary>
        public bool DeleteCustomer(int customerId)
        {
            try
            {
                var query = "DELETE FROM Customers WHERE customer_id = @customerId";
                var parameters = new Dictionary<string, object> { ["@customerId"] = customerId };
                
                return ExecuteNonQuery(query, parameters) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Read customer from data reader
        /// </summary>
        private Customer ReadCustomer(MySqlDataReader reader)
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
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Job Methods

        /// <summary>
        /// Get all jobs
        /// </summary>
        public List<Job> GetAllJobs()
        {
            var jobs = new List<Job>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT j.*, c.name as customer_name
                    FROM Jobs j
                    INNER JOIN Customers c ON j.customer_id = c.customer_id
                    ORDER BY j.job_number DESC";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var job = ReadJob(reader);
                        job.Customer = new Customer
                        {
                            CustomerId = job.CustomerId,
                            Name = reader.GetString("customer_name")
                        };
                        jobs.Add(job);
                    }
                }
            }
            
            return jobs;
        }

        /// <summary>
        /// Get job by ID
        /// </summary>
        public Job GetJobById(int jobId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT j.*, c.*
                    FROM Jobs j
                    INNER JOIN Customers c ON j.customer_id = c.customer_id
                    WHERE j.job_id = @jobId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var job = ReadJob(reader);
                            job.Customer = ReadCustomer(reader);
                            
                            // Load job stages
                            job.Stages = GetJobStages(jobId);
                            
                            return job;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get job by job number
        /// </summary>
        public Job GetJobByNumber(string jobNumber)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT j.*, c.*
                    FROM Jobs j
                    INNER JOIN Customers c ON j.customer_id = c.customer_id
                    WHERE j.job_number = @jobNumber";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@jobNumber", jobNumber);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var job = ReadJob(reader);
                            job.Customer = ReadCustomer(reader);
                            return job;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add new job
        /// </summary>
        public int AddJob(Job job)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var query = @"
                            INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip,
                                            square_footage, num_floors, status, create_date, total_estimate, notes, estimate_id)
                            VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                                    @square_footage, @num_floors, @status, @create_date, @total_estimate, @notes, @estimate_id)";
                        
                        using (var cmd = new MySqlCommand(query, connection, transaction))
                        {
                            AddJobParameters(cmd, job);
                            cmd.ExecuteNonQuery();
                            job.JobId = (int)cmd.LastInsertedId;
                        }
                        
                        // Create default job stages if they don't exist
                        CreateDefaultJobStages(job.JobId, connection, transaction);
                        
                        transaction.Commit();
                        return job.JobId;
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
        /// Update job
        /// </summary>
        public void UpdateJob(Job job)
        {
            var query = @"
                UPDATE Jobs SET
                    customer_id = @customer_id, job_name = @job_name, address = @address,
                    city = @city, state = @state, zip = @zip, square_footage = @square_footage,
                    num_floors = @num_floors, status = @status, completion_date = @completion_date,
                    total_estimate = @total_estimate, total_actual = @total_actual, notes = @notes
                WHERE job_id = @job_id";
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", job.JobId);
                    AddJobParameters(cmd, job);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Delete job
        /// </summary>
        public bool DeleteJob(int jobId)
        {
            try
            {
                var query = "DELETE FROM Jobs WHERE job_id = @jobId";
                var parameters = new Dictionary<string, object> { ["@jobId"] = jobId };
                
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
            var query = "SELECT MAX(CAST(job_number AS UNSIGNED)) FROM Jobs WHERE job_number REGEXP '^[0-9]+$'";
            var maxNumber = ExecuteScalar<int>(query);
            return (maxNumber + 1).ToString();
        }

        /// <summary>
        /// Helper method to add job parameters
        /// </summary>
        private void AddJobParameters(MySqlCommand cmd, Job job)
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
            cmd.Parameters.AddWithValue("@completion_date", job.CompletionDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@total_actual", job.TotalActual ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@estimate_id", job.EstimateId ?? (object)DBNull.Value);
        }

        /// <summary>
        /// Read job from data reader
        /// </summary>
        private Job ReadJob(MySqlDataReader reader)
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
                CompletionDate = reader.IsDBNull(reader.GetOrdinal("completion_date")) ? (DateTime?)null : reader.GetDateTime("completion_date"),
                TotalEstimate = reader.IsDBNull(reader.GetOrdinal("total_estimate")) ? (decimal?)null : reader.GetDecimal("total_estimate"),
                TotalActual = reader.IsDBNull(reader.GetOrdinal("total_actual")) ? (decimal?)null : reader.GetDecimal("total_actual"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                EstimateId = reader.IsDBNull(reader.GetOrdinal("estimate_id")) ? (int?)null : reader.GetInt32("estimate_id")
            };
        }

        #endregion

        #region Job Stage Methods

        /// <summary>
        /// Get job stages for a job
        /// </summary>
        public List<JobStage> GetJobStages(int jobId)
        {
            var stages = new List<JobStage>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM JobStages WHERE job_id = @jobId ORDER BY stage_name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stages.Add(ReadJobStage(reader));
                        }
                    }
                }
            }
            
            return stages;
        }

        /// <summary>
        /// Create default job stages for a new job
        /// </summary>
        private void CreateDefaultJobStages(int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var stageNames = new[] { "Demo", "Rough", "Service", "Finish", "Extra", "Temp Service", "Inspection", "Other" };
            
            foreach (var stageName in stageNames)
            {
                var query = @"
                    INSERT INTO JobStages (job_id, stage_name, estimated_hours, estimated_material_cost, actual_hours, actual_material_cost)
                    VALUES (@job_id, @stage_name, 0, 0, 0, 0)";
                
                using (var cmd = new MySqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    cmd.Parameters.AddWithValue("@stage_name", stageName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update job stage
        /// </summary>
        public void UpdateJobStage(JobStage stage)
        {
            var query = @"
                UPDATE JobStages SET
                    estimated_hours = @estimated_hours,
                    estimated_material_cost = @estimated_material_cost,
                    actual_hours = @actual_hours,
                    actual_material_cost = @actual_material_cost,
                    notes = @notes
                WHERE stage_id = @stage_id";
            
            var parameters = new Dictionary<string, object>
            {
                ["@stage_id"] = stage.StageId,
                ["@estimated_hours"] = stage.EstimatedHours,
                ["@estimated_material_cost"] = stage.EstimatedMaterialCost,
                ["@actual_hours"] = stage.ActualHours,
                ["@actual_material_cost"] = stage.ActualMaterialCost,
                ["@notes"] = stage.Notes
            };

            ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Read job stage from data reader
        /// </summary>
        private JobStage ReadJobStage(MySqlDataReader reader)
        {
            return new JobStage
            {
                StageId = reader.GetInt32("stage_id"),
                JobId = reader.GetInt32("job_id"),
                StageName = reader.GetString("stage_name"),
                EstimatedHours = reader.GetDecimal("estimated_hours"),
                EstimatedMaterialCost = reader.GetDecimal("estimated_material_cost"),
                ActualHours = reader.GetDecimal("actual_hours"),
                ActualMaterialCost = reader.GetDecimal("actual_material_cost"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Employee Methods

        /// <summary>
        /// Get all employees
        /// </summary>
        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Employees ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(ReadEmployee(reader));
                    }
                }
            }
            
            return employees;
        }

        /// <summary>
        /// Get active employees
        /// </summary>
        public List<Employee> GetActiveEmployees()
        {
            var employees = new List<Employee>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Employees WHERE status = 'Active' ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(ReadEmployee(reader));
                    }
                }
            }
            
            return employees;
        }

        /// <summary>
        /// Add employee
        /// </summary>
        public int AddEmployee(Employee employee)
        {
            var query = @"
                INSERT INTO Employees (name, hourly_rate, burden_rate, status, notes)
                VALUES (@name, @hourly_rate, @burden_rate, @status, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@name"] = employee.Name,
                ["@hourly_rate"] = employee.HourlyRate,
                ["@burden_rate"] = employee.BurdenRate,
                ["@status"] = employee.Status,
                ["@notes"] = employee.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Update employee
        /// </summary>
        public void UpdateEmployee(Employee employee)
        {
            var query = @"
                UPDATE Employees SET
                    name = @name, hourly_rate = @hourly_rate, burden_rate = @burden_rate,
                    status = @status, notes = @notes
                WHERE employee_id = @employee_id";
            
            var parameters = new Dictionary<string, object>
            {
                ["@employee_id"] = employee.EmployeeId,
                ["@name"] = employee.Name,
                ["@hourly_rate"] = employee.HourlyRate,
                ["@burden_rate"] = employee.BurdenRate,
                ["@status"] = employee.Status,
                ["@notes"] = employee.Notes
            };

            ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Read employee from data reader
        /// </summary>
        private Employee ReadEmployee(MySqlDataReader reader)
        {
            return new Employee
            {
                EmployeeId = reader.GetInt32("employee_id"),
                Name = reader.GetString("name"),
                HourlyRate = reader.GetDecimal("hourly_rate"),
                BurdenRate = reader.IsDBNull(reader.GetOrdinal("burden_rate")) ? (decimal?)null : reader.GetDecimal("burden_rate"),
                Status = reader.GetString("status"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Labor Entry Methods

        /// <summary>
        /// Add labor entry
        /// </summary>
        public int AddLaborEntry(LaborEntry entry)
        {
            var query = @"
                INSERT INTO LaborEntries (job_id, employee_id, stage_id, date, hours, notes)
                VALUES (@job_id, @employee_id, @stage_id, @date, @hours, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@job_id"] = entry.JobId,
                ["@employee_id"] = entry.EmployeeId,
                ["@stage_id"] = entry.StageId,
                ["@date"] = entry.Date,
                ["@hours"] = entry.Hours,
                ["@notes"] = entry.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Get labor entries for a date range
        /// </summary>
        public List<LaborEntry> GetLaborEntries(DateTime startDate, DateTime endDate)
        {
            var entries = new List<LaborEntry>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT le.*, e.name as employee_name, j.job_number, js.stage_name
                    FROM LaborEntries le
                    INNER JOIN Employees e ON le.employee_id = e.employee_id
                    INNER JOIN Jobs j ON le.job_id = j.job_id
                    INNER JOIN JobStages js ON le.stage_id = js.stage_id
                    WHERE le.date BETWEEN @start_date AND @end_date
                    ORDER BY le.date, e.name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@start_date", startDate);
                    cmd.Parameters.AddWithValue("@end_date", endDate);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entry = ReadLaborEntry(reader);
                            entry.Employee = new Employee
                            {
                                EmployeeId = entry.EmployeeId,
                                Name = reader.GetString("employee_name")
                            };
                            entry.Job = new Job
                            {
                                JobId = entry.JobId,
                                JobNumber = reader.GetString("job_number")
                            };
                            entry.Stage = new JobStage
                            {
                                StageId = entry.StageId,
                                StageName = reader.GetString("stage_name")
                            };
                            entries.Add(entry);
                        }
                    }
                }
            }
            
            return entries;
        }

        /// <summary>
        /// Read labor entry from data reader
        /// </summary>
        private LaborEntry ReadLaborEntry(MySqlDataReader reader)
        {
            return new LaborEntry
            {
                EntryId = reader.GetInt32("entry_id"),
                JobId = reader.GetInt32("job_id"),
                EmployeeId = reader.GetInt32("employee_id"),
                StageId = reader.GetInt32("stage_id"),
                Date = reader.GetDateTime("date"),
                Hours = reader.GetDecimal("hours"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Material Entry Methods

        /// <summary>
        /// Add material entry
        /// </summary>
        public int AddMaterialEntry(MaterialEntry entry)
        {
            var query = @"
                INSERT INTO MaterialEntries (job_id, stage_id, vendor_id, date, cost, invoice_number, invoice_total, notes)
                VALUES (@job_id, @stage_id, @vendor_id, @date, @cost, @invoice_number, @invoice_total, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@job_id"] = entry.JobId,
                ["@stage_id"] = entry.StageId,
                ["@vendor_id"] = entry.VendorId,
                ["@date"] = entry.Date,
                ["@cost"] = entry.Cost,
                ["@invoice_number"] = entry.InvoiceNumber,
                ["@invoice_total"] = entry.InvoiceTotal,
                ["@notes"] = entry.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Get material entries for a job
        /// </summary>
        public List<MaterialEntry> GetMaterialEntriesByJob(int jobId)
        {
            var entries = new List<MaterialEntry>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT me.*, v.name as vendor_name, js.stage_name
                    FROM MaterialEntries me
                    INNER JOIN Vendors v ON me.vendor_id = v.vendor_id
                    INNER JOIN JobStages js ON me.stage_id = js.stage_id
                    WHERE me.job_id = @job_id
                    ORDER BY me.date";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entry = ReadMaterialEntry(reader);
                            entry.Vendor = new Vendor
                            {
                                VendorId = entry.VendorId,
                                Name = reader.GetString("vendor_name")
                            };
                            entry.Stage = new JobStage
                            {
                                StageId = entry.StageId,
                                StageName = reader.GetString("stage_name")
                            };
                            entries.Add(entry);
                        }
                    }
                }
            }
            
            return entries;
        }

        /// <summary>
        /// Read material entry from data reader
        /// </summary>
        private MaterialEntry ReadMaterialEntry(MySqlDataReader reader)
        {
            return new MaterialEntry
            {
                EntryId = reader.GetInt32("entry_id"),
                JobId = reader.GetInt32("job_id"),
                StageId = reader.GetInt32("stage_id"),
                VendorId = reader.GetInt32("vendor_id"),
                Date = reader.GetDateTime("date"),
                Cost = reader.GetDecimal("cost"),
                InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("invoice_number")) ? null : reader.GetString("invoice_number"),
                InvoiceTotal = reader.IsDBNull(reader.GetOrdinal("invoice_total")) ? (decimal?)null : reader.GetDecimal("invoice_total"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Vendor Methods

        /// <summary>
        /// Get all vendors
        /// </summary>
        public List<Vendor> GetAllVendors()
        {
            var vendors = new List<Vendor>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Vendors ORDER BY name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        vendors.Add(ReadVendor(reader));
                    }
                }
            }
            
            return vendors;
        }

        /// <summary>
        /// Add vendor
        /// </summary>
        public int AddVendor(Vendor vendor)
        {
            var query = @"
                INSERT INTO Vendors (name, address, city, state, zip, phone, email, notes)
                VALUES (@name, @address, @city, @state, @zip, @phone, @email, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@name"] = vendor.Name,
                ["@address"] = vendor.Address,
                ["@city"] = vendor.City,
                ["@state"] = vendor.State,
                ["@zip"] = vendor.Zip,
                ["@phone"] = vendor.Phone,
                ["@email"] = vendor.Email,
                ["@notes"] = vendor.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Read vendor from data reader
        /// </summary>
        private Vendor ReadVendor(MySqlDataReader reader)
        {
            return new Vendor
            {
                VendorId = reader.GetInt32("vendor_id"),
                Name = reader.GetString("name"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Price List Methods

        /// <summary>
        /// Get all price list items
        /// </summary>
        public List<PriceListItem> GetAllPriceListItems()
        {
            var items = new List<PriceListItem>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM PriceList WHERE is_active = 1 ORDER BY category, name";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(ReadPriceListItem(reader));
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// Get price list item by code
        /// </summary>
        public PriceListItem GetPriceListItemByCode(string itemCode)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM PriceList WHERE item_code = @item_code AND is_active = 1";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@item_code", itemCode);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadPriceListItem(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Add price list item
        /// </summary>
        public int AddPriceListItem(PriceListItem item)
        {
            var query = @"
                INSERT INTO PriceList (category, item_code, name, description, base_cost, 
                                     tax_rate, labor_minutes, markup_percentage, is_active, notes)
                VALUES (@category, @item_code, @name, @description, @base_cost,
                        @tax_rate, @labor_minutes, @markup_percentage, @is_active, @notes)";
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddPriceListItemParameters(cmd, item);
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Update price list item
        /// </summary>
        public void UpdatePriceListItem(PriceListItem item)
        {
            var query = @"
                UPDATE PriceList SET
                    category = @category, name = @name, description = @description,
                    base_cost = @base_cost, tax_rate = @tax_rate, labor_minutes = @labor_minutes,
                    markup_percentage = @markup_percentage, is_active = @is_active, notes = @notes
                WHERE item_id = @item_id";
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@item_id", item.ItemId);
                    AddPriceListItemParameters(cmd, item);
                    cmd.ExecuteNonQuery();
                }
            }
        }

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

        #region Room Specification Methods

        /// <summary>
        /// Get room specifications for a job
        /// </summary>
        public List<RoomSpecification> GetRoomSpecificationsByJob(int jobId)
        {
            var specs = new List<RoomSpecification>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM RoomSpecifications WHERE job_id = @job_id ORDER BY room_name";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            specs.Add(ReadRoomSpecification(reader));
                        }
                    }
                }
            }
            
            return specs;
        }

        /// <summary>
        /// Add room specification
        /// </summary>
        public int AddRoomSpecification(RoomSpecification spec)
        {
            var query = @"
                INSERT INTO RoomSpecifications (job_id, room_name, item_description, quantity, 
                                              item_code, unit_price, total_price)
                VALUES (@job_id, @room_name, @item_description, @quantity,
                        @item_code, @unit_price, @total_price)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@job_id"] = spec.JobId,
                ["@room_name"] = spec.RoomName,
                ["@item_description"] = spec.ItemDescription,
                ["@quantity"] = spec.Quantity,
                ["@item_code"] = spec.ItemCode,
                ["@unit_price"] = spec.UnitPrice,
                ["@total_price"] = spec.TotalPrice
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Read room specification from data reader
        /// </summary>
        private RoomSpecification ReadRoomSpecification(MySqlDataReader reader)
        {
            return new RoomSpecification
            {
                SpecId = reader.GetInt32("spec_id"),
                JobId = reader.GetInt32("job_id"),
                RoomName = reader.GetString("room_name"),
                ItemDescription = reader.GetString("item_description"),
                Quantity = reader.GetInt32("quantity"),
                ItemCode = reader.IsDBNull(reader.GetOrdinal("item_code")) ? null : reader.GetString("item_code"),
                UnitPrice = reader.GetDecimal("unit_price"),
                TotalPrice = reader.GetDecimal("total_price")
            };
        }

        #endregion

        #region Permit Item Methods

        /// <summary>
        /// Get permit items for a job
        /// </summary>
        public List<PermitItem> GetPermitItemsByJob(int jobId)
        {
            var items = new List<PermitItem>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM PermitItems WHERE job_id = @job_id ORDER BY category";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(ReadPermitItem(reader));
                        }
                    }
                }
            }
            
            return items;
        }

        /// <summary>
        /// Add permit item
        /// </summary>
        public int AddPermitItem(PermitItem item)
        {
            var query = @"
                INSERT INTO PermitItems (job_id, category, quantity, description, notes)
                VALUES (@job_id, @category, @quantity, @description, @notes)";
            
            var parameters = new Dictionary<string, object>
            {
                ["@job_id"] = item.JobId,
                ["@category"] = item.Category,
                ["@quantity"] = item.Quantity,
                ["@description"] = item.Description,
                ["@notes"] = item.Notes
            };

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        /// <summary>
        /// Read permit item from data reader
        /// </summary>
        private PermitItem ReadPermitItem(MySqlDataReader reader)
        {
            return new PermitItem
            {
                PermitId = reader.GetInt32("permit_id"),
                JobId = reader.GetInt32("job_id"),
                Category = reader.GetString("category"),
                Quantity = reader.GetInt32("quantity"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion
    }
}
