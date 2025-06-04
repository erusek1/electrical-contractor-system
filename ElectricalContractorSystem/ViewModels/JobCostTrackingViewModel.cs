using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class JobCostTrackingViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Job> _availableJobs;
        private Job _selectedJob;
        private string _activeTab = "labor";

        public JobCostTrackingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // Initialize commands
            PrintReportCommand = new RelayCommand(() => PrintReport());
            
            try
            {
                LoadJobs();
                if (AvailableJobs?.Count > 0)
                {
                    SelectedJob = AvailableJobs.First();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobCostTracking: Using test data due to error: {ex.Message}");
                LoadTestData();
            }
        }

        #region Properties

        public ObservableCollection<Job> AvailableJobs
        {
            get => _availableJobs;
            set => SetProperty(ref _availableJobs, value);
        }

        public Job SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (SetProperty(ref _selectedJob, value))
                {
                    LoadJobCostData();
                }
            }
        }

        public string ActiveTab
        {
            get => _activeTab;
            set => SetProperty(ref _activeTab, value);
        }

        // Sample cost data properties for display
        public decimal EstimatedTotal => 40807.29m;
        public decimal ActualTotal => 21780.98m;
        public decimal Profit => EstimatedTotal - ActualTotal;
        public decimal ProfitPercentage => (Profit / EstimatedTotal) * 100;
        public decimal TotalEstimatedHours => 268.25m;
        public decimal TotalActualHours => 263m;

        #endregion

        #region Commands

        public ICommand PrintReportCommand { get; }

        #endregion

        #region Private Methods

        private void LoadJobs()
        {
            var allJobs = _databaseService.GetAllJobs();
            AvailableJobs = new ObservableCollection<Job>(allJobs);
        }

        private void LoadTestData()
        {
            AvailableJobs = new ObservableCollection<Job>
            {
                new Job 
                { 
                    JobId = 1, 
                    JobNumber = "619", 
                    JobName = "Smith Residence", 
                    Address = "2315 Riverside Terr, Manasquan, NJ",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 3, 15),
                    TotalEstimate = 40807.29m,
                    TotalActual = 21780.98m,
                    Customer = new Customer { Name = "Smith Residence" }
                },
                new Job 
                { 
                    JobId = 2, 
                    JobNumber = "621", 
                    JobName = "Bayshore Contractors", 
                    Address = "789 Beach Blvd, Belmar, NJ",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 4, 25),
                    TotalEstimate = 15780.50m,
                    TotalActual = 6250.75m,
                    Customer = new Customer { Name = "Bayshore Contractors" }
                },
                new Job 
                { 
                    JobId = 3, 
                    JobNumber = "623", 
                    JobName = "MPC Builders - Shore House", 
                    Address = "555 Seaside Dr, Brielle, NJ",
                    Status = "In Progress",
                    CreateDate = new DateTime(2025, 5, 1),
                    TotalEstimate = 85690.75m,
                    TotalActual = 22450.30m,
                    Customer = new Customer { Name = "MPC Builders - Shore House" }
                }
            };

            if (AvailableJobs.Count > 0)
            {
                SelectedJob = AvailableJobs.First();
            }
        }

        private void LoadJobCostData()
        {
            if (SelectedJob == null) return;

            try
            {
                // This is where you would load job-specific cost data from the database
                // For now, we'll just update the UI to show that data is loading
                OnPropertyChanged(nameof(EstimatedTotal));
                OnPropertyChanged(nameof(ActualTotal));
                OnPropertyChanged(nameof(Profit));
                OnPropertyChanged(nameof(ProfitPercentage));
                OnPropertyChanged(nameof(TotalEstimatedHours));
                OnPropertyChanged(nameof(TotalActualHours));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading job cost data: {ex.Message}");
            }
        }

        private void PrintReport()
        {
            try
            {
                if (SelectedJob == null)
                {
                    System.Windows.MessageBox.Show("Please select a job first.", "No Job Selected");
                    return;
                }

                var message = $"Job Cost Report for Job {SelectedJob.JobNumber}\n\n" +
                             $"Customer: {SelectedJob.Customer?.Name ?? SelectedJob.JobName}\n" +
                             $"Address: {SelectedJob.Address}\n" +
                             $"Status: {SelectedJob.Status}\n\n" +
                             $"Estimated Total: ${EstimatedTotal:N2}\n" +
                             $"Actual Total: ${ActualTotal:N2}\n" +
                             $"Profit: ${Profit:N2} ({ProfitPercentage:F1}%)\n\n" +
                             $"Estimated Hours: {TotalEstimatedHours}\n" +
                             $"Actual Hours: {TotalActualHours}\n\n" +
                             $"Print functionality will be implemented next.";

                System.Windows.MessageBox.Show(message, "Job Cost Report");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Error");
            }
        }

        #endregion
    }
}
