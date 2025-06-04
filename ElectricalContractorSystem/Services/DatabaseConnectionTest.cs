using System;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public static class DatabaseConnectionTest
    {
        public static bool TestConnection(string connectionString)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
                return false;
            }
        }

        public static bool TestConnection()
        {
            return TestConnection(DatabaseService.ConnectionString);
        }

        public static string GetConnectionStatus()
        {
            if (TestConnection())
            {
                return "Connected to database successfully";
            }
            else
            {
                return "Failed to connect to database";
            }
        }
    }
}