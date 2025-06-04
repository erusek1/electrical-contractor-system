using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private Dictionary<string, Dictionary<string, LaborEntryDay>> _timeEntries;
        private Dictionary<string, decimal> _employeeTotals;
        private readonly List<string> _daysOfWeek = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        private readonly List<string> _stageNames = new List<string> { "Demo", "Rough", "Service", "Finish", "Extra", "Inspection", "Temp Service", "Other" };

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

        public Dictionary<string, Dictionary<string, LaborEntryDay>> TimeEntries
        {
            get => _timeEntries;
            set => SetProperty(ref _timeEntries, value);
        }

        public Dictionary<string, decimal> EmployeeTotals
        {
            get => _employeeTotals;
            set => SetProperty(ref _employeeTotals, value);
        }

        public List<string> DaysOfWeek => _daysOfWeek;
        public List<string> StageNames => _stageNames;

        #endregion

        #region Commands

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand SaveAllEntriesCommand { get; }

        #endregion

        #region Private Methods

        private void LoadEmployees()
        {
            try
            {
                // Try to get employees from database
                var jobs = _databaseService.GetAllJobs();
                
                // For now, create test employees
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
                ActiveJobs = new ObservableCollection<Job>(
                    allJobs.Where(j => j.Status != "Complete"));
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
                new Job { JobId = 1, JobNumber = "619", JobName = "Smith Residence", Status = "In Progress" },
                new Job { JobId = 2, JobNumber = "621", JobName = "Bayshore Contractors", Status = "In Progress" },
                new Job { JobId = 3, JobNumber = "623", JobName = "MPC Builders - Shore House", Status = "In Progress" }
            };
        }

        private void LoadWeekEntries()
        {
            try
            {
                // Initialize time entries dictionary
                TimeEntries = new Dictionary<string, Dictionary<string, LaborEntryDay>>();
                EmployeeTotals = new Dictionary<string, decimal>();
                
                // Initialize for all employees
                foreach (var employee in Employees)
                {
                    var employeeName = employee.Name;
                    TimeEntries[employeeName] = new Dictionary<string, LaborEntryDay>();
                    EmployeeTotals[employeeName] = 0;
                    
                    // Initialize days with empty entries
                    foreach (var day in DaysOfWeek)
                    {
                        TimeEntries[employeeName][day] = new LaborEntryDay();
                    }
                }

                // Load some sample data for demonstration
                LoadSampleTimeEntries();
                
                OnPropertyChanged(nameof(TimeEntries));
                OnPropertyChanged(nameof(EmployeeTotals));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading week entries: {ex.Message}");
                LoadTestData();
            }
        }

        private void LoadSampleTimeEntries()
        {
            // Add some sample time entries for demonstration
            if (TimeEntries.ContainsKey("Erik"))
            {
                TimeEntries["Erik"]["Monday"] = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 5 };
                TimeEntries["Erik"]["Tuesday"] = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                TimeEntries["Erik"]["Wednesday"] = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 6 };
                TimeEntries["Erik"]["Thursday"] = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                TimeEntries["Erik"]["Friday"] = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 7 };
                EmployeeTotals["Erik"] = 34;
            }

            if (TimeEntries.ContainsKey("Lee"))
            {
                TimeEntries["Lee"]["Monday"] = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                TimeEntries["Lee"]["Tuesday"] = new LaborEntryDay { JobNumber = "619", Stage = "Finish", Hours = 8 };
                TimeEntries["Lee"]["Wednesday"] = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                TimeEntries["Lee"]["Thursday"] = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                TimeEntries["Lee"]["Friday"] = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                EmployeeTotals["Lee"] = 40;
            }

            if (TimeEntries.ContainsKey("Carlos"))
            {
                TimeEntries["Carlos"]["Monday"] = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                TimeEntries["Carlos"]["Tuesday"] = new LaborEntryDay { JobNumber = "621", Stage = "Rough", Hours = 8 };
                TimeEntries["Carlos"]["Wednesday"] = new LaborEntryDay { JobNumber = "621", Stage = "Service", Hours = 8 };
                TimeEntries["Carlos"]["Thursday"] = new LaborEntryDay { JobNumber = "621", Stage = "Service", Hours = 8 };
                TimeEntries["Carlos"]["Friday"] = new LaborEntryDay { JobNumber = "623", Stage = "Demo", Hours = 8 };
                EmployeeTotals["Carlos"] = 40;
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
                // For now, just show a message that entries would be saved
                int totalEntries = 0;
                foreach (var employee in TimeEntries)
                {
                    foreach (var day in employee.Value)
                    {
                        if (day.Value.Hours > 0)
                        {
                            totalEntries++;
                        }
                    }
                }

                System.Windows.MessageBox.Show(
                    $"Would save {totalEntries} time entries to database.\n\nSave functionality will be implemented next.", 
                    "Save All Entries", 
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

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        #endregion
    }

    // Helper class for labor entry days
    public class LaborEntryDay
    {
        public string JobNumber { get; set; } = "";
        public string Stage { get; set; } = "";
        public decimal Hours { get; set; } = 0;
    }
}
