using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Simple database test class for verifying database connectivity and operations
    /// FIXED: String-to-int conversion errors and table naming issues
    /// </summary>
    public class SimpleDatabaseTest
    {
        /// <summary>
        /// Test customer retrieval from database - FIXED table naming and type conversions
        /// </summary>
        public static void TestCustomerRetrieval()
        {
            // Update this connection string with your MySQL password
            string connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn;Port=3306;";
            
            try
            {
                Console.WriteLine("Testing customer retrieval...");
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✓ Connected to database");
                    
                    // FIXED: Use capitalized table name to match database schema
                    string query = "SELECT customer_id, name, address FROM Customers LIMIT 5";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("Customers found:");
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            
                            // FIXED: Add proper type conversion error handling
                            int id;
                            string name;
                            string address;
                            
                            try
                            {
                                id = reader.GetInt32("customer_id");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  Error reading customer_id: {ex.Message}");
                                continue;
                            }
                            
                            try
                            {
                                name = reader.IsDBNull("name") ? "NULL" : reader.GetString("name");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  Error reading name for customer {id}: {ex.Message}");
                                name = "ERROR";
                            }
                            
                            try
                            {
                                address = reader.IsDBNull("address") ? "NULL" : reader.GetString("address");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  Error reading address for customer {id}: {ex.Message}");
                                address = "ERROR";
                            }
                            
                            Console.WriteLine($"  {id}: {name} - {address}");
                        }
                        Console.WriteLine($"✓ Found {count} customers");
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"✗ MySQL Error: {ex.Message}");
                Console.WriteLine($"  Error Code: {ex.Number}");
                
                switch (ex.Number)
                {
                    case 1045:
                        Console.WriteLine("  → Check your username/password");
                        break;
                    case 1049:
                        Console.WriteLine("  → Database 'electrical_contractor_db' does not exist");
                        break;
                    case 1146:
                        Console.WriteLine("  → Table 'Customers' does not exist - run database schema script");
                        break;
                    case 2003:
                        Console.WriteLine("  → MySQL server is not running or wrong port");
                        break;
                    default:
                        Console.WriteLine($"  → Unknown MySQL error");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ General Error: {ex.Message}");
                Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Test full database connectivity with all main tables
        /// </summary>
        public static void TestFullDatabaseConnectivity()
        {
            string connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn;Port=3306;";
            
            try
            {
                Console.WriteLine("=== FULL DATABASE CONNECTIVITY TEST ===");
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✓ Connected to database");
                    
                    // Test each main table
                    TestTable(connection, "Customers", "customer_id", "name");
                    TestTable(connection, "Jobs", "job_id", "job_number");
                    TestTable(connection, "Employees", "employee_id", "name");
                    TestTable(connection, "Vendors", "vendor_id", "name");
                    
                    Console.WriteLine("✓ Database connectivity test completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Database connectivity test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test a specific table for existence and basic data retrieval
        /// </summary>
        private static void TestTable(MySqlConnection connection, string tableName, string idColumn, string nameColumn)
        {
            try
            {
                string query = $"SELECT COUNT(*) FROM {tableName}";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    Console.WriteLine($"  ✓ Table '{tableName}': {count} records");
                    
                    // Get a sample record if any exist
                    if (count > 0)
                    {
                        string sampleQuery = $"SELECT {idColumn}, {nameColumn} FROM {tableName} LIMIT 1";
                        using (var sampleCmd = new MySqlCommand(sampleQuery, connection))
                        using (var reader = sampleCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var id = reader.GetInt32(idColumn);
                                var name = reader.IsDBNull(nameColumn) ? "NULL" : reader.GetString(nameColumn);
                                Console.WriteLine($"    Sample: {id} - {name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Table '{tableName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Test using the DatabaseService class
        /// </summary>
        public static void TestDatabaseService()
        {
            try
            {
                Console.WriteLine("=== DATABASE SERVICE TEST ===");
                
                var dbService = new DatabaseService();
                
                // Test connection
                bool connected = dbService.TestConnection();
                Console.WriteLine($"DatabaseService connection: {(connected ? "SUCCESS" : "FAILED")}");
                
                if (connected)
                {
                    // Test customer retrieval
                    var customers = dbService.GetAllCustomers();
                    Console.WriteLine($"✓ Retrieved {customers.Count} customers via DatabaseService");
                    
                    // Test employee retrieval
                    var employees = dbService.GetAllEmployees();
                    Console.WriteLine($"✓ Retrieved {employees.Count} employees via DatabaseService");
                    
                    // Test vendor retrieval
                    var vendors = dbService.GetAllVendors();
                    Console.WriteLine($"✓ Retrieved {vendors.Count} vendors via DatabaseService");
                    
                    // Test job retrieval
                    var jobs = dbService.GetAllJobs();
                    Console.WriteLine($"✓ Retrieved {jobs.Count} jobs via DatabaseService");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ DatabaseService test failed: {ex.Message}");
            }
        }
    }
}