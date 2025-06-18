using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem
{
    public class DatabaseConnectionTest
    {
        private string connectionString = "Server=localhost;Database=electrical_contractor_db;Uid=root;Pwd=your_password;";
        
        public void TestCustomerRetrieval()
        {
            Console.WriteLine("Testing database connection and customer retrieval...");
            
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✓ Database connection successful");
                    
                    // Test basic customer count
                    string countQuery = "SELECT COUNT(*) FROM customers";
                    using (var countCmd = new MySqlCommand(countQuery, connection))
                    {
                        int customerCount = Convert.ToInt32(countCmd.ExecuteScalar());
                        Console.WriteLine($"✓ Total customers in database: {customerCount}");
                    }
                    
                    // Test retrieving first 5 customers
                    string selectQuery = @"
                        SELECT customer_id, name, address, city, state, zip, email, phone 
                        FROM customers 
                        ORDER BY customer_id 
                        LIMIT 5";
                        
                    using (var cmd = new MySqlCommand(selectQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("\n✓ First 5 customers:");
                        Console.WriteLine("ID\tName\t\t\tAddress");
                        Console.WriteLine("--\t----\t\t\t-------");
                        
                        while (reader.Read())
                        {
                            int id = reader.GetInt32("customer_id");
                            string name = reader.IsDBNull("name") ? "" : reader.GetString("name");
                            string address = reader.IsDBNull("address") ? "" : reader.GetString("address");
                            
                            Console.WriteLine($"{id}\t{name.PadRight(20)}\t{address}");
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"✗ MySQL Error: {ex.Message}");
                Console.WriteLine($"  Error Number: {ex.Number}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ General Error: {ex.Message}");
            }
        }
        
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT customer_id, name, address, city, state, zip, email, phone, notes 
                        FROM customers 
                        ORDER BY name";
                        
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving customers: {ex.Message}");
            }
            
            return customers;
        }
    }
    
    // Simple Customer class for testing
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Notes { get; set; }
        
        public override string ToString()
        {
            return $"{CustomerId}: {Name} - {Address}, {City}, {State} {Zip}";
        }
    }
}
