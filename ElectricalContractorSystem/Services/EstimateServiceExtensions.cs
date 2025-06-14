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
    }
}
