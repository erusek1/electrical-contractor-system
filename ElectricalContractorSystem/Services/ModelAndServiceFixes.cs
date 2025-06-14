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
            entry.Stage = stage;
            entry.StageId = stage.StageId;
        }
    }
    
    // Extension for EstimateBuilderViewModel fix
    public static class EstimateBuilderViewModelExtensions
    {
        public static void RemoveLineItem(EstimateBuilderViewModel viewModel, EstimateLineItem item, EstimateRoom room)
        {
            if (room != null && room.Items != null)
            {
                room.Items.Remove(item);
                // Use the public RaisePropertyChanged method
                viewModel.RaisePropertyChanged("SelectedRoom");
            }
        }
    }
}
