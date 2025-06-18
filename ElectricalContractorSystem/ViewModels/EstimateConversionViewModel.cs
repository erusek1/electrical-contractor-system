using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Helpers;

namespace ElectricalContractorSystem.ViewModels
{
    public class EstimateConversionViewModel : INotifyPropertyChanged
    {
        private readonly Estimate _estimate;
        private readonly DatabaseService _databaseService;
        private string _jobName;
        private string _newJobNumber;
        private bool _includeLineItems = true;
        private bool _createJobStages = true;
        private bool _setEstimatedCosts = true;
        private bool _markEstimateConverted = true;
        private string _notes;

        public event Action<Job> JobCreated;
        public event Action DialogCancelled;
        public event PropertyChangedEventHandler PropertyChanged;

        public EstimateConversionViewModel(Estimate estimate, DatabaseService databaseService)
        {
            _estimate = estimate ?? throw new ArgumentNullException(nameof(estimate));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            InitializeProperties();
            InitializeCommands();
        }

        private void InitializeProperties()
        {
            JobName = _estimate.JobName;
            NewJobNumber = _databaseService.GetNextJobNumber();
        }

        private void InitializeCommands()
        {
            ConvertCommand = new RelayCommand(_ => ConvertEstimateToJob(), _ => CanConvert());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        // Properties
        public string EstimateNumber => _estimate.EstimateNumber;
        public string CustomerName => _estimate.Customer?.Name ?? "Unknown Customer";
        public decimal TotalValue => _estimate.TotalPrice;

        public string JobName
        {
            get => _jobName;
            set
            {
                _jobName = value;
                OnPropertyChanged();
                ((RelayCommand)ConvertCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewJobNumber
        {
            get => _newJobNumber;
            set
            {
                _newJobNumber = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeLineItems
        {
            get => _includeLineItems;
            set
            {
                _includeLineItems = value;
                OnPropertyChanged();
            }
        }

        public bool CreateJobStages
        {
            get => _createJobStages;
            set
            {
                _createJobStages = value;
                OnPropertyChanged();
            }
        }

        public bool SetEstimatedCosts
        {
            get => _setEstimatedCosts;
            set
            {
                _setEstimatedCosts = value;
                OnPropertyChanged();
            }
        }

        public bool MarkEstimateConverted
        {
            get => _markEstimateConverted;
            set
            {
                _markEstimateConverted = value;
                OnPropertyChanged();
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand ConvertCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private bool CanConvert()
        {
            return !string.IsNullOrWhiteSpace(JobName) && !string.IsNullOrWhiteSpace(NewJobNumber);
        }

        private void ConvertEstimateToJob()
        {
            try
            {
                var conversionOptions = new EstimateToJobConversionOptions
                {
                    JobNumber = NewJobNumber,
                    JobName = JobName,
                    IncludeLineItems = IncludeLineItems,
                    CreateJobStages = CreateJobStages,
                    SetEstimatedCosts = SetEstimatedCosts,
                    MarkEstimateConverted = MarkEstimateConverted,
                    Notes = Notes
                };

                var job = _databaseService.ConvertEstimateToJob(_estimate.EstimateId, conversionOptions);

                if (job != null)
                {
                    JobCreated?.Invoke(job);
                }
                else
                {
                    // Handle conversion failure
                    throw new InvalidOperationException("Failed to convert estimate to job.");
                }
            }
            catch (Exception ex)
            {
                // Log the error and show message to user
                // For now, just throw to be handled by the calling code
                throw new InvalidOperationException($"Error converting estimate to job: {ex.Message}", ex);
            }
        }

        private void Cancel()
        {
            DialogCancelled?.Invoke();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for conversion options
    public class EstimateToJobConversionOptions
    {
        public string JobNumber { get; set; }
        public string JobName { get; set; }
        public bool IncludeLineItems { get; set; }
        public bool CreateJobStages { get; set; }
        public bool SetEstimatedCosts { get; set; }
        public bool MarkEstimateConverted { get; set; }
        public string Notes { get; set; }
    }
}