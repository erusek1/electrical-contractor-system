using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    public partial class DatabaseService
    {
        #region Estimate Methods - Updated

        // Override the methods that have EstimateStatus conversion issues
        public new List<Estimate> GetAllEstimates()
        {
            var estimates = new List<Estimate>();
            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    SELECT e.*, c.name as customer_name, c.address, c.city, c.state, c.zip, c.email, c.phone
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
                    ORDER BY e.created_date DESC";
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var estimate = new Estimate
                            {
                                EstimateId = reader.GetInt32("estimate_id"),
                                EstimateNumber = reader.GetString("estimate_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                JobAddress = reader.IsDBNull(reader.GetOrdinal("job_address")) ? null : reader.GetString("job_address"),
                                JobCity = reader.IsDBNull(reader.GetOrdinal("job_city")) ? null : reader.GetString("job_city"),
                                JobState = reader.IsDBNull(reader.GetOrdinal("job_state")) ? null : reader.GetString("job_state"),
                                JobZip = reader.IsDBNull(reader.GetOrdinal("job_zip")) ? null : reader.GetString("job_zip"),
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                                Status = ParseEstimateStatus(reader.GetString("status")),
                                CreatedDate = reader.GetDateTime("created_date"),
                                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (DateTime?)null : reader.GetDateTime("expiration_date"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                                TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                                TotalPrice = reader.GetDecimal("total_price"),
                                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? (int?)null : reader.GetInt32("job_id"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
                                }
                            };
                            estimates.Add(estimate);
                        }
                    }
                }
            }
            
            return estimates;
        }

        public new Estimate GetEstimateById(int estimateId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get estimate details
                var query = @"
                    SELECT e.*, c.name as customer_name, c.address, c.city, c.state, c.zip, c.email, c.phone
                    FROM Estimates e
                    INNER JOIN Customers c ON e.customer_id = c.customer_id
                    WHERE e.estimate_id = @estimateId";
                
                Estimate estimate = null;
                
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@estimateId", estimateId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estimate = new Estimate
                            {
                                EstimateId = reader.GetInt32("estimate_id"),
                                EstimateNumber = reader.GetString("estimate_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                JobName = reader.GetString("job_name"),
                                JobAddress = reader.IsDBNull(reader.GetOrdinal("job_address")) ? null : reader.GetString("job_address"),
                                JobCity = reader.IsDBNull(reader.GetOrdinal("job_city")) ? null : reader.GetString("job_city"),
                                JobState = reader.IsDBNull(reader.GetOrdinal("job_state")) ? null : reader.GetString("job_state"),
                                JobZip = reader.IsDBNull(reader.GetOrdinal("job_zip")) ? null : reader.GetString("job_zip"),
                                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                                Status = ParseEstimateStatus(reader.GetString("status")),
                                CreatedDate = reader.GetDateTime("created_date"),
                                ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? (DateTime?)null : reader.GetDateTime("expiration_date"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                TaxRate = reader.GetDecimal("tax_rate"),
                                MaterialMarkup = reader.GetDecimal("material_markup"),
                                LaborRate = reader.GetDecimal("labor_rate"),
                                TotalMaterialCost = reader.GetDecimal("total_material_cost"),
                                TotalLaborMinutes = reader.GetInt32("total_labor_minutes"),
                                TotalPrice = reader.GetDecimal("total_price"),
                                JobId = reader.IsDBNull(reader.GetOrdinal("job_id")) ? (int?)null : reader.GetInt32("job_id"),
                                Customer = new Customer
                                {
                                    CustomerId = reader.GetInt32("customer_id"),
                                    Name = reader.GetString("customer_name"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                    City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                    State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                    Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone")
                                }
                            };
                        }
                    }
                }
                
                if (estimate != null)
                {
                    // Load rooms
                    LoadEstimateRooms(estimate, connection);
                }
                
                return estimate;
            }
        }

        private new void InsertEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                INSERT INTO Estimates (
                    estimate_number, customer_id, job_name, job_address, job_city, 
                    job_state, job_zip, square_footage, num_floors, status, 
                    created_date, expiration_date, notes, tax_rate, material_markup, 
                    labor_rate, total_material_cost, total_labor_minutes, total_price
                ) VALUES (
                    @estimate_number, @customer_id, @job_name, @job_address, @job_city,
                    @job_state, @job_zip, @square_footage, @num_floors, @status,
                    @created_date, @expiration_date, @notes, @tax_rate, @material_markup,
                    @labor_rate, @total_material_cost, @total_labor_minutes, @total_price
                )";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_number", estimate.EstimateNumber);
                cmd.Parameters.AddWithValue("@customer_id", estimate.CustomerId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@job_address", estimate.JobAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_city", estimate.JobCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_state", estimate.JobState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_zip", estimate.JobZip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status.ToString());
                cmd.Parameters.AddWithValue("@created_date", estimate.CreatedDate);
                cmd.Parameters.AddWithValue("@expiration_date", estimate.ExpirationDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@tax_rate", estimate.TaxRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost);
                cmd.Parameters.AddWithValue("@total_labor_minutes", estimate.TotalLaborMinutes);
                cmd.Parameters.AddWithValue("@total_price", estimate.TotalPrice);
                
                cmd.ExecuteNonQuery();
                estimate.EstimateId = (int)cmd.LastInsertedId;
            }

            // Save rooms
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
            }
        }

        private new void UpdateEstimate(Estimate estimate, MySqlConnection connection, MySqlTransaction transaction)
        {
            var query = @"
                UPDATE Estimates SET
                    job_name = @job_name,
                    job_address = @job_address,
                    job_city = @job_city,
                    job_state = @job_state,
                    job_zip = @job_zip,
                    square_footage = @square_footage,
                    num_floors = @num_floors,
                    status = @status,
                    expiration_date = @expiration_date,
                    notes = @notes,
                    tax_rate = @tax_rate,
                    material_markup = @material_markup,
                    labor_rate = @labor_rate,
                    total_material_cost = @total_material_cost,
                    total_labor_minutes = @total_labor_minutes,
                    total_price = @total_price
                WHERE estimate_id = @estimate_id";

            using (var cmd = new MySqlCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.Parameters.AddWithValue("@job_name", estimate.JobName);
                cmd.Parameters.AddWithValue("@job_address", estimate.JobAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_city", estimate.JobCity ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_state", estimate.JobState ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@job_zip", estimate.JobZip ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@square_footage", estimate.SquareFootage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@num_floors", estimate.NumFloors ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", estimate.Status.ToString());
                cmd.Parameters.AddWithValue("@expiration_date", estimate.ExpirationDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", estimate.Notes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@tax_rate", estimate.TaxRate);
                cmd.Parameters.AddWithValue("@material_markup", estimate.MaterialMarkup);
                cmd.Parameters.AddWithValue("@labor_rate", estimate.LaborRate);
                cmd.Parameters.AddWithValue("@total_material_cost", estimate.TotalMaterialCost);
                cmd.Parameters.AddWithValue("@total_labor_minutes", estimate.TotalLaborMinutes);
                cmd.Parameters.AddWithValue("@total_price", estimate.TotalPrice);
                
                cmd.ExecuteNonQuery();
            }

            // Delete existing rooms (they'll be re-inserted)
            using (var cmd = new MySqlCommand("DELETE FROM EstimateRooms WHERE estimate_id = @estimate_id", connection, transaction))
            {
                cmd.Parameters.AddWithValue("@estimate_id", estimate.EstimateId);
                cmd.ExecuteNonQuery();
            }

            // Save rooms
            foreach (var room in estimate.Rooms)
            {
                SaveRoom(room, estimate.EstimateId, connection, transaction);
            }
        }

        private EstimateStatus ParseEstimateStatus(string status)
        {
            if (Enum.TryParse<EstimateStatus>(status, true, out var result))
            {
                return result;
            }
            return EstimateStatus.Draft; // Default
        }

        #endregion
    }
}
