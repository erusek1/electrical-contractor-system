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

        public List<Job> GetJobsByStatus(string status)
        {
            var jobs = new List<Job>();
            var parameters = new Dictionary<string, object> { { "@status", status } };
            string query = @"SELECT j.*, c.name as customer_name FROM Jobs j LEFT JOIN Customers c ON j.customer_id = c.customer_id WHERE j.status = @status ORDER BY j.job_number DESC";
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
                TotalEstimate = row["total_estimate"] != DBNull.Value ? Convert.ToDecimal(row["total_estimate"]) : (decimal?)null,
                TotalActual = row["total_actual"] != DBNull.Value ? Convert.ToDecimal(row["total_actual"]) : (decimal?)null,
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
            return job.JobId == 0 ? AddJob(job) : (UpdateJob(job), job.JobId).Item2;
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

        #region JobStage Operations
        public List<JobStage> GetJobStages(int jobId)
        {
            var stages = new List<JobStage>();
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = "SELECT * FROM JobStages WHERE job_id = @jobId ORDER BY stage_name";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                stages.Add(CreateJobStageFromRow(row));
            }
            return stages;
        }

        public JobStage GetJobStage(int jobId, string stageName)
        {
            var parameters = new Dictionary<string, object> { { "@jobId", jobId }, { "@stageName", stageName } };
            string query = "SELECT * FROM JobStages WHERE job_id = @jobId AND stage_name = @stageName";
            var dataTable = ExecuteQuery(query, parameters);
            return dataTable.Rows.Count == 0 ? null : CreateJobStageFromRow(dataTable.Rows[0]);
        }

        private JobStage CreateJobStageFromRow(DataRow row)
        {
            return new JobStage
            {
                StageId = Convert.ToInt32(row["stage_id"]),
                JobId = Convert.ToInt32(row["job_id"]),
                StageName = row["stage_name"].ToString(),
                EstimatedHours = row["estimated_hours"] != DBNull.Value ? Convert.ToDecimal(row["estimated_hours"]) : (decimal?)null,
                EstimatedMaterialCost = row["estimated_material_cost"] != DBNull.Value ? Convert.ToDecimal(row["estimated_material_cost"]) : (decimal?)null,
                ActualHours = row["actual_hours"] != DBNull.Value ? Convert.ToDecimal(row["actual_hours"]) : (decimal?)null,
                ActualMaterialCost = row["actual_material_cost"] != DBNull.Value ? Convert.ToDecimal(row["actual_material_cost"]) : (decimal?)null,
                Notes = row["notes"]?.ToString()
            };
        }

        public int AddJobStage(JobStage stage)
        {
            string query = @"INSERT INTO JobStages (job_id, stage_name, estimated_hours, estimated_material_cost, actual_hours, actual_material_cost, notes) VALUES (@jobId, @stageName, @estimatedHours, @estimatedMaterialCost, @actualHours, @actualMaterialCost, @notes)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", stage.JobId }, { "@stageName", stage.StageName },
                { "@estimatedHours", stage.EstimatedHours }, { "@estimatedMaterialCost", stage.EstimatedMaterialCost },
                { "@actualHours", stage.ActualHours }, { "@actualMaterialCost", stage.ActualMaterialCost },
                { "@notes", stage.Notes }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }

        public void UpdateJobStage(JobStage stage)
        {
            string query = @"UPDATE JobStages SET estimated_hours = @estimatedHours, estimated_material_cost = @estimatedMaterialCost, actual_hours = @actualHours, actual_material_cost = @actualMaterialCost, notes = @notes WHERE stage_id = @stageId";
            var parameters = new Dictionary<string, object>
            {
                { "@stageId", stage.StageId }, { "@estimatedHours", stage.EstimatedHours },
                { "@estimatedMaterialCost", stage.EstimatedMaterialCost }, { "@actualHours", stage.ActualHours },
                { "@actualMaterialCost", stage.ActualMaterialCost }, { "@notes", stage.Notes }
            };
            ExecuteNonQuery(query, parameters);
        }

        public void DeleteJobStage(int stageId)
        {
            var parameters = new Dictionary<string, object> { { "@stageId", stageId } };
            ExecuteNonQuery("DELETE FROM JobStages WHERE stage_id = @stageId", parameters);
        }
        #endregion

        #region Labor Entry Operations
        public List<LaborEntry> GetLaborEntriesByJob(int jobId)
        {
            var entries = new List<LaborEntry>();
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = @"SELECT le.*, e.name as employee_name, js.stage_name FROM LaborEntries le JOIN Employees e ON le.employee_id = e.employee_id JOIN JobStages js ON le.stage_id = js.stage_id WHERE le.job_id = @jobId ORDER BY le.date";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                entries.Add(CreateLaborEntryFromRow(row));
            }
            return entries;
        }

        public List<LaborEntry> GetLaborEntriesByDate(DateTime startDate, DateTime endDate)
        {
            var entries = new List<LaborEntry>();
            var parameters = new Dictionary<string, object> { { "@startDate", startDate }, { "@endDate", endDate } };
            string query = @"SELECT le.*, e.name as employee_name, j.job_number, js.stage_name FROM LaborEntries le JOIN Employees e ON le.employee_id = e.employee_id JOIN Jobs j ON le.job_id = j.job_id JOIN JobStages js ON le.stage_id = js.stage_id WHERE le.date BETWEEN @startDate AND @endDate ORDER BY le.date, e.name";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                entries.Add(CreateLaborEntryFromRow(row));
            }
            return entries;
        }

        private LaborEntry CreateLaborEntryFromRow(DataRow row)
        {
            return new LaborEntry
            {
                EntryId = Convert.ToInt32(row["entry_id"]),
                JobId = Convert.ToInt32(row["job_id"]),
                EmployeeId = Convert.ToInt32(row["employee_id"]),
                StageId = Convert.ToInt32(row["stage_id"]),
                Date = Convert.ToDateTime(row["date"]),
                Hours = Convert.ToDecimal(row["hours"]),
                Notes = row["notes"]?.ToString()
            };
        }

        public int AddLaborEntry(LaborEntry entry)
        {
            string query = @"INSERT INTO LaborEntries (job_id, employee_id, stage_id, date, hours, notes) VALUES (@jobId, @employeeId, @stageId, @date, @hours, @notes)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", entry.JobId }, { "@employeeId", entry.EmployeeId }, { "@stageId", entry.StageId },
                { "@date", entry.Date }, { "@hours", entry.Hours }, { "@notes", entry.Notes }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }

        public void DeleteLaborEntriesByDate(DateTime startDate, DateTime endDate, int employeeId)
        {
            var parameters = new Dictionary<string, object> { { "@startDate", startDate }, { "@endDate", endDate }, { "@employeeId", employeeId } };
            ExecuteNonQuery("DELETE FROM LaborEntries WHERE date BETWEEN @startDate AND @endDate AND employee_id = @employeeId", parameters);
        }
        #endregion

        #region Material Entry Operations
        public List<MaterialEntry> GetRecentMaterialEntries(int limit = 50)
        {
            var entries = new List<MaterialEntry>();
            var parameters = new Dictionary<string, object> { { "@limit", limit } };
            string query = @"SELECT me.*, j.job_number, js.stage_name, v.name as vendor_name FROM MaterialEntries me JOIN Jobs j ON me.job_id = j.job_id JOIN JobStages js ON me.stage_id = js.stage_id JOIN Vendors v ON me.vendor_id = v.vendor_id ORDER BY me.date DESC LIMIT @limit";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                entries.Add(CreateMaterialEntryFromRow(row));
            }
            return entries;
        }

        public List<MaterialEntry> GetMaterialEntriesByJob(int jobId)
        {
            var entries = new List<MaterialEntry>();
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = @"SELECT me.*, v.name as vendor_name, js.stage_name FROM MaterialEntries me JOIN Vendors v ON me.vendor_id = v.vendor_id JOIN JobStages js ON me.stage_id = js.stage_id WHERE me.job_id = @jobId ORDER BY me.date";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                entries.Add(CreateMaterialEntryFromRow(row));
            }
            return entries;
        }

        private MaterialEntry CreateMaterialEntryFromRow(DataRow row)
        {
            return new MaterialEntry
            {
                EntryId = Convert.ToInt32(row["entry_id"]),
                JobId = Convert.ToInt32(row["job_id"]),
                StageId = Convert.ToInt32(row["stage_id"]),
                VendorId = Convert.ToInt32(row["vendor_id"]),
                Date = Convert.ToDateTime(row["date"]),
                Cost = Convert.ToDecimal(row["cost"]),
                InvoiceNumber = row["invoice_number"]?.ToString(),
                InvoiceTotal = row["invoice_total"] != DBNull.Value ? Convert.ToDecimal(row["invoice_total"]) : (decimal?)null,
                Notes = row["notes"]?.ToString()
            };
        }

        public int AddMaterialEntry(MaterialEntry entry)
        {
            string query = @"INSERT INTO MaterialEntries (job_id, stage_id, vendor_id, date, cost, invoice_number, invoice_total, notes) VALUES (@jobId, @stageId, @vendorId, @date, @cost, @invoiceNumber, @invoiceTotal, @notes)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", entry.JobId }, { "@stageId", entry.StageId }, { "@vendorId", entry.VendorId },
                { "@date", entry.Date }, { "@cost", entry.Cost }, { "@invoiceNumber", entry.InvoiceNumber },
                { "@invoiceTotal", entry.InvoiceTotal }, { "@notes", entry.Notes }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }
        #endregion

        #region Room Specification Operations
        public List<RoomSpecification> GetRoomSpecifications(int jobId)
        {
            var specs = new List<RoomSpecification>();
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = "SELECT * FROM RoomSpecifications WHERE job_id = @jobId ORDER BY room_name";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                specs.Add(new RoomSpecification
                {
                    SpecId = Convert.ToInt32(row["spec_id"]),
                    JobId = Convert.ToInt32(row["job_id"]),
                    RoomName = row["room_name"].ToString(),
                    ItemDescription = row["item_description"].ToString(),
                    Quantity = Convert.ToInt32(row["quantity"]),
                    ItemCode = row["item_code"]?.ToString(),
                    UnitPrice = Convert.ToDecimal(row["unit_price"]),
                    TotalPrice = Convert.ToDecimal(row["total_price"])
                });
            }
            return specs;
        }

        public int AddRoomSpecification(RoomSpecification spec)
        {
            string query = @"INSERT INTO RoomSpecifications (job_id, room_name, item_description, quantity, item_code, unit_price, total_price) VALUES (@jobId, @roomName, @itemDescription, @quantity, @itemCode, @unitPrice, @totalPrice)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", spec.JobId }, { "@roomName", spec.RoomName }, { "@itemDescription", spec.ItemDescription },
                { "@quantity", spec.Quantity }, { "@itemCode", spec.ItemCode }, { "@unitPrice", spec.UnitPrice },
                { "@totalPrice", spec.TotalPrice }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }

        public void UpdateRoomSpecification(RoomSpecification spec)
        {
            string query = @"UPDATE RoomSpecifications SET room_name = @roomName, item_description = @itemDescription, quantity = @quantity, item_code = @itemCode, unit_price = @unitPrice, total_price = @totalPrice WHERE spec_id = @specId";
            var parameters = new Dictionary<string, object>
            {
                { "@roomName", spec.RoomName }, { "@itemDescription", spec.ItemDescription }, { "@quantity", spec.Quantity },
                { "@itemCode", spec.ItemCode }, { "@unitPrice", spec.UnitPrice }, { "@totalPrice", spec.TotalPrice },
                { "@specId", spec.SpecId }
            };
            ExecuteNonQuery(query, parameters);
        }

        public void DeleteRoomSpecification(int specId)
        {
            var parameters = new Dictionary<string, object> { { "@specId", specId } };
            ExecuteNonQuery("DELETE FROM RoomSpecifications WHERE spec_id = @specId", parameters);
        }
        #endregion

        #region Permit Item Operations
        public List<PermitItem> GetPermitItems(int jobId)
        {
            var items = new List<PermitItem>();
            var parameters = new Dictionary<string, object> { { "@jobId", jobId } };
            string query = "SELECT * FROM PermitItems WHERE job_id = @jobId ORDER BY category";
            var dataTable = ExecuteQuery(query, parameters);
            foreach (DataRow row in dataTable.Rows)
            {
                items.Add(new PermitItem
                {
                    PermitId = Convert.ToInt32(row["permit_id"]),
                    JobId = Convert.ToInt32(row["job_id"]),
                    Category = row["category"].ToString(),
                    Quantity = Convert.ToInt32(row["quantity"]),
                    Description = row["description"]?.ToString(),
                    Notes = row["notes"]?.ToString()
                });
            }
            return items;
        }

        public int AddPermitItem(PermitItem item)
        {
            string query = @"INSERT INTO PermitItems (job_id, category, quantity, description, notes) VALUES (@jobId, @category, @quantity, @description, @notes)";
            var parameters = new Dictionary<string, object>
            {
                { "@jobId", item.JobId }, { "@category", item.Category }, { "@quantity", item.Quantity },
                { "@description", item.Description }, { "@notes", item.Notes }
            };
            ExecuteNonQuery(query, parameters);
            return (int)GetLastInsertedId();
        }

        public void UpdatePermitItem(PermitItem item)
        {
            string query = @"UPDATE PermitItems SET category = @category, quantity = @quantity, description = @description, notes = @notes WHERE permit_id = @permitId";
            var parameters = new Dictionary<string, object>
            {
                { "@category", item.Category }, { "@quantity", item.Quantity }, { "@description", item.Description },
                { "@notes", item.Notes }, { "@permitId", item.PermitId }
            };
            ExecuteNonQuery(query, parameters);
        }

        public void DeletePermitItem(int permitId)
        {
            var parameters = new Dictionary<string, object> { { "@permitId", permitId } };
            ExecuteNonQuery("DELETE FROM PermitItems WHERE permit_id = @permitId", parameters);
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