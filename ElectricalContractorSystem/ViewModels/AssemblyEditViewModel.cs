using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyEditViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private AssemblyTemplate _assembly;
        private bool _isNewAssembly;
        private string _windowTitle;

        public AssemblyEditViewModel(DatabaseService databaseService, AssemblyTemplate assembly = null)
        {
            _databaseService = databaseService;
            _isNewAssembly = assembly == null;
            
            if (_isNewAssembly)
            {
                Assembly = new AssemblyTemplate
                {
                    IsActive = true,
                    IsDefault = true,
                    CreatedBy = "System",
                    CreatedDate = DateTime.Now
                };
                WindowTitle = "Create New Assembly";
            }
            else
            {
                // Clone the assembly to avoid modifying the original
                Assembly = new AssemblyTemplate
                {
                    AssemblyId = assembly.AssemblyId,
                    AssemblyCode = assembly.AssemblyCode,
                    Name = assembly.Name,
                    Description = assembly.Description,
                    Category = assembly.Category,
                    RoughMinutes = assembly.RoughMinutes,
                    FinishMinutes = assembly.FinishMinutes,
                    ServiceMinutes = assembly.ServiceMinutes,
                    ExtraMinutes = assembly.ExtraMinutes,
                    IsDefault = assembly.IsDefault,
                    IsActive = assembly.IsActive,
                    CreatedBy = assembly.CreatedBy,
                    CreatedDate = assembly.CreatedDate,
                    UpdatedBy = assembly.UpdatedBy,
                    UpdatedDate = assembly.UpdatedDate
                };
                
                // Load components
                LoadComponents();
                WindowTitle = $"Edit Assembly - {assembly.Name}";
            }

            LoadPriceListItems();
            InitializeCommands();
        }

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public AssemblyTemplate Assembly
        {
            get => _assembly;
            set => SetProperty(ref _assembly, value);
        }

        public bool IsNewAssembly => _isNewAssembly;

        public ObservableCollection<AssemblyComponent> Components { get; } = new ObservableCollection<AssemblyComponent>();
        public ObservableCollection<PriceListItem> AvailableItems { get; } = new ObservableCollection<PriceListItem>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>
        {
            "Devices",
            "Lighting",
            "Rough-In",
            "Service",
            "Special",
            "Other"
        };

        private PriceListItem _selectedAvailableItem;
        public PriceListItem SelectedAvailableItem
        {
            get => _selectedAvailableItem;
            set => SetProperty(ref _selectedAvailableItem, value);
        }

        private AssemblyComponent _selectedComponent;
        public AssemblyComponent SelectedComponent
        {
            get => _selectedComponent;
            set => SetProperty(ref _selectedComponent, value);
        }

        private decimal _newComponentQuantity = 1;
        public decimal NewComponentQuantity
        {
            get => _newComponentQuantity;
            set => SetProperty(ref _newComponentQuantity, value);
        }

        public decimal TotalMaterialCost => Components.Sum(c => c.TotalCost);
        public decimal TotalLaborMinutes => Assembly.RoughMinutes + Assembly.FinishMinutes + Assembly.ServiceMinutes + Assembly.ExtraMinutes;
        public decimal TotalLaborHours => TotalLaborMinutes / 60m;

        public bool DialogResult { get; private set; }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand AddComponentCommand { get; private set; }
        public ICommand RemoveComponentCommand { get; private set; }
        public ICommand UpdateComponentQuantityCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddComponentCommand = new RelayCommand(AddComponent, CanAddComponent);
            RemoveComponentCommand = new RelayCommand(RemoveComponent, CanRemoveComponent);
            UpdateComponentQuantityCommand = new RelayCommand(UpdateComponentQuantity);
        }

        #endregion

        #region Methods

        private void LoadComponents()
        {
            Components.Clear();
            if (!_isNewAssembly && Assembly.AssemblyId > 0)
            {
                var components = _databaseService.GetAssemblyComponents(Assembly.AssemblyId);
                foreach (var component in components)
                {
                    Components.Add(component);
                }
            }
        }

        private void LoadPriceListItems()
        {
            AvailableItems.Clear();
            var items = _databaseService.GetAllPriceListItems()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name);
                
            foreach (var item in items)
            {
                AvailableItems.Add(item);
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Assembly?.AssemblyCode) &&
                   !string.IsNullOrWhiteSpace(Assembly?.Name) &&
                   !string.IsNullOrWhiteSpace(Assembly?.Category);
        }

        private void Save()
        {
            try
            {
                if (_isNewAssembly)
                {
                    // Check if code already exists
                    var existing = _databaseService.GetAssemblyVariants(Assembly.AssemblyCode);
                    if (existing.Any())
                    {
                        // This is a variant of an existing assembly
                        Assembly.IsDefault = false;
                    }
                }
                else
                {
                    Assembly.UpdatedBy = "System";
                    Assembly.UpdatedDate = DateTime.Now;
                }

                // Save the assembly
                _databaseService.SaveAssembly(Assembly);

                // Save components
                if (_isNewAssembly)
                {
                    foreach (var component in Components)
                    {
                        component.AssemblyId = Assembly.AssemblyId;
                        _databaseService.SaveAssemblyComponent(component);
                    }
                }
                else
                {
                    // For existing assembly, we need to handle adds/updates/deletes
                    var existingComponents = _databaseService.GetAssemblyComponents(Assembly.AssemblyId);
                    
                    // Remove deleted components
                    foreach (var existing in existingComponents)
                    {
                        if (!Components.Any(c => c.ComponentId == existing.ComponentId))
                        {
                            _databaseService.DeleteAssemblyComponent(existing.ComponentId);
                        }
                    }

                    // Add new or update existing
                    foreach (var component in Components)
                    {
                        if (component.ComponentId == 0)
                        {
                            component.AssemblyId = Assembly.AssemblyId;
                            _databaseService.SaveAssemblyComponent(component);
                        }
                        else
                        {
                            _databaseService.UpdateAssemblyComponent(component);
                        }
                    }
                }

                DialogResult = true;
                CloseWindow();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving assembly: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            DialogResult = false;
            CloseWindow();
        }

        private bool CanAddComponent()
        {
            return SelectedAvailableItem != null && NewComponentQuantity > 0 &&
                   !Components.Any(c => c.PriceListItemId == SelectedAvailableItem.ItemId);
        }

        private void AddComponent()
        {
            if (SelectedAvailableItem == null) return;

            var component = new AssemblyComponent
            {
                AssemblyId = Assembly.AssemblyId,
                PriceListItemId = SelectedAvailableItem.ItemId,
                Quantity = NewComponentQuantity,
                ItemName = SelectedAvailableItem.Name,
                ItemCode = SelectedAvailableItem.MaterialCode,  // Changed from ItemCode to MaterialCode
                UnitPrice = SelectedAvailableItem.BaseCost
            };

            Components.Add(component);
            OnPropertyChanged(nameof(TotalMaterialCost));
            
            // Reset
            NewComponentQuantity = 1;
            SelectedAvailableItem = null;
        }

        private bool CanRemoveComponent()
        {
            return SelectedComponent != null;
        }

        private void RemoveComponent()
        {
            if (SelectedComponent == null) return;

            Components.Remove(SelectedComponent);
            OnPropertyChanged(nameof(TotalMaterialCost));
        }

        private void UpdateComponentQuantity(object parameter)
        {
            OnPropertyChanged(nameof(TotalMaterialCost));
        }

        private void CloseWindow()
        {
            // This will be hooked up to close the actual window
            if (System.Windows.Application.Current.Windows.Count > 0)
            {
                var window = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
        }

        #endregion
    }
}
