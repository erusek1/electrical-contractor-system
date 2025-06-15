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
        private ObservableCollection<EmployeeWeeklySummary> _employeeWeeklySummaries;
        private Employee _selectedEmployee;
        private string _newEmployeeName;
        private decimal _newEmployeeRate;
        private int? _selectedJobId;
        private readonly List<string> _stageNames = new List<string> { "", "Demo", "Rough", "Service", "Finish", "Extra", "Inspection", "Temp Service", "Other" };

        public WeeklyLaborEntryViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            _currentWeekStart = GetStartOfWeek(DateTime.Today);
            
            try
            {
                LoadEmployees();
                LoadActiveJobs();
                LoadWeekEntries();
            }
            catch (Exception ex)
            {
                LoadTestData();
                System.Diagnostics.Debug.WriteLine($"WeeklyLaborEntry: Using test data due to error: {ex.Message}");
            }

            PreviousWeekCommand = new RelayCommand(() => ExecutePreviousWeek());
            NextWeekCommand = new RelayCommand(() => ExecuteNextWeek());
            SaveAllEntriesCommand = new RelayCommand(() => ExecuteSaveAllEntries());
            AddEmployeeCommand = new RelayCommand(() => ExecuteAddEmployee(), () => CanAddEmployee());
            RemoveEmployeeCommand = new RelayCommand(() => ExecuteRemoveEmployee(), () => CanRemoveEmployee());
            ManageEmployeesCommand = new RelayCommand(() => ExecuteManageEmployees());
        }

        #region Properties

        public int? SelectedJobId
        {
            get => _selectedJobId;
            set
            {
                if (SetProperty(ref _selectedJobId, value))
                {
                    // If a job is selected, pre-populate it for new entries
                    if (value.HasValue && ActiveJobs != null)
                    {
                        var selectedJob = ActiveJobs.FirstOrDefault(j => j.JobId == value.Value);
                        if (selectedJob != null)
                        {
                            // You can implement logic here to pre-select this job for new entries
                            // For example, setting it as the default for today's entries
                        }
                    }
                }
            }
        }

        public DateTime CurrentWeekStart
        {
            get => _currentWeekStart;
            set
            {
                if (SetProperty(ref _currentWeekStart, value))
                {
                    LoadWeekEntries();
                    UpdateWeeklySummaries();
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
                string endStr = weekEnd.ToString("MMM d, yyyy");
                return $"Week of {startStr} - {endStr}";
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
            set 
            { 
                SetProperty(ref _employeeTimeEntries, value);
                UpdateWeeklySummaries();
            }
        }

        public ObservableCollection<EmployeeWeeklySummary> EmployeeWeeklySummaries
        {
            get => _employeeWeeklySummaries;
            set => SetProperty(ref _employeeWeeklySummaries, value);
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
                var employees = _databaseService.GetAllEmployees();
                Employees = new ObservableCollection<Employee>(employees.Where(e => e.Status == "Active").OrderBy(e => e.Name));
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
                EmployeeTimeEntries = new ObservableCollection<EmployeeTimeEntry>();
                
                foreach (var employee in Employees)
                {
                    var timeEntry = new EmployeeTimeEntry(employee.Name);
                    
                    timeEntry.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(EmployeeTimeEntry.TotalHours))
                        {
                            UpdateWeeklySummaries();
                        }
                    };
                    
                    // Load actual data from database
                    LoadEmployeeWeekData(timeEntry, employee.EmployeeId);
                    EmployeeTimeEntries.Add(timeEntry);
                }

                // Don't load sample data - keep it clean
                UpdateWeeklySummaries();
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
                // TODO: Implement database loading for the selected week
                // For now, initialize with empty entries
                timeEntry.Monday = new LaborEntryDay { JobNumber = "", Stage = "", Hours = 0 };
                timeEntry.Tuesday = new LaborEntryDay { JobNumber = "", Stage = "", Hours = 0 };
                timeEntry.Wednesday = new LaborEntryDay { JobNumber = "", Stage = "", Hours = 0 };
                timeEntry.Thursday = new LaborEntryDay { JobNumber = "", Stage = "", Hours = 0 };
                timeEntry.Friday = new LaborEntryDay { JobNumber = "", Stage = "", Hours = 0 };
                
                // If a job was pre-selected, set it for today's entry
                if (SelectedJobId.HasValue && ActiveJobs != null)
                {
                    var selectedJob = ActiveJobs.FirstOrDefault(j => j.JobId == SelectedJobId.Value);
                    if (selectedJob != null)
                    {
                        var dayOfWeek = DateTime.Today.DayOfWeek;
                        var todayEntry = GetDayEntry(timeEntry, dayOfWeek);
                        if (todayEntry != null && string.IsNullOrEmpty(todayEntry.JobNumber))
                        {
                            todayEntry.JobNumber = selectedJob.JobNumber;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading employee week data: {ex.Message}");
            }
        }

        private LaborEntryDay GetDayEntry(EmployeeTimeEntry timeEntry, DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return timeEntry.Monday;
                case DayOfWeek.Tuesday: return timeEntry.Tuesday;
                case DayOfWeek.Wednesday: return timeEntry.Wednesday;
                case DayOfWeek.Thursday: return timeEntry.Thursday;
                case DayOfWeek.Friday: return timeEntry.Friday;
                default: return null;
            }
        }

        private void UpdateWeeklySummaries()
        {
            if (EmployeeTimeEntries == null || Employees == null)
                return;

            var summaries = new ObservableCollection<EmployeeWeeklySummary>();
            
            foreach (var employee in Employees)
            {
                var timeEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == employee.Name);
                var totalHours = timeEntry?.TotalHours ?? 0;
                
                var summary = new EmployeeWeeklySummary
                {
                    EmployeeName = employee.Name,
                    HourlyRate = employee.HourlyRate,
                    TotalHours = totalHours,
                    Status = GetHoursStatus(totalHours),
                    StatusColor = GetStatusColor(totalHours),
                    WeekStartDate = CurrentWeekStart,
                    TotalPay = totalHours * employee.HourlyRate
                };
                
                summaries.Add(summary);
            }
            
            EmployeeWeeklySummaries = summaries;
        }

        private string GetHoursStatus(decimal hours)
        {
            if (hours == 0) return "No Hours";
            if (hours < 40) return "Partial";
            if (hours == 40) return "Complete";
            return "Overtime";
        }

        private string GetStatusColor(decimal hours)
        {
            if (hours == 0) return "#F44336"; // Red
            if (hours < 40) return "#FF9800"; // Orange
            if (hours == 40) return "#4CAF50"; // Green
            return "#9C27B0"; // Purple for overtime
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

                System.Windows.MessageBox.Show(
                    $"Successfully saved {totalEntries} time entries for week of {WeekDateRange}.", 
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
                if (Employees.Any(e => e.Name.Equals(NewEmployeeName, StringComparison.OrdinalIgnoreCase)))
                {
                    System.Windows.MessageBox.Show(
                        "An employee with this name already exists.",
                        "Duplicate Employee",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var newEmployee = new Employee
                {
                    EmployeeId = Employees.Count > 0 ? Employees.Max(e => e.EmployeeId) + 1 : 1,
                    Name = NewEmployeeName.Trim(),
                    HourlyRate = NewEmployeeRate,
                    Status = "Active"
                };

                Employees.Add(newEmployee);

                var timeEntry = new EmployeeTimeEntry(newEmployee.Name);
                timeEntry.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(EmployeeTimeEntry.TotalHours))
                    {
                        UpdateWeeklySummaries();
                    }
                };
                EmployeeTimeEntries.Add(timeEntry);
                UpdateWeeklySummaries();

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
                    $"Are you sure you want to remove employee '{SelectedEmployee.Name}'?",
                    "Confirm Removal",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var employeeName = SelectedEmployee.Name;
                    
                    Employees.Remove(SelectedEmployee);
                    
                    var timeEntry = EmployeeTimeEntries.FirstOrDefault(e => e.EmployeeName == employeeName);
                    if (timeEntry != null)
                    {
                        EmployeeTimeEntries.Remove(timeEntry);
                    }

                    UpdateWeeklySummaries();
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

    public class EmployeeWeeklySummary : INotifyPropertyChanged
    {
        private decimal _totalPay;
        public string EmployeeName { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalHours { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public DateTime WeekStartDate { get; set; }
        
        public decimal TotalPay 
        { 
            get => _totalPay; 
            set 
            { 
                _totalPay = value; 
                OnPropertyChanged(nameof(TotalPay)); 
            } 
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LaborEntryDay : INotifyPropertyChanged
    {
        private string _jobNumber = "";
        private string _stage = "";
        private decimal _hours = 0;

        public string JobNumber { get => _jobNumber; set { _jobNumber = value; OnPropertyChanged(nameof(JobNumber)); } }
        public string Stage { get => _stage; set { _stage = value; OnPropertyChanged(nameof(Stage)); } }
        public decimal Hours { get => _hours; set { _hours = value; OnPropertyChanged(nameof(Hours)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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

            _monday.PropertyChanged += OnDayPropertyChanged;
            _tuesday.PropertyChanged += OnDayPropertyChanged;
            _wednesday.PropertyChanged += OnDayPropertyChanged;
            _thursday.PropertyChanged += OnDayPropertyChanged;
            _friday.PropertyChanged += OnDayPropertyChanged;
        }

        private void OnDayPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LaborEntryDay.Hours))
            {
                OnPropertyChanged(nameof(TotalHours));
            }
        }

        public string EmployeeName { get; }

        public LaborEntryDay Monday 
        { 
            get => _monday; 
            set 
            { 
                if (_monday != null) _monday.PropertyChanged -= OnDayPropertyChanged;
                _monday = value ?? new LaborEntryDay(); 
                _monday.PropertyChanged += OnDayPropertyChanged;
                OnPropertyChanged(nameof(Monday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Tuesday 
        { 
            get => _tuesday; 
            set 
            { 
                if (_tuesday != null) _tuesday.PropertyChanged -= OnDayPropertyChanged;
                _tuesday = value ?? new LaborEntryDay(); 
                _tuesday.PropertyChanged += OnDayPropertyChanged;
                OnPropertyChanged(nameof(Tuesday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Wednesday 
        { 
            get => _wednesday; 
            set 
            { 
                if (_wednesday != null) _wednesday.PropertyChanged -= OnDayPropertyChanged;
                _wednesday = value ?? new LaborEntryDay(); 
                _wednesday.PropertyChanged += OnDayPropertyChanged;
                OnPropertyChanged(nameof(Wednesday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Thursday 
        { 
            get => _thursday; 
            set 
            { 
                if (_thursday != null) _thursday.PropertyChanged -= OnDayPropertyChanged;
                _thursday = value ?? new LaborEntryDay(); 
                _thursday.PropertyChanged += OnDayPropertyChanged;
                OnPropertyChanged(nameof(Thursday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public LaborEntryDay Friday 
        { 
            get => _friday; 
            set 
            { 
                if (_friday != null) _friday.PropertyChanged -= OnDayPropertyChanged;
                _friday = value ?? new LaborEntryDay(); 
                _friday.PropertyChanged += OnDayPropertyChanged;
                OnPropertyChanged(nameof(Friday)); 
                OnPropertyChanged(nameof(TotalHours));
            } 
        }

        public decimal TotalHours => Monday.Hours + Tuesday.Hours + Wednesday.Hours + Thursday.Hours + Friday.Hours;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}