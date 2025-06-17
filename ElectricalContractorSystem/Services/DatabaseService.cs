using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Core database service for electrical contractor system
    /// Handles all database operations for jobs, customers, estimates, employees, materials, etc.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            try
            {
                _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString;
                if (string.IsNullOrEmpty(_connectionString))
                {
                    throw new InvalidOperationException("Database connection string 'ElectricalDB' not found in configuration");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize database service: {ex.Message}", ex);
            }
        }

        #region Connection and Utility Methods

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
        /// Execute a scalar query
        /// </summary>
        public object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Execute a non-query command
        /// </summary>
        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddParameters(cmd, parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Get a new database connection
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Helper method to add parameters to a command
        /// </summary>
        private void AddParameters(MySqlCommand cmd, Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
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
        /// Save customer (insert or update)
        /// </summary>
        public int SaveCustomer(Customer customer)
        {
            if (customer.CustomerId == 0)
            {
                return AddCustomer(customer);
            }
            else
            {
                UpdateCustomer(customer);
                return customer.CustomerId;
            }
        }

        /// <summary>
        /// Add new customer
        /// </summary>
        public int AddCustomer(Customer customer)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
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
                    return customer.CustomerId;
                }
            }
        }

        /// <summary>
        /// Update existing customer
        /// </summary>
        public void UpdateCustomer(Customer customer)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE Customers SET
                        name = @name, address = @address, city = @city, state = @state,
                        zip = @zip, email = @email, phone = @phone, notes = @notes
                    WHERE customer_id = @customer_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@customer_id", customer.CustomerId);
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

        #region Employee Methods

        /// <summary>
        /// Get all active employees
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
        /// Save employee (insert or update)
        /// </summary>
        public int SaveEmployee(Employee employee)
        {
            if (employee.EmployeeId == 0)
            {
                return AddEmployee(employee);
            }
            else
            {
                UpdateEmployee(employee);
                return employee.EmployeeId;
            }
        }

        /// <summary>
        /// Add new employee
        /// </summary>
        public int AddEmployee(Employee employee)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO Employees (name, hourly_rate, burden_rate, status, notes)
                    VALUES (@name, @hourly_rate, @burden_rate, @status, @notes)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", employee.Name);
                    cmd.Parameters.AddWithValue("@hourly_rate", employee.HourlyRate);
                    cmd.Parameters.AddWithValue("@burden_rate", employee.BurdenRate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", employee.Status);
                    cmd.Parameters.AddWithValue("@notes", employee.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                    employee.EmployeeId = (int)cmd.LastInsertedId;
                    return employee.EmployeeId;
                }
            }
        }

        /// <summary>
        /// Update existing employee
        /// </summary>
        public void UpdateEmployee(Employee employee)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE Employees SET
                        name = @name, hourly_rate = @hourly_rate, burden_rate = @burden_rate,
                        status = @status, notes = @notes
                    WHERE employee_id = @employee_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@employee_id", employee.EmployeeId);
                    cmd.Parameters.AddWithValue("@name", employee.Name);
                    cmd.Parameters.AddWithValue("@hourly_rate", employee.HourlyRate);
                    cmd.Parameters.AddWithValue("@burden_rate", employee.BurdenRate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", employee.Status);
                    cmd.Parameters.AddWithValue("@notes", employee.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }
            }
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

        #region Job Methods

        /// <summary>
        /// Get all jobs with customer information
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
        /// Get job by ID with full customer information
        /// </summary>
        public Job GetJobById(int jobId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT j.*, c.name as customer_name, c.address as customer_address, 
                           c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                           c.email as customer_email, c.phone as customer_phone
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
                            job.Customer = new Customer
                            {
                                CustomerId = job.CustomerId,
                                Name = reader.GetString("customer_name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("customer_address")) ? null : reader.GetString("customer_address"),
                                City = reader.IsDBNull(reader.GetOrdinal("customer_city")) ? null : reader.GetString("customer_city"),
                                State = reader.IsDBNull(reader.GetOrdinal("customer_state")) ? null : reader.GetString("customer_state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("customer_zip")) ? null : reader.GetString("customer_zip"),
                                Email = reader.IsDBNull(reader.GetOrdinal("customer_email")) ? null : reader.GetString("customer_email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("customer_phone")) ? null : reader.GetString("customer_phone")
                            };
                            return job;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get next available job number
        /// </summary>
        public string GetNextJobNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT MAX(CAST(job_number AS UNSIGNED)) FROM Jobs";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        var lastNumber = Convert.ToInt32(result);
                        return (lastNumber + 1).ToString();
                    }
                    return "401"; // Start from 401 if no jobs exist
                }
            }
        }

        /// <summary>
        /// Save job (insert or update)
        /// </summary>
        public int SaveJob(Job job)
        {
            if (job.JobId == 0)
            {
                return AddJob(job);
            }
            else
            {
                UpdateJob(job);
                return job.JobId;
            }
        }

        /// <summary>
        /// Add new job
        /// </summary>
        public int AddJob(Job job)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                      square_footage, num_floors, status, create_date, completion_date, 
                                      total_estimate, total_actual, notes)
                    VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                            @square_footage, @num_floors, @status, @create_date, @completion_date,
                            @total_estimate, @total_actual, @notes)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddJobParameters(cmd, job);
                    cmd.ExecuteNonQuery();
                    job.JobId = (int)cmd.LastInsertedId;
                    return job.JobId;
                }
            }
        }

        /// <summary>
        /// Update existing job
        /// </summary>
        public void UpdateJob(Job job)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE Jobs SET
                        customer_id = @customer_id, job_name = @job_name, address = @address,
                        city = @city, state = @state, zip = @zip, square_footage = @square_footage,
                        num_floors = @num_floors, status = @status, completion_date = @completion_date,
                        total_estimate = @total_estimate, total_actual = @total_actual, notes = @notes
                    WHERE job_id = @job_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", job.JobId);
                    AddJobParameters(cmd, job);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update job status
        /// </summary>
        public bool UpdateJobStatus(int jobId, string status)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "UPDATE Jobs SET status = @status WHERE job_id = @job_id";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@job_id", jobId);
                        cmd.Parameters.AddWithValue("@status", status);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Helper method to add job parameters to command
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
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Labor Entry Methods

        /// <summary>
        /// Get labor entries for a specific week
        /// </summary>
        public List<LaborEntry> GetLaborEntriesForWeek(DateTime weekStart)
        {
            var entries = new List<LaborEntry>();
            var weekEnd = weekStart.AddDays(6);
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT le.*, e.name as employee_name, j.job_number, js.stage_name
                    FROM LaborEntries le
                    INNER JOIN Employees e ON le.employee_id = e.employee_id
                    INNER JOIN Jobs j ON le.job_id = j.job_id
                    INNER JOIN JobStages js ON le.stage_id = js.stage_id
                    WHERE le.date >= @weekStart AND le.date <= @weekEnd
                    ORDER BY e.name, le.date";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@weekStart", weekStart);
                    cmd.Parameters.AddWithValue("@weekEnd", weekEnd);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(new LaborEntry
                            {
                                EntryId = reader.GetInt32("entry_id"),
                                JobId = reader.GetInt32("job_id"),
                                EmployeeId = reader.GetInt32("employee_id"),
                                StageId = reader.GetInt32("stage_id"),
                                Date = reader.GetDateTime("date"),
                                Hours = reader.GetDecimal("hours"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                EmployeeName = reader.GetString("employee_name"),
                                JobNumber = reader.GetString("job_number"),
                                StageName = reader.GetString("stage_name")
                            });
                        }
                    }
                }
            }
            
            return entries;
        }

        /// <summary>
        /// Save labor entry
        /// </summary>
        public int SaveLaborEntry(LaborEntry entry)
        {
            if (entry.EntryId == 0)
            {
                return AddLaborEntry(entry);
            }
            else
            {
                UpdateLaborEntry(entry);
                return entry.EntryId;
            }
        }

        /// <summary>
        /// Add new labor entry
        /// </summary>
        public int AddLaborEntry(LaborEntry entry)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO LaborEntries (job_id, employee_id, stage_id, date, hours, notes)
                    VALUES (@job_id, @employee_id, @stage_id, @date, @hours, @notes)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", entry.JobId);
                    cmd.Parameters.AddWithValue("@employee_id", entry.EmployeeId);
                    cmd.Parameters.AddWithValue("@stage_id", entry.StageId);
                    cmd.Parameters.AddWithValue("@date", entry.Date);
                    cmd.Parameters.AddWithValue("@hours", entry.Hours);
                    cmd.Parameters.AddWithValue("@notes", entry.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                    entry.EntryId = (int)cmd.LastInsertedId;
                    return entry.EntryId;
                }
            }
        }

        /// <summary>
        /// Update existing labor entry
        /// </summary>
        public void UpdateLaborEntry(LaborEntry entry)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE LaborEntries SET
                        job_id = @job_id, employee_id = @employee_id, stage_id = @stage_id,
                        date = @date, hours = @hours, notes = @notes
                    WHERE entry_id = @entry_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@entry_id", entry.EntryId);
                    cmd.Parameters.AddWithValue("@job_id", entry.JobId);
                    cmd.Parameters.AddWithValue("@employee_id", entry.EmployeeId);
                    cmd.Parameters.AddWithValue("@stage_id", entry.StageId);
                    cmd.Parameters.AddWithValue("@date", entry.Date);
                    cmd.Parameters.AddWithValue("@hours", entry.Hours);
                    cmd.Parameters.AddWithValue("@notes", entry.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Material Entry Methods

        /// <summary>
        /// Get all material entries for a job
        /// </summary>
        public List<MaterialEntry> GetMaterialEntriesForJob(int jobId)
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
                    WHERE me.job_id = @jobId
                    ORDER BY me.date DESC";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entries.Add(new MaterialEntry
                            {
                                EntryId = reader.GetInt32("entry_id"),
                                JobId = reader.GetInt32("job_id"),
                                StageId = reader.GetInt32("stage_id"),
                                VendorId = reader.GetInt32("vendor_id"),
                                Date = reader.GetDateTime("date"),
                                Cost = reader.GetDecimal("cost"),
                                InvoiceNumber = reader.IsDBNull(reader.GetOrdinal("invoice_number")) ? null : reader.GetString("invoice_number"),
                                InvoiceTotal = reader.IsDBNull(reader.GetOrdinal("invoice_total")) ? (decimal?)null : reader.GetDecimal("invoice_total"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                VendorName = reader.GetString("vendor_name"),
                                StageName = reader.GetString("stage_name")
                            });
                        }
                    }
                }
            }
            
            return entries;
        }

        /// <summary>
        /// Save material entry
        /// </summary>
        public int SaveMaterialEntry(MaterialEntry entry)
        {
            if (entry.EntryId == 0)
            {
                return AddMaterialEntry(entry);
            }
            else
            {
                UpdateMaterialEntry(entry);
                return entry.EntryId;
            }
        }

        /// <summary>
        /// Add new material entry
        /// </summary>
        public int AddMaterialEntry(MaterialEntry entry)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO MaterialEntries (job_id, stage_id, vendor_id, date, cost, invoice_number, invoice_total, notes)
                    VALUES (@job_id, @stage_id, @vendor_id, @date, @cost, @invoice_number, @invoice_total, @notes)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@job_id", entry.JobId);
                    cmd.Parameters.AddWithValue("@stage_id", entry.StageId);
                    cmd.Parameters.AddWithValue("@vendor_id", entry.VendorId);
                    cmd.Parameters.AddWithValue("@date", entry.Date);
                    cmd.Parameters.AddWithValue("@cost", entry.Cost);
                    cmd.Parameters.AddWithValue("@invoice_number", entry.InvoiceNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@invoice_total", entry.InvoiceTotal ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", entry.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                    entry.EntryId = (int)cmd.LastInsertedId;
                    return entry.EntryId;
                }
            }
        }

        /// <summary>
        /// Update existing material entry
        /// </summary>
        public void UpdateMaterialEntry(MaterialEntry entry)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    UPDATE MaterialEntries SET
                        job_id = @job_id, stage_id = @stage_id, vendor_id = @vendor_id,
                        date = @date, cost = @cost, invoice_number = @invoice_number,
                        invoice_total = @invoice_total, notes = @notes
                    WHERE entry_id = @entry_id";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@entry_id", entry.EntryId);
                    cmd.Parameters.AddWithValue("@job_id", entry.JobId);
                    cmd.Parameters.AddWithValue("@stage_id", entry.StageId);
                    cmd.Parameters.AddWithValue("@vendor_id", entry.VendorId);
                    cmd.Parameters.AddWithValue("@date", entry.Date);
                    cmd.Parameters.AddWithValue("@cost", entry.Cost);
                    cmd.Parameters.AddWithValue("@invoice_number", entry.InvoiceNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@invoice_total", entry.InvoiceTotal ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", entry.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }
            }
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
                        vendors.Add(new Vendor
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
                        });
                    }
                }
            }
            
            return vendors;
        }

        #endregion

        #region Price List Methods

        /// <summary>
        /// Get all active price list items
        /// </summary>
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
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PriceListItem
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
                        });
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
    }
}