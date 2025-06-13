using System;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    // This partial class contains fixes for the DatabaseService estimate methods
    public partial class DatabaseService
    {
        /// <summary>
        /// Fixed version of reading estimate from database with proper status conversion
        /// </summary>
        private Estimate ReadEstimateFromReader(MySqlDataReader reader)
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
                Status = ConvertToEstimateStatus(reader.GetString("status")), // Fixed conversion
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
            
            return estimate;
        }
    }
}
