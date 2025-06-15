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
        private decimal _monthlyOverhead;
        private decimal _totalMonthlyHours = 160; // Default to 160 hours/month
        private string _statusMessage;

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
                UpdateStatistics();
            }
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                // Unsubscribe from previous employee's property changes
                if (_selectedEmployee != null)
                {
                    _selectedEmployee.PropertyChanged -= OnEmployeePropertyChanged;
                }
                
                _selectedEmployee = value;
                
                // Subscribe to new employee's property changes
                if (_selectedEmployee != null)
                {
                    _selectedEmployee.PropertyChanged += OnEmployeePropertyChanged;
                }
                
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEmployeeSelected));
                UpdateCalculatedFields();
            }
        }

        private void OnEmployeePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Auto-save when specific properties change
            if (e.PropertyName == nameof(Employee.HourlyRate) || 
                e.PropertyName == nameof(Employee.BurdenRate) ||
                e.PropertyName == nameof(Employee.VehicleCostPerHour) ||
                e.PropertyName == nameof(Employee.OverheadPercentage))
            {
                var employee = sender as Employee;
                if (employee != null)
                {
                    SaveEmployeeData(employee);
                }
            }
            
            // Update calculated fields when relevant properties change
            UpdateCalculatedFields();
            UpdateStatistics();
        }

        private void SaveEmployeeData(Employee employee)
        {
            try
            {
                _databaseService.UpdateEmployee(employee);
                StatusMessage = $"Auto-saved {employee.Name}'s rates";
                
                // Update the effective rate calculation after save
                employee.OnPropertyChanged(nameof(employee.EffectiveRate));
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-saving employee: {ex.Message}");
                StatusMessage = $"Error auto-saving {employee.Name}";
            }
        }

        public decimal MonthlyOverhead
        {
            get => _monthlyOverhead;
            set
            {
                _monthlyOverhead = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OverheadPerHour));
            }
        }

        public decimal TotalMonthlyHours
        {
            get => _totalMonthlyHours;
            set
            {
                _totalMonthlyHours = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OverheadPerHour));
            }
        }

        public decimal OverheadPerHour => TotalMonthlyHours > 0 ? MonthlyOverhead / TotalMonthlyHours : 0;

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int ActiveEmployeeCount => Employees?.Count(e => e.Status == "Active") ?? 0;
        
        public decimal AverageEffectiveRate
        {
            get
            {
                var activeEmployees = Employees?.Where(e => e.Status == "Active").ToList();
                if (activeEmployees == null || !activeEmployees.Any()) return 0;
                return activeEmployees.Average(e => e.EffectiveRate);
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
        public ICommand SaveEmployeeCommand { get; private set; }
        public ICommand AddEmployeeCommand { get; private set; }
        public ICommand DeleteEmployeeCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand CalculateEffectiveRatesCommand { get; private set; }
        public ICommand ApplyOverheadToAllCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveEmployeeCommand = new RelayCommand(SaveEmployee);
            AddEmployeeCommand = new RelayCommand(AddEmployee);
            DeleteEmployeeCommand = new RelayCommand(DeleteEmployee);
            RefreshCommand = new RelayCommand(RefreshEmployees);
            CalculateEffectiveRatesCommand = new RelayCommand(CalculateEffectiveRates);
            ApplyOverheadToAllCommand = new RelayCommand(ApplyOverheadToAll);
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = _databaseService.GetAllEmployees();
                
                // Subscribe to property changes for each employee
                foreach (var employee in employees)
                {
                    employee.PropertyChanged += OnEmployeePropertyChanged;
                }
                
                Employees = new ObservableCollection<Employee>(employees.OrderBy(e => e.Name));
                StatusMessage = $"Loaded {employees.Count} employees";
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex.Message}");
                Employees = new ObservableCollection<Employee>();
                StatusMessage = "Error loading employees";
            }
        }

        private void SaveEmployee(object parameter)
        {
            var employee = parameter as Employee ?? SelectedEmployee;
            if (employee == null) return;

            try
            {
                _databaseService.UpdateEmployee(employee);
                StatusMessage = $"Saved {employee.Name} successfully";
                
                // Update the effective rate calculation after save
                employee.OnPropertyChanged(nameof(employee.EffectiveRate));
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving employee: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = $"Error saving {employee.Name}";
            }
        }

        private void AddEmployee(object parameter)
        {
            var newEmployee = new Employee
            {
                Name = "New Employee",
                HourlyRate = 0,
                BurdenRate = 0,
                VehicleCostPerHour = 0,
                OverheadPercentage = 0,
                Status = "Active"
            };

            try
            {
                _databaseService.SaveEmployee(newEmployee);
                
                // Subscribe to property changes
                newEmployee.PropertyChanged += OnEmployeePropertyChanged;
                
                LoadEmployees();
                
                // Select the new employee
                SelectedEmployee = Employees.FirstOrDefault(e => e.EmployeeId == newEmployee.EmployeeId);
                StatusMessage = "Added new employee";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error adding employee: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                StatusMessage = "Error adding employee";
            }
        }

        private void DeleteEmployee(object parameter)
        {
            var employee = parameter as Employee ?? SelectedEmployee;
            if (employee == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete employee '{employee.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.DeleteEmployee(employee.EmployeeId);
                    
                    // Unsubscribe from property changes
                    employee.PropertyChanged -= OnEmployeePropertyChanged;
                    
                    LoadEmployees();
                    if (SelectedEmployee?.EmployeeId == employee.EmployeeId)
                    {
                        SelectedEmployee = null;
                    }
                    StatusMessage = $"Deleted {employee.Name}";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deleting employee: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    StatusMessage = "Error deleting employee";
                }
            }
        }

        private void RefreshEmployees(object parameter)
        {
            LoadEmployees();
        }

        private void CalculateEffectiveRates(object parameter)
        {
            foreach (var employee in Employees)
            {
                employee.OnPropertyChanged(nameof(employee.EffectiveRate));
            }
            UpdateStatistics();
            StatusMessage = "Recalculated effective rates for all employees";
        }

        private void ApplyOverheadToAll(object parameter)
        {
            if (TotalMonthlyHours <= 0)
            {
                System.Windows.MessageBox.Show("Total monthly hours must be greater than 0", "Invalid Input", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var overheadPerHour = MonthlyOverhead / TotalMonthlyHours;
            
            foreach (var employee in Employees.Where(e => e.Status == "Active"))
            {
                // Calculate overhead as a percentage of base rate
                if (employee.HourlyRate > 0)
                {
                    employee.OverheadPercentage = (overheadPerHour / employee.HourlyRate) * 100;
                }
            }
            
            // Save all changes
            foreach (var employee in Employees.Where(e => e.Status == "Active"))
            {
                try
                {
                    _databaseService.UpdateEmployee(employee);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating employee {employee.Name}: {ex.Message}");
                }
            }
            
            UpdateStatistics();
            StatusMessage = $"Applied overhead of ${overheadPerHour:F2}/hour to all active employees";
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

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(ActiveEmployeeCount));
            OnPropertyChanged(nameof(AverageEffectiveRate));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}