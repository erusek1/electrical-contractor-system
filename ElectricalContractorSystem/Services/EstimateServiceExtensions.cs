using System;
using System.Collections.Generic;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Services
{
    public partial class EstimateService
    {
        // Add missing GetConnection method
        public MySql.Data.MySqlClient.MySqlConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        // Add GetEstimateStageSummaries method
        public List<EstimateStageSummary> GetEstimateStageSummaries(MySql.Data.MySqlClient.MySqlConnection connection, int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            var query = @"SELECT stage, labor_hours, material_cost
                         FROM EstimateStageSummary
                         WHERE estimate_id = @estimateId";

            using (var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@estimateId", estimateId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summaries.Add(new EstimateStageSummary
                        {
                            Stage = reader.GetString("stage"),
                            LaborHours = reader.GetDecimal("labor_hours"),
                            MaterialCost = reader.GetDecimal("material_cost")
                        });
                    }
                }
            }

            return summaries;
        }
    }
}
