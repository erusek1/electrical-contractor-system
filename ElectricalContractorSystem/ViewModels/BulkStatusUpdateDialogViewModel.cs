using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for the Bulk Status Update Dialog
    /// </summary>
    public class BulkStatusUpdateDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;

        // Filter Properties
        private string _filterStatus;
        private string _newStatus;
        private DateTime? _filterDateFrom;
        private DateTime? _filterDateTo;

        // Collections
        private ObservableCollection<string> _filterStatusOptions;
        private ObservableCollection<string> _newStatusOptions;
        private ObservableCollection<BulkUpdateJobItem> _allJobs;
        private ObservableCollection<BulkUpdateJobItem> _filteredJobs;

        // Display Properties
        private string _jobCountDisplay;
        private string _selectedJobsCount;

        // Events
        public event EventHandler<bool?> RequestClose;

        #region Properties

        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        public string NewStatus
        {
            get => _newStatus;
            set => SetProperty(ref _newStatus, value);
        }

        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set => SetProperty(ref _filterDateFrom, value);
        }

        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set => SetProperty(ref _filterDateTo, value);
        }

        public ObservableCollection<string> FilterStatusOptions
        {
            get => _filterStatusOptions;
            set => SetProperty(ref _filterStatusOptions, value);
        }

        public ObservableCollection<string> NewStatusOptions
        {
            get => _newStatusOptions;
            set => SetProperty(ref _newStatusOptions, value);
        }

        public ObservableCollection<BulkUpdateJobItem> AllJobs
        {
            get => _allJobs;
            set => SetProperty(ref _allJobs, value);
        }

        public ObservableCollection<BulkUpdateJobItem> FilteredJobs
        {
            get => _filteredJobs;
            set
            {
                if (SetProperty(ref _filteredJobs, value))
                {
                    UpdateDisplayCounts();
                }
            }
        }

        public string JobCountDisplay
        {
            get => _jobCountDisplay;
            set => SetProperty(ref _jobCountDisplay, value);
        }

        public string SelectedJobsCount
        {
            get => _selectedJobsCount;
            set => SetProperty(ref _selectedJobsCount, value);
        }

        #endregion

        #region Commands

        public ICommand ApplyFilterCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand SelectNoneCommand { get; }
        public ICommand UpdateJobsCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public BulkStatusUpdateDialogViewModel() : this(new DatabaseService())
        {
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BulkStatusUpdateDialogViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));

            // Initialize commands
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            SelectAllCommand = new RelayCommand(SelectAll, () => FilteredJobs?.Count > 0);
            SelectNoneCommand = new RelayCommand(SelectNone, () => FilteredJobs?.Count > 0);
            UpdateJobsCommand = new RelayCommand(UpdateJobs, CanUpdateJobs);
            CancelCommand = new RelayCommand(Cancel);

            // Initialize collections
            FilterStatusOptions = new ObservableCollection<string>
            {
                "All Statuses",
                "Estimate",
                "In Progress",
                "Complete"
            };

            NewStatusOptions = new ObservableCollection<string>
            {
                "Estimate",
                "In Progress",
                "Complete"
            };

            // Set defaults
            FilterStatus = "Complete"; // Default to show completed jobs for status updates
            NewStatus = "In Progress"; // Default to changing to In Progress
            FilterDateFrom = DateTime.Now.AddYears(-2); // Default to last 2 years
            FilterDateTo = DateTime.Now;

            // Load data
            LoadJobs();
        }

        /// <summary>
        /// Loads all jobs from the database
        /// </summary>
        private void LoadJobs()
        {
            try
            {
                var jobs = _databaseService.GetAllJobs();
                var jobItems = jobs.Select(j => new BulkUpdateJobItem
                {
                    JobId = j.JobId,
                    JobNumber = j.JobNumber,
                    JobName = j.JobName,
                    CustomerName = j.Customer?.Name ?? "Unknown Customer",
                    Status = j.Status,
                    CreateDate = j.CreateDate,
                    IsSelected = false
                }).ToList();

                AllJobs = new ObservableCollection<BulkUpdateJobItem>(jobItems);
                
                // Subscribe to selection changes
                foreach (var job in AllJobs)
                {
                    job.PropertyChanged += OnJobSelectionChanged;
                }

                // Apply initial filter
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading jobs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                AllJobs = new ObservableCollection<BulkUpdateJobItem>();
                FilteredJobs = new ObservableCollection<BulkUpdateJobItem>();
            }
        }

        /// <summary>
        /// Applies the current filter to show relevant jobs
        /// </summary>
        private void ApplyFilter()
        {
            if (AllJobs == null)
                return;

            var filtered = AllJobs.AsEnumerable();

            // Filter by status
            if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "All Statuses")
            {
                filtered = filtered.Where(j => j.Status == FilterStatus);
            }

            // Filter by date range
            if (FilterDateFrom.HasValue)
            {
                filtered = filtered.Where(j => j.CreateDate >= FilterDateFrom.Value);
            }

            if (FilterDateTo.HasValue)
            {
                filtered = filtered.Where(j => j.CreateDate <= FilterDateTo.Value.AddDays(1));
            }

            FilteredJobs = new ObservableCollection<BulkUpdateJobItem>(filtered);
            UpdateDisplayCounts();
        }

        /// <summary>
        /// Selects all filtered jobs
        /// </summary>
        private void SelectAll()
        {
            if (FilteredJobs != null)
            {
                foreach (var job in FilteredJobs)
                {
                    job.IsSelected = true;
                }
                UpdateDisplayCounts();
            }
        }

        /// <summary>
        /// Deselects all jobs
        /// </summary>
        private void SelectNone()
        {
            if (FilteredJobs != null)
            {
                foreach (var job in FilteredJobs)
                {
                    job.IsSelected = false;
                }
                UpdateDisplayCounts();
            }
        }

        /// <summary>
        /// Updates the status of selected jobs
        /// </summary>
        private void UpdateJobs()
        {
            if (string.IsNullOrEmpty(NewStatus))
            {
                MessageBox.Show("Please select a new status for the jobs.", "Missing Status", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedJobs = FilteredJobs?.Where(j => j.IsSelected).ToList();
            if (selectedJobs == null || selectedJobs.Count == 0)
            {
                MessageBox.Show("Please select at least one job to update.", "No Jobs Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to update {selectedJobs.Count} job(s) to status '{NewStatus}'?",
                "Confirm Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                int successCount = 0;
                int errorCount = 0;

                foreach (var job in selectedJobs)
                {
                    try
                    {
                        bool success = _databaseService.UpdateJobStatus(job.JobId, NewStatus);
                        if (success)
                        {
                            job.Status = NewStatus;
                            successCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating job {job.JobNumber}: {ex.Message}");
                        errorCount++;
                    }
                }

                string message = $"Successfully updated {successCount} job(s).";
                if (errorCount > 0)
                {
                    message += $"\n{errorCount} job(s) failed to update.";
                }

                MessageBox.Show(message, "Update Complete", MessageBoxButton.OK, 
                    errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

                if (successCount > 0)
                {
                    RequestClose?.Invoke(this, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating jobs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Determines if jobs can be updated
        /// </summary>
        private bool CanUpdateJobs()
        {
            return !string.IsNullOrEmpty(NewStatus) &&
                   FilteredJobs?.Any(j => j.IsSelected) == true;
        }

        /// <summary>
        /// Cancels the dialog
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        /// <summary>
        /// Updates display counts when job selection changes
        /// </summary>
        private void OnJobSelectionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BulkUpdateJobItem.IsSelected))
            {
                UpdateDisplayCounts();
            }
        }

        /// <summary>
        /// Updates the display counts for filtered and selected jobs
        /// </summary>
        private void UpdateDisplayCounts()
        {
            if (FilteredJobs == null)
            {
                JobCountDisplay = "0 jobs found";
                SelectedJobsCount = "0";
                return;
            }

            JobCountDisplay = $"{FilteredJobs.Count} job(s) found";
            
            var selectedCount = FilteredJobs.Count(j => j.IsSelected);
            SelectedJobsCount = selectedCount.ToString();

            // Update command can execute status
            ((RelayCommand)UpdateJobsCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SelectAllCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SelectNoneCommand).RaiseCanExecuteChanged();
        }
    }
}