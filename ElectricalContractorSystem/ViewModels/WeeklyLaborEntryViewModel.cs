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
            _databaseService = databaseService;
            
            // Initialize to the beginning of the current week (Monday)
            _currentWeekStart = GetStartOfWeek(DateTime.Today);
            
            // Load initial data
            LoadEmployees();
            LoadActiveJobs();
            LoadWeekEntries();

            // Create commands
            PreviousWeekCommand = new RelayCommand(() => ExecutePreviousWeek());
            NextWeekCommand = new RelayCommand(() => ExecuteNextWeek());
            SaveAllEntriesCommand = new RelayCommand(() => ExecuteSaveAllEntries());
            EntryChangedCommand = new RelayCommand<EntryChangeArgs>(args => ExecuteEntryChanged(args));
        }

        public DateTime CurrentWeekStart
        {
            get => _currentWeekStart;
            set
            {
                if (SetProperty(ref _currentWeekStart, value))
                {
                    // Reload entries when week changes
                    LoadWeekEntries();
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

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand SaveAllEntriesCommand { get; }
        public ICommand EntryChangedCommand { get; }

        private void LoadEmployees()
        {
            Employees = new ObservableCollection<Employee>(
                _databaseService.GetActiveEmployees());
        }

        private void LoadActiveJobs()
        {
            ActiveJobs = new ObservableCollection<Job>(
                _databaseService.GetJobsByStatus("Estimate", "In Progress")
            );
        }

        private void LoadWeekEntries()
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
                
                // Initialize days
                foreach (var day in DaysOfWeek)
                {
                    TimeEntries[employeeName][day] = new LaborEntryDay();
                }
            }

            // Get labor entries for this week from the database
            Dictionary<int, string> employeeMap = Employees.ToDictionary(e => e.EmployeeId, e => e.Name);
            
            // Calculate dates for each day of the week
            var weekDates = DaysOfWeek.Select((day, index) => 
                new { Day = day, Date = CurrentWeekStart.AddDays(index) }).ToList();
            
            // Load entries from database for current week
            foreach (var dateInfo in weekDates)
            {
                DateTime endDate = dateInfo.Date.AddDays(1); // Next day for range query
                var entries = _databaseService.GetLaborEntriesByDate(dateInfo.Date, endDate);
                
                foreach (var entry in entries)
                {
                    if (employeeMap.TryGetValue(entry.EmployeeId, out string employeeName))
                    {
                        var job = ActiveJobs.FirstOrDefault(j => j.JobId == entry.JobId);
                        if (job != null)
                        {
                            // Get the stage name
                            var stage = _databaseService.GetJobStage(entry.JobId, entry.StageId.ToString());
                            if (stage != null)
                            {
                                TimeEntries[employeeName][dateInfo.Day] = new LaborEntryDay
                                {
                                    JobNumber = job.JobNumber,
                                    Stage = stage.StageName,
                                    Hours = entry.Hours
                                };
                                
                                // Update employee totals
                                EmployeeTotals[employeeName] += entry.Hours;
                            }
                        }
                    }
                }
            }
            
            OnPropertyChanged(nameof(TimeEntries));
            OnPropertyChanged(nameof(EmployeeTotals));
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
            // Delete existing entries for the current week to avoid duplicates
            Dictionary<int, string> employeeMap = Employees.ToDictionary(e => e.EmployeeId, e => e.Name);
            Dictionary<string, int> employeeNameMap = Employees.ToDictionary(e => e.Name, e => e.EmployeeId);
            Dictionary<string, int> jobNumberMap = ActiveJobs.ToDictionary(j => j.JobNumber, j => j.JobId);
            
            // Get stage IDs
            Dictionary<string, Dictionary<int, int>> stageMap = new Dictionary<string, Dictionary<int, int>>();
            foreach (var job in ActiveJobs)
            {
                var stages = _databaseService.GetJobStages(job.JobId);
                stageMap[job.JobNumber] = stages.ToDictionary(s => s.StageName.GetHashCode(), s => s.StageId);
            }
            
            // Delete existing entries for this week
            for (int i = 0; i < 5; i++)
            {
                DateTime date = CurrentWeekStart.AddDays(i);
                DateTime endDate = date.AddDays(1);
                _databaseService.DeleteLaborEntriesByDate(date, endDate, 0); // 0 for all employees
            }
            
            // Add new entries
            foreach (var employeeEntry in TimeEntries)
            {
                string employeeName = employeeEntry.Key;
                if (!employeeNameMap.TryGetValue(employeeName, out int employeeId))
                    continue;
                
                foreach (var dayEntry in employeeEntry.Value)
                {
                    string day = dayEntry.Key;
                    var entry = dayEntry.Value;
                    
                    // Skip if no hours or job number
                    if (entry.Hours <= 0 || string.IsNullOrEmpty(entry.JobNumber) || string.IsNullOrEmpty(entry.Stage))
                        continue;
                    
                    // Get job ID
                    if (!jobNumberMap.TryGetValue(entry.JobNumber, out int jobId))
                        continue;
                    
                    // Get stage ID
                    if (!stageMap.TryGetValue(entry.JobNumber, out var jobStages) || 
                        !jobStages.TryGetValue(entry.Stage.GetHashCode(), out int stageId))
                    {
                        // Create stage if not exists
                        var newStage = new JobStage
                        {
                            JobId = jobId,
                            StageName = entry.Stage,
                            EstimatedHours = 0,
                            EstimatedMaterialCost = 0,
                            ActualHours = 0,
                            ActualMaterialCost = 0
                        };
                        stageId = _databaseService.AddJobStage(newStage);
                        
                        // Add to map
                        if (!stageMap.ContainsKey(entry.JobNumber))
                            stageMap[entry.JobNumber] = new Dictionary<int, int>();
                        stageMap[entry.JobNumber][entry.Stage.GetHashCode()] = stageId;
                    }
                    
                    // Calculate date based on day of week
                    int dayIndex = DaysOfWeek.IndexOf(day);
                    DateTime entryDate = CurrentWeekStart.AddDays(dayIndex);
                    
                    // Create and save labor entry
                    var laborEntry = new LaborEntry
                    {
                        JobId = jobId,
                        EmployeeId = employeeId,
                        StageId = stageId,
                        Date = entryDate,
                        Hours = entry.Hours
                    };
                    
                    _databaseService.AddLaborEntry(laborEntry);
                    
                    // Update actual hours in stage
                    var stage = _databaseService.GetJobStage(jobId, stageId.ToString());
                    if (stage != null)
                    {
                        stage.ActualHours += entry.Hours;
                        _databaseService.UpdateJobStage(stage);
                    }
                }
            }
            
            // Reload to ensure data consistency
            LoadWeekEntries();
        }

        private void ExecuteEntryChanged(EntryChangeArgs args)
        {
            if (args == null || string.IsNullOrEmpty(args.Employee) || string.IsNullOrEmpty(args.Day))
                return;
            
            // Update the entry
            if (!TimeEntries.TryGetValue(args.Employee, out var employeeDays))
            {
                employeeDays = new Dictionary<string, LaborEntryDay>();
                TimeEntries[args.Employee] = employeeDays;
            }
            
            if (!employeeDays.TryGetValue(args.Day, out var entry))
            {
                entry = new LaborEntryDay();
                TimeEntries[args.Employee][args.Day] = entry;
            }
            
            // Update the appropriate field
            switch (args.Field)
            {
                case "JobNumber":
                    entry.JobNumber = args.Value;
                    break;
                case "Stage":
                    entry.Stage = args.Value;
                    break;
                case "Hours":
                    entry.Hours = args.NumericValue;
                    break;
            }
            
            // Update employee total
            decimal total = 0;
            foreach (var day in DaysOfWeek)
            {
                if (TimeEntries[args.Employee].TryGetValue(day, out var dayEntry))
                {
                    total += dayEntry.Hours;
                }
            }
            EmployeeTotals[args.Employee] = total;
            
            OnPropertyChanged(nameof(TimeEntries));
            OnPropertyChanged(nameof(EmployeeTotals));
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }
    }

    // Helper class for labor entry days
    public class LaborEntryDay
    {
        public string JobNumber { get; set; } = "";
        public string Stage { get; set; } = "";
        public decimal Hours { get; set; } = 0;
    }

    // Helper class for entry change events
    public class EntryChangeArgs
    {
        public string Employee { get; set; }
        public string Day { get; set; }
        public string Field { get; set; }
        public string Value { get; set; }
        public decimal NumericValue { get; set; }
    }
}
