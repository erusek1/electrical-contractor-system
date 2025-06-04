using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class MaterialEntryViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Job> _activeJobs;
        private ObservableCollection<Vendor> _vendors;
        private Job _selectedJob;
        private string _selectedStage;
        private Vendor _selectedVendor;
        private DateTime _selectedDate = DateTime.Today;
        private decimal _cost;
        private string _invoiceNumber;
        private decimal _invoiceTotal;
        private string _notes;

        public MaterialEntryViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            // Initialize commands
            SaveEntryCommand = new RelayCommand(() => SaveEntry(), () => CanSaveEntry());
            ClearFormCommand = new RelayCommand(() => ClearForm());
            
            try
            {
                LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MaterialEntry: Using test data due to error: {ex.Message}");
                LoadTestData();
            }
        }

        #region Properties

        public ObservableCollection<Job> ActiveJobs
        {
            get => _activeJobs;
            set => SetProperty(ref _activeJobs, value);
        }

        public ObservableCollection<Vendor> Vendors
        {
            get => _vendors;
            set => SetProperty(ref _vendors, value);
        }

        public Job SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        public string SelectedStage
        {
            get => _selectedStage;
            set => SetProperty(ref _selectedStage, value);
        }

        public Vendor SelectedVendor
        {
            get => _selectedVendor;
            set => SetProperty(ref _selectedVendor, value);
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public decimal Cost
        {
            get => _cost;
            set => SetProperty(ref _cost, value);
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set => SetProperty(ref _invoiceNumber, value);
        }

        public decimal InvoiceTotal
        {
            get => _invoiceTotal;
            set => SetProperty(ref _invoiceTotal, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public ObservableCollection<string> AvailableStages { get; } = new ObservableCollection<string>
        {
            "Demo", "Rough", "Service", "Finish", "Extra", "Inspection", "Temp Service", "Other"
        };

        #endregion

        #region Commands

        public ICommand SaveEntryCommand { get; }
        public ICommand ClearFormCommand { get; }

        #endregion

        #region Private Methods

        private void LoadData()
        {
            // Load active jobs
            var allJobs = _databaseService.GetAllJobs();
            ActiveJobs = new ObservableCollection<Job>(allJobs.Where(j => j.Status != "Complete"));

            // Load vendors (create test vendors for now)
            LoadTestVendors();
        }

        private void LoadTestData()
        {
            // Test jobs
            ActiveJobs = new ObservableCollection<Job>
            {
                new Job { JobId = 1, JobNumber = "619", JobName = "Smith Residence", Status = "In Progress" },
                new Job { JobId = 2, JobNumber = "621", JobName = "Bayshore Contractors", Status = "In Progress" },
                new Job { JobId = 3, JobNumber = "623", JobName = "MPC Builders - Shore House", Status = "In Progress" }
            };

            LoadTestVendors();
        }

        private void LoadTestVendors()
        {
            Vendors = new ObservableCollection<Vendor>
            {
                new Vendor { VendorId = 1, Name = "Home Depot", Phone = "732-555-0100" },
                new Vendor { VendorId = 2, Name = "Cooper Electric", Phone = "732-555-0200" },
                new Vendor { VendorId = 3, Name = "Warshauer Electric", Phone = "732-555-0300" },
                new Vendor { VendorId = 4, Name = "Good Friend Electric", Phone = "732-555-0400" },
                new Vendor { VendorId = 5, Name = "Lowes", Phone = "732-555-0500" }
            };
        }

        private void SaveEntry()
        {
            try
            {
                if (!CanSaveEntry())
                {
                    System.Windows.MessageBox.Show("Please fill in all required fields.", "Validation Error");
                    return;
                }

                // For now, just show what would be saved
                var message = $"Material Entry to Save:\n\n" +
                             $"Job: {SelectedJob.JobNumber} - {SelectedJob.JobName}\n" +
                             $"Stage: {SelectedStage}\n" +
                             $"Vendor: {SelectedVendor.Name}\n" +
                             $"Date: {SelectedDate:MM/dd/yyyy}\n" +
                             $"Cost: ${Cost:F2}\n" +
                             $"Invoice #: {InvoiceNumber}\n" +
                             $"Invoice Total: ${InvoiceTotal:F2}\n" +
                             $"Notes: {Notes}";

                System.Windows.MessageBox.Show(message, "Save Material Entry");

                // Clear form after successful save
                ClearForm();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving material entry: {ex.Message}", "Error");
            }
        }

        private bool CanSaveEntry()
        {
            return SelectedJob != null && 
                   !string.IsNullOrWhiteSpace(SelectedStage) && 
                   SelectedVendor != null && 
                   Cost > 0;
        }

        private void ClearForm()
        {
            SelectedJob = null;
            SelectedStage = null;
            SelectedVendor = null;
            SelectedDate = DateTime.Today;
            Cost = 0;
            InvoiceNumber = "";
            InvoiceTotal = 0;
            Notes = "";
        }

        #endregion
    }
}