using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;
using ElectricalContractorSystem.Helpers;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private readonly PricingService _pricingService;
        
        #region Properties
        
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilter();
            }
        }
        
        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                ApplyFilter();
            }
        }
        
        private AssemblyTemplate _selectedAssembly;
        public AssemblyTemplate SelectedAssembly
        {
            get => _selectedAssembly;
            set
            {
                SetProperty(ref _selectedAssembly, value);
                LoadAssemblyDetails();
                UpdateCommands();
            }
        }
        
        private AssemblyComponent _selectedComponent;
        public AssemblyComponent SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                SetProperty(ref _selectedComponent, value);
                UpdateCommands();
            }
        }
        
        public ObservableCollection<AssemblyTemplate> Assemblies { get; }
        public ICollectionView AssembliesView { get; }
        
        public ObservableCollection<AssemblyComponent> Components { get; }
        
        public ObservableCollection<string> Categories { get; }
        
        public ObservableCollection<AssemblyTemplate> Variants { get; }
        
        #endregion
        
        #region Calculated Properties
        
        public decimal TotalMaterialCost => Components?.Sum(c => c.TotalCost) ?? 0;
        
        public decimal TotalLaborHours => SelectedAssembly != null ? 
            (SelectedAssembly.RoughMinutes + SelectedAssembly.FinishMinutes + 
             SelectedAssembly.ServiceMinutes + SelectedAssembly.ExtraMinutes) / 60m : 0;
        
        public decimal EstimatedTotalCost => TotalMaterialCost + (TotalLaborHours * 85); // $85/hr default
        
        #endregion
        
        #region Commands
        
        public ICommand CreateAssemblyCommand { get; }
        public ICommand EditAssemblyCommand { get; }
        public ICommand DeleteAssemblyCommand { get; }
        public ICommand CreateVariantCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand EditComponentCommand { get; }
        public ICommand RemoveComponentCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        
        #endregion
        
        #region Constructor
        
        public AssemblyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            _pricingService = new PricingService(databaseService);
            
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            Components = new ObservableCollection<AssemblyComponent>();
            Variants = new ObservableCollection<AssemblyTemplate>();
            
            AssembliesView = CollectionViewSource.GetDefaultView(Assemblies);
            AssembliesView.Filter = FilterAssembly;
            AssembliesView.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
            AssembliesView.SortDescriptions.Add(new SortDescription("AssemblyCode", ListSortDirection.Ascending));
            AssembliesView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            
            Categories = new ObservableCollection<string>
            {
                "All",
                "Devices",
                "Lighting", 
                "Rough-In",
                "Service",
                "Special",
                "Other"
            };
            
            // Initialize commands
            CreateAssemblyCommand = new RelayCommand(CreateAssembly);
            EditAssemblyCommand = new RelayCommand(EditAssembly, () => SelectedAssembly != null);
            DeleteAssemblyCommand = new RelayCommand(DeleteAssembly, () => SelectedAssembly != null);
            CreateVariantCommand = new RelayCommand(CreateVariant, () => SelectedAssembly != null);
            AddComponentCommand = new RelayCommand(AddComponent, () => SelectedAssembly != null);
            EditComponentCommand = new RelayCommand(EditComponent, () => SelectedComponent != null);
            RemoveComponentCommand = new RelayCommand(RemoveComponent, () => SelectedComponent != null);
            RefreshCommand = new RelayCommand(LoadData);
            ExportCommand = new RelayCommand(ExportAssemblies);
            ImportCommand = new RelayCommand(ImportAssemblies);
            
            LoadData();
        }
        
        #endregion
        
        #region Methods
        
        private void LoadData()
        {
            try
            {
                Assemblies.Clear();
                
                // For now, create assembly templates from PriceList items until the assembly tables are created
                var priceListItems = _databaseService.GetActivePriceListItems();
                
                foreach (var item in priceListItems.Where(i => !string.IsNullOrEmpty(i.ItemCode)))
                {
                    // Create a temporary assembly template from price list item
                    var assembly = new AssemblyTemplate
                    {
                        AssemblyId = item.ItemId,
                        AssemblyCode = item.ItemCode,
                        Name = item.Name,
                        Description = item.Description,
                        Category = item.Category ?? "Other",
                        RoughMinutes = item.LaborMinutes,
                        FinishMinutes = 0,
                        ServiceMinutes = 0,
                        ExtraMinutes = 0,
                        IsDefault = true,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedDate = DateTime.Now,
                        Components = new List<AssemblyComponent>()
                    };
                    
                    // Add a single component representing the price list item
                    assembly.Components.Add(new AssemblyComponent
                    {
                        ComponentId = 1,
                        AssemblyId = assembly.AssemblyId,
                        PriceListItemId = item.ItemId,
                        Quantity = 1,
                        ItemCode = item.ItemCode,
                        ItemName = item.Name,
                        UnitPrice = item.BaseCost,
                        PriceListItem = item
                    });
                    
                    Assemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                // Just show empty list if there's an error
                Assemblies.Clear();
            }
        }
        
        private void LoadAssemblyDetails()
        {
            Components.Clear();
            Variants.Clear();
            
            if (SelectedAssembly == null) return;
            
            // Load components
            foreach (var component in SelectedAssembly.Components ?? new List<AssemblyComponent>())
            {
                Components.Add(component);
            }
            
            // Load variants
            // For now, skip loading variants since we're using PriceList items
            // var variants = _assemblyService.GetAssembliesByCode(SelectedAssembly.AssemblyCode);
            // foreach (var variant in variants.Where(v => v.AssemblyId != SelectedAssembly.AssemblyId))
            // {
            //     Variants.Add(variant);
            // }
            
            OnPropertyChanged(nameof(TotalMaterialCost));
            OnPropertyChanged(nameof(TotalLaborHours));
            OnPropertyChanged(nameof(EstimatedTotalCost));
        }
        
        private bool FilterAssembly(object obj)
        {
            if (!(obj is AssemblyTemplate assembly)) return false;
            
            // Category filter
            if (SelectedCategory != "All" && assembly.Category != SelectedCategory)
                return false;
            
            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                return assembly.AssemblyCode.ToLower().Contains(searchLower) ||
                       assembly.Name.ToLower().Contains(searchLower) ||
                       (assembly.Description?.ToLower().Contains(searchLower) ?? false);
            }
            
            return true;
        }
        
        private void ApplyFilter()
        {
            AssembliesView?.Refresh();
        }
        
        private void UpdateCommands()
        {
            (EditAssemblyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteAssemblyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CreateVariantCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveComponentCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        #endregion
        
        #region Command Implementations
        
        private void CreateAssembly()
        {
            // Graceful fallback for missing dialog
            System.Windows.MessageBox.Show(
                "Assembly management requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void EditAssembly()
        {
            // Graceful fallback for missing dialog
            System.Windows.MessageBox.Show(
                "Assembly editing requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void DeleteAssembly()
        {
            System.Windows.MessageBox.Show(
                "Assembly deletion requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void CreateVariant()
        {
            // Graceful fallback for missing dialog
            System.Windows.MessageBox.Show(
                "Assembly variants require the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void AddComponent()
        {
            // Graceful fallback for missing dialog
            System.Windows.MessageBox.Show(
                "Component management requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void EditComponent()
        {
            // Graceful fallback for missing dialog
            System.Windows.MessageBox.Show(
                "Component management requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void RemoveComponent()
        {
            System.Windows.MessageBox.Show(
                "Component management requires the advanced pricing tables to be created.\n\n" +
                "For now, you can manage items through the Price List Management screen.",
                "Assembly Management",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void ExportAssemblies()
        {
            // TODO: Implement export to Excel
            System.Windows.MessageBox.Show("Export functionality will be implemented soon.", 
                "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private void ImportAssemblies()
        {
            // TODO: Implement import from Excel
            System.Windows.MessageBox.Show("Import functionality will be implemented soon.", 
                "Import", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        #endregion
    }
}
