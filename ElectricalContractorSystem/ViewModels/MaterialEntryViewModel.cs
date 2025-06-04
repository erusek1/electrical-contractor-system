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
        private ObservableCollection<MaterialEntry> _recentMaterialEntries;
        private Job _selectedJob;
        private JobStage _selectedStage;
        private Vendor _selectedVendor;
        private DateTime _entryDate;
        private decimal _cost;
        private string _invoiceNumber;
        private decimal _invoiceTotal;
        private string _notes;
        private ObservableCollection<JobStage> _jobStages;

        public MaterialEntryViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            EntryDate = DateTime.Today;
            
            // Load data from database
            LoadActiveJobs();
            LoadVendors();
            LoadRecentEntries();

            // Initialize commands
            SaveEntryCommand = new RelayCommand(() => ExecuteSaveEntry(), () => CanExecuteSaveEntry());
            ClearFormCommand = new RelayCommand(() => ExecuteClearForm());
        }

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

        public ObservableCollection<MaterialEntry> RecentMaterialEntries
        {
            get => _recentMaterialEntries;
            set => SetProperty(ref _recentMaterialEntries, value);
        }

        public Job SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (SetProperty(ref _selectedJob, value))
                {
                    LoadJobStages();
                }
            }
        }

        public ObservableCollection<JobStage> JobStages
        {
            get => _jobStages;
            set => SetProperty(ref _jobStages, value);
        }

        public JobStage SelectedStage
        {
            get => _selectedStage;
            set => SetProperty(ref _selectedStage, value);
        }

        public Vendor SelectedVendor
        {
            get => _selectedVendor;
            set => SetProperty(ref _selectedVendor, value);
        }

        public DateTime EntryDate
        {
            get => _entryDate;
            set => SetProperty(ref _entryDate, value);
        }

        public decimal Cost
        {
            get => _cost;
            set
            {
                if (SetProperty(ref _cost, value))
                {
                    // If invoice total is not set or is equal to previous cost, update it
                    if (_invoiceTotal == 0 || _invoiceTotal == _cost)
                    {
                        InvoiceTotal = value;
                    }
                }
            }
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

        public ICommand SaveEntryCommand { get; }
        public ICommand ClearFormCommand { get; }

        private void LoadActiveJobs()
        {
            // Get all jobs with status "Estimate" or "In Progress"
            ActiveJobs = new ObservableCollection<Job>(
                _databaseService.GetJobsByStatus("Estimate", "In Progress")
            );
        }

        private void LoadVendors()
        {
            Vendors = new ObservableCollection<Vendor>(_databaseService.GetAllVendors());
        }

        private void LoadRecentEntries()
        {
            // Load the 20 most recent material entries
            RecentMaterialEntries = new ObservableCollection<MaterialEntry>(
                _databaseService.GetRecentMaterialEntries(20)
            );
        }

        private void LoadJobStages()
        {
            if (SelectedJob != null)
            {
                JobStages = new ObservableCollection<JobStage>(
                    _databaseService.GetJobStages(SelectedJob.JobId)
                );

                // If no stages exist yet, create default ones
                if (!JobStages.Any())
                {
                    CreateDefaultStages();
                }
            }
            else
            {
                JobStages = new ObservableCollection<JobStage>();
            }
        }

        private void CreateDefaultStages()
        {
            if (SelectedJob == null) return;

            // Standard stages for a job
            string[] defaultStages = { "Demo", "Rough", "Service", "Finish", "Extra", "Temp Service", "Inspection", "Other" };

            foreach (var stageName in defaultStages)
            {
                var stage = new JobStage
                {
                    JobId = SelectedJob.JobId,
                    StageName = stageName,
                    EstimatedHours = 0,
                    EstimatedMaterialCost = 0,
                    ActualHours = 0,
                    ActualMaterialCost = 0
                };

                // Save to database
                int stageId = _databaseService.AddJobStage(stage);
                stage.StageId = stageId;
                JobStages.Add(stage);
            }
        }

        private bool CanExecuteSaveEntry()
        {
            return SelectedJob != null 
                && SelectedStage != null 
                && SelectedVendor != null 
                && Cost > 0;
        }

        private void ExecuteSaveEntry()
        {
            // Create the material entry
            var materialEntry = new MaterialEntry
            {
                JobId = SelectedJob.JobId,
                StageId = SelectedStage.StageId,
                VendorId = SelectedVendor.VendorId,
                Date = EntryDate,
                Cost = Cost,
                InvoiceNumber = InvoiceNumber,
                InvoiceTotal = InvoiceTotal,
                Notes = Notes
            };

            // Save to database
            int entryId = _databaseService.AddMaterialEntry(materialEntry);
            materialEntry.EntryId = entryId;

            // Update the selected stage's actual material cost
            SelectedStage.ActualMaterialCost += Cost;
            _databaseService.UpdateJobStage(SelectedStage);

            // Add to recent entries and clear form
            RecentMaterialEntries.Insert(0, materialEntry);
            if (RecentMaterialEntries.Count > 20)
            {
                RecentMaterialEntries.RemoveAt(RecentMaterialEntries.Count - 1);
            }

            ExecuteClearForm();
        }

        private void ExecuteClearForm()
        {
            // Keep the selected job and vendor but clear the rest
            SelectedStage = null;
            EntryDate = DateTime.Today;
            Cost = 0;
            InvoiceNumber = string.Empty;
            InvoiceTotal = 0;
            Notes = string.Empty;
        }
    }
}
