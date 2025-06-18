using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        /// <summary>
        /// Get all customers from the database
        /// </summary>
        public List<Customer> GetAllCustomers()
        {
            var customers = new List<Customer>();
            
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT customer_id, name, address, city, state, zip, email, phone, notes
                        FROM Customers 
                        ORDER BY name";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            customers.Add(new Customer
                            {
                                CustomerId = reader.GetInt32("customer_id"),
                                Name = reader.GetString("name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting customers: {ex.Message}");
                // Return empty list on error
            }
            
            return customers;
        }
        
        /// <summary>
        /// Get customer by ID
        /// </summary>
        public Customer GetCustomerById(int customerId)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT * FROM Customers WHERE customer_id = @customerId";
                    
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@customerId", customerId);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting customer {customerId}: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Save customer (add new or update existing)
        /// </summary>
        public void SaveCustomer(Customer customer)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    if (customer.CustomerId == 0)
                    {
                        // Add new customer
                        var query = @"
                            INSERT INTO Customers (name, address, city, state, zip, email, phone, notes)
                            VALUES (@name, @address, @city, @state, @zip, @email, @phone, @notes);
                            SELECT LAST_INSERT_ID();";
                        
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            AddCustomerParameters(cmd, customer);
                            customer.CustomerId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    else
                    {
                        // Update existing customer
                        var query = @"
                            UPDATE Customers SET
                                name = @name,
                                address = @address,
                                city = @city,
                                state = @state,
                                zip = @zip,
                                email = @email,
                                phone = @phone,
                                notes = @notes
                            WHERE customer_id = @customerId";
                        
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            AddCustomerParameters(cmd, customer);
                            cmd.Parameters.AddWithValue("@customerId", customer.CustomerId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }