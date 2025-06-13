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

        // Method is already defined in DatabaseService.cs, so removing from here
    }
}