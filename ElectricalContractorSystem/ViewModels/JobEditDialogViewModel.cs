using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for the Job Edit Dialog
    /// </summary>
    public class JobEditDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly int _jobId;
        private Job _originalJob;

        // Basic Properties
        private string _windowTitle;
        private string _jobNumberDisplay;
        private string _jobNumber;
        private string _jobName;
        private string _address;
        private string _city;
        private string _state;
        private string _zip;
        private int? _squareFootage;
        private int? _numFloors;
        private string _status;
        private DateTime _createDate;
        private DateTime? _completionDate;
        private decimal? _totalEstimate;
        private decimal? _totalActual;
        private string _notes;

        // Customer Properties
        private int _customerId;
        private string _customerPhone;
        private string _customerEmail;

        // Collections
        private ObservableCollection<string> _statusOptions;
        private ObservableCollection<Customer> _customers;

        // Events
        public event EventHandler<bool?> RequestClose;

        #region Properties

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public string JobNumberDisplay
        {
            get => _jobNumberDisplay;
            set => SetProperty(ref _jobNumberDisplay, value);
        }

        public string JobNumber
        {
            get => _jobNumber;
            set => SetProperty(ref _jobNumber, value);
        }

        public string JobName
        {
            get => _jobName;
            set => SetProperty(ref _jobName, value);
        }

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string City
        {
            get => _city;
            set => SetProperty(ref _city, value);
        }

        public string State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        public string Zip
        {
            get => _zip;
            set => SetProperty(ref _zip, value);
        }

        public int? SquareFootage
        {
            get => _squareFootage;
            set => SetProperty(ref _squareFootage, value);
        }

        public int? NumFloors
        {
            get => _numFloors;
            set => SetProperty(ref _numFloors, value);
        }

        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    // Update completion date based on status
                    if (value == "Complete" && CompletionDate == null)
                    {
                        CompletionDate = DateTime.Now;
                    }
                    else if (value != "Complete")
                    {
                        CompletionDate = null;
                    }
                }
            }
        }

        public DateTime CreateDate
        {
            get => _createDate;
            set => SetProperty(ref _createDate, value);
        }

        public DateTime? CompletionDate
        {
            get => _completionDate;
            set => SetProperty(ref _completionDate, value);
        }

        public decimal? TotalEstimate
        {
            get => _totalEstimate;
            set => SetProperty(ref _totalEstimate, value);
        }

        public decimal? TotalActual
        {
            get => _totalActual;
            set => SetProperty(ref _totalActual, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public int CustomerId
        {
            get => _customerId;
            set
            {
                if (SetProperty(ref _customerId, value))
                {
                    UpdateCustomerInfo();
                }
            }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set => SetProperty(ref _customerPhone, value);
        }

        public string CustomerEmail
        {
            get => _customerEmail;
            set => SetProperty(ref _customerEmail, value);
        }

        public ObservableCollection<string> StatusOptions
        {
            get => _statusOptions;
            set => SetProperty(ref _statusOptions, value);
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        /// <summary>
        /// Constructor for editing existing job
        /// </summary>
        public JobEditDialogViewModel(int jobId) : this(new DatabaseService(), jobId)
        {
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public JobEditDialogViewModel(DatabaseService databaseService, int jobId)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _jobId = jobId;

            // Initialize commands
            SaveCommand = new RelayCommand(SaveJob, CanSaveJob);
            CancelCommand = new RelayCommand(Cancel);

            // Initialize collections
            StatusOptions = new ObservableCollection<string>
            {
                "Estimate",
                "In Progress", 
                "Complete"
            };

            // Load data
            LoadCustomers();
            LoadJob();
        }

        /// <summary>
        /// Loads customer data
        /// </summary>
        private void LoadCustomers()
        {
            try
            {
                var customers = _databaseService.GetAllCustomers();
                Customers = new ObservableCollection<Customer>(customers);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading customers: {ex.Message}", "Error");
                Customers = new ObservableCollection<Customer>();
            }
        }

        /// <summary>
        /// Loads job data for editing
        /// </summary>
        private void LoadJob()
        {
            if (_jobId == 0)
            {
                // New job
                SetupNewJob();
                return;
            }

            try
            {
                _originalJob = _databaseService.GetJobById(_jobId);
                if (_originalJob == null)
                {
                    System.Windows.MessageBox.Show("Job not found.", "Error");
                    RequestClose?.Invoke(this, false);
                    return;
                }

                // Populate form fields
                WindowTitle = "Edit Job";
                JobNumberDisplay = $"Job #{_originalJob.JobNumber}";
                JobNumber = _originalJob.JobNumber;
                JobName = _originalJob.JobName;
                Address = _originalJob.Address;
                City = _originalJob.City;
                State = _originalJob.State;
                Zip = _originalJob.Zip;
                SquareFootage = _originalJob.SquareFootage;
                NumFloors = _originalJob.NumFloors;
                Status = _originalJob.Status;
                CreateDate = _originalJob.CreateDate;
                CompletionDate = _originalJob.CompletionDate;
                TotalEstimate = _originalJob.TotalEstimate;
                TotalActual = _originalJob.TotalActual;
                Notes = _originalJob.Notes;
                CustomerId = _originalJob.CustomerId;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading job: {ex.Message}", "Error");
                RequestClose?.Invoke(this, false);
            }
        }

        /// <summary>
        /// Sets up form for new job creation
        /// </summary>
        private void SetupNewJob()
        {
            WindowTitle = "New Job";
            JobNumberDisplay = "New Job";
            
            try
            {
                JobNumber = _databaseService.GetNextJobNumber();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error getting next job number: {ex.Message}", "Error");
                JobNumber = "NEW";
            }

            // Set defaults
            Status = "Estimate";
            CreateDate = DateTime.Now;
            State = "NJ"; // Default state
            TotalEstimate = 0;
            TotalActual = 0;
        }

        /// <summary>
        /// Updates customer contact information when customer changes
        /// </summary>
        private void UpdateCustomerInfo()
        {
            var customer = Customers?.FirstOrDefault(c => c.CustomerId == CustomerId);
            if (customer != null)
            {
                CustomerPhone = customer.Phone;
                CustomerEmail = customer.Email;
            }
            else
            {
                CustomerPhone = string.Empty;
                CustomerEmail = string.Empty;
            }
        }

        /// <summary>
        /// Saves the job
        /// </summary>
        private void SaveJob()
        {
            try
            {
                var job = CreateJobFromForm();
                
                if (_jobId == 0)
                {
                    // Create new job
                    _databaseService.AddJob(job);
                    System.Windows.MessageBox.Show($"Job {job.JobNumber} created successfully!", "Success");
                }
                else
                {
                    // Update existing job
                    job.JobId = _jobId;
                    _databaseService.UpdateJob(job);
                    System.Windows.MessageBox.Show($"Job {job.JobNumber} updated successfully!", "Success");
                }

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving job: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Creates a Job object from the form data
        /// </summary>
        private Job CreateJobFromForm()
        {
            return new Job
            {
                JobId = _jobId,
                JobNumber = JobNumber?.Trim(),
                CustomerId = CustomerId,
                JobName = JobName?.Trim(),
                Address = Address?.Trim(),
                City = City?.Trim(),
                State = State?.Trim(),
                Zip = Zip?.Trim(),
                SquareFootage = SquareFootage,
                NumFloors = NumFloors,
                Status = Status,
                CreateDate = CreateDate,
                CompletionDate = CompletionDate,
                TotalEstimate = TotalEstimate ?? 0,
                TotalActual = TotalActual ?? 0,
                Notes = Notes?.Trim()
            };
        }

        /// <summary>
        /// Determines if the job can be saved
        /// </summary>
        private bool CanSaveJob()
        {
            return !string.IsNullOrWhiteSpace(JobNumber) &&
                   !string.IsNullOrWhiteSpace(JobName) &&
                   CustomerId > 0 &&
                   !string.IsNullOrWhiteSpace(Status);
        }

        /// <summary>
        /// Cancels editing and closes dialog
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }
    }
}