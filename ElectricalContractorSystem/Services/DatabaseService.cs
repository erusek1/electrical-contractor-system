using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Service for handling database operations
    /// </summary>
    public class DatabaseService : IDisposable
    {
        private readonly string _connectionString;
        private MySqlConnection _connection;
        private bool _disposed = false;

        public DatabaseService()
        {
            // Get connection string from App.config
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            
            // If connection string not found in config, use default
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=your_password;";
            }

            // Initialize connection
            _connection = new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        public bool TestConnection()
        {
            try
            {
                _connection.Open();
                _connection.Close();
                return true;
            }
            catch
            {
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
                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    _connection.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error or handle appropriately
                throw new Exception($"Database query error: {ex.Message}", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
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
                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    _connection.Open();
                    result = command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Log error or handle appropriately
                throw new Exception($"Database command error: {ex.Message}", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
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
                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    // Add parameters if any
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }

                    _connection.Open();
                    result = command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                // Log error or handle appropriately
                throw new Exception($"Database scalar query error: {ex.Message}", ex);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
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
                if (disposing)
                {
                    _connection?.Dispose();
                }

                _connection = null;
                _disposed = true;
            }
        }
    }
}
