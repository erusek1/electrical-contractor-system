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
    /// <summary>
    /// ViewModel for the Job Management screen
    /// </summary>
    public class JobManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Job> _jobs;
        private ObservableCollection<Job> _filteredJobs;
        private string _searchText = string.Empty;
        private string _activeFilter = "active";
        private Job _selectedJob;
        private bool _isLoading = false;
        private string _errorMessage;

        // Summary statistics
        private int _activeJobCount;
        private decimal _totalEstimate;
        private decimal _totalActual;

        #region Properties

        /// <summary>
        /// List of all jobs
        /// </summary>
        public ObservableCollection<Job> Jobs
        {
            get => _jobs;
            set => SetProperty(ref _jobs, value);
        }

        /// <summary>
        /// Filtered list of jobs based on search and filters
        /// </summary>
        public ObservableCollection<Job> FilteredJobs
        {
            get => _filteredJobs;
            set => SetProperty(ref _filteredJobs, value);
        }

        /// <summary>
        /// Currently selected job
        /// </summary>
        public Job SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (SetProperty(ref _selectedJob, value))
                {
                    // Enable command execution state to be re-evaluated
                    EditJobCommand.RaiseCanExecuteChanged();
                    DeleteJobCommand.RaiseCanExecuteChanged();
                    ViewJobDetailsCommand.RaiseCanExecuteChanged();
                    EnterJobDataCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Search text for filtering jobs
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Active filter (active, completed, all)
        /// </summary>
        public string ActiveFilter
        {
            get => _activeFilter;
            set
            {
                if (SetProperty(ref _activeFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Indicates if data is being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Error message to display
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Count of active jobs
        /// </summary>
        public int ActiveJobCount
        {
            get => _activeJobCount;
            set => SetProperty(ref _activeJobCount, value);
        }

        /// <summary>
        /// Total estimated amount for active jobs
        /// </summary>
        public decimal TotalEstimate
        {
            get => _totalEstimate;
            set => SetProperty(ref _totalEstimate, value);
        }

        /// <summary>
        /// Total actual amount spent on active jobs
        /// </summary>
        public decimal TotalActual
        {
            get => _totalActual;
            set => SetProperty(ref _totalActual, value);
        }

        /// <summary>
        /// Indicates if there is an error
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Indicates if there are no jobs to display
        /// </summary>
        public bool HasNoJobs => FilteredJobs != null && FilteredJobs.Count == 0 && !IsLoading && !HasError;

        #endregion

        #region Commands

        /// <summary>
        /// Command to create a new job
        /// </summary>
        public RelayCommand NewJobCommand { get; }

        /// <summary>
        /// Command to edit the selected job
        /// </summary>
        public RelayCommandWithCanExecute EditJobCommand { get; }

        /// <summary>
        /// Command to delete the selected job
        /// </summary>
        public RelayCommandWithCanExecute DeleteJobCommand { get; }

        /// <summary>
        /// Command to view details of the selected job
        /// </summary>
        public RelayCommandWithCanExecute ViewJobDetailsCommand { get; }

        /// <summary>
        /// Command to enter data for the selected job
        /// </summary>
        public RelayCommandWithCanExecute EnterJobDataCommand { get; }

        /// <summary>
        /// Command to refresh the job list
        /// </summary>
        public RelayCommand RefreshJobsCommand { get; }

        /// <summary>
        /// Command to filter jobs by "Active" status
        /// </summary>
        public RelayCommand ShowActiveJobsCommand { get; }

        /// <summary>
        /// Command to filter jobs by "Completed" status
        /// </summary>
        public RelayCommand ShowCompletedJobsCommand { get; }

        /// <summary>
        /// Command to show all jobs (no status filter)
        /// </summary>
        public RelayCommand ShowAllJobsCommand { get; }

        /// <summary>
        /// Command to clear the search text
        /// </summary>
        public RelayCommand ClearSearchCommand { get; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public JobManagementViewModel() : this(new DatabaseService())
        {
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public JobManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Initialize collections
            Jobs = new ObservableCollection<Job>();
            FilteredJobs = new ObservableCollection<Job>();

            // Initialize commands
            NewJobCommand = new RelayCommand(CreateNewJob);
            EditJobCommand = new RelayCommandWithCanExecute(EditJob, CanEditJob);
            DeleteJobCommand = new RelayCommandWithCanExecute(DeleteJob, CanDeleteJob);
            ViewJobDetailsCommand = new RelayCommandWithCanExecute(ViewJobDetails, CanViewJobDetails);
            EnterJobDataCommand = new RelayCommandWithCanExecute(EnterJobData, CanEnterJobData);
            RefreshJobsCommand = new RelayCommand(LoadJobs);
            ShowActiveJobsCommand = new RelayCommand(ShowActiveJobs);
            ShowCompletedJobsCommand = new RelayCommand(ShowCompletedJobs);
            ShowAllJobsCommand = new RelayCommand(ShowAllJobs);
            ClearSearchCommand = new RelayCommand(ClearSearch);

            // Load data
            LoadJobs();
        }

        /// <summary>
        /// Loads jobs from the database
        /// </summary>
        private void LoadJobs()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                // Test database connection first
                if (!_databaseService.TestConnection())
                {
                    ErrorMessage = "Unable to connect to database. Please check your connection settings.";
                    return;
                }

                // Load jobs from database
                var jobsFromDb = _databaseService.GetAllJobs();
                Jobs = new ObservableCollection<Job>(jobsFromDb);
                
                // Apply filters to update the filtered list
                ApplyFilters();
                
                // Calculate summary statistics
                UpdateSummaryStatistics();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading jobs: {ex.Message}";
                
                // Fallback to test data if database connection fails
                try
                {
                    var testJobs = GetTestJobs();
                    Jobs = new ObservableCollection<Job>(testJobs);
                    ApplyFilters();
                    UpdateSummaryStatistics();
                    ErrorMessage += " (Using test data)";
                }
                catch (Exception testEx)
                {
                    ErrorMessage = $"Error loading jobs and test data: {testEx.Message}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the filtered jobs list based on search text and filter
        /// </summary>
        private void ApplyFilters()
        {
            if (Jobs == null)
                return;

            IEnumerable<Job> filtered = Jobs;

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(ActiveFilter))
            {
                if (ActiveFilter.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(j => j.Status != "Complete");
                }
                else if (ActiveFilter.Equals("completed", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = filtered.Where(j => j.Status == "Complete");
                }
                // "all" doesn't filter anything
            }

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.ToLower();
                filtered = filtered.Where(j =>
                    (j.JobNumber != null && j.JobNumber.ToLower().Contains(search)) ||
                    (j.JobName != null && j.JobName.ToLower().Contains(search)) ||
                    (j.Address != null && j.Address.ToLower().Contains(search)) ||
                    (j.Customer != null && j.Customer.Name != null && j.Customer.Name.ToLower().Contains(search)));
            }

            // Update the filtered list
            FilteredJobs = new ObservableCollection<Job>(filtered);
            
            // Update summary statistics based on the filter
            UpdateSummaryStatistics();
            
            // Notify that HasNoJobs may have changed
            OnPropertyChanged(nameof(HasNoJobs));
        }

        /// <summary>
        /// Updates the summary statistics based on the current filter
        /// </summary>
        private void UpdateSummaryStatistics()
        {
            if (Jobs == null)
            {
                ActiveJobCount = 0;
                TotalEstimate = 0;
                TotalActual = 0;
                return;
            }

            // Calculate summary statistics based on active jobs only
            var activeJobs = Jobs.Where(j => j.Status != "Complete").ToList();
            
            ActiveJobCount = activeJobs.Count;
            TotalEstimate = activeJobs.Sum(j => j.TotalEstimate ?? 0);
            TotalActual = activeJobs.Sum(j => j.TotalActual ?? 0);
        }

        #region Command Handlers

        private void CreateNewJob(object parameter)
        {
            // In a real implementation, this would navigate to a job creation screen
            // For now, just output a message
            System.Windows.MessageBox.Show("Create New Job functionality will be implemented next. This will open a job details form.");
        }

        private void EditJob(object parameter)
        {
            if (SelectedJob == null)
                return;

            // In a real implementation, this would navigate to a job editing screen
            System.Windows.MessageBox.Show($"Edit Job {SelectedJob.JobNumber} functionality will be implemented next. This will open the job details form in edit mode.");
        }

        private bool CanEditJob(object parameter)
        {
            return SelectedJob != null;
        }

        private void DeleteJob(object parameter)
        {
            if (SelectedJob == null)
                return;

            // Confirm deletion
            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete job {SelectedJob.JobNumber}?",
                "Confirm Deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Implement actual job deletion in database
                    System.Windows.MessageBox.Show($"Delete Job {SelectedJob.JobNumber} functionality will be implemented next.");
                    
                    // For now, remove from local collection
                    Jobs.Remove(SelectedJob);
                    ApplyFilters();
                    UpdateSummaryStatistics();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error deleting job: {ex.Message}";
                }
            }
        }

        private bool CanDeleteJob(object parameter)
        {
            return SelectedJob != null;
        }

        private void ViewJobDetails(object parameter)
        {
            if (SelectedJob == null)
                return;

            // In a real implementation, this would navigate to a job details screen
            System.Windows.MessageBox.Show($"View Job {SelectedJob.JobNumber} Details functionality will be implemented next. This will show the job cost tracking view.");
        }

        private bool CanViewJobDetails(object parameter)
        {
            return SelectedJob != null;
        }

        private void EnterJobData(object parameter)
        {
            if (SelectedJob == null)
                return;

            // In a real implementation, this would navigate to a job data entry screen
            System.Windows.MessageBox.Show($"Enter Data for Job {SelectedJob.JobNumber} functionality will be implemented next. This will open the weekly labor entry or material entry forms.");
        }

        private bool CanEnterJobData(object parameter)
        {
            return SelectedJob != null && SelectedJob.Status != "Complete";
        }

        private void ShowActiveJobs(object parameter)
        {
            ActiveFilter = "active";
        }

        private void ShowCompletedJobs(object parameter)
        {
            ActiveFilter = "completed";
        }

        private void ShowAllJobs(object parameter)
        {
            ActiveFilter = "all";
        }

        private void ClearSearch(object parameter)
        {
            SearchText = string.Empty;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets test jobs for UI development (fallback when database is not available)
        /// </summary>
        private List<Job> GetTestJobs()
        {
            // Create test customers
            var customers = new List<Customer>
            {
                new Customer { CustomerId = 1, Name = "Smith Residence", Address = "123 Main St", City = "Manasquan", State = "NJ", Zip = "08736" },
                new Customer { CustomerId = 2, Name = "Johnson Remodel", Address = "456 Oak Ave", City = "Wall", State = "NJ", Zip = "07719" },
                new Customer { CustomerId = 3, Name = "Bayshore Contractors", Address = "789 Beach Blvd", City = "Belmar", State = "NJ", Zip = "07719" },
                new Customer { CustomerId = 4, Name = "Williams Kitchen", Address = "321 Pine St", City = "Spring Lake", State = "NJ", Zip = "07762" },
                new Customer { CustomerId = 5, Name = "MPC Builders - Shore House", Address = "555 Seaside Dr", City = "Brielle", State = "NJ", Zip = "08736" }
            };

            // Create test jobs
            return new List<Job>
            {
                new Job
                {
                    JobId = 1,
                    JobNumber = "619",
                    CustomerId = 1,
                    JobName = "Smith Residence",
                    Address = "123 Main St",
                    City = "Manasquan",
                    State = "NJ",
                    Zip = "08736",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 3, 15),
                    TotalEstimate = 40807.29m,
                    TotalActual = 21780.98m,
                    Customer = customers[0]
                },
                new Job
                {
                    JobId = 2,
                    JobNumber = "620",
                    CustomerId = 2,
                    JobName = "Johnson Remodel",
                    Address = "456 Oak Ave",
                    City = "Wall",
                    State = "NJ",
                    Zip = "07719",
                    Status = "Estimate",
                    CreateDate = new DateTime(2025, 4, 20),
                    TotalEstimate = 25450.00m,
                    TotalActual = 0m,
                    Customer = customers[1]
                },
                new Job
                {
                    JobId = 3,
                    JobNumber = "621",
                    CustomerId = 3,
                    JobName = "Bayshore Contractors",
                    Address = "789 Beach Blvd",
                    City = "Belmar",
                    State = "NJ",
                    Zip = "07719",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 4, 25),
                    TotalEstimate = 15780.50m,
                    TotalActual = 6250.75m,
                    Customer = customers[2]
                },
                new Job
                {
                    JobId = 4,
                    JobNumber = "622",
                    CustomerId = 4,
                    JobName = "Williams Kitchen",
                    Address = "321 Pine St",
                    City = "Spring Lake",
                    State = "NJ",
                    Zip = "07762",
                    Status = "Complete",
                    CreateDate = new DateTime(2025, 2, 10),
                    CompletionDate = new DateTime(2025, 4, 5),
                    TotalEstimate = 12560.00m,
                    TotalActual = 13458.65m,
                    Customer = customers[3]
                },
                new Job
                {
                    JobId = 5,
                    JobNumber = "623",
                    CustomerId = 5,
                    JobName = "MPC Builders - Shore House",
                    Address = "555 Seaside Dr",
                    City = "Brielle",
                    State = "NJ",
                    Zip = "08736",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 5, 1),
                    TotalEstimate = 85690.75m,
                    TotalActual = 22450.30m,
                    Customer = customers[4]
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// Extended RelayCommand that can raise CanExecuteChanged
    /// </summary>
    public class RelayCommandWithCanExecute : RelayCommand
    {
        public RelayCommandWithCanExecute(Action<object> execute, Predicate<object> canExecute) 
            : base(execute, canExecute)
        {
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
