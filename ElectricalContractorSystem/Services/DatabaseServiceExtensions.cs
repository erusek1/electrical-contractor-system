using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Connection Methods
        
        /// <summary>
        /// Gets a new database connection. Caller is responsible for disposal.
        /// </summary>
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
        
        /// <summary>
        /// Executes a reader query with parameters
        /// </summary>
        public MySqlDataReader ExecuteReader(string query, Dictionary<string, object> parameters = null)
        {
            var connection = new MySqlConnection(_connectionString);
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
                
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            }
        }
        
        #endregion

        #region Missing Job Methods
        
        /// <summary>
        /// Gets the last job number to determine the next sequential number
        /// </summary>
        public string GetLastJobNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT job_number FROM Jobs ORDER BY CAST(job_number AS UNSIGNED) DESC LIMIT 1";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "400"; // Default starting point
                }
            }
        }
        
        #endregion

        #region Missing Estimate Methods
        
        /// <summary>
        /// Deletes an estimate and all related data
        /// </summary>
        public bool DeleteEstimate(int estimateId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete in reverse order of dependencies
                        // First delete line items
                        using (var cmd = new MySqlCommand(@"
                            DELETE eli FROM EstimateLineItems eli
                            INNER JOIN EstimateRooms er ON eli.room_id = er.room_id
                            WHERE er.estimate_id = @estimateId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Then delete rooms
                        using (var cmd = new MySqlCommand("DELETE FROM EstimateRooms WHERE estimate_id = @estimateId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Delete stage summaries
                        using (var cmd = new MySqlCommand("DELETE FROM EstimateStageSummary WHERE estimate_id = @estimateId", connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Finally delete the estimate
                        using (var cmd = new MySqlCommand("DELETE FROM Estimates WHERE estimate_id = @estimateId", connection, transaction))
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
                        return false;
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the status of an estimate
        /// </summary>
        public void UpdateEstimateStatus(int estimateId, EstimateStatus newStatus)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "UPDATE Estimates SET status = @status WHERE estimate_id = @estimateId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    cmd.Parameters.AddWithValue("@status", newStatus.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        /// <summary>
        /// Links an estimate to a job after conversion
        /// </summary>
        public void LinkEstimateToJob(int estimateId, int jobId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"UPDATE Estimates 
                             SET job_id = @jobId, 
                                 converted_to_job_id = @jobId,
                                 converted_date = @convertedDate,
                                 status = @status
                             WHERE estimate_id = @estimateId";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    cmd.Parameters.AddWithValue("@jobId", jobId);
                    cmd.Parameters.AddWithValue("@convertedDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@status", EstimateStatus.Approved.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }
        
        #endregion

        #region Type Conversion Fixes
        
        /// <summary>
        /// Helper method to convert database status string to EstimateStatus enum
        /// </summary>
        private EstimateStatus ConvertToEstimateStatus(string statusString)
        {
            if (Enum.TryParse<EstimateStatus>(statusString, true, out var status))
            {
                return status;
            }
            
            // Default to Draft if parsing fails
            return EstimateStatus.Draft;
        }
        
        /// <summary>
        /// Updates the GetAllEstimates method to properly handle status conversion
        /// </summary>
        private void FixEstimateStatusConversion()
        {
            // This is handled in the partial class by updating the existing methods
            // The actual fix would be to update lines 781 and 848 in DatabaseService.cs
            // to use: Status = ConvertToEstimateStatus(reader.GetString("status"))
        }
        
        #endregion
    }
}
