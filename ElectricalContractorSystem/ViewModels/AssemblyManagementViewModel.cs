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

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        
        private ObservableCollection<AssemblyTemplate> _assemblies;
        private AssemblyTemplate _selectedAssembly;
        private ObservableCollection<AssemblyComponent> _components;
        private AssemblyComponent _selectedComponent;
        private ObservableCollection<AssemblyVariant> _variants;
        private AssemblyVariant _selectedVariant;
        private ObservableCollection<Material> _availableMaterials;
        private Material _selectedMaterial;
        private string _searchText;
        private bool _isEditingAssembly;
        private bool _isEditingComponent;
        
        // Edit mode properties
        private string _editCode;
        private string _editName;
        private string _editDescription;
        private string _editCategory;
        private int _editRoughMinutes;
        private int _editFinishMinutes;
        private int _editServiceMinutes;
        private int _editExtraMinutes;
        private decimal _editQuantity;
        
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
                OnPropertyChanged(nameof(CanEditComponent));
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
        
        public ObservableCollection<Material> AvailableMaterials
        {
            get => _availableMaterials;
            set
            {
                _availableMaterials = value;
                OnPropertyChanged();
            }
        }
        
        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                _selectedMaterial = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddComponent));
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
        
        public bool IsEditingAssembly
        {
            get => _isEditingAssembly;
            set
            {
                _isEditingAssembly = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsEditingComponent
        {
            get => _isEditingComponent;
            set
            {
                _isEditingComponent = value;
                OnPropertyChanged();
            }
        }
        
        // Edit properties
        public string EditCode
        {
            get => _editCode;
            set
            {
                _editCode = value;
                OnPropertyChanged();
            }
        }
        
        public string EditName
        {
            get => _editName;
            set
            {
                _editName = value;
                OnPropertyChanged();
            }
        }
        
        public string EditDescription
        {
            get => _editDescription;
            set
            {
                _editDescription = value;
                OnPropertyChanged();
            }
        }
        
        public string EditCategory
        {
            get => _editCategory;
            set
            {
                _editCategory = value;
                OnPropertyChanged();
            }
        }
        
        public int EditRoughMinutes
        {
            get => _editRoughMinutes;
            set
            {
                _editRoughMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int EditFinishMinutes
        {
            get => _editFinishMinutes;
            set
            {
                _editFinishMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int EditServiceMinutes
        {
            get => _editServiceMinutes;
            set
            {
                _editServiceMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int EditExtraMinutes
        {
            get => _editExtraMinutes;
            set
            {
                _editExtraMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public decimal EditQuantity
        {
            get => _editQuantity;
            set
            {
                _editQuantity = value;
                OnPropertyChanged();
            }
        }
        
        // Calculated properties
        public bool IsAssemblySelected => SelectedAssembly != null;
        public bool CanEditAssembly => SelectedAssembly != null;
        public bool CanDeleteAssembly => SelectedAssembly != null && !SelectedAssembly.IsDefault;
        public bool CanAddComponent => SelectedAssembly != null && SelectedMaterial != null;
        public bool CanEditComponent => SelectedComponent != null;
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
        public ICommand SaveAssemblyCommand { get; }
        public ICommand CancelEditAssemblyCommand { get; }
        public ICommand DeleteAssemblyCommand { get; }
        public ICommand DuplicateAssemblyCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand EditComponentCommand { get; }
        public ICommand SaveComponentCommand { get; }
        public ICommand CancelEditComponentCommand { get; }
        public ICommand DeleteComponentCommand { get; }
        public ICommand CreateVariantCommand { get; }
        public ICommand SetDefaultVariantCommand { get; }
        public ICommand DeleteVariantCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public AssemblyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            // Initialize collections
            Assemblies = new ObservableCollection<AssemblyTemplate>();
            Components = new ObservableCollection<AssemblyComponent>();
            Variants = new ObservableCollection<AssemblyVariant>();
            AvailableMaterials = new ObservableCollection<Material>();
            
            // Initialize commands
            CreateAssemblyCommand = new RelayCommand(CreateAssembly);
            EditAssemblyCommand = new RelayCommand(EditAssembly, () => CanEditAssembly);
            SaveAssemblyCommand = new RelayCommand(SaveAssembly);
            CancelEditAssemblyCommand = new RelayCommand(CancelEditAssembly);
            DeleteAssemblyCommand = new RelayCommand(DeleteAssembly, () => CanDeleteAssembly);
            DuplicateAssemblyCommand = new RelayCommand(DuplicateAssembly, () => SelectedAssembly != null);
            
            AddComponentCommand = new RelayCommand(AddComponent, () => CanAddComponent);
            EditComponentCommand = new RelayCommand(EditComponent, () => CanEditComponent);
            SaveComponentCommand = new RelayCommand(SaveComponent);
            CancelEditComponentCommand = new RelayCommand(CancelEditComponent);
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
                
                // Load available materials
                LoadMaterials();
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }
        
        private void LoadMaterials()
        {
            try
            {
                var materials = _databaseService.GetAllMaterials();
                AvailableMaterials.Clear();
                foreach (var material in materials)
                {
                    AvailableMaterials.Add(material);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading materials: {ex.Message}");
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
                // Load components
                var components = _databaseService.GetAssemblyComponents(SelectedAssembly.AssemblyId);
                Components.Clear();
                foreach (var component in components)
                {
                    // Material should already be loaded by the database service
                    Components.Add(component);
                }
                
                // Load variants - need to find other assemblies with same code
                var variantAssemblies = _databaseService.GetAssemblyVariants(SelectedAssembly.AssemblyCode);
                Variants.Clear();
                int sortOrder = 0;
                foreach (var variantAssembly in variantAssemblies)
                {
                    // Create AssemblyVariant objects for display
                    var variant = new AssemblyVariant
                    {
                        ParentAssemblyId = SelectedAssembly.AssemblyId,
                        VariantAssemblyId = variantAssembly.AssemblyId,
                        SortOrder = sortOrder++,
                        VariantAssembly = variantAssembly
                    };
                    Variants.Add(variant);
                }
                
                // Notify property changes
                OnPropertyChanged(nameof(TotalMaterialCost));
                OnPropertyChanged(nameof(TotalLaborMinutes));
                OnPropertyChanged(nameof(TotalLaborHours));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading assembly details: {ex.Message}");
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
            // Set up for creating new assembly
            SelectedAssembly = null;
            EditCode = "";
            EditName = "";
            EditDescription = "";
            EditCategory = "General"; // Default category
            EditRoughMinutes = 0;
            EditFinishMinutes = 0;
            EditServiceMinutes = 0;
            EditExtraMinutes = 0;
            IsEditingAssembly = true;
        }
        
        private void EditAssembly()
        {
            if (SelectedAssembly == null) return;
            
            EditCode = SelectedAssembly.AssemblyCode;
            EditName = SelectedAssembly.Name;
            EditDescription = SelectedAssembly.Description;
            EditCategory = SelectedAssembly.Category;
            EditRoughMinutes = SelectedAssembly.RoughMinutes;
            EditFinishMinutes = SelectedAssembly.FinishMinutes;
            EditServiceMinutes = SelectedAssembly.ServiceMinutes;
            EditExtraMinutes = SelectedAssembly.ExtraMinutes;
            IsEditingAssembly = true;
        }
        
        private void SaveAssembly()
        {
            try
            {
                if (SelectedAssembly == null)
                {
                    // Creating new assembly
                    var newAssembly = new AssemblyTemplate
                    {
                        AssemblyCode = EditCode,
                        Name = EditName,
                        Description = EditDescription,
                        Category = EditCategory,
                        RoughMinutes = EditRoughMinutes,
                        FinishMinutes = EditFinishMinutes,
                        ServiceMinutes = EditServiceMinutes,
                        ExtraMinutes = EditExtraMinutes,
                        IsDefault = Assemblies.Count == 0 || !Assemblies.Any(a => a.AssemblyCode == EditCode),
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System" // TODO: Get current user
                    };
                    
                    _databaseService.SaveAssembly(newAssembly);
                    Assemblies.Add(newAssembly);
                    SelectedAssembly = newAssembly;
                }
                else
                {
                    // Updating existing assembly
                    SelectedAssembly.AssemblyCode = EditCode;
                    SelectedAssembly.Name = EditName;
                    SelectedAssembly.Description = EditDescription;
                    SelectedAssembly.Category = EditCategory;
                    SelectedAssembly.RoughMinutes = EditRoughMinutes;
                    SelectedAssembly.FinishMinutes = EditFinishMinutes;
                    SelectedAssembly.ServiceMinutes = EditServiceMinutes;
                    SelectedAssembly.ExtraMinutes = EditExtraMinutes;
                    
                    _databaseService.UpdateAssembly(SelectedAssembly);
                    
                    // Refresh the display
                    OnPropertyChanged(nameof(SelectedAssembly));
                    OnPropertyChanged(nameof(TotalLaborMinutes));
                    OnPropertyChanged(nameof(TotalLaborHours));
                }
                
                IsEditingAssembly = false;
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error saving assembly: {ex.Message}");
            }
        }
        
        private void CancelEditAssembly()
        {
            IsEditingAssembly = false;
        }
        
        private void DeleteAssembly()
        {
            if (SelectedAssembly == null || SelectedAssembly.IsDefault) return;
            
            try
            {
                // Need to implement delete in database service
                // For now, just remove from collection
                Assemblies.Remove(SelectedAssembly);
                SelectedAssembly = null;
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error deleting assembly: {ex.Message}");
            }
        }
        
        private void DuplicateAssembly()
        {
            if (SelectedAssembly == null) return;
            
            try
            {
                var newAssembly = new AssemblyTemplate
                {
                    AssemblyCode = $"{SelectedAssembly.AssemblyCode}-copy",
                    Name = $"{SelectedAssembly.Name} (Copy)",
                    Description = SelectedAssembly.Description,
                    Category = SelectedAssembly.Category,
                    RoughMinutes = SelectedAssembly.RoughMinutes,
                    FinishMinutes = SelectedAssembly.FinishMinutes,
                    ServiceMinutes = SelectedAssembly.ServiceMinutes,
                    ExtraMinutes = SelectedAssembly.ExtraMinutes,
                    IsDefault = false,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System" // TODO: Get current user
                };
                
                // Save the new assembly
                _databaseService.SaveAssembly(newAssembly);
                
                // Copy components
                var components = _databaseService.GetAssemblyComponents(SelectedAssembly.AssemblyId);
                foreach (var component in components)
                {
                    var newComponent = new AssemblyComponent
                    {
                        AssemblyId = newAssembly.AssemblyId,
                        MaterialId = component.MaterialId,
                        Quantity = component.Quantity,
                        IsOptional = component.IsOptional
                    };
                    _databaseService.SaveAssemblyComponent(newComponent);
                }
                
                // Reload data
                LoadData();
                SelectedAssembly = newAssembly;
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error duplicating assembly: {ex.Message}");
            }
        }
        
        private void AddComponent()
        {
            if (SelectedAssembly == null || SelectedMaterial == null) return;
            
            SelectedComponent = null;
            EditQuantity = 1;
            IsEditingComponent = true;
        }
        
        private void EditComponent()
        {
            if (SelectedComponent == null) return;
            
            SelectedMaterial = AvailableMaterials.FirstOrDefault(m => m.MaterialId == SelectedComponent.MaterialId);
            EditQuantity = SelectedComponent.Quantity;
            IsEditingComponent = true;
        }
        
        private void SaveComponent()
        {
            if (SelectedAssembly == null || SelectedMaterial == null) return;
            
            try
            {
                if (SelectedComponent == null)
                {
                    // Adding new component
                    var newComponent = new AssemblyComponent
                    {
                        AssemblyId = SelectedAssembly.AssemblyId,
                        MaterialId = SelectedMaterial.MaterialId,
                        Quantity = EditQuantity,
                        IsOptional = false,
                        Material = SelectedMaterial // Set the material reference
                    };
                    
                    // Save to database and get the generated ComponentId
                    _databaseService.SaveAssemblyComponent(newComponent);
                    
                    // Reload the component from database to get the ComponentId
                    var savedComponent = _databaseService.GetAssemblyComponents(SelectedAssembly.AssemblyId)
                        .FirstOrDefault(c => c.MaterialId == newComponent.MaterialId && 
                                           c.Quantity == newComponent.Quantity);
                    
                    if (savedComponent != null)
                    {
                        Components.Add(savedComponent);
                    }
                }
                else
                {
                    // Updating existing component
                    SelectedComponent.MaterialId = SelectedMaterial.MaterialId;
                    SelectedComponent.Material = SelectedMaterial;
                    SelectedComponent.Quantity = EditQuantity;
                    
                    _databaseService.UpdateAssemblyComponent(SelectedComponent);
                    
                    // Refresh display by reloading components
                    LoadAssemblyDetails();
                }
                
                IsEditingComponent = false;
                SelectedMaterial = null; // Clear selection
                OnPropertyChanged(nameof(TotalMaterialCost));
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error saving component: {ex.Message}");
            }
        }
        
        private void CancelEditComponent()
        {
            IsEditingComponent = false;
            SelectedMaterial = null; // Clear selection
        }
        
        private void DeleteComponent()
        {
            if (SelectedComponent == null) return;
            
            try
            {
                _databaseService.DeleteAssemblyComponent(SelectedComponent.ComponentId);
                Components.Remove(SelectedComponent);
                SelectedComponent = null;
                OnPropertyChanged(nameof(TotalMaterialCost));
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error deleting component: {ex.Message}");
            }
        }
        
        private void CreateVariant()
        {
            if (SelectedAssembly == null) return;
            
            try
            {
                // TODO: Show dialog to get variant details
                // For now, create a simple variant
                var variantNumber = Variants.Count + 1;
                var newVariant = new AssemblyTemplate
                {
                    AssemblyCode = SelectedAssembly.AssemblyCode, // Keep same code
                    Name = $"{SelectedAssembly.Name} - Variant {variantNumber}",
                    Description = $"Variant {variantNumber} of {SelectedAssembly.Name}",
                    Category = SelectedAssembly.Category,
                    RoughMinutes = SelectedAssembly.RoughMinutes,
                    FinishMinutes = SelectedAssembly.FinishMinutes,
                    ServiceMinutes = SelectedAssembly.ServiceMinutes,
                    ExtraMinutes = SelectedAssembly.ExtraMinutes,
                    IsDefault = false,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System" // TODO: Get current user
                };
                
                // Save the variant assembly
                _databaseService.SaveAssembly(newVariant);
                
                // Create the variant relationship
                _databaseService.CreateAssemblyVariantRelationship(SelectedAssembly.AssemblyId, newVariant.AssemblyId);
                
                // Reload assembly details
                LoadAssemblyDetails();
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error creating variant: {ex.Message}");
            }
        }
        
        private void SetDefaultVariant()
        {
            if (SelectedVariant == null || IsVariantDefault(SelectedVariant)) return;
            
            try
            {
                // Update all variants to set the selected one as default
                foreach (var variant in Variants)
                {
                    if (variant.VariantAssembly != null)
                    {
                        variant.VariantAssembly.IsDefault = (variant.VariantAssemblyId == SelectedVariant.VariantAssemblyId);
                        _databaseService.UpdateAssembly(variant.VariantAssembly);
                    }
                }
                
                // Refresh display
                LoadAssemblyDetails();
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error setting default variant: {ex.Message}");
            }
        }
        
        private void DeleteVariant()
        {
            if (SelectedVariant == null || IsVariantDefault(SelectedVariant)) return;
            
            try
            {
                // TODO: Implement delete variant in database
                Variants.Remove(SelectedVariant);
                SelectedVariant = null;
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error deleting variant: {ex.Message}");
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
