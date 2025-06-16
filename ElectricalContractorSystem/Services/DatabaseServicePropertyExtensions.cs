using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Job Methods - Property Support

        /// <summary>
        /// Get a job by job number
        /// </summary>
        public Job GetJobByNumber(string jobNumber)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT j.*, c.name as customer_name
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
                            return new Job
                            {
                                JobId = reader.GetInt32("job_id"),
                                JobNumber = reader.GetString("job_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                PropertyId = reader.IsDBNull(reader.GetOrdinal("property_id")) ? (int?)null : reader.GetInt32("property_id"),
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
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name")
                                }
                            };
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Create a job stage
        /// </summary>
        public int CreateJobStage(JobStage stage)
        {
            return AddJobStage(stage);
        }

        #endregion
    }
}
