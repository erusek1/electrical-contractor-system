using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem
{
    class CustomerDebugProgram
    {
        private static string connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=;";
        
        static void Main(string[] args)
        {
            Console.WriteLine("=== Customer Database Debug Tool ===\n");
            
            // Test 1: Basic connection
            Console.WriteLine("Test 1: Testing database connection...");
            if (TestConnection())
            {
                Console.WriteLine("✓ Database connection successful\n");
            }
            else
            {
                Console.WriteLine("✗ Database connection failed\n");
                Console.WriteLine("Please check:");
                Console.WriteLine("1. MySQL server is running");
                Console.WriteLine("2. Database 'electrical_contractor_db' exists");
                Console.WriteLine("3. Username/password are correct");
                Console.WriteLine("4. Update the connectionString variable above with your MySQL password");
                Console.ReadKey();
                return;
            }
            
            // Test 2: Check if customers table exists
            Console.WriteLine("Test 2: Checking if customers table exists...");
            if (TableExists("customers"))
            {
                Console.WriteLine("✓ Customers table exists\n");
            }
            else
            {
                Console.WriteLine("✗ Customers table does not exist\n");
                Console.ReadKey();
                return;
            }
            
            // Test 3: Count customers
            Console.WriteLine("Test 3: Counting customers in database...");
            int customerCount = GetCustomerCount();
            Console.WriteLine($"✓ Found {customerCount} customers in database\n");
            
            if (customerCount == 0)
            {
                Console.WriteLine("No customers found. This could mean:");
                Console.WriteLine("1. The table is empty");
                Console.WriteLine("2. There's an issue with the query");
                Console.ReadKey();
                return;
            }
            
            // Test 4: Try to retrieve customers using your existing method
            Console.WriteLine("Test 4: Testing GetAllCustomers() method...");
            var databaseService = new ElectricalContractorSystem.Services.DatabaseService(connectionString);
            try
            {
                var customers = databaseService.GetAllCustomers();
                Console.WriteLine($"✓ Retrieved {customers.Count} customers using DatabaseService\n");
                
                if (customers.Count > 0)
                {
                    Console.WriteLine("First 5 customers:");
                    for (int i = 0; i < Math.Min(5, customers.Count); i++)
                    {
                        var customer = customers[i];
                        Console.WriteLine($"  {customer.CustomerId}: {customer.Name} - {customer.Address}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in GetAllCustomers(): {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}\n");
            }
            
            // Test 5: Raw SQL query test
            Console.WriteLine("Test 5: Testing raw SQL query...");
            try
            {
                var customersRaw = GetCustomersRaw();
                Console.WriteLine($"✓ Retrieved {customersRaw.Count} customers using raw SQL\n");
                
                if (customersRaw.Count > 0)
                {
                    Console.WriteLine("Sample data from raw query:");
                    var sample = customersRaw[0];
                    Console.WriteLine($"  ID: {sample.CustomerId}");
                    Console.WriteLine($"  Name: '{sample.Name}'");
                    Console.WriteLine($"  Address: '{sample.Address}'");
                    Console.WriteLine($"  City: '{sample.City}'");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error in raw SQL query: {ex.Message}\n");
            }
            
            Console.WriteLine("Testing complete. Press any key to exit...");
            Console.ReadKey();
        }
        
        private static bool TestConnection()
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
                Console.WriteLine($"Connection error: {ex.Message}");
                return false;
            }
        }
        
        private static bool TableExists(string tableName)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = @tableName";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@tableName", tableName);
                        var count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Table check error: {ex.Message}");
                return false;
            }
        }
        
        private static int GetCustomerCount()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var query = "SELECT COUNT(*) FROM customers";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Count error: {ex.Message}");
                return 0;
            }
        }
        
        private static List<Customer> GetCustomersRaw()
        {
            var customers = new List<Customer>();
            
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT customer_id, name, address, city, state, zip, email, phone, notes FROM customers ORDER BY name LIMIT 10";
                
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var customer = new Customer
                            {
                                CustomerId = reader.GetInt32("customer_id"),
                                Name = reader.IsDBNull("name") ? "" : reader.GetString("name"),
                                Address = reader.IsDBNull("address") ? "" : reader.GetString("address"),
                                City = reader.IsDBNull("city") ? "" : reader.GetString("city"),
                                State = reader.IsDBNull("state") ? "" : reader.GetString("state"),
                                Zip = reader.IsDBNull("zip") ? "" : reader.GetString("zip"),
                                Email = reader.IsDBNull("email") ? "" : reader.GetString("email"),
                                Phone = reader.IsDBNull("phone") ? "" : reader.GetString("phone"),
                                Notes = reader.IsDBNull("notes") ? "" : reader.GetString("notes")
                            };
                            customers.Add(customer);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error reading customer row: {ex.Message}");
                        }
                    }
                }
            }
            
            return customers;
        }
    }
}
