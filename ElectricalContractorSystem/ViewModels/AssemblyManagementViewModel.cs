using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<AssemblyTemplate> _assemblies;
        private AssemblyTemplate _selectedAssembly;
        private ObservableCollection<AssemblyComponent> _components;
        private AssemblyComponent _selectedComponent;
        private ObservableCollection<AssemblyVariant> _variants;
        private AssemblyVariant _selectedVariant;
        private string _searchText;
        
        public ObservableCollection<AssemblyTemplate> Assemblies
        {
            get => _assemblies;
            set
            {
                _assemblies = value;
                OnPropertyChanged();
            }
        }
        
        public AssemblyTemplate SelectedAssembly
        {
            get => _selectedAssembly;
            set
            {
                _selectedAssembly = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAssemblySelected));
                OnPropertyChanged(nameof(CanEditAssembly));
                OnPropertyChanged(nameof(CanDeleteAssembly));
                LoadAssemblyDetails();
            }
        }
        
        public ObservableCollection<AssemblyComponent> Components
        {
            get => _components;
            set
            {
                _components = value;
                OnPropertyChanged();
            }
        }
        
        public AssemblyComponent SelectedComponent
        {
            get => _selectedComponent;
            set
            {
                _selectedComponent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDeleteComponent));
            }
        }
        
        public ObservableCollection<AssemblyVariant> Variants
        {
            get => _variants;
            set
            {
                _variants = value;
                OnPropertyChanged();
            }
        }
        
        public AssemblyVariant SelectedVariant
        {
            get => _selectedVariant;
            set
            {
                _selectedVariant = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSetAsDefault));
                OnPropertyChanged(nameof(CanDeleteVariant));
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterAssemblies();
            }
        }
        
        // Calculated properties
        public bool IsAssemblySelected => SelectedAssembly != null;
        public bool CanEditAssembly => SelectedAssembly != null;
        public bool CanDeleteAssembly => SelectedAssembly != null && !SelectedAssembly.IsDefault;
        public bool CanDeleteComponent => SelectedComponent != null;
        public bool CanSetAsDefault => SelectedVariant != null && !IsVariantDefault(SelectedVariant);
        public bool CanDeleteVariant => SelectedVariant != null && !IsVariantDefault(SelectedVariant);
        
        public decimal TotalMaterialCost
        {
            get
            {
                if (Components == null) return 0;
                return Components.Sum(c => (c.Material?.CurrentPrice ?? 0) * c.Quantity);
            }
        }
        
        public int TotalLaborMinutes
        {
            get
            {
                if (SelectedAssembly == null) return 0;
                return SelectedAssembly.RoughMinutes + SelectedAssembly.FinishMinutes + 
                       SelectedAssembly.ServiceMinutes + SelectedAssembly.ExtraMinutes;
            }
        }
        
        public decimal TotalLaborHours => Math.Round(TotalLaborMinutes / 60m, 2);
        
        // Commands
        public ICommand CreateAssemblyCommand { get; }
        public ICommand EditAssemblyCommand { get; }
        public ICommand DeleteAssemblyCommand { get; }
        public ICommand DuplicateAssemblyCommand { get; }
        public ICommand DeleteComponentCommand { get; }
        public ICommand CreateVariantCommand { get; }
        public ICommand SetDefaultVariantCommand { get; }
        public ICommand DeleteVariantCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public AssemblyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            _pricingService = new PricingService(databaseService);
            
            // Initialize collections
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            Components = new ObservableCollection<AssemblyComponent>();
            Variants = new ObservableCollection<AssemblyVariant>();
            
            // Initialize commands
            CreateAssemblyCommand = new RelayCommand(CreateAssembly);
            EditAssemblyCommand = new RelayCommand(EditAssembly, () => CanEditAssembly);
            DeleteAssemblyCommand = new RelayCommand(DeleteAssembly, () => CanDeleteAssembly);
            DuplicateAssemblyCommand = new RelayCommand(DuplicateAssembly, () => SelectedAssembly != null);
            DeleteComponentCommand = new RelayCommand(DeleteComponent, () => CanDeleteComponent);
            CreateVariantCommand = new RelayCommand(CreateVariant, () => SelectedAssembly != null);
            SetDefaultVariantCommand = new RelayCommand(SetDefaultVariant, () => CanSetAsDefault);
            DeleteVariantCommand = new RelayCommand(DeleteVariant, () => CanDeleteVariant);
            RefreshCommand = new RelayCommand(LoadData);
            
            // Load initial data
            LoadData();
        }
        
        private void LoadData()
        {
            try
            {
                // Load assemblies using DatabaseService extension method
                var assemblies = _databaseService.GetAllAssemblies();
                Assemblies.Clear();
                foreach (var assembly in assemblies)
                {
                    Assemblies.Add(assembly);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading assemblies: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void LoadAssemblyDetails()
        {
            if (SelectedAssembly == null)
            {
                Components.Clear();
                Variants.Clear();
                return;
            }
            
            try
            {
                // Load components using DatabaseService extension method
                var components = _databaseService.GetAssemblyComponents(SelectedAssembly.AssemblyId);
                Components.Clear();
                foreach (var component in components)
                {
                    // Load the material details
                    component.Material = _databaseService.GetMaterial(component.MaterialId);
                    Components.Add(component);
                }
                
                // Load variants using DatabaseService extension method
                var variantAssemblies = _databaseService.GetAssemblyVariants(SelectedAssembly.AssemblyCode);
                Variants.Clear();
                int sortOrder = 0;
                foreach (var variantAssembly in variantAssemblies)
                {
                    if (variantAssembly.AssemblyId != SelectedAssembly.AssemblyId)
                    {
                        var variant = new AssemblyVariant
                        {
                            ParentAssemblyId = SelectedAssembly.AssemblyId,
                            VariantAssemblyId = variantAssembly.AssemblyId,
                            SortOrder = sortOrder++,
                            VariantAssembly = variantAssembly
                        };
                        Variants.Add(variant);
                    }
                }
                
                // Notify property changes
                OnPropertyChanged(nameof(TotalMaterialCost));
                OnPropertyChanged(nameof(TotalLaborMinutes));
                OnPropertyChanged(nameof(TotalLaborHours));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading assembly details: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void FilterAssemblies()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                LoadData();
                return;
            }
            
            var filtered = _databaseService.GetAllAssemblies()
                .Where(a => a.AssemblyCode.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           a.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                           (a.Description?.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
                .ToList();
            
            Assemblies.Clear();
            foreach (var assembly in filtered)
            {
                Assemblies.Add(assembly);
            }
        }
        
        private void CreateAssembly()
        {
            var dialog = new AssemblyEditDialog();
            var viewModel = new AssemblyEditViewModel(_databaseService);
            dialog.DataContext = viewModel;
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
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
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                // Try to reselect the edited assembly
                SelectedAssembly = Assemblies.FirstOrDefault(a => a.AssemblyId == SelectedAssembly.AssemblyId);
            }
        }
        
        private void DeleteAssembly()
        {
            if (SelectedAssembly == null || SelectedAssembly.IsDefault) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete the assembly '{SelectedAssembly.AssemblyCode} - {SelectedAssembly.Name}'?\n\n" +
                "This will also delete all components and cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.DeleteAssembly(SelectedAssembly.AssemblyId);
                    LoadData();
                    SelectedAssembly = null;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting assembly: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private void DuplicateAssembly()
        {
            if (SelectedAssembly == null) return;
            
            try
            {
                // Create a copy of the assembly with a new code
                var newAssembly = new AssemblyTemplate
                {
                    AssemblyCode = GetNextAssemblyCode(SelectedAssembly.AssemblyCode),
                    Name = $"{SelectedAssembly.Name} (Copy)",
                    Description = SelectedAssembly.Description,
                    Category = SelectedAssembly.Category ?? "General",
                    RoughMinutes = SelectedAssembly.RoughMinutes,
                    FinishMinutes = SelectedAssembly.FinishMinutes,
                    ServiceMinutes = SelectedAssembly.ServiceMinutes,
                    ExtraMinutes = SelectedAssembly.ExtraMinutes,
                    IsDefault = false,
                    IsActive = true,
                    CreatedBy = "System" // TODO: Get current user
                };
                
                // Get the components
                var components = _databaseService.GetAssemblyComponents(SelectedAssembly.AssemblyId)
                    .Select(c => new AssemblyComponent
                    {
                        MaterialId = c.MaterialId,
                        Quantity = c.Quantity,
                        IsOptional = c.IsOptional
                    }).ToList();
                
                // Create the new assembly with components
                _assemblyService.CreateAssembly(newAssembly, components);
                
                // Reload and select the new assembly
                LoadData();
                SelectedAssembly = Assemblies.FirstOrDefault(a => a.AssemblyCode == newAssembly.AssemblyCode);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error duplicating assembly: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private string GetNextAssemblyCode(string baseCode)
        {
            // Find the next available code by appending numbers
            int suffix = 2;
            string newCode;
            do
            {
                newCode = $"{baseCode}-{suffix}";
                suffix++;
            } while (Assemblies.Any(a => a.AssemblyCode == newCode));
            
            return newCode;
        }
        
        private void DeleteComponent()
        {
            if (SelectedComponent == null || SelectedAssembly == null) return;
            
            try
            {
                _databaseService.DeleteAssemblyComponent(SelectedComponent.ComponentId);
                Components.Remove(SelectedComponent);
                SelectedComponent = null;
                OnPropertyChanged(nameof(TotalMaterialCost));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error deleting component: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void CreateVariant()
        {
            if (SelectedAssembly == null) return;
            
            // Create a new assembly as a variant with the same code
            var dialog = new AssemblyEditDialog();
            
            // Create a copy of the current assembly to use as template
            var variantTemplate = new AssemblyTemplate
            {
                AssemblyCode = SelectedAssembly.AssemblyCode, // Keep the same code for variants
                Name = $"{SelectedAssembly.Name} - Variant",
                Description = SelectedAssembly.Description,
                Category = SelectedAssembly.Category ?? "General",
                RoughMinutes = SelectedAssembly.RoughMinutes,
                FinishMinutes = SelectedAssembly.FinishMinutes,
                ServiceMinutes = SelectedAssembly.ServiceMinutes,
                ExtraMinutes = SelectedAssembly.ExtraMinutes,
                IsDefault = false,
                IsActive = true,
                CreatedBy = "System" // TODO: Get current user
            };
            
            var viewModel = new AssemblyEditViewModel(_databaseService, variantTemplate);
            dialog.DataContext = viewModel;
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                LoadAssemblyDetails();
            }
        }
        
        private void SetDefaultVariant()
        {
            if (SelectedVariant == null || IsVariantDefault(SelectedVariant)) return;
            
            try
            {
                // Update all assemblies with this code to set the selected one as default
                var allVariants = _databaseService.GetAssemblyVariants(SelectedAssembly.AssemblyCode);
                foreach (var variant in allVariants)
                {
                    variant.IsDefault = (variant.AssemblyId == SelectedVariant.VariantAssemblyId);
                    _databaseService.UpdateAssembly(variant);
                }
                
                // Refresh display
                LoadAssemblyDetails();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error setting default variant: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void DeleteVariant()
        {
            if (SelectedVariant == null || IsVariantDefault(SelectedVariant)) return;
            
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete this variant?\n\nThis cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.DeleteAssembly(SelectedVariant.VariantAssemblyId);
                    LoadAssemblyDetails();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting variant: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        private bool IsVariantDefault(AssemblyVariant variant)
        {
            return variant?.VariantAssembly?.IsDefault ?? false;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}