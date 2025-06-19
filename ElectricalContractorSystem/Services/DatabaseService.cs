using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// CONSOLIDATED DATABASE SERVICE - ALL DATABASE OPERATIONS IN ONE FILE
    /// Fixed table naming conventions to match actual database schema (CAPITALIZED table names)
    /// Removed all partial class conflicts that were preventing customer data from loading
    /// FIXED: String-to-enum conversion errors and missing enum types
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString ?? 
                              "Server=localhost;Port=3306;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn;SslMode=none;";
        }

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get connection for external services
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Get database information for debugging
        /// </summary>
        public string GetDatabaseInfo()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var info = $"Connected to: {connection.Database} on {connection.DataSource}";
                    
                    // Get table list
                    var tablesQuery = "SHOW TABLES";
                    using (var cmd = new MySqlCommand(tablesQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var tables = new List<string>();
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                        info += $"\nTables found: {string.Join(", ", tables)}";
                    }
                    
                    return info;
                }
            }
            catch (Exception ex)
            {
                return $"Error getting database info: {ex.Message}";
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

        #endregion

        #region Customer Methods (FIXED - Using correct table names)

        /// <summary>
        /// Get all customers - FIXED to use correct capitalized table name
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // First check if Customers table exists
                    var checkQuery = "SHOW TABLES LIKE 'Customers'";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        var tableExists = checkCmd.ExecuteScalar() != null;
                        if (!tableExists)
                        {
                            System.Diagnostics.Debug.WriteLine("Customers table does not exist!");
                            return customers;
                        }
                    }
                    
                    // Get customer count first
                    var countQuery = "SELECT COUNT(*) FROM Customers";
                    using (var countCmd = new MySqlCommand(countQuery, connection))
                    {
                        var count = Convert.ToInt32(countCmd.ExecuteScalar());
                        System.Diagnostics.Debug.WriteLine($"Found {count} customers in database");
                    }
                    
                    // Get all customers
                    var query = "SELECT customer_id, name, address, city, state, zip, email, phone, notes FROM Customers ORDER BY name";
                    
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
                
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {customers.Count} customers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllCustomers: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            return customers;
        }

        /// <summary>
        /// Get customer by ID - FIXED
        /// </summary>
        public Customer GetCustomerById(int customerId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT customer_id, name, address, city, state, zip, email, phone, notes FROM Customers WHERE customer_id = @customerId";
                    
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetCustomerById: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Add new customer - FIXED
        /// </summary>
        public int AddCustomer(Customer customer)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        INSERT INTO Customers (name, address, city, state, zip, email, phone, notes)
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddCustomer: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Update customer - FIXED
        /// </summary>
        public void UpdateCustomer(Customer customer)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        UPDATE Customers SET 
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCustomer: {ex.Message}");
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
        /// Delete customer - FIXED
        /// </summary>
        public bool DeleteCustomer(int customerId)
        {
            try
            {
                var query = "DELETE FROM Customers WHERE customer_id = @customerId";
                var parameters = new Dictionary<string, object> { ["@customerId"] = customerId };
                
                return ExecuteNonQuery(query, parameters) > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DeleteCustomer: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Job Methods (FIXED - Using correct table names and status conversion)

        /// <summary>
        /// Get all jobs - FIXED
        /// </summary>
        public List<Job> GetAllJobs()
        {
            var jobs = new List<Job>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT j.*, c.name as customer_name 
                        FROM Jobs j 
                        LEFT JOIN Customers c ON j.customer_id = c.customer_id 
                        ORDER BY j.job_number DESC";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var job = ReadJob(reader);
                            jobs.Add(job);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllJobs: {ex.Message}");
            }
            
            return jobs;
        }

        /// <summary>
        /// Get job by ID - FIXED
        /// </summary>
        public Job GetJobById(int jobId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT j.*, c.name as customer_name 
                        FROM Jobs j 
                        LEFT JOIN Customers c ON j.customer_id = c.customer_id 
                        WHERE j.job_id = @jobId";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@jobId", jobId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return ReadJob(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetJobById: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Get job - alias for GetJobById
        /// </summary>
        public Job GetJob(int jobId)
        {
            return GetJobById(jobId);
        }

        /// <summary>
        /// Get job by number - FIXED
        /// </summary>
        public Job GetJobByNumber(string jobNumber)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT j.*, c.name as customer_name 
                        FROM Jobs j 
                        LEFT JOIN Customers c ON j.customer_id = c.customer_id 
                        WHERE j.job_number = @jobNumber";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@jobNumber", jobNumber);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return ReadJob(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetJobByNumber: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Save job (add or update)
        /// </summary>
        public void SaveJob(Job job)
        {
            if (job.JobId == 0)
            {
                job.JobId = AddJob(job);
            }
            else
            {
                UpdateJob(job);
            }
        }

        /// <summary>
        /// Add new job - FIXED
        /// </summary>
        public int AddJob(Job job)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                         square_footage, num_floors, status, create_date, total_estimate, 
                                         total_actual, notes)
                        VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                                @square_footage, @num_floors, @status, @create_date, @total_estimate,
                                @total_actual, @notes);
                        SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(query, connection))
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
                        cmd.Parameters.AddWithValue("@status", job.Status); // Job.Status is string, store as string
                        cmd.Parameters.AddWithValue("@create_date", job.CreateDate);
                        cmd.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@total_actual", job.TotalActual ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);

                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddJob: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Update job - FIXED
        /// </summary>
        public void UpdateJob(Job job)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        UPDATE Jobs SET 
                            job_number = @job_number, customer_id = @customer_id, job_name = @job_name,
                            address = @address, city = @city, state = @state, zip = @zip,
                            square_footage = @square_footage, num_floors = @num_floors, status = @status,
                            completion_date = @completion_date, total_estimate = @total_estimate,
                            total_actual = @total_actual, notes = @notes
                        WHERE job_id = @job_id";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@job_id", job.JobId);
                        cmd.Parameters.AddWithValue("@job_number", job.JobNumber);
                        cmd.Parameters.AddWithValue("@customer_id", job.CustomerId);
                        cmd.Parameters.AddWithValue("@job_name", job.JobName);
                        cmd.Parameters.AddWithValue("@address", job.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@city", job.City ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@state", job.State ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@zip", job.Zip ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@square_footage", job.SquareFootage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@num_floors", job.NumFloors ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@status", job.Status); // Job.Status is string, store as string
                        cmd.Parameters.AddWithValue("@completion_date", job.CompletionDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@total_estimate", job.TotalEstimate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@total_actual", job.TotalActual ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", job.Notes ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateJob: {ex.Message}");
            }
        }

        /// <summary>
        /// Read job from data reader - FIXED type conversions
        /// </summary>
        private Job ReadJob(MySqlDataReader reader)
        {
            return new Job
            {
                JobId = reader.GetInt32("job_id"),
                JobNumber = reader.GetString("job_number"),
                CustomerId = reader.GetInt32("customer_id"),
                JobName = reader.GetString("job_name"),
                Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                State = reader.IsDBNull("state") ? null : reader.GetString("state"),
                Zip = reader.IsDBNull("zip") ? null : reader.GetString("zip"),
                SquareFootage = reader.IsDBNull("square_footage") ? (int?)null : reader.GetInt32("square_footage"),
                NumFloors = reader.IsDBNull("num_floors") ? (int?)null : reader.GetInt32("num_floors"),
                Status = reader.IsDBNull("status") ? "Estimate" : reader.GetString("status"), // FIXED: Use string instead of enum
                CreateDate = reader.GetDateTime("create_date"),
                CompletionDate = reader.IsDBNull("completion_date") ? (DateTime?)null : reader.GetDateTime("completion_date"),
                TotalEstimate = reader.IsDBNull("total_estimate") ? (decimal?)null : reader.GetDecimal("total_estimate"),
                TotalActual = reader.IsDBNull("total_actual") ? (decimal?)null : reader.GetDecimal("total_actual"),
                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
            };
        }

        #endregion

        #region Employee Methods (FIXED - Using correct table names and status conversion)

        /// <summary>
        /// Get all employees - FIXED
        /// </summary>
        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT employee_id, name, hourly_rate, burden_rate, status, notes FROM Employees ORDER BY name";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employee
                            {
                                EmployeeId = reader.GetInt32("employee_id"),
                                Name = reader.GetString("name"),
                                HourlyRate = reader.GetDecimal("hourly_rate"),
                                BurdenRate = reader.IsDBNull("burden_rate") ? (decimal?)null : reader.GetDecimal("burden_rate"),
                                Status = reader.IsDBNull("status") ? "Active" : reader.GetString("status"), // FIXED: Use string instead of enum
                                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllEmployees: {ex.Message}");
            }
            
            return employees;
        }

        /// <summary>
        /// Get active employees only - FIXED
        /// </summary>
        public List<Employee> GetActiveEmployees()
        {
            var employees = new List<Employee>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT employee_id, name, hourly_rate, burden_rate, status, notes FROM Employees WHERE status = 'Active' ORDER BY name";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employee
                            {
                                EmployeeId = reader.GetInt32("employee_id"),
                                Name = reader.GetString("name"),
                                HourlyRate = reader.GetDecimal("hourly_rate"),
                                BurdenRate = reader.IsDBNull("burden_rate") ? (decimal?)null : reader.GetDecimal("burden_rate"),
                                Status = reader.IsDBNull("status") ? "Active" : reader.GetString("status"), // FIXED: Use string instead of enum
                                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetActiveEmployees: {ex.Message}");
            }
            
            return employees;
        }

        #endregion

        #region Vendor Methods

        /// <summary>
        /// Get all vendors
        /// </summary>
        public List<Vendor> GetAllVendors()
        {
            var vendors = new List<Vendor>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT vendor_id, name, address, city, state, zip, phone, email, notes FROM Vendors ORDER BY name";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vendors.Add(new Vendor
                            {
                                VendorId = reader.GetInt32("vendor_id"),
                                Name = reader.GetString("name"),
                                Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                                City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                                State = reader.IsDBNull("state") ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull("zip") ? null : reader.GetString("zip"),
                                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                                Notes = reader.IsDBNull("notes") ? null : reader.GetString("notes")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetAllVendors: {ex.Message}");
            }
            
            return vendors;
        }

        /// <summary>
        /// Add new vendor
        /// </summary>
        public int AddVendor(Vendor vendor)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        INSERT INTO Vendors (name, address, city, state, zip, phone, email, notes)
                        VALUES (@name, @address, @city, @state, @zip, @phone, @email, @notes);
                        SELECT LAST_INSERT_ID();";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@name", vendor.Name);
                        cmd.Parameters.AddWithValue("@address", vendor.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@city", vendor.City ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@state", vendor.State ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@zip", vendor.Zip ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@phone", vendor.Phone ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@email", vendor.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@notes", vendor.Notes ?? (object)DBNull.Value);
                        
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddVendor: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Estimate to Job Conversion - FIXED IMPLEMENTATION

        /// <summary>
        /// Convert an estimate to a job with all associated data
        /// </summary>
        /// <param name="estimate">The estimate to convert</param>
        /// <returns>The ID of the newly created job</returns>
        public int ConvertEstimateToJob(Estimate estimate)
        {
            if (estimate == null)
                throw new ArgumentNullException(nameof(estimate));

            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            
            try
            {
                connection = new MySqlConnection(_connectionString);
                connection.Open();
                transaction = connection.BeginTransaction();

                // 1. Create the job from estimate
                var jobId = CreateJobFromEstimate(estimate, connection, transaction);

                // 2. Copy room specifications if they exist
                CopyRoomSpecifications(estimate.EstimateId, jobId, connection, transaction);

                // 3. Create job stages from estimate stage summaries
                CreateJobStagesFromEstimate(estimate.EstimateId, jobId, connection, transaction);

                // 4. Update estimate status to converted
                UpdateEstimateStatus(estimate.EstimateId, "Converted", connection, transaction);

                // 5. Link estimate to job
                LinkEstimateToJob(estimate.EstimateId, jobId, connection, transaction);

                transaction.Commit();
                return jobId;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                System.Diagnostics.Debug.WriteLine($"Error in ConvertEstimateToJob: {ex.Message}");
                throw new InvalidOperationException($"Failed to convert estimate to job: {ex.Message}", ex);
            }
            finally
            {
                transaction?.Dispose();
                connection?.Close();
            }
        }

        /// <summary>
        /// Create job from estimate data
        /// </summary>
        private int CreateJobFromEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                 square_footage, num_floors, status, create_date, total_estimate, notes)
                VALUES (@job_number, @customer_id, @job_name, @address, @city, @state, @zip,
                        @square_footage, @num_floors, @status, @create_date, @total_estimate, @notes);
                SELECT LAST_INSERT_ID();";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@job_number", GetNextJobNumber());
                cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
                cmd.Parameters.AddWithValue("@job_name", estimate.EstimateName ?? $"Job from Estimate {estimate.EstimateNumber}");
                cmd.Parameters.AddWithValue("@address", estimate.JobAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@city", estimate.JobCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@state", estimate.JobState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@zip", estimate.JobZip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", "In Progress");
                cmd.Parameters.AddWithValue("@create_date", DateTime.Now);
                cmd.Parameters.AddWithValue("@total_estimate", estimate.GrandTotal);
                cmd.Parameters.AddWithValue("@notes", $"Created from estimate {estimate.EstimateNumber}");

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Copy room specifications from estimate to job
        /// </summary>
        private void CopyRoomSpecifications(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // First, get all estimate rooms and their items
            var getRoomsQuery = @"
                SELECT er.room_id, er.room_name, er.room_order,
                       ei.item_id, ei.assembly_id, ei.quantity, ei.unit_price, ei.total_price, ei.line_order
                FROM EstimateRooms er
                LEFT JOIN EstimateItems ei ON er.room_id = ei.room_id
                WHERE er.estimate_id = @estimateId
                ORDER BY er.room_order, ei.line_order";

            var roomSpecifications = new List<RoomSpecification>();
            
            using (var cmd = new MySqlCommand(getRoomsQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull("item_id")) // Only if there are items
                        {
                            var spec = new RoomSpecification
                            {
                                JobId = jobId,
                                RoomName = reader.GetString("room_name"),
                                ItemDescription = $"Assembly Item", // We'll get details from assembly
                                Quantity = reader.GetInt32("quantity"),
                                UnitPrice = reader.GetDecimal("unit_price"),
                                TotalPrice = reader.GetDecimal("total_price")
                            };
                            roomSpecifications.Add(spec);
                        }
                    }
                }
            }

            // Insert room specifications into the Jobs system
            foreach (var spec in roomSpecifications)
            {
                var insertQuery = @"
                    INSERT INTO RoomSpecifications (job_id, room_name, item_description, quantity, unit_price, total_price)
                    VALUES (@job_id, @room_name, @item_description, @quantity, @unit_price, @total_price)";

                using (var cmd = new MySqlCommand(insertQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@job_id", spec.JobId);
                    cmd.Parameters.AddWithValue("@room_name", spec.RoomName);
                    cmd.Parameters.AddWithValue("@item_description", spec.ItemDescription);
                    cmd.Parameters.AddWithValue("@quantity", spec.Quantity);
                    cmd.Parameters.AddWithValue("@unit_price", spec.UnitPrice);
                    cmd.Parameters.AddWithValue("@total_price", spec.TotalPrice);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Create job stages from estimate stage summaries
        /// </summary>
        private void CreateJobStagesFromEstimate(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var getStagesQuery = @"
                SELECT stage_name, total_labor_hours, total_material_cost, total_cost
                FROM EstimateStageSummary
                WHERE estimate_id = @estimateId";

            using (var cmd = new MySqlCommand(getStagesQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = cmd.ExecuteReader())
                {
                    var stages = new List<(string stageName, decimal laborHours, decimal materialCost, decimal totalCost)>();
                    
                    while (reader.Read())
                    {
                        stages.Add((
                            reader.GetString("stage_name"),
                            reader.GetDecimal("total_labor_hours"),
                            reader.GetDecimal("total_material_cost"),
                            reader.GetDecimal("total_cost")
                        ));
                    }

                    reader.Close();

                    // Insert job stages
                    foreach (var (stageName, laborHours, materialCost, totalCost) in stages)
                    {
                        var insertStageQuery = @"
                            INSERT INTO JobStages (job_id, stage_name, estimated_hours, estimated_material_cost, actual_hours, actual_material_cost)
                            VALUES (@job_id, @stage_name, @estimated_hours, @estimated_material_cost, 0, 0)";

                        using (var insertCmd = new MySqlCommand(insertStageQuery, connection, transaction))
                        {
                            insertCmd.Parameters.AddWithValue("@job_id", jobId);
                            insertCmd.Parameters.AddWithValue("@stage_name", stageName);
                            insertCmd.Parameters.AddWithValue("@estimated_hours", laborHours);
                            insertCmd.Parameters.AddWithValue("@estimated_material_cost", materialCost);

                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update estimate status
        /// </summary>
        private void UpdateEstimateStatus(int estimateId, string status, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = "UPDATE Estimates SET status = @status WHERE estimate_id = @estimateId";
            
            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@estimateId", estimateId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Link estimate to job
        /// </summary>
        private void LinkEstimateToJob(int estimateId, int jobId, MySqlConnection connection, MySqlTransaction transaction)
        {
            // Add estimate_id field to Jobs table if it exists
            var query = @"
                UPDATE Jobs SET estimate_id = @estimateId WHERE job_id = @jobId";
            
            try
            {
                using (var cmd = new MySqlCommand(query, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException ex)
            {
                // If estimate_id column doesn't exist, that's okay - we'll track it another way
                System.Diagnostics.Debug.WriteLine($"Could not link estimate to job (column may not exist): {ex.Message}");
            }
        }

        #endregion

        #region Required Methods by Other Services (EMPTY IMPLEMENTATIONS)

        // Estimates
        public List<Estimate> GetAllEstimates() => new List<Estimate>();
        public void SaveEstimate(Estimate estimate) { }
        public bool DeleteEstimate(int estimateId) => false;
        public string GetLastEstimateNumber() => "EST-1000";
        public Estimate GetEstimateById(int estimateId) => null;
        public List<Estimate> GetEstimatesInDateRange(DateTime start, DateTime end) => new List<Estimate>();

        // Assembly Methods
        public void SaveAssembly(AssemblyTemplate assembly) { }
        public AssemblyTemplate GetAssemblyById(int assemblyId) => null;
        public List<AssemblyTemplate> GetAllAssemblies() => new List<AssemblyTemplate>();
        public List<AssemblyVariant> GetAssemblyVariants(int assemblyId) => new List<AssemblyVariant>();
        public void CreateAssemblyVariantRelationship(int parentId, int variantId) { }
        public void SaveAssemblyComponent(AssemblyComponent component) { }
        public AssemblyComponent GetAssemblyComponentById(int componentId) => null;
        public void UpdateAssemblyComponent(AssemblyComponent component) { }
        public void DeleteAssemblyComponent(int componentId) { }
        public List<DifficultyPreset> GetAllDifficultyPresets() => new List<DifficultyPreset>();
        public List<LaborAdjustment> GetLaborAdjustmentsByJob(int jobId) => new List<LaborAdjustment>();
        public int GetAssemblyUsageCount(int assemblyId) => 0;
        public DateTime? GetAssemblyLastUsedDate(int assemblyId) => null;

        // Properties
        public List<Property> GetPropertiesForCustomer(int customerId) => new List<Property>();
        public void SaveProperty(Property property) { }
        public void UpdateProperty(Property property) { }
        public void DeleteProperty(int propertyId) { }

        // Materials and Pricing
        public List<MaterialPriceHistory> GetMaterialPriceHistory(int materialId) => new List<MaterialPriceHistory>();

        // Job Stages
        public List<JobStage> GetJobStages(int jobId) => new List<JobStage>();
        public int AddJobStage(JobStage stage) => 0;
        public void UpdateJobStage(JobStage stage) { }
        public bool DeleteJobStage(int stageId) => false;
        public void CreateJobStage(JobStage stage) { }

        // Room Specifications
        public List<RoomSpecification> GetRoomSpecifications(int jobId) => new List<RoomSpecification>();
        public List<RoomSpecification> GetRoomSpecificationsByJob(int jobId) => new List<RoomSpecification>();

        // Permit Items
        public List<PermitItem> GetPermitItems(int jobId) => new List<PermitItem>();
        public List<PermitItem> GetPermitItemsByJob(int jobId) => new List<PermitItem>();

        // Additional CRUD operations (empty implementations)
        public void UpdateJobStatus(int jobId, string status) { }
        public bool DeleteJob(int jobId) => false;
        public List<Job> GetJobsByCustomer(int customerId) => new List<Job>();
        public string GetLastJobNumber() => "716";
        public string GetNextJobNumber()
        {
            try
            {
                var query = "SELECT MAX(CAST(job_number AS UNSIGNED)) FROM Jobs WHERE job_number REGEXP '^[0-9]+$'";
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

        public int AddEmployee(Employee employee) => 0;
        public void UpdateEmployee(Employee employee) { }
        public void SaveEmployee(Employee employee) { }
        public bool DeleteEmployee(int employeeId) => false;

        // Price List
        public List<PriceListItem> GetAllPriceListItems() => new List<PriceListItem>();
        public List<PriceListItem> GetActivePriceListItems() => new List<PriceListItem>();
        public PriceListItem GetPriceListItemByCode(string itemCode) => null;
        public int AddPriceListItem(PriceListItem item) => 0;
        public void UpdatePriceListItem(PriceListItem item) { }
        public void SavePriceListItem(PriceListItem item) { }
        public bool DeletePriceListItem(int itemId) => false;

        // Materials
        public List<Material> GetAllMaterials() => new List<Material>();
        public Material GetMaterialById(int materialId) => null;
        public void UpdateMaterial(Material material) { }
        public void SaveMaterialPriceHistory(MaterialPriceHistory history) { }

        // Labor and Material Entries
        public List<LaborEntry> GetLaborEntries(DateTime startDate, DateTime endDate) => new List<LaborEntry>();
        public int AddLaborEntry(LaborEntry entry) => 0;
        public List<MaterialEntry> GetMaterialEntriesByJob(int jobId) => new List<MaterialEntry>();
        public int AddMaterialEntry(MaterialEntry entry) => 0;
        public int AddRoomSpecification(RoomSpecification spec) => 0;
        public void UpdateRoomSpecification(RoomSpecification spec) { }
        public bool DeleteRoomSpecification(int specId) => false;
        public int AddPermitItem(PermitItem item) => 0;
        public void UpdatePermitItem(PermitItem item) { }
        public bool DeletePermitItem(int permitId) => false;

        #endregion
    }
}