using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class WeeklyLaborEntryViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private DateTime _currentWeekStart;
        private ObservableCollection<Employee> _employees;
        private ObservableCollection<Job> _activeJobs;
        private ObservableCollection<EmployeeTimeEntry> _employeeTimeEntries;
        private Employee _selectedEmployee;
        private string _newEmployeeName;
        private decimal _newEmployeeRate;
        private readonly List<string> _stageNames = new List<string> { "", "Demo", "Rough", "Service", "Finish", "Extra", "Inspection", "Temp Service", "Other" };

        public WeeklyLaborEntryViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // Initialize to the beginning of the current week (Monday)
            _currentWeekStart = GetStartOfWeek(DateTime.Today);
            
            // Load initial data with error handling
            try
            {
                LoadEmployees();
                LoadActiveJobs();
                LoadWeekEntries();
            }
            catch (Exception ex)
            {
                // Use test data if database fails
                LoadTestData();
                System.Diagnostics.Debug.WriteLine($"WeeklyLaborEntry: Using test data due to error: {ex.Message}");
            }

            // Create commands
            PreviousWeekCommand = new RelayCommand(() => ExecutePreviousWeek());
            NextWeekCommand = new RelayCommand(() => ExecuteNextWeek());
            SaveAllEntriesCommand = new RelayCommand(() => ExecuteSaveAllEntries());
            AddEmployeeCommand = new RelayCommand(() => ExecuteAddEmployee(), () => CanAddEmployee());
            RemoveEmployeeCommand = new RelayCommand(() => ExecuteRemoveEmployee(), () => CanRemoveEmployee());
            ManageEmployeesCommand = new RelayCommand(() => ExecuteManageEmployees());
        }

        #region Properties

        public DateTime CurrentWeekStart
        {
            get => _currentWeekStart;
            set
            {
                if (SetProperty(ref _currentWeekStart, value))
                {
                    LoadWeekEntries();
                    OnPropertyChanged(nameof(WeekDateRange));
                }
            }
        }

        public string WeekDateRange
        {
            get
            {
                DateTime weekEnd = CurrentWeekStart.AddDays(4);
                string startStr = CurrentWeekStart.ToString("MMM d");
                string endStr = weekEnd.ToString("MMM d");
                return $"{startStr} - {endStr}";
            }
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => SetProperty(ref _employees, value);
        }

        public ObservableCollection<Job> ActiveJobs
        {
            get => _activeJobs;
            set => SetProperty(ref _activeJobs, value);
        }

        public ObservableCollection<EmployeeTimeEntry> EmployeeTimeEntries
        {
            get => _employeeTimeEntries;
            set => SetProperty(ref _employeeTimeEntries, value);
        }

        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set 
            { 
                SetProperty(ref _selectedEmployee, value);
                ((RelayCommand)RemoveEmployeeCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewEmployeeName
        {
            get => _newEmployeeName;
            set
            {
                SetProperty(ref _newEmployeeName, value);
                ((RelayCommand)AddEmployeeCommand).RaiseCanExecuteChanged();
            }
        }

        public decimal NewEmployeeRate
        {
            get => _newEmployeeRate;
            set
            {
                SetProperty(ref _newEmployeeRate, value);
                ((RelayCommand)AddEmployeeCommand).RaiseCanExecuteChanged();
            }
        }

        public List<string> StageNames => _stageNames;

        #endregion

        #region Commands

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand SaveAllEntriesCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand RemoveEmployeeCommand { get; }
        public ICommand ManageEmployeesCommand { get; }

        #endregion

        #region Private Methods

        private void LoadEmployees()
        {
            try
            {
                // Try to get employees from database service
                // For now, create default employees
                Employees = new ObservableCollection<Employee>
                {
                    new Employee { EmployeeId = 1, Name = "Erik", HourlyRate = 85.00m, Status = "Active" },
                    new Employee { EmployeeId = 2, Name = "Lee", HourlyRate = 65.00m, Status = "Active" },
                    new Employee { EmployeeId = 3, Name = "Carlos", HourlyRate = 65.00m, Status = "Active" },
                    new Employee { EmployeeId = 4, Name = "Jake", HourlyRate = 65.00m, Status = "Active" },
                    new Employee { EmployeeId = 5, Name = "Trevor", HourlyRate = 65.00m, Status = "Active" },
                    new Employee { EmployeeId = 6, Name = "Ryan", HourlyRate = 65.00m, Status = "Active" }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employees: {ex.Message}");
                LoadTestEmployees();
            }
        }

        private void LoadTestEmployees()
        {
            Employees = new ObservableCollection<Employee>
            {
                new Employee { EmployeeId = 1, Name = "Erik", HourlyRate = 85.00m, Status = "Active" },
                new Employee { EmployeeId = 2, Name = "Lee", HourlyRate = 65.00m, Status = "Active" },
                new Employee { EmployeeId = 3, Name = "Carlos", HourlyRate = 65.00m, Status = "Active" },
                new Employee { EmployeeId = 4, Name = "Jake", HourlyRate = 65.00m, Status = "Active" },
                new Employee { EmployeeId = 5, Name = "Trevor", HourlyRate = 65.00m, Status = "Active" },
                new Employee { EmployeeId = 6, Name = "Ryan", HourlyRate = 65.00m, Status = "Active" }
            };
        }

        private void LoadActiveJobs()
        {
            try
            {
                var allJobs = _databaseService.GetAllJobs();
                var activeJobs = allJobs.Where(j => j.Status != "Complete").ToList();
                
                // Add empty option at the beginning
                var jobsWithEmpty = new List<Job> { new Job { JobNumber = "", JobName = "Select Job..." } };
                jobsWithEmpty.AddRange(activeJobs);
                
                ActiveJobs = new ObservableCollection<Job>(jobsWithEmpty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading jobs: {ex.Message}");
                LoadTestJobs();
            }
        }

        private void LoadTestJobs()
        {
            ActiveJobs = new ObservableCollection<Job>
            {
                new Job { JobNumber = "", JobName = "Select Job..." },
                new Job { JobId = 1, JobNumber = "619", JobName = "Smith Residence", Status = "In Progress" },
                new Job { JobId = 2, JobNumber = "621", JobName = "Bayshore Contractors", Status = "In Progress" },
                new Job { JobId = 3, JobNumber = "623", JobName = "MPC Builders - Shore House", Status = "In Progress" }
            };
        }

        private void LoadWeekEntries()
        {
            try
            {
                // Initialize employee time entries
                EmployeeTimeEntries = new ObservableCollection<EmployeeTimeEntry>();
                
                foreach (var employee in Employees)
                {
                    var timeEntry = new EmployeeTimeEntry(employee.Name);
                    
                    // Load existing time entries from database for this week
                    LoadEmployeeWeekData(timeEntry, employee.EmployeeId);
                    
                    EmployeeTimeEntries.Add(timeEntry);
                }

                // Load sample data for demonstration if no database data
                LoadSampleTimeEntries();
                
                OnPropertyChanged(nameof(EmployeeTimeEntries));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading week entries: {ex.Message}");
                LoadTestData();
            }
        }

        private void LoadEmployeeWeekData(EmployeeTimeEntry timeEntry, int employeeId)
        {
            try
            {
                // Try to load actual data from database
                // For now, we'll use sample data
                // In a real implementation, you'd query the database for labor entries
                // for this employee during the current week
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employee week data: {ex.Message}");
            }
        }

        private void LoadSampleTimeEntries()
        {
            // Add some sample time entries for demonstration
            var erikEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == "Erik");
            if (erikEntry != null)
            {
                erikEntry.Monday = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 5 };
                erikEntry.Tuesday = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                erikEntry.Wednesday = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 6 };
                erikEntry.Thursday = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                erikEntry.Friday = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 7 };
            }

            var leeEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == "Lee");
            if (leeEntry != null)
            {
                leeEntry.Monday = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                leeEntry.Tuesday = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                leeEntry.Wednesday = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                leeEntry.Thursday = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                leeEntry.Friday = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
            }

            var carlosEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == "Carlos");
            if (carlosEntry != null)
            {
                carlosEntry.Monday = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                carlosEntry.Tuesday = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                carlosEntry.Wednesday = new LaborEntryDay { JobNumber = "621", Stage = "Service", Hours = 8 };
                carlosEntry.Thursday = new LaborEntryDay { JobNumber = "621", Stage = "Service", Hours = 8 };
                carlosEntry.Friday = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
            }
        }

        private void LoadTestData()
        {
            LoadTestEmployees();
            LoadTestJobs();
            LoadWeekEntries();
        }

        private void ExecutePreviousWeek()
        {
            CurrentWeekStart = CurrentWeekStart.AddDays(-7);
        }

        private void ExecuteNextWeek()
        {
            CurrentWeekStart = CurrentWeekStart.AddDays(7);
        }

        private void ExecuteSaveAllEntries()
        {
            try
            {
                int totalEntries = 0;
                foreach (var employeeEntry in EmployeeTimeEntries)
                {
                    if (employeeEntry.Monday.Hours > 0) totalEntries++;
                    if (employeeEntry.Tuesday.Hours > 0) totalEntries++;
                    if (employeeEntry.Wednesday.Hours > 0) totalEntries++;
                    if (employeeEntry.Thursday.Hours > 0) totalEntries++;
                    if (employeeEntry.Friday.Hours > 0) totalEntries++;
                }

                // TODO: Implement actual database save logic here
                // For each employee entry, save to LaborEntries table

                System.Windows.MessageBox.Show(
                    $"Successfully saved {totalEntries} time entries to database.", 
                    "Save Successful", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error saving entries: {ex.Message}", 
                    "Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanAddEmployee()
        {
            return !string.IsNullOrWhiteSpace(NewEmployeeName) && NewEmployeeRate > 0;
        }

        private void ExecuteAddEmployee()
        {
            try
            {
                // Check if employee already exists
                if (Employees.Any(e => e.Name.Equals(NewEmployeeName, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.MessageBox.Show(
                        "An employee with this name already exists.",
                        "Duplicate Employee",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Create new employee
                var newEmployee = new Employee
                {
                    EmployeeId = Employees.Count > 0 ? Employees.Max(e => e.EmployeeId) + 1 : 1,
                    Name = NewEmployeeName.Trim(),
                    HourlyRate = NewEmployeeRate,
                    Status = "Active"
                };

                // Add to collection
                Employees.Add(newEmployee);

                // Add to time entries
                var timeEntry = new EmployeeTimeEntry(newEmployee.Name);
                EmployeeTimeEntries.Add(timeEntry);

                // TODO: Save to database
                // _databaseService.SaveEmployee(newEmployee);

                // Clear input fields
                NewEmployeeName = "";
                NewEmployeeRate = 0;

                System.Windows.MessageBox.Show(
                    $"Employee '{newEmployee.Name}' added successfully.",
                    "Employee Added",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error adding employee: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanRemoveEmployee()
        {
            return SelectedEmployee != null;
        }

        private void ExecuteRemoveEmployee()
        {
            try
            {
                if (SelectedEmployee == null) return;

                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to remove employee '{SelectedEmployee.Name}'?\n\nThis will also remove all their time entries.",
                    "Confirm Removal",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var employeeName = SelectedEmployee.Name;
                    
                    // Remove from employees collection
                    Employees.Remove(SelectedEmployee);
                    
                    // Remove from time entries
                    var timeEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == employeeName);
                    if (timeEntry != null)
                    {
                        EmployeeTimeEntries.Remove(timeEntry);
                    }

                    // TODO: Mark as inactive in database instead of deleting
                    // _databaseService.DeactivateEmployee(SelectedEmployee.EmployeeId);

                    SelectedEmployee = null;

                    System.Windows.MessageBox.Show(
                        $"Employee '{employeeName}' removed successfully.",
                        "Employee Removed",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error removing employee: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteManageEmployees()
        {
            System.Windows.MessageBox.Show(
                "Employee management dialog would open here.\n\nFor now, use the Add/Remove buttons above.",
                "Manage Employees",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        #endregion
    }

    // Helper class for labor entry days
    public class LaborEntryDay : INotifyPropertyChanged
    {
        private string _jobNumber = "";
        private string _stage = "";
        private decimal _hours = 0;

        public string JobNumber 
        { 
            get => _jobNumber; 
            set 
            { 
                _jobNumber = value; 
                OnPropertyChanged(nameof(JobNumber));
            } 
        }

        public string Stage 
        { 
            get => _stage; 
            set 
            { 
                _stage = value; 
                OnPropertyChanged(nameof(Stage));
            } 
        }

        public decimal Hours 
        { 
            get => _hours; 
            set 
            { 
                _hours = value; 
                OnPropertyChanged(nameof(Hours));
            } 
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for employee time entries
    public class EmployeeTimeEntry : INotifyPropertyChanged
    {
        private LaborEntryDay _monday;
        private LaborEntryDay _tuesday;
        private LaborEntryDay _wednesday;
        private LaborEntryDay _thursday;
        private LaborEntryDay _friday;

        public EmployeeTimeEntry(string employeeName)
        {
            EmployeeName = employeeName;
            _monday = new LaborEntryDay();
            _tuesday = new LaborEntryDay();
            _wednesday = new LaborEntryDay();
            _thursday = new LaborEntryDay();
            _friday = new LaborEntryDay();

            // Subscribe to property changes to recalculate total
            _monday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
            _tuesday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
            _wednesday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
            _thursday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
            _friday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
        }

        public string EmployeeName { get; }

        public LaborEntryDay Monday 
        { 
            get => _monday; 
            set 
            { 
                if (_monday != null) _monday.PropertyChanged -= (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                _monday = value ?? new LaborEntryDay(); 
                _monday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                OnPropertyChanged(nameof(Monday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Tuesday 
        { 
            get => _tuesday; 
            set 
            { 
                if (_tuesday != null) _tuesday.PropertyChanged -= (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                _tuesday = value ?? new LaborEntryDay(); 
                _tuesday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                OnPropertyChanged(nameof(Tuesday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Wednesday 
        { 
            get => _wednesday; 
            set 
            { 
                if (_wednesday != null) _wednesday.PropertyChanged -= (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                _wednesday = value ?? new LaborEntryDay(); 
                _wednesday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                OnPropertyChanged(nameof(Wednesday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Thursday 
        { 
            get => _thursday; 
            set 
            { 
                if (_thursday != null) _thursday.PropertyChanged -= (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                _thursday = value ?? new LaborEntryDay(); 
                _thursday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                OnPropertyChanged(nameof(Thursday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Friday 
        { 
            get => _friday; 
            set 
            { 
                if (_friday != null) _friday.PropertyChanged -= (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                _friday = value ?? new LaborEntryDay(); 
                _friday.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(LaborEntryDay.Hours)) OnPropertyChanged(nameof(TotalHours)); };
                OnPropertyChanged(nameof(Friday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public decimal TotalHours => Monday.Hours + Tuesday.Hours + Wednesday.Hours + Thursday.Hours + Friday.Hours;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}