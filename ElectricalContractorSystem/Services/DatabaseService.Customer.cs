using System;
using System.Collections.Generic;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        public int AddCustomer(Customer customer)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                var query = @"
                    INSERT INTO Customers (name, address, city, state, zip, email, phone, notes)
                    VALUES (@name, @address, @city, @state, @zip, @email, @phone, @notes)";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@name", customer.Name);
                    cmd.Parameters.AddWithValue("@address", customer.Address ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@city", customer.City ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@state", customer.State ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@zip", customer.Zip ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", customer.Email ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", customer.Notes ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                    customer.CustomerId = (int)cmd.LastInsertedId;
                    return customer.CustomerId;
                }
            }
        }
    }
}