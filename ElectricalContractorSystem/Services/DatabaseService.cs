using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Service for handling database operations
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private bool _disposed = false;

        public DatabaseService()
        {
            // Get connection string from App.config - corrected to use "MySQLConnection"
            _connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"]?.ConnectionString;
            
            // If connection string not found in config, use fallback
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "Server=localhost;Port=3306;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn!;";
            }
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
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
                System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes a SQL query and returns the result as a DataTable
        /// </summary>
        public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    connection.Open();
                    using (var adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database query error: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Executes a SQL command that doesn't return results (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <returns>Number of affected rows</returns>
        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            int result = 0;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    connection.Open();
                    result = command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database command error: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Executes a SQL query and returns a single value
        /// </summary>
        public object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            object result = null;

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    connection.Open();
                    result = command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database scalar query error: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets the last inserted ID
        /// </summary>
        public long GetLastInsertedId()
        {
            return Convert.ToInt64(ExecuteScalar("SELECT LAST_INSERT_ID()"));
        }

        #region Job Operations

        /// <summary>
        /// Gets all jobs with customer information
        /// </summary>
        public List<Job> GetAllJobs()
        {
            var jobs = new List<Job>();

            string query = @"
                SELECT j.*, c.name as customer_name, c.address as customer_address,
                       c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                       c.email as customer_email, c.phone as customer_phone
                FROM Jobs j 
                LEFT JOIN Customers c ON j.customer_id = c.customer_id 
                ORDER BY j.job_number DESC";

            var dataTable = ExecuteQuery(query);

            foreach (DataRow row in dataTable.Rows)
            {
                var job = new Job
                {
                    JobId = Convert.ToInt32(row["job_id"]),
                    JobNumber = row["job_number"].ToString(),
                    CustomerId = Convert.ToInt32(row["customer_id"]),
                    JobName = row["job_name"].ToString(),
                    Address = row["address"].ToString(),
                    City = row["city"].ToString(),
                    State = row["state"].ToString(),
                    Zip = row["zip"].ToString(),
                    SquareFootage = row["square_footage"] != DBNull.Value ? Convert.ToInt32(row["square_footage"]) : (int?)null,
                    NumFloors = row["num_floors"] != DBNull.Value ? Convert.ToInt32(row["num_floors"]) : (int?)null,
                    Status = row["status"].ToString(),
                    CreateDate = Convert.ToDateTime(row["create_date"]),
                    CompletionDate = row["completion_date"] != DBNull.Value ? Convert.ToDateTime(row["completion_date"]) : (DateTime?)null,
                    TotalEstimate = row["total_estimate"] != DBNull.Value ? Convert.ToDecimal(row["total_estimate"]) : (decimal?)null,
                    TotalActual = row["total_actual"] != DBNull.Value ? Convert.ToDecimal(row["total_actual"]) : (decimal?)null,
                    Notes = row["notes"].ToString()
                };

                // Add customer information
                if (row["customer_name"] != DBNull.Value)
                {
                    job.Customer = new Customer
                    {
                        CustomerId = job.CustomerId,
                        Name = row["customer_name"].ToString(),
                        Address = row["customer_address"].ToString(),
                        City = row["customer_city"].ToString(),
                        State = row["customer_state"].ToString(),
                        Zip = row["customer_zip"].ToString(),
                        Email = row["customer_email"].ToString(),
                        Phone = row["customer_phone"].ToString()
                    };
                }

                jobs.Add(job);
            }

            return jobs;
        }

        /// <summary>
        /// Gets a job by its ID
        /// </summary>
        public Job GetJobById(int jobId)
        {
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            
            string query = @"
                SELECT j.*, c.name as customer_name, c.address as customer_address,
                       c.city as customer_city, c.state as customer_state, c.zip as customer_zip,
                       c.email as customer_email, c.phone as customer_phone
                FROM Jobs j 
                LEFT JOIN Customers c ON j.customer_id = c.customer_id 
                WHERE j.job_id = @jobId";

            var dataTable = ExecuteQuery(query, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            var row = dataTable.Rows[0];
            var job = new Job
            {
                JobId = Convert.ToInt32(row["job_id"]),
                JobNumber = row["job_number"].ToString(),
                CustomerId = Convert.ToInt32(row["customer_id"]),
                JobName = row["job_name"].ToString(),
                Address = row["address"].ToString(),
                City = row["city"].ToString(),
                State = row["state"].ToString(),
                Zip = row["zip"].ToString(),
                SquareFootage = row["square_footage"] != DBNull.Value ? Convert.ToInt32(row["square_footage"]) : (int?)null,
                NumFloors = row["num_floors"] != DBNull.Value ? Convert.ToInt32(row["num_floors"]) : (int?)null,
                Status = row["status"].ToString(),
                CreateDate = Convert.ToDateTime(row["create_date"]),
                CompletionDate = row["completion_date"] != DBNull.Value ? Convert.ToDateTime(row["completion_date"]) : (DateTime?)null,
                TotalEstimate = row["total_estimate"] != DBNull.Value ? Convert.ToDecimal(row["total_estimate"]) : (decimal?)null,
                TotalActual = row["total_actual"] != DBNull.Value ? Convert.ToDecimal(row["total_actual"]) : (decimal?)null,
                Notes = row["notes"].ToString()
            };

            // Add customer information
            if (row["customer_name"] != DBNull.Value)
            {
                job.Customer = new Customer
                {
                    CustomerId = job.CustomerId,
                    Name = row["customer_name"].ToString(),
                    Address = row["customer_address"].ToString(),
                    City = row["customer_city"].ToString(),
                    State = row["customer_state"].ToString(),
                    Zip = row["customer_zip"].ToString(),
                    Email = row["customer_email"].ToString(),
                    Phone = row["customer_phone"].ToString()
                };
            }

            return job;
        }

        /// <summary>
        /// Saves a job (insert or update)
        /// </summary>
        public int SaveJob(Job job)
        {
            if (job.JobId == 0)
            {
                // Insert new job
                string query = @"
                    INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, 
                                    square_footage, num_floors, status, create_date, completion_date, 
                                    total_estimate, total_actual, notes)
                    VALUES (@jobNumber, @customerId, @jobName, @address, @city, @state, @zip,
                           @squareFootage, @numFloors, @status, @createDate, @completionDate,
                           @totalEstimate, @totalActual, @notes)";

                var parameters = new Dictionary<string, object>
                {
                    { "@jobNumber", job.JobNumber },
                    { "@customerId", job.CustomerId },
                    { "@jobName", job.JobName },
                    { "@address", job.Address },
                    { "@city", job.City },
                    { "@state", job.State },
                    { "@zip", job.Zip },
                    { "@squareFootage", job.SquareFootage },
                    { "@numFloors", job.NumFloors },
                    { "@status", job.Status },
                    { "@createDate", job.CreateDate },
                    { "@completionDate", job.CompletionDate },
                    { "@totalEstimate", job.TotalEstimate },
                    { "@totalActual", job.TotalActual },
                    { "@notes", job.Notes }
                };

                ExecuteNonQuery(query, parameters);
                job.JobId = (int)GetLastInsertedId();
                return job.JobId;
            }
            else
            {
                // Update existing job
                string query = @"
                    UPDATE Jobs SET 
                        job_number = @jobNumber, customer_id = @customerId, job_name = @jobName,
                        address = @address, city = @city, state = @state, zip = @zip,
                        square_footage = @squareFootage, num_floors = @numFloors, status = @status,
                        completion_date = @completionDate, total_estimate = @totalEstimate,
                        total_actual = @totalActual, notes = @notes
                    WHERE job_id = @jobId";

                var parameters = new Dictionary<string, object>
                {
                    { "@jobId", job.JobId },
                    { "@jobNumber", job.JobNumber },
                    { "@customerId", job.CustomerId },
                    { "@jobName", job.JobName },
                    { "@address", job.Address },
                    { "@city", job.City },
                    { "@state", job.State },
                    { "@zip", job.Zip },
                    { "@squareFootage", job.SquareFootage },
                    { "@numFloors", job.NumFloors },
                    { "@status", job.Status },
                    { "@completionDate", job.CompletionDate },
                    { "@totalEstimate", job.TotalEstimate },
                    { "@totalActual", job.TotalActual },
                    { "@notes", job.Notes }
                };

                ExecuteNonQuery(query, parameters);
                return job.JobId;
            }
        }

        #endregion

        #region Customer Operations

        /// <summary>
        /// Gets all customers
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            string query = "SELECT * FROM Customers ORDER BY name";
            var dataTable = ExecuteQuery(query);

            foreach (DataRow row in dataTable.Rows)
            {
                customers.Add(new Customer
                {
                    CustomerId = Convert.ToInt32(row["customer_id"]),
                    Name = row["name"].ToString(),
                    Address = row["address"].ToString(),
                    City = row["city"].ToString(),
                    State = row["state"].ToString(),
                    Zip = row["zip"].ToString(),
                    Email = row["email"].ToString(),
                    Phone = row["phone"].ToString(),
                    Notes = row["notes"].ToString()
                });
            }

            return customers;
        }

        #endregion

        #region Employee Operations

        /// <summary>
        /// Gets all active employees
        /// </summary>
        public List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();
            string query = "SELECT * FROM Employees WHERE status = 'Active' ORDER BY name";
            var dataTable = ExecuteQuery(query);

            foreach (DataRow row in dataTable.Rows)
            {
                employees.Add(new Employee
                {
                    EmployeeId = Convert.ToInt32(row["employee_id"]),
                    Name = row["name"].ToString(),
                    HourlyRate = Convert.ToDecimal(row["hourly_rate"]),
                    BurdenRate = row["burden_rate"] != DBNull.Value ? Convert.ToDecimal(row["burden_rate"]) : (decimal?)null,
                    Status = row["status"].ToString(),
                    Notes = row["notes"].ToString()
                });
            }

            return employees;
        }

        #endregion

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // Nothing to dispose in this implementation since we're using 'using' statements
                _disposed = true;
            }
        }
    }
}
