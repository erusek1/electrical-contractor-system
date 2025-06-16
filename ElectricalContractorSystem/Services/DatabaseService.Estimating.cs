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
        /// Options for converting an estimate to a job
        /// </summary>
        public class ConversionOptions
        {
            public bool IncludeRoomDetails { get; set; } = true;
            public bool CreateJobStages { get; set; } = true;
            public bool CopyPermitItems { get; set; } = true;
            public string InitialJobStatus { get; set; } = "In Progress";
        }

        #region Estimate Methods

        public List<EstimateRoom> GetEstimateRooms(int estimateId)
        {
            var rooms = new List<EstimateRoom>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var query = @"SELECT room_id, room_name, room_order, notes
                             FROM EstimateRooms
                             WHERE estimate_id = @estimateId
                             ORDER BY room_order";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new EstimateRoom
                            {
                                RoomId = reader.GetInt32("room_id"),
                                EstimateId = estimateId,
                                RoomName = reader.GetString("room_name"),
                                RoomOrder = reader.GetInt32("room_order"),
                                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                                Items = new List<EstimateLineItem>()
                            });
                        }
                    }
                }
            }
            return rooms;
        }

        public List<EstimateStageSummary> GetEstimateStageSummaries(int estimateId)
        {
            var summaries = new List<EstimateStageSummary>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var query = @"SELECT summary_id, stage, labor_hours, material_cost, labor_cost
                             FROM EstimateStageSummaries
                             WHERE estimate_id = @estimateId
                             ORDER BY FIELD(stage, 'Demo', 'Rough', 'Service', 'Finish', 'Extra', 'Temp Service', 'Inspection', 'Other')";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@estimateId", estimateId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            summaries.Add(new EstimateStageSummary
                            {
                                SummaryId = reader.GetInt32("summary_id"),
                                EstimateId = estimateId,
                                Stage = reader.GetString("stage"),
                                LaborHours = reader.GetDecimal("labor_hours"),
                                MaterialCost = reader.GetDecimal("material_cost"),
                                LaborCost = reader.GetDecimal("labor_cost")
                            });
                        }
                    }
                }
            }
            return summaries;
        }

        #endregion
    }
}
