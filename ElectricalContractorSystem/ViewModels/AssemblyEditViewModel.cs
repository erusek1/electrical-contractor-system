using System;
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
        private DatabaseService _databaseService;
        private AssemblyTemplate _assembly;
        private ObservableCollection<Material> _availableMaterials;
        private bool _isEditMode;
        private string _title;

        public AssemblyEditViewModel(DatabaseService databaseService, AssemblyTemplate assembly = null)
        {
            _databaseService = databaseService;
            _assembly = assembly ?? new AssemblyTemplate
            {
                Components = new ObservableCollection<AssemblyComponent>(),
                IsActive = true,
                IsDefault = false,
                Category = "Devices",
                CreatedBy = "System",
                CreatedDate = DateTime.Now
            };
            
            _isEditMode = assembly != null;
            _title = _isEditMode ? "Edit Assembly" : "Create New Assembly";
            
            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddComponentCommand = new RelayCommand(AddComponent);
            RemoveComponentCommand = new RelayCommand<AssemblyComponent>(RemoveComponent);
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get => _assembly.AssemblyCode;
            set
            {
                _assembly.AssemblyCode = value;
                OnPropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public string Name
        {
            get => _assembly.Name;
            set
            {
                _assembly.Name = value;
                OnPropertyChanged();
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public string Description
        {
            get => _assembly.Description;
            set
            {
                _assembly.Description = value;
                OnPropertyChanged();
            }
        }

        public string Category
        {
            get => _assembly.Category;
            set
            {
                _assembly.Category = value;
                OnPropertyChanged();
            }
        }

        public int RoughMinutes
        {
            get => _assembly.RoughMinutes;
            set
            {
                _assembly.RoughMinutes = value;
                OnPropertyChanged();
            }
        }

        public int FinishMinutes
        {
            get => _assembly.FinishMinutes;
            set
            {
                _assembly.FinishMinutes = value;
                OnPropertyChanged();
            }
        }

        public int ServiceMinutes
        {
            get => _assembly.ServiceMinutes;
            set
            {
                _assembly.ServiceMinutes = value;
                OnPropertyChanged();
            }
        }

        public int ExtraMinutes
        {
            get => _assembly.ExtraMinutes;
            set
            {
                _assembly.ExtraMinutes = value;
                OnPropertyChanged();
            }
        }

        public bool IsDefault
        {
            get => _assembly.IsDefault;
            set
            {
                _assembly.IsDefault = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _assembly.IsActive;
            set
            {
                _assembly.IsActive = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<AssemblyComponent> Components
        {
            get
            {
                if (_assembly.Components == null)
                {
                    _assembly.Components = new ObservableCollection<AssemblyComponent>();
                }
                else if (!(_assembly.Components is ObservableCollection<AssemblyComponent>))
                {
                    _assembly.Components = new ObservableCollection<AssemblyComponent>(_assembly.Components);
                }
                return (ObservableCollection<AssemblyComponent>)_assembly.Components;
            }
            set
            {
                _assembly.Components = value;
                OnPropertyChanged();
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

        public AssemblyTemplate Assembly => _assembly;

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddComponentCommand { get; }
        public ICommand RemoveComponentCommand { get; }

        // Dialog result
        public bool? DialogResult { get; set; }
        
        // Event to request close
        public event EventHandler RequestClose;

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Code) && 
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Category);
        }

        private void Save()
        {
            try
            {
                // Save the assembly
                if (_isEditMode)
                {
                    _assembly.UpdatedBy = "System";
                    _assembly.UpdatedDate = DateTime.Now;
                }
                
                _databaseService.SaveAssembly(_assembly);
                
                DialogResult = true;
                RequestClose?.Invoke(this, EventArgs.Empty);
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
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void AddComponent()
        {
            // This would typically show a dialog to select a material
            // For now, just add a placeholder
            var component = new AssemblyComponent
            {
                AssemblyId = _assembly.AssemblyId,
                Quantity = 1,
                IsOptional = false
            };
            
            Components.Add(component);
        }

        private void RemoveComponent(AssemblyComponent component)
        {
            if (component != null)
            {
                Components.Remove(component);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
