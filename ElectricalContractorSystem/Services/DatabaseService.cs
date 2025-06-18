using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Services
{
    /// <summary>
    /// CONSOLIDATED DATABASE SERVICE - ALL DATABASE OPERATIONS IN ONE FILE
    /// Fixed table naming conventions to match actual database schema (CAPITALIZED table names)
    /// Removed all partial class conflicts that were preventing customer data from loading
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["ElectricalDB"]?.ConnectionString ?? 
                              "Server=localhost;Port=3306;Database=electrical_contractor_db;Uid=root;Pwd=215Osborn;SslMode=none;";
        }

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Connection and Utility Methods

        /// <summary>
        /// Test database connection
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch