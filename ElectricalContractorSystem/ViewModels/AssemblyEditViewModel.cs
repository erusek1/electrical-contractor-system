using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.ViewModels
{
    public class AssemblyEditViewModel : INotifyPropertyChanged
    {
        private AssemblyTemplate _assembly;
        private ObservableCollection<string> _categories;
        private bool _isNew;

        public AssemblyEditViewModel(AssemblyTemplate assembly = null)
        {
            _isNew = assembly == null;
            _assembly = assembly ?? new AssemblyTemplate
            {
                AssemblyCode = "",
                Name = "",
                Category = "Outlets/Switches",
                RoughMinutes = 0,
                FinishMinutes = 0,
                ServiceMinutes = 0,
                ExtraMinutes = 0,
                IsDefault = false,
                IsActive = true,
                CreatedBy = Environment.UserName,
                CreatedDate = DateTime.Now
            };

            LoadCategories();
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        public AssemblyTemplate Assembly
        {
            get => _assembly;
            set
            {
                _assembly = value;
                OnPropertyChanged();
            }
        }

        public string AssemblyCode
        {
            get => _assembly.AssemblyCode;
            set
            {
                _assembly.AssemblyCode = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _assembly.Name;
            set
            {
                _assembly.Name = value;
                OnPropertyChanged();
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
                OnPropertyChanged(nameof(TotalMinutes));
            }
        }

        public int FinishMinutes
        {
            get => _assembly.FinishMinutes;
            set
            {
                _assembly.FinishMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
            }
        }

        public int ServiceMinutes
        {
            get => _assembly.ServiceMinutes;
            set
            {
                _assembly.ServiceMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
            }
        }

        public int ExtraMinutes
        {
            get => _assembly.ExtraMinutes;
            set
            {
                _assembly.ExtraMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMinutes));
            }
        }

        public int TotalMinutes => RoughMinutes + FinishMinutes + ServiceMinutes + ExtraMinutes;

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

        public ObservableCollection<string> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged();
            }
        }

        public bool IsNew => _isNew;
        public string Title => IsNew ? "New Assembly" : $"Edit Assembly: {_assembly.Name}";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public bool? DialogResult { get; set; }

        private void LoadCategories()
        {
            Categories = new ObservableCollection<string>
            {
                "Outlets/Switches",
                "Lighting",
                "Panels/Service",
                "Low Voltage",
                "HVAC",
                "Special Systems",
                "Other"
            };
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(AssemblyCode) &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Category);
        }

        private void Save()
        {
            if (!_isNew)
            {
                _assembly.UpdatedBy = Environment.UserName;
                _assembly.UpdatedDate = DateTime.Now;
            }

            DialogResult = true;
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
