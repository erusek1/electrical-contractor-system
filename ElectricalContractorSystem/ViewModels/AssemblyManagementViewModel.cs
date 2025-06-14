using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using System.Collections.Generic;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<AssemblyTemplate> _assemblies;
        private ObservableCollection<AssemblyTemplate> _filteredAssemblies;
        private ObservableCollection<Material> _materials;
        private AssemblyTemplate _selectedAssembly;
        private AssemblyTemplate _selectedVariant;
        private Material _selectedMaterial;
        private string _searchText;
        private string _selectedCategory;
        private bool _showInactiveAssemblies;
        
        public AssemblyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            _pricingService = new PricingService(databaseService);
            
            // Initialize collections
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            FilteredAssemblies = new ObservableCollection<AssemblyTemplate>();
            Materials = new ObservableCollection<Material>();
            Categories = new ObservableCollection<string>();
            
            // Initialize commands
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            CreateAssemblyCommand = new RelayCommand(ExecuteCreateAssembly);
            EditAssemblyCommand = new RelayCommand(ExecuteEditAssembly, CanExecuteEditAssembly);
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
        
        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set => SetProperty(ref _materials, value);
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
        
        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                SetProperty(ref _selectedMaterial, value);
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
        public ICommand EditAssemblyCommand { get; }
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
            // TODO: Remove dialog reference - implement inline creation
            System.Windows.MessageBox.Show("Create assembly feature will be implemented in the UI.", "Create Assembly", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private bool CanExecuteEditAssembly(object parameter)
        {
            return SelectedAssembly != null;
        }
        
        private void ExecuteEditAssembly(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            // TODO: Remove dialog reference - implement inline editing
            System.Windows.MessageBox.Show($"Edit assembly '{SelectedAssembly.Name}' feature will be implemented in the UI.", "Edit Assembly", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private bool CanExecuteCreateVariant(object parameter)
        {
            return SelectedAssembly != null;
        }
        
        private void ExecuteCreateVariant(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            // TODO: Remove dialog reference - implement inline variant creation
            System.Windows.MessageBox.Show($"Create variant for '{SelectedAssembly.Name}' feature will be implemented in the UI.", "Create Variant", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private bool CanExecuteDeleteAssembly(object parameter)
        {
            return SelectedAssembly != null && !SelectedAssembly.IsDefault;
        }
        
        private void ExecuteDeleteAssembly(object parameter)
        {
            if (SelectedAssembly == null) return;
            
            if (System.Windows.MessageBox.Show(
                $"Are you sure you want to delete assembly '{SelectedAssembly.Name}'?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
            {
                SelectedAssembly.IsActive = false;
                _databaseService.UpdateAssembly(SelectedAssembly);
                LoadData();
            }
        }
        
        private bool CanExecuteAddComponent(object parameter)
        {
            return SelectedAssembly != null && SelectedMaterial != null;
        }
        
        private void ExecuteAddComponent(object parameter)
        {
            if (SelectedAssembly == null || SelectedMaterial == null) return;
            
            // TODO: Remove dialog reference - implement inline component addition
            System.Windows.MessageBox.Show("Add component feature will be implemented in the UI.", "Add Component", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private void ExecuteRemoveComponent(object parameter)
        {
            if (parameter is AssemblyComponent component)
            {
                if (System.Windows.MessageBox.Show(
                    $"Remove {component.DisplayText} from assembly?",
                    "Confirm Remove",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
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
                    component.MaterialId,
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
            System.Windows.MessageBox.Show(
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
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void ExecuteExportToExcel(object parameter)
        {
            // TODO: Implement export to Excel
            System.Windows.MessageBox.Show("Export to Excel feature coming soon!", "Export", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
            
            // Load materials
            var materials = _databaseService.GetAllMaterials();
            Materials.Clear();
            foreach (var material in materials.OrderBy(m => m.Category).ThenBy(m => m.Name))
            {
                Materials.Add(material);
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