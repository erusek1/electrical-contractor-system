using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public class SimpleDatabaseTest
    {
        public static void TestCustomerRetrieval()
        {
            // Update this connection string with your MySQL password
            string connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=;Port=3306;";
            
            try
            {
                Console.WriteLine("Testing customer retrieval...");
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✓ Connected to database");
                    
                    string query = "SELECT customer_id, name, address FROM customers LIMIT 5";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("Customers found:");
                        int count = 0;
                        while (reader.Read())
                        {
                            count++;
                            int id = reader.GetInt32("customer_id");
                            string name = reader.IsDBNull("name") ? "NULL" : reader.GetString("name");
                            string address = reader.IsDBNull("address") ? "NULL" : reader.GetString("address");
                            
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
            }
        }
    }
}
