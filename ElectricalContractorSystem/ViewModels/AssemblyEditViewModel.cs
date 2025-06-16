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
    public class AssemblyEditViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly AssemblyService _assemblyService;
        private readonly PricingService _pricingService;
        private AssemblyTemplate _originalAssembly;
        private bool _isNewAssembly;
        private bool _dialogResult;

        public AssemblyEditViewModel(DatabaseService databaseService, AssemblyTemplate assembly = null)
        {
            _databaseService = databaseService;
            _assemblyService = new AssemblyService(databaseService);
            _pricingService = new PricingService(databaseService);
            
            _isNewAssembly = assembly == null;
            _originalAssembly = assembly;

            InitializeCommands();
            InitializeData();
            
            if (assembly != null)
            {
                LoadAssembly(assembly);
            }
            else
            {
                // Set defaults for new assembly
                Code = "";
                Name = "";
                Description = "";
                RoughMinutes = 0;
                FinishMinutes = 0;
                ServiceMinutes = 0;
                ExtraMinutes = 0;
                IsDefault = true;
                ComponentQuantity = 1;
            }
        }

        // Properties
        private string _windowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        private string _code;
        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); UpdateWindowTitle(); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); UpdateWindowTitle(); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private int _roughMinutes;
        public int RoughMinutes
        {
            get => _roughMinutes;
            set { _roughMinutes = value; OnPropertyChanged(); }
        }

        private int _finishMinutes;
        public int FinishMinutes
        {
            get => _finishMinutes;
            set { _finishMinutes = value; OnPropertyChanged(); }
        }

        private int _serviceMinutes;
        public int ServiceMinutes
        {
            get => _serviceMinutes;
            set { _serviceMinutes = value; OnPropertyChanged(); }
        }

        private int _extraMinutes;
        public int ExtraMinutes
        {
            get => _extraMinutes;
            set { _extraMinutes = value; OnPropertyChanged(); }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get => _isDefault;
            set { _isDefault = value; OnPropertyChanged(); }
        }

        public bool IsNewAssembly => _isNewAssembly;

        public bool ShowVariantsSection => !_isNewAssembly && !string.IsNullOrEmpty(Code);

        // Material Selection
        private ObservableCollection<Material> _allMaterials;
        public ObservableCollection<Material> AllMaterials
        {
            get => _allMaterials;
            set { _allMaterials = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Material> _filteredMaterials;
        public ObservableCollection<Material> FilteredMaterials
        {
            get => _filteredMaterials;
            set { _filteredMaterials = value; OnPropertyChanged(); }
        }

        private string _materialSearchText;
        public string MaterialSearchText
        {
            get => _materialSearchText;
            set 
            { 
                _materialSearchText = value; 
                OnPropertyChanged(); 
                FilterMaterials();
            }
        }

        private ObservableCollection<string> _materialCategories;
        public ObservableCollection<string> MaterialCategories
        {
            get => _materialCategories;
            set { _materialCategories = value; OnPropertyChanged(); }
        }

        private string _selectedMaterialCategory;
        public string SelectedMaterialCategory
        {
            get => _selectedMaterialCategory;
            set 
            { 
                _selectedMaterialCategory = value; 
                OnPropertyChanged(); 
                FilterMaterials();
            }
        }

        private Material _selectedMaterial;
        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set { _selectedMaterial = value; OnPropertyChanged(); }
        }

        // Assembly Components
        private ObservableCollection<AssemblyComponentViewModel> _components;
        public ObservableCollection<AssemblyComponentViewModel> Components
        {
            get => _components;
            set { _components = value; OnPropertyChanged(); UpdateTotalCost(); }
        }

        private AssemblyComponentViewModel _selectedComponent;
        public AssemblyComponentViewModel SelectedComponent
        {
            get => _selectedComponent;
            set { _selectedComponent = value; OnPropertyChanged(); }
        }

        private decimal _componentQuantity;
        public decimal ComponentQuantity
        {
            get => _componentQuantity;
            set { _componentQuantity = value; OnPropertyChanged(); }
        }

        private decimal _totalMaterialCost;
        public decimal TotalMaterialCost
        {
            get => _totalMaterialCost;
            set { _totalMaterialCost = value; OnPropertyChanged(); }
        }

        // Variants
        private ObservableCollection<AssemblyVariantInfo> _existingVariants;
        public ObservableCollection<AssemblyVariantInfo> ExistingVariants
        {
            get => _existingVariants;
            set { _existingVariants = value; OnPropertyChanged(); }
        }

        public bool DialogResult
        {
            get => _dialogResult;
            set { _dialogResult = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand AddSelectedMaterialCommand { get; set; }
        public ICommand RemoveComponentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand CreateVariantCommand { get; set; }

        private void InitializeCommands()
        {
            AddSelectedMaterialCommand = new RelayCommand(AddSelectedMaterial, CanAddSelectedMaterial);
            RemoveComponentCommand = new RelayCommand(RemoveComponent);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            CreateVariantCommand = new RelayCommand(CreateVariant);
        }

        private void InitializeData()
        {
            Components = new ObservableCollection<AssemblyComponentViewModel>();
            ExistingVariants = new ObservableCollection<AssemblyVariantInfo>();
            
            // Load materials from database
            LoadMaterials();
            
            // Set up property change handlers for components
            Components.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (AssemblyComponentViewModel item in e.NewItems)
                    {
                        item.PropertyChanged += Component_PropertyChanged;
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (AssemblyComponentViewModel item in e.OldItems)
                    {
                        item.PropertyChanged -= Component_PropertyChanged;
                    }
                }
                UpdateTotalCost();
            };
        }

        private void Component_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssemblyComponentViewModel.Quantity))
            {
                UpdateTotalCost();
            }
        }

        private void LoadMaterials()
        {
            try
            {
                var materials = _pricingService.GetAllMaterials();
                AllMaterials = new ObservableCollection<Material>(materials);
                FilteredMaterials = new ObservableCollection<Material>(materials);
                
                // Get unique categories
                var categories = materials.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();
                categories.Insert(0, "All Categories");
                MaterialCategories = new ObservableCollection<string>(categories);
                SelectedMaterialCategory = "All Categories";
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error loading materials: {ex.Message}");
            }
        }

        private void LoadAssembly(AssemblyTemplate assembly)
        {
            Code = assembly.AssemblyCode;
            Name = assembly.Name;
            Description = assembly.Description;
            RoughMinutes = assembly.RoughMinutes;
            FinishMinutes = assembly.FinishMinutes;
            ServiceMinutes = assembly.ServiceMinutes;
            ExtraMinutes = assembly.ExtraMinutes;
            IsDefault = assembly.IsDefault;
            
            // Load components using DatabaseService extension
            var components = _databaseService.GetAssemblyComponents(assembly.AssemblyId);
            foreach (var component in components)
            {
                var material = _databaseService.GetMaterial(component.MaterialId);
                if (material != null)
                {
                    Components.Add(new AssemblyComponentViewModel
                    {
                        Material = material,
                        Quantity = component.Quantity
                    });
                }
            }
            
            // Load variants if this is an existing assembly
            if (!_isNewAssembly)
            {
                LoadVariants();
            }
            
            UpdateWindowTitle();
        }

        private void LoadVariants()
        {
            try
            {
                var variants = _assemblyService.GetAssemblyVariants(Code);
                ExistingVariants.Clear();
                foreach (var variant in variants.Where(v => v.AssemblyId != _originalAssembly?.AssemblyId))
                {
                    var componentCount = _databaseService.GetAssemblyComponents(variant.AssemblyId).Count();
                    ExistingVariants.Add(new AssemblyVariantInfo
                    {
                        AssemblyId = variant.AssemblyId,
                        Name = variant.Name,
                        IsDefault = variant.IsDefault,
                        ComponentCount = componentCount
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading variants: {ex.Message}");
            }
        }

        private void UpdateWindowTitle()
        {
            if (_isNewAssembly)
            {
                WindowTitle = "Create New Assembly";
            }
            else
            {
                WindowTitle = $"Edit Assembly: {Code} - {Name}";
            }
        }

        private void FilterMaterials()
        {
            if (AllMaterials == null) return;
            
            var filtered = AllMaterials.AsEnumerable();
            
            // Filter by category
            if (!string.IsNullOrEmpty(SelectedMaterialCategory) && SelectedMaterialCategory != "All Categories")
            {
                filtered = filtered.Where(m => m.Category == SelectedMaterialCategory);
            }
            
            // Filter by search text
            if (!string.IsNullOrEmpty(MaterialSearchText))
            {
                var searchLower = MaterialSearchText.ToLower();
                filtered = filtered.Where(m => 
                    m.Name.ToLower().Contains(searchLower) ||
                    m.ItemCode.ToLower().Contains(searchLower) ||
                    (m.Description?.ToLower().Contains(searchLower) ?? false));
            }
            
            FilteredMaterials = new ObservableCollection<Material>(filtered.OrderBy(m => m.Name));
        }

        private void UpdateTotalCost()
        {
            TotalMaterialCost = Components.Sum(c => c.TotalCost);
        }

        // Command implementations
        private bool CanAddSelectedMaterial(object parameter)
        {
            return SelectedMaterial != null;
        }

        private void AddSelectedMaterial(object parameter)
        {
            if (SelectedMaterial == null) return;
            
            // Check if material already exists in components
            var existing = Components.FirstOrDefault(c => c.Material.MaterialId == SelectedMaterial.MaterialId);
            if (existing != null)
            {
                // Add to existing quantity
                existing.Quantity += ComponentQuantity;
            }
            else
            {
                // Add new component
                Components.Add(new AssemblyComponentViewModel
                {
                    Material = SelectedMaterial,
                    Quantity = ComponentQuantity
                });
            }
            
            // Reset quantity to 1
            ComponentQuantity = 1;
        }

        private void RemoveComponent(object parameter)
        {
            if (parameter is AssemblyComponentViewModel component)
            {
                component.PropertyChanged -= Component_PropertyChanged;
                Components.Remove(component);
            }
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Code) && 
                   !string.IsNullOrWhiteSpace(Name) &&
                   Components.Count > 0;
        }

        private void Save(object parameter)
        {
            try
            {
                var assembly = new AssemblyTemplate
                {
                    AssemblyId = _originalAssembly?.AssemblyId ?? 0,
                    AssemblyCode = Code.Trim(),
                    Name = Name.Trim(),
                    Description = Description?.Trim(),
                    RoughMinutes = RoughMinutes,
                    FinishMinutes = FinishMinutes,
                    ServiceMinutes = ServiceMinutes,
                    ExtraMinutes = ExtraMinutes,
                    IsDefault = IsDefault,
                    IsActive = true
                };
                
                // Convert component view models to models
                var components = Components.Select(c => new AssemblyComponent
                {
                    MaterialId = c.Material.MaterialId,
                    Quantity = c.Quantity
                }).ToList();
                
                if (_isNewAssembly)
                {
                    _assemblyService.CreateAssembly(assembly, components);
                }
                else
                {
                    _assemblyService.UpdateAssembly(assembly, components);
                }
                
                DialogResult = true;
                CloseDialog();
            }
            catch (Exception ex)
            {
                // Show error message
                System.Windows.MessageBox.Show($"Error saving assembly: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
            CloseDialog();
        }

        private void CreateVariant(object parameter)
        {
            // This would open a new instance of the dialog with the current assembly as a template
            // Implementation depends on how you want to handle this
        }

        private void CloseDialog()
        {
            // This should close the dialog window
            // The actual implementation depends on how you handle dialog closing
            if (System.Windows.Application.Current.Windows.OfType<Views.AssemblyEditDialog>().FirstOrDefault() is Views.AssemblyEditDialog dialog)
            {
                dialog.DialogResult = DialogResult;
                dialog.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper view model for assembly components
    public class AssemblyComponentViewModel : INotifyPropertyChanged
    {
        private Material _material;
        public Material Material
        {
            get => _material;
            set { _material = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCost)); }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalCost)); }
        }

        public decimal TotalCost => Material?.CurrentPrice * Quantity ?? 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for variant info
    public class AssemblyVariantInfo
    {
        public int AssemblyId { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public int ComponentCount { get; set; }
    }
}