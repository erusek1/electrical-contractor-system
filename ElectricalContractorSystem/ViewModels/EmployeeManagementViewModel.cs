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
    public class EmployeeManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Employee> _employees;
        private Employee _selectedEmployee;

        public EmployeeManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadEmployees();
            InitializeCommands();
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set
            {
                _employees = value;
                OnPropertyChanged();
            }
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmployeeSelected));
                UpdateCalculatedFields();
            }
        }

        public bool IsEmployeeSelected => SelectedEmployee != null;

        // Calculated properties for display
        public decimal? TotalHourlyCost => SelectedEmployee?.TotalHourlyCost;
        public decimal? YearlyLaborCost => SelectedEmployee?.YearlyLaborCost;
        public decimal? YearlyVehicleCost => SelectedEmployee?.YearlyVehicleCost;
        public decimal? YearlyOverheadCost => SelectedEmployee?.YearlyOverheadCost;
        public decimal? TotalYearlyCost => SelectedEmployee?.TotalYearlyCost;
        public decimal? CostPerBillableHour => SelectedEmployee?.CostPerBillableHour;

        // Commands
        public ICommand SaveCommand { get; private set; }
        public ICommand AddEmployeeCommand { get; private set; }
        public ICommand DeleteEmployeeCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(SaveEmployee, CanSaveEmployee);
            AddEmployeeCommand = new RelayCommand(AddEmployee);
            DeleteEmployeeCommand = new RelayCommand(DeleteEmployee, CanDeleteEmployee);
            RefreshCommand = new RelayCommand(RefreshEmployees);
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = _databaseService.GetAllEmployees();
                Employees = new ObservableCollection<Employee>(employees.OrderBy(e => e.Name));
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex.Message}");
                Employees = new ObservableCollection<Employee>();
            }
        }

        private void SaveEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            try
            {
                _databaseService.UpdateEmployee(SelectedEmployee);
                LoadEmployees(); // Reload to ensure consistency
                System.Windows.MessageBox.Show("Employee information saved successfully.", "Success", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving employee: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanSaveEmployee(object parameter)
        {
            return SelectedEmployee != null;
        }

        private void AddEmployee(object parameter)
        {
            var newEmployee = new Employee
            {
                Name = "New Employee",
                HourlyRate = 0,
                BurdenRate = 0,
                VehicleCostPerMonth = 0,
                OverheadPercentage = 10,
                Status = "Active"
            };

            _databaseService.SaveEmployee(newEmployee);
            LoadEmployees();
            
            // Select the new employee
            SelectedEmployee = Employees.FirstOrDefault(e => e.EmployeeId == newEmployee.EmployeeId);
        }

        private void DeleteEmployee(object parameter)
        {
            if (SelectedEmployee == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete employee '{SelectedEmployee.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _databaseService.DeleteEmployee(SelectedEmployee.EmployeeId);
                LoadEmployees();
                SelectedEmployee = null;
            }
        }

        private bool CanDeleteEmployee(object parameter)
        {
            return SelectedEmployee != null;
        }

        private void RefreshEmployees(object parameter)
        {
            LoadEmployees();
        }

        private void UpdateCalculatedFields()
        {
            OnPropertyChanged(nameof(TotalHourlyCost));
            OnPropertyChanged(nameof(YearlyLaborCost));
            OnPropertyChanged(nameof(YearlyVehicleCost));
            OnPropertyChanged(nameof(YearlyOverheadCost));
            OnPropertyChanged(nameof(TotalYearlyCost));
            OnPropertyChanged(nameof(CostPerBillableHour));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}