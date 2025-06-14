using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Missing Estimate Methods
        
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
        
        #endregion
    }
}
