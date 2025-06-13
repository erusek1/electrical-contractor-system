using System;
using System.Linq;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Services
{
    public static class ModelAndServiceFixes
    {
        // Fix for EstimateListViewModel decimal comparison
        public static decimal GetTotalValue(System.Collections.ObjectModel.ObservableCollection<Estimate> estimates)
        {
            return estimates.Sum(e => e.TotalCost);
        }
        
        // Fix for DataPopulationService - use Stage instead of JobStage
        public static void FixLaborEntry(LaborEntry entry, JobStage stage)
        {
            entry.Stage = stage;
            entry.StageId = stage.StageId;
        }
        
        // Fix for MaterialEntry - add missing JobStage property
        public static void FixMaterialEntry(MaterialEntry entry, JobStage stage)
        {
            entry.StageId = stage.StageId;
            entry.Stage = stage;
        }
    }
    
    // Extension for MaterialEntry model to add missing Stage property
    public partial class MaterialEntry
    {
        public JobStage Stage { get; set; }
    }
    
    // Extension for EstimateBuilderViewModel fix
    public static class EstimateBuilderViewModelExtensions
    {
        public static void RemoveLineItem(EstimateBuilderViewModel viewModel, EstimateLineItem item, EstimateRoom room)
        {
            if (room != null && room.Items != null)
            {
                room.Items.Remove(item);
                viewModel.OnPropertyChanged("SelectedRoom");
            }
        }
    }
    
    // Extension for DatabaseService to add missing method
    public partial class DatabaseService
    {
        public Customer GetCustomerById(int customerId)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT * FROM Customers WHERE customer_id = @customerId";
                
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, connection))
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
