using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        // Customer-specific methods that aren't in the main file
        public Customer GetCustomerById(int customerId)
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
            
            return null;
        }
    }
}