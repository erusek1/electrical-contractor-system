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
                var assemblies = _assemblyService.SearchAssemblies("");
                
                foreach (var assembly in assemblies.Where(a => a.IsDefault))
                {
                    Assemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading assemblies: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            var variants = _assemblyService.GetAssembliesByCode(SelectedAssembly.AssemblyCode);
            foreach (var variant in variants.Where(v => v.AssemblyId != SelectedAssembly.AssemblyId))
            {
                Variants.Add(variant);
            }
            
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
            var dialog = new AssemblyEditDialog();
            var viewModel = new AssemblyEditViewModel(_databaseService);
            dialog.DataContext = viewModel;
            
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }
        
        private void EditAssembly()
        {
            if (SelectedAssembly == null) return;
            
            var dialog = new AssemblyEditDialog();
            var viewModel = new AssemblyEditViewModel(_databaseService, SelectedAssembly);
            dialog.DataContext = viewModel;
            
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                // Try to reselect the edited assembly
                SelectedAssembly = Assemblies.FirstOrDefault(a => a.AssemblyId == SelectedAssembly.AssemblyId);
            }
        }
        
        private void DeleteAssembly()
        {
            if (SelectedAssembly == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the assembly '{SelectedAssembly.Name}'?\n\n" +
                "This action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    SelectedAssembly.IsActive = false;
                    SelectedAssembly.UpdatedBy = "System";
                    SelectedAssembly.UpdatedDate = DateTime.Now;
                    _databaseService.SaveAssembly(SelectedAssembly);
                    
                    Assemblies.Remove(SelectedAssembly);
                    SelectedAssembly = null;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting assembly: {ex.Message}", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void CreateVariant()
        {
            if (SelectedAssembly == null) return;
            
            var dialog = new CreateVariantDialog();
            dialog.ParentAssemblyName = SelectedAssembly.Name;
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Create the variant with a default category
                    var variant = _assemblyService.CreateAssembly(
                        SelectedAssembly.AssemblyCode,
                        dialog.VariantName,
                        SelectedAssembly.Category,  // Use the parent's category
                        "System"
                    );
                    
                    // Copy properties from parent
                    variant.Description = dialog.VariantDescription;
                    variant.RoughMinutes = SelectedAssembly.RoughMinutes;
                    variant.FinishMinutes = SelectedAssembly.FinishMinutes;
                    variant.ServiceMinutes = SelectedAssembly.ServiceMinutes;
                    variant.ExtraMinutes = SelectedAssembly.ExtraMinutes;
                    variant.IsDefault = false;
                    
                    _databaseService.SaveAssembly(variant);
                    
                    // Copy components
                    foreach (var component in SelectedAssembly.Components)
                    {
                        var newComponent = new AssemblyComponent
                        {
                            AssemblyId = variant.AssemblyId,
                            PriceListItemId = component.PriceListItemId,
                            Quantity = component.Quantity,
                            Notes = component.Notes
                        };
                        _databaseService.SaveAssemblyComponent(newComponent);
                    }
                    
                    // Create variant relationship
                    _databaseService.CreateAssemblyVariantRelationship(
                        SelectedAssembly.AssemblyId, 
                        variant.AssemblyId
                    );
                    
                    // Reload to show the new variant
                    LoadAssemblyDetails();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating variant: {ex.Message}", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void AddComponent()
        {
            if (SelectedAssembly == null) return;
            
            var dialog = new AddComponentDialog();
            var items = _databaseService.GetAllPriceListItems()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToList();
            
            dialog.AvailableItems = items;
            
            if (dialog.ShowDialog() == true && dialog.SelectedItem != null)
            {
                try
                {
                    _assemblyService.AddComponentToAssembly(
                        SelectedAssembly.AssemblyId,
                        dialog.SelectedItem.ItemId,
                        dialog.Quantity
                    );
                    
                    LoadAssemblyDetails();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error adding component: {ex.Message}", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void EditComponent()
        {
            if (SelectedComponent == null) return;
            
            var dialog = new EditComponentDialog();
            dialog.ComponentName = SelectedComponent.ItemName;
            dialog.CurrentQuantity = SelectedComponent.Quantity;
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _assemblyService.UpdateAssemblyComponent(
                        SelectedComponent.ComponentId,
                        dialog.NewQuantity
                    );
                    
                    LoadAssemblyDetails();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error updating component: {ex.Message}", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void RemoveComponent()
        {
            if (SelectedComponent == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to remove '{SelectedComponent.ItemName}' from this assembly?",
                "Confirm Remove",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _assemblyService.RemoveComponentFromAssembly(SelectedComponent.ComponentId);
                    LoadAssemblyDetails();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error removing component: {ex.Message}", 
                        "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
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
