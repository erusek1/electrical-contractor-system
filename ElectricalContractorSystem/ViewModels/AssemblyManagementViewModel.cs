using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using System.Collections.Generic;
using System.Windows;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<AssemblyTemplate> _assemblies;
        private ObservableCollection<AssemblyTemplate> _filteredAssemblies;
        private ObservableCollection<PriceListItem> _priceListItems;
        private AssemblyTemplate _selectedAssembly;
        private AssemblyTemplate _selectedVariant;
        private PriceListItem _selectedPriceListItem;
        private string _searchText;
        private string _selectedCategory;
        private bool _showInactiveAssemblies;
        
        // New assembly creation fields
        private bool _isCreatingAssembly;
        private string _newAssemblyCode;
        private string _newAssemblyName;
        private string _newAssemblyCategory;
        private string _newAssemblyDescription;
        private int _newRoughMinutes;
        private int _newFinishMinutes;
        private int _newServiceMinutes;
        private int _newExtraMinutes;
        
        // Edit mode fields
        private bool _isEditingAssembly;
        private AssemblyTemplate _editingAssembly;
        
        // Component addition fields
        private decimal _componentQuantity = 1;
        
        public AssemblyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            _pricingService = new PricingService(databaseService);
            
            // Initialize collections
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            FilteredAssemblies = new ObservableCollection<AssemblyTemplate>();
            PriceListItems = new ObservableCollection<PriceListItem>();
            Categories = new ObservableCollection<string>();
            
            // Initialize commands
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            CreateAssemblyCommand = new RelayCommand(ExecuteCreateAssembly);
            SaveNewAssemblyCommand = new RelayCommand(ExecuteSaveNewAssembly, CanExecuteSaveNewAssembly);
            CancelNewAssemblyCommand = new RelayCommand(ExecuteCancelNewAssembly);
            EditAssemblyCommand = new RelayCommand(ExecuteEditAssembly, CanExecuteEditAssembly);
            SaveEditAssemblyCommand = new RelayCommand(ExecuteSaveEditAssembly, CanExecuteSaveEditAssembly);
            CancelEditAssemblyCommand = new RelayCommand(ExecuteCancelEditAssembly);
            CreateVariantCommand = new RelayCommand(ExecuteCreateVariant, CanExecuteCreateVariant);
            DeleteAssemblyCommand = new RelayCommand(ExecuteDeleteAssembly, CanExecuteDeleteAssembly);
            AddComponentCommand = new RelayCommand(ExecuteAddComponent, CanExecuteAddComponent);
            RemoveComponentCommand = new RelayCommand(ExecuteRemoveComponent);
            DuplicateAssemblyCommand = new RelayCommand(ExecuteDuplicateAssembly, CanExecuteDuplicateAssembly);
            ImportFromExcelCommand = new RelayCommand(ExecuteImportFromExcel);
            ExportToExcelCommand = new RelayCommand(ExecuteExportToExcel);
            
            LoadData();
        }
        
        #region Properties
        
        public ObservableCollection<AssemblyTemplate> Assemblies
        {
            get => _assemblies;
            set => SetProperty(ref _assemblies, value);
        }
        
        public ObservableCollection<AssemblyTemplate> FilteredAssemblies
        {
            get => _filteredAssemblies;
            set => SetProperty(ref _filteredAssemblies, value);
        }
        
        public ObservableCollection<PriceListItem> PriceListItems
        {
            get => _priceListItems;
            set => SetProperty(ref _priceListItems, value);
        }
        
        public ObservableCollection<string> Categories { get; }
        
        public AssemblyTemplate SelectedAssembly
        {
            get => _selectedAssembly;
            set
            {
                SetProperty(ref _selectedAssembly, value);
                OnPropertyChanged(nameof(AssemblyVariants));
                OnPropertyChanged(nameof(TotalMaterialCost));
                OnPropertyChanged(nameof(TotalLaborHours));
                CommandManager.InvalidateRequerySuggested();
                
                if (_selectedAssembly != null)
                {
                    LoadAssemblyVariants();
                }
            }
        }
        
        public AssemblyTemplate SelectedVariant
        {
            get => _selectedVariant;
            set
            {
                SetProperty(ref _selectedVariant, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public PriceListItem SelectedPriceListItem
        {
            get => _selectedPriceListItem;
            set
            {
                SetProperty(ref _selectedPriceListItem, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }
        
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                ApplyFilters();
            }
        }
        
        public bool ShowInactiveAssemblies
        {
            get => _showInactiveAssemblies;
            set
            {
                SetProperty(ref _showInactiveAssemblies, value);
                ApplyFilters();
            }
        }
        
        // New assembly creation properties
        public bool IsCreatingAssembly
        {
            get => _isCreatingAssembly;
            set => SetProperty(ref _isCreatingAssembly, value);
        }
        
        public string NewAssemblyCode
        {
            get => _newAssemblyCode;
            set
            {
                SetProperty(ref _newAssemblyCode, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string NewAssemblyName
        {
            get => _newAssemblyName;
            set
            {
                SetProperty(ref _newAssemblyName, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string NewAssemblyCategory
        {
            get => _newAssemblyCategory;
            set => SetProperty(ref _newAssemblyCategory, value);
        }
        
        public string NewAssemblyDescription
        {
            get => _newAssemblyDescription;
            set => SetProperty(ref _newAssemblyDescription, value);
        }
        
        public int NewRoughMinutes
        {
            get => _newRoughMinutes;
            set => SetProperty(ref _newRoughMinutes, value);
        }
        
        public int NewFinishMinutes
        {
            get => _newFinishMinutes;
            set => SetProperty(ref _newFinishMinutes, value);
        }
        
        public int NewServiceMinutes
        {
            get => _newServiceMinutes;
            set => SetProperty(ref _newServiceMinutes, value);
        }
        
        public int NewExtraMinutes
        {
            get => _newExtraMinutes;
            set => SetProperty(ref _newExtraMinutes, value);
        }
        
        // Edit mode properties
        public bool IsEditingAssembly
        {
            get => _isEditingAssembly;
            set => SetProperty(ref _isEditingAssembly, value);
        }
        
        // Component addition properties
        public decimal ComponentQuantity
        {
            get => _componentQuantity;
            set => SetProperty(ref _componentQuantity, value);
        }
        
        // Calculated properties
        public decimal TotalMaterialCost => SelectedAssembly?.TotalMaterialCost ?? 0;
        public decimal TotalLaborHours => SelectedAssembly?.TotalLaborHours ?? 0;
        
        public ObservableCollection<AssemblyTemplate> AssemblyVariants { get; private set; } = new ObservableCollection<AssemblyTemplate>();
        
        // Quick entry mode
        public bool IsQuickEntryMode { get; set; }
        public string QuickEntryCode { get; set; }
        
        #endregion
        
        #region Commands
        
        public ICommand RefreshCommand { get; }
        public ICommand CreateAssemblyCommand { get; }
        public ICommand SaveNewAssemblyCommand { get; }
        public ICommand CancelNewAssemblyCommand { get; }
        public ICommand EditAssemblyCommand { get; }
        public ICommand SaveEditAssemblyCommand { get; }
        public ICommand CancelEditAssemblyCommand { get; }
        public ICommand CreateVariantCommand { get; }
        public ICommand DeleteAssemblyCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand RemoveComponentCommand { get; }
        public ICommand DuplicateAssemblyCommand { get; }
        public ICommand ImportFromExcelCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }
        
        private void ExecuteCreateAssembly(object parameter)
        {
            // Clear the form fields
            NewAssemblyCode = string.Empty;
            NewAssemblyName = string.Empty;
            NewAssemblyCategory = Categories.FirstOrDefault(c => c != "All Categories") ?? "General";
            NewAssemblyDescription = string.Empty;
            NewRoughMinutes = 0;
            NewFinishMinutes = 0;
            NewServiceMinutes = 0;
            NewExtraMinutes = 0;
            
            // Show the creation panel
            IsCreatingAssembly = true;
            IsEditingAssembly = false;
        }
        
        private bool CanExecuteSaveNewAssembly(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewAssemblyCode) && 
                   !string.IsNullOrWhiteSpace(NewAssemblyName);
        }
        
        private void ExecuteSaveNewAssembly(object parameter)
        {
            try
            {
                // Check if assembly code already exists
                var existing = _assemblyService.GetAssembliesByCode(NewAssemblyCode);
                bool isDefault = !existing.Any();
                
                var newAssembly = _assemblyService.CreateAssembly(
                    NewAssemblyCode,
                    NewAssemblyName,
                    NewAssemblyCategory,
                    Environment.UserName);
                
                newAssembly.Description = NewAssemblyDescription;
                newAssembly.RoughMinutes = NewRoughMinutes;
                newAssembly.FinishMinutes = NewFinishMinutes;
                newAssembly.ServiceMinutes = NewServiceMinutes;
                newAssembly.ExtraMinutes = NewExtraMinutes;
                newAssembly.IsDefault = isDefault;
                
                _databaseService.UpdateAssembly(newAssembly);
                
                LoadData();
                SelectedAssembly = newAssembly;
                IsCreatingAssembly = false;
                
                MessageBox.Show($"Assembly '{NewAssemblyName}' created successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating assembly: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExecuteCancelNewAssembly(object parameter)
        {
            IsCreatingAssembly = false;
        }
        
        private bool CanExecuteEditAssembly(object parameter)
        {
            return SelectedAssembly != null;
        }
        
        private void ExecuteEditAssembly(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            _editingAssembly = SelectedAssembly;
            
            // Load current values into edit fields
            NewAssemblyCode = SelectedAssembly.AssemblyCode;
            NewAssemblyName = SelectedAssembly.Name;
            NewAssemblyCategory = SelectedAssembly.Category;
            NewAssemblyDescription = SelectedAssembly.Description;
            NewRoughMinutes = SelectedAssembly.RoughMinutes;
            NewFinishMinutes = SelectedAssembly.FinishMinutes;
            NewServiceMinutes = SelectedAssembly.ServiceMinutes;
            NewExtraMinutes = SelectedAssembly.ExtraMinutes;
            
            IsEditingAssembly = true;
            IsCreatingAssembly = false;
        }
        
        private bool CanExecuteSaveEditAssembly(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewAssemblyCode) && 
                   !string.IsNullOrWhiteSpace(NewAssemblyName);
        }
        
        private void ExecuteSaveEditAssembly(object parameter)
        {
            if (_editingAssembly == null) return;
            
            try
            {
                _editingAssembly.AssemblyCode = NewAssemblyCode;
                _editingAssembly.Name = NewAssemblyName;
                _editingAssembly.Category = NewAssemblyCategory;
                _editingAssembly.Description = NewAssemblyDescription;
                _editingAssembly.RoughMinutes = NewRoughMinutes;
                _editingAssembly.FinishMinutes = NewFinishMinutes;
                _editingAssembly.ServiceMinutes = NewServiceMinutes;
                _editingAssembly.ExtraMinutes = NewExtraMinutes;
                
                _databaseService.UpdateAssembly(_editingAssembly);
                
                LoadData();
                SelectedAssembly = _editingAssembly;
                IsEditingAssembly = false;
                _editingAssembly = null;
                
                MessageBox.Show($"Assembly '{NewAssemblyName}' updated successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating assembly: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExecuteCancelEditAssembly(object parameter)
        {
            IsEditingAssembly = false;
            _editingAssembly = null;
        }
        
        private bool CanExecuteCreateVariant(object parameter)
        {
            return SelectedAssembly != null;
        }
        
        private void ExecuteCreateVariant(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            // Pre-fill with selected assembly data for variant
            NewAssemblyCode = SelectedAssembly.AssemblyCode; // Same code for variant
            NewAssemblyName = SelectedAssembly.Name + " - Variant";
            NewAssemblyCategory = SelectedAssembly.Category;
            NewAssemblyDescription = SelectedAssembly.Description;
            NewRoughMinutes = SelectedAssembly.RoughMinutes;
            NewFinishMinutes = SelectedAssembly.FinishMinutes;
            NewServiceMinutes = SelectedAssembly.ServiceMinutes;
            NewExtraMinutes = SelectedAssembly.ExtraMinutes;
            
            IsCreatingAssembly = true;
            IsEditingAssembly = false;
        }
        
        private bool CanExecuteDeleteAssembly(object parameter)
        {
            return SelectedAssembly != null && !SelectedAssembly.IsDefault;
        }
        
        private void ExecuteDeleteAssembly(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            if (MessageBox.Show(
                $"Are you sure you want to delete assembly '{SelectedAssembly.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                SelectedAssembly.IsActive = false;
                _databaseService.UpdateAssembly(SelectedAssembly);
                LoadData();
            }
        }
        
        private bool CanExecuteAddComponent(object parameter)
        {
            return SelectedAssembly != null && SelectedPriceListItem != null && ComponentQuantity > 0;
        }
        
        private void ExecuteAddComponent(object parameter)
        {
            if (SelectedAssembly == null || SelectedPriceListItem == null) return;
            
            try
            {
                // Check if component already exists
                var existingComponent = SelectedAssembly.Components
                    .FirstOrDefault(c => c.PriceListItemId == SelectedPriceListItem.ItemId);
                
                if (existingComponent != null)
                {
                    // Update quantity
                    existingComponent.Quantity += ComponentQuantity;
                    _databaseService.UpdateAssemblyComponent(existingComponent);
                }
                else
                {
                    // Add new component
                    _assemblyService.AddComponentToAssembly(
                        SelectedAssembly.AssemblyId,
                        SelectedPriceListItem.ItemId,
                        ComponentQuantity,
                        null);
                }
                
                LoadAssemblyDetails();
                ComponentQuantity = 1; // Reset quantity
                
                MessageBox.Show($"Added {SelectedPriceListItem.Name} to assembly.", "Component Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding component: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ExecuteRemoveComponent(object parameter)
        {
            if (parameter is AssemblyComponent component)
            {
                if (MessageBox.Show(
                    $"Remove {component.DisplayText} from assembly?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _assemblyService.RemoveComponentFromAssembly(component.ComponentId);
                    LoadAssemblyDetails();
                }
            }
        }
        
        private bool CanExecuteDuplicateAssembly(object parameter)
        {
            return SelectedAssembly != null;
        }
        
        private void ExecuteDuplicateAssembly(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            var newAssembly = _assemblyService.CreateAssembly(
                SelectedAssembly.AssemblyCode + "_copy",
                SelectedAssembly.Name + " (Copy)",
                SelectedAssembly.Category,
                Environment.UserName);
                
            // Copy components
            foreach (var component in SelectedAssembly.Components)
            {
                _assemblyService.AddComponentToAssembly(
                    newAssembly.AssemblyId,
                    component.PriceListItemId,
                    component.Quantity,
                    component.Notes);
            }
            
            // Copy labor minutes
            newAssembly.RoughMinutes = SelectedAssembly.RoughMinutes;
            newAssembly.FinishMinutes = SelectedAssembly.FinishMinutes;
            newAssembly.ServiceMinutes = SelectedAssembly.ServiceMinutes;
            newAssembly.ExtraMinutes = SelectedAssembly.ExtraMinutes;
            
            _databaseService.UpdateAssembly(newAssembly);
            LoadData();
            SelectedAssembly = newAssembly;
        }
        
        private void ExecuteImportFromExcel(object parameter)
        {
            MessageBox.Show(
                "To import assemblies from Excel:\n\n" +
                "1. Ensure your Excel file has columns for:\n" +
                "   - Code (Column C)\n" +
                "   - Description (Column D)\n" +
                "   - Material formulas (Column I)\n" +
                "   - Labor minutes (Columns M-P)\n\n" +
                "2. Run the import script:\n" +
                "   python migration/import_assemblies_from_excel.py\n\n" +
                "The script will parse formulas and create assemblies automatically.",
                "Import from Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        private void ExecuteExportToExcel(object parameter)
        {
            // TODO: Implement export to Excel
            MessageBox.Show("Export to Excel feature coming soon!", "Export", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadData()
        {
            // Load assemblies
            var assemblies = _databaseService.GetAllAssemblies();
            Assemblies.Clear();
            foreach (var assembly in assemblies.OrderBy(a => a.Category).ThenBy(a => a.AssemblyCode))
            {
                Assemblies.Add(assembly);
            }
            
            // Load price list items (instead of materials)
            var priceListItems = _databaseService.GetAllPriceListItems();
            PriceListItems.Clear();
            foreach (var item in priceListItems.Where(p => p.IsActive).OrderBy(p => p.Category).ThenBy(p => p.Name))
            {
                PriceListItems.Add(item);
            }
            
            // Load categories
            Categories.Clear();
            Categories.Add("All Categories");
            var categories = assemblies.Select(a => a.Category).Distinct().OrderBy(c => c);
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
            
            ApplyFilters();
        }
        
        private void LoadAssemblyDetails()
        {
            if (SelectedAssembly == null) return;
            
            // Reload the selected assembly to get updated components
            SelectedAssembly = _databaseService.GetAssemblyById(SelectedAssembly.AssemblyId);
            OnPropertyChanged(nameof(TotalMaterialCost));
            OnPropertyChanged(nameof(TotalLaborHours));
        }
        
        private void LoadAssemblyVariants()
        {
            AssemblyVariants.Clear();
            
            if (SelectedAssembly != null)
            {
                var variants = _assemblyService.GetAssembliesByCode(SelectedAssembly.AssemblyCode);
                foreach (var variant in variants)
                {
                    AssemblyVariants.Add(variant);
                }
            }
        }
        
        private void ApplyFilters()
        {
            FilteredAssemblies.Clear();
            
            var query = Assemblies.AsEnumerable();
            
            // Filter by active status
            if (!ShowInactiveAssemblies)
            {
                query = query.Where(a => a.IsActive);
            }
            
            // Filter by category
            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All Categories")
            {
                query = query.Where(a => a.Category == SelectedCategory);
            }
            
            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(a => 
                    a.AssemblyCode.ToLower().Contains(searchLower) ||
                    a.Name.ToLower().Contains(searchLower) ||
                    (a.Description?.ToLower().Contains(searchLower) ?? false));
            }
            
            foreach (var assembly in query)
            {
                FilteredAssemblies.Add(assembly);
            }
        }
        
        #endregion
        
        #region Quick Entry Support
        
        public AssemblyTemplate GetAssemblyByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            
            var assemblies = _assemblyService.GetAssembliesByCode(code);
            return assemblies.FirstOrDefault(a => a.IsDefault) ?? assemblies.FirstOrDefault();
        }
        
        public List<AssemblyTemplate> GetAssemblyVariantsByCode(string code)
        {
            return _assemblyService.GetAssembliesByCode(code);
        }
        
        #endregion
    }
}
