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
            _connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "Server=localhost;Port=3306;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn!;";
            }
        }

        public static string ConnectionString
        {
            get
            {
                var connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = "Server=localhost;Port=3306;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn!;";
                }
                return connectionString;
            }
        }

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

        public DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            DataTable dataTable = new DataTable();
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
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

        public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            int result = 0;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
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

        public object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            object result = null;
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                using (var command = new MySqlCommand(query, connection))
                {
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

        public long GetLastInsertedId()
        {
            return Convert.ToInt64(ExecuteScalar("SELECT LAST_INSERT_ID()"));
        }

        #region Job Operations
        public List<Job> GetAllJobs()
        {
            var jobs = new List<Job>();
            string query = @"SELECT j.*, c.name as customer_name FROM Jobs j LEFT JOIN Customers c ON j.customer_id = c.customer_id ORDER BY j.job_number DESC";
            var dataTable = ExecuteQuery(query);
            foreach (DataRow row in dataTable.Rows)
            {
                jobs.Add(CreateJobFromRow(row));
            }
            return jobs;
        }

        public List<Job> GetJobsByStatus(params string[] statuses)
        {
            var jobs = new List<Job>();
            var statusParams = new List<string>();
            var parameters = new Dictionary<string, object>();
            
            for (int i = 0; i < statuses.Length; i++)
            {
                var paramName = $"@status{i}";
                statusParams.Add(paramName);
                parameters[paramName] = statuses[i];
            }
            
            string query = $@"SELECT j.*, c.name as customer_name FROM Jobs j LEFT JOIN Customers c ON j.customer_id = c.customer_id WHERE j.status IN ({string.Join(",", statusParams)}) ORDER BY j.job_number DESC";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                jobs.Add(CreateJobFromRow(row));
            }
            return jobs;
        }

        public Job GetJob(int jobId)
        {
            return GetJobById(jobId);
        }

        public Job GetJobByNumber(string jobNumber)
        {
            var parameters = new Dictionary<string, object> { { "@jobNumber", jobNumber } };
            string query = @"SELECT j.*, c.name as customer_name FROM Jobs j LEFT JOIN Customers c ON j.customer_id = c.customer_id WHERE j.job_number = @jobNumber";
            var dataTable = ExecuteQuery(query, parameters);
            return dataTable.Rows.Count == 0 ? null : CreateJobFromRow(dataTable.Rows[0]);
        }

        public Job GetJobById(int jobId)
        {
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = @"SELECT j.*, c.name as customer_name FROM Jobs j LEFT JOIN Customers c ON j.customer_id = c.customer_id WHERE j.job_id = @jobId";
            var dataTable = ExecuteQuery(query, parameters);
            return dataTable.Rows.Count == 0 ? null : CreateJobFromRow(dataTable.Rows[0]);
        }

        /// <summary>
        /// Updates the status of a specific job
        /// </summary>
        public bool UpdateJobStatus(int jobId, string status)
        {
            try
            {
                string query = "UPDATE Jobs SET status = @status WHERE job_id = @jobId";
                var parameters = new Dictionary<string, object>
                {
                    { "@jobId", jobId },
                    { "@status", status }
                };
                
                int rowsAffected = ExecuteNonQuery(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating job status: {ex.Message}");
                return false;
            }
        }

        private Job CreateJobFromRow(DataRow row)
        {
            return new Job
            {
                JobId = Convert.ToInt32(row["job_id"]),
                JobNumber = row["job_number"].ToString(),
                CustomerId = Convert.ToInt32(row["customer_id"]),
                JobName = row["job_name"].ToString(),
                Address = row["address"]?.ToString(),
                City = row["city"]?.ToString(),
                State = row["state"]?.ToString(),
                Zip = row["zip"]?.ToString(),
                SquareFootage = row["square_footage"] != DBNull.Value ? Convert.ToInt32(row["square_footage"]) : (int?)null,
                NumFloors = row["num_floors"] != DBNull.Value ? Convert.ToInt32(row["num_floors"]) : (int?)null,
                Status = row["status"].ToString(),
                CreateDate = Convert.ToDateTime(row["create_date"]),
                CompletionDate = row["completion_date"] != DBNull.Value ? Convert.ToDateTime(row["completion_date"]) : (DateTime?)null,
                TotalEstimate = row["total_estimate"] != DBNull.Value ? Convert.ToDecimal(row["total_estimate"]) : 0m,
                TotalActual = row["total_actual"] != DBNull.Value ? Convert.ToDecimal(row["total_actual"]) : 0m,
                Notes = row["notes"]?.ToString()
            };
        }

        public int AddJob(Job job)
        {
            string query = @"INSERT INTO Jobs (job_number, customer_id, job_name, address, city, state, zip, square_footage, num_floors, status, create_date, completion_date, total_estimate, total_actual, notes) VALUES (@jobNumber, @customerId, @jobName, @address, @city, @state, @zip, @squareFootage, @numFloors, @status, @createDate, @completionDate, @totalEstimate, @totalActual, @notes)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobNumber", job.JobNumber }, { "@customerId", job.CustomerId }, { "@jobName", job.JobName },
                { "@address", job.Address }, { "@city", job.City }, { "@state", job.State }, { "@zip", job.Zip },
                { "@squareFootage", job.SquareFootage }, { "@numFloors", job.NumFloors }, { "@status", job.Status },
                { "@createDate", job.CreateDate }, { "@completionDate", job.CompletionDate },
                { "@totalEstimate", job.TotalEstimate }, { "@totalActual", job.TotalActual }, { "@notes", job.Notes }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }

        public void UpdateJob(Job job)
        {
            string query = @"UPDATE Jobs SET job_number = @jobNumber, customer_id = @customerId, job_name = @jobName, address = @address, city = @city, state = @state, zip = @zip, square_footage = @squareFootage, num_floors = @numFloors, status = @status, completion_date = @completionDate, total_estimate = @totalEstimate, total_actual = @totalActual, notes = @notes WHERE job_id = @jobId";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", job.JobId }, { "@jobNumber", job.JobNumber }, { "@customerId", job.CustomerId },
                { "@jobName", job.JobName }, { "@address", job.Address }, { "@city", job.City }, { "@state", job.State },
                { "@zip", job.Zip }, { "@squareFootage", job.SquareFootage }, { "@numFloors", job.NumFloors },
                { "@status", job.Status }, { "@completionDate", job.CompletionDate }, { "@totalEstimate", job.TotalEstimate },
                { "@totalActual", job.TotalActual }, { "@notes", job.Notes }
            };
            ExecuteNonQuery(query, parameters);
        }

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

        public string GetNextJobNumber()
        {
            var result = ExecuteScalar("SELECT COALESCE(MAX(CAST(job_number AS UNSIGNED)), 0) + 1 FROM Jobs");
            return result.ToString();
        }
        #endregion

        #region Customer Operations
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
                    Address = row["address"]?.ToString(),
                    City = row["city"]?.ToString(),
                    State = row["state"]?.ToString(),
                    Zip = row["zip"]?.ToString(),
                    Email = row["email"]?.ToString(),
                    Phone = row["phone"]?.ToString(),
                    Notes = row["notes"]?.ToString()
                });
            }
            return customers;
        }
        #endregion

        #region Employee Operations
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
                    Notes = row["notes"]?.ToString()
                });
            }
            return employees;
        }

        public List<Employee> GetActiveEmployees()
        {
            return GetAllEmployees();
        }
        #endregion

        #region Vendor Operations
        public List<Vendor> GetAllVendors()
        {
            var vendors = new List<Vendor>();
            string query = "SELECT * FROM Vendors ORDER BY name";
            var dataTable = ExecuteQuery(query);
            foreach (DataRow row in dataTable.Rows)
            {
                vendors.Add(new Vendor
                {
                    VendorId = Convert.ToInt32(row["vendor_id"]),
                    Name = row["name"].ToString(),
                    Address = row["address"]?.ToString(),
                    City = row["city"]?.ToString(),
                    State = row["state"]?.ToString(),
                    Zip = row["zip"]?.ToString(),
                    Phone = row["phone"]?.ToString(),
                    Email = row["email"]?.ToString(),
                    Notes = row["notes"]?.ToString()
                });
            }
            return vendors;
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}