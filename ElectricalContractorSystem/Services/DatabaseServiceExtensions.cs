using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    // Extension methods for DatabaseService
    public partial class DatabaseService
    {
        public string ConnectionString => _connectionString;
        
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
        
        public MySqlDataReader ExecuteReader(string query)
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var cmd = new MySqlCommand(query, connection);
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
        
        public MySqlDataReader ExecuteReader(string query, params MySqlParameter[] parameters)
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var cmd = new MySqlCommand(query, connection);
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }
        
        public string GetLastJobNumber()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT job_number FROM Jobs ORDER BY job_id DESC LIMIT 1";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "400"; // Start from 401 if no jobs exist
                }
            }
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
                        var deleteLineItemsQuery = @"
                            DELETE eli FROM EstimateLineItems eli
                            INNER JOIN EstimateRooms er ON eli.room_id = er.room_id
                            WHERE er.estimate_id = @estimateId";
                        
                        using (var cmd = new MySqlCommand(deleteLineItemsQuery, connection, transaction))
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
    }
}
