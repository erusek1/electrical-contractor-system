using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// Service for managing properties and property-related operations
    /// </summary>
    public class PropertyService
    {
        private readonly DatabaseService _databaseService;

        public PropertyService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Get all properties for a customer
        /// </summary>
        public List<Property> GetCustomerProperties(int customerId)
        {
            var properties = new List<Property>();
            string query = @"
                SELECT p.*, c.name as customer_name
                FROM Properties p
                INNER JOIN Customers c ON p.customer_id = c.customer_id
                WHERE p.customer_id = @customerId
                ORDER BY p.address";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            properties.Add(MapPropertyFromReader(reader));
                        }
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Get property by ID
        /// </summary>
        public Property GetProperty(int propertyId)
        {
            Property property = null;
            string query = @"
                SELECT p.*, c.name as customer_name
                FROM Properties p
                INNER JOIN Customers c ON p.customer_id = c.customer_id
                WHERE p.property_id = @propertyId";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@propertyId", propertyId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            property = MapPropertyFromReader(reader);
                        }
                    }
                }
            }

            return property;
        }

        /// <summary>
        /// Get all jobs at a property
        /// </summary>
        public List<Job> GetPropertyJobs(int propertyId)
        {
            var jobs = new List<Job>();
            string query = @"
                SELECT j.*, c.name as customer_name
                FROM Jobs j
                INNER JOIN Customers c ON j.customer_id = c.customer_id
                WHERE j.property_id = @propertyId
                ORDER BY j.create_date DESC";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@propertyId", propertyId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var job = new Job
                            {
                                JobId = reader.GetInt32("job_id"),
                                JobNumber = reader.GetString("job_number"),
                                CustomerId = reader.GetInt32("customer_id"),
                                PropertyId = reader.IsDBNull(reader.GetOrdinal("property_id")) ? (int?)null : reader.GetInt32("property_id"),
                                JobName = reader.GetString("job_name"),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                                Status = reader.GetString("status"),
                                CreateDate = reader.GetDateTime("create_date"),
                                CompletionDate = reader.IsDBNull(reader.GetOrdinal("completion_date")) ? (DateTime?)null : reader.GetDateTime("completion_date"),
                                TotalEstimate = reader.IsDBNull(reader.GetOrdinal("total_estimate")) ? (decimal?)null : reader.GetDecimal("total_estimate"),
                                TotalActual = reader.IsDBNull(reader.GetOrdinal("total_actual")) ? (decimal?)null : reader.GetDecimal("total_actual"),
                                Customer = new Customer { Name = reader.GetString("customer_name") }
                            };
                            jobs.Add(job);
                        }
                    }
                }
            }

            return jobs;
        }

        /// <summary>
        /// Create a new property
        /// </summary>
        public int CreateProperty(Property property)
        {
            string query = @"
                INSERT INTO Properties (customer_id, address, city, state, zip, property_type, 
                    square_footage, num_floors, year_built, electrical_panel_info, notes)
                VALUES (@customerId, @address, @city, @state, @zip, @propertyType,
                    @squareFootage, @numFloors, @yearBuilt, @electricalPanelInfo, @notes);
                SELECT LAST_INSERT_ID();";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@customerId", property.CustomerId);
                    command.Parameters.AddWithValue("@address", property.Address);
                    command.Parameters.AddWithValue("@city", property.City ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@state", property.State ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@zip", property.Zip ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@propertyType", property.PropertyType ?? "Residential");
                    command.Parameters.AddWithValue("@squareFootage", property.SquareFootage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@numFloors", property.NumFloors ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@yearBuilt", property.YearBuilt ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@electricalPanelInfo", property.ElectricalPanelInfo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@notes", property.Notes ?? (object)DBNull.Value);

                    property.PropertyId = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return property.PropertyId;
        }

        /// <summary>
        /// Update an existing property
        /// </summary>
        public void UpdateProperty(Property property)
        {
            string query = @"
                UPDATE Properties SET 
                    address = @address,
                    city = @city,
                    state = @state,
                    zip = @zip,
                    property_type = @propertyType,
                    square_footage = @squareFootage,
                    num_floors = @numFloors,
                    year_built = @yearBuilt,
                    electrical_panel_info = @electricalPanelInfo,
                    notes = @notes
                WHERE property_id = @propertyId";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@propertyId", property.PropertyId);
                    command.Parameters.AddWithValue("@address", property.Address);
                    command.Parameters.AddWithValue("@city", property.City ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@state", property.State ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@zip", property.Zip ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@propertyType", property.PropertyType ?? "Residential");
                    command.Parameters.AddWithValue("@squareFootage", property.SquareFootage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@numFloors", property.NumFloors ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@yearBuilt", property.YearBuilt ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@electricalPanelInfo", property.ElectricalPanelInfo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@notes", property.Notes ?? (object)DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Check if a property already exists for a customer at a specific address
        /// </summary>
        public Property FindExistingProperty(int customerId, string address, string city, string state)
        {
            Property property = null;
            string query = @"
                SELECT p.*, c.name as customer_name
                FROM Properties p
                INNER JOIN Customers c ON p.customer_id = c.customer_id
                WHERE p.customer_id = @customerId 
                    AND p.address = @address
                    AND IFNULL(p.city, '') = IFNULL(@city, '')
                    AND IFNULL(p.state, '') = IFNULL(@state, '')";

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@customerId", customerId);
                    command.Parameters.AddWithValue("@address", address);
                    command.Parameters.AddWithValue("@city", city ?? "");
                    command.Parameters.AddWithValue("@state", state ?? "");

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            property = MapPropertyFromReader(reader);
                        }
                    }
                }
            }

            return property;
        }

        /// <summary>
        /// Create a new job at an existing property
        /// </summary>
        public int CreateJobAtProperty(int propertyId, string jobNumber, string jobName, string status, DateTime createDate, decimal? totalEstimate, string notes)
        {
            int jobId = 0;

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using (var command = new MySqlCommand("CreateJobAtProperty", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@p_property_id", propertyId);
                    command.Parameters.AddWithValue("@p_job_number", jobNumber);
                    command.Parameters.AddWithValue("@p_job_name", jobName);
                    command.Parameters.AddWithValue("@p_status", status);
                    command.Parameters.AddWithValue("@p_create_date", createDate);
                    command.Parameters.AddWithValue("@p_total_estimate", totalEstimate ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@p_notes", notes ?? (object)DBNull.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            jobId = reader.GetInt32("job_id");
                        }
                    }
                }
            }

            return jobId;
        }

        /// <summary>
        /// Helper method to map property from data reader
        /// </summary>
        private Property MapPropertyFromReader(IDataReader reader)
        {
            return new Property
            {
                PropertyId = reader.GetInt32("property_id"),
                CustomerId = reader.GetInt32("customer_id"),
                Address = reader.GetString("address"),
                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString("city"),
                State = reader.IsDBNull(reader.GetOrdinal("state")) ? null : reader.GetString("state"),
                Zip = reader.IsDBNull(reader.GetOrdinal("zip")) ? null : reader.GetString("zip"),
                PropertyType = reader.GetString("property_type"),
                SquareFootage = reader.IsDBNull(reader.GetOrdinal("square_footage")) ? (int?)null : reader.GetInt32("square_footage"),
                NumFloors = reader.IsDBNull(reader.GetOrdinal("num_floors")) ? (int?)null : reader.GetInt32("num_floors"),
                YearBuilt = reader.IsDBNull(reader.GetOrdinal("year_built")) ? (int?)null : reader.GetInt32("year_built"),
                ElectricalPanelInfo = reader.IsDBNull(reader.GetOrdinal("electrical_panel_info")) ? null : reader.GetString("electrical_panel_info"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreatedDate = reader.GetDateTime("created_date"),
                Customer = new Customer { Name = reader.GetString("customer_name") }
            };
        }
    }
}
