using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Extension Methods for Legacy Compatibility

        public MySqlConnection GetConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public MySqlDataReader ExecuteReader(string query, Dictionary<string, object> parameters = null)
        {
            var connection = GetConnection();
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

        public string GetLastJobNumber()
        {
            return GetNextJobNumber();
        }

        public void DeleteEstimate(int estimateId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete line items first
                        var deleteItemsQuery = @"
                            DELETE FROM EstimateLineItems 
                            WHERE room_id IN (SELECT room_id FROM EstimateRooms WHERE estimate_id = @estimateId)";
                        using (var cmd = new MySqlCommand(deleteItemsQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete rooms
                        var deleteRoomsQuery = "DELETE FROM EstimateRooms WHERE estimate_id = @estimateId";
                        using (var cmd = new MySqlCommand(deleteRoomsQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete stage summaries
                        var deleteSummariesQuery = "DELETE FROM EstimateStageSummary WHERE estimate_id = @estimateId";
                        using (var cmd = new MySqlCommand(deleteSummariesQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }

                        // Delete permit items
                        var deletePermitItemsQuery = "DELETE FROM EstimatePermitItems WHERE estimate_id = @estimateId";
                        using (var cmd = new MySqlCommand(deletePermitItemsQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }

                        // Finally delete the estimate
                        var deleteEstimateQuery = "DELETE FROM Estimates WHERE estimate_id = @estimateId";
                        using (var cmd = new MySqlCommand(deleteEstimateQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@estimateId", estimateId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}
