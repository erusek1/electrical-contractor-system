using System;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class EstimateConversionViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly EstimateConversionService _conversionService;
        private readonly Estimate _sourceEstimate;
        
        private string _jobNumber;
        private bool _includeAllStages = true;
        private bool _includeMaterialCosts = true;
        private bool _includeRoomSpecifications = true;
        private bool _includePermitItems = true;
        private string _notes;
        private Job _createdJob;
        private bool _isConverting;
        private string _conversionStatus;
        
        public EstimateConversionViewModel(DatabaseService databaseService, Estimate sourceEstimate)
        {
            _databaseService = databaseService;
            _conversionService = new EstimateConversionService(databaseService);
            _sourceEstimate = sourceEstimate;
            
            // Initialize commands
            ConvertCommand = new RelayCommand(ExecuteConvert, CanExecuteConvert);
            CancelCommand = new RelayCommand(ExecuteCancel);
            
            // Generate next job number
            GenerateJobNumber();
        }
        
        #region Properties
        
        public Estimate SourceEstimate => _sourceEstimate;
        
        public string EstimateInfo => $"Estimate {_sourceEstimate.EstimateNumber} v{_sourceEstimate.Version}";
        
        public string CustomerInfo => $"{_sourceEstimate.Customer?.Name} - {_sourceEstimate.JobName}";
        
        public string JobNumber
        {
            get => _jobNumber;
            set
            {
                SetProperty(ref _jobNumber, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public bool IncludeAllStages
        {
            get => _includeAllStages;
            set => SetProperty(ref _includeAllStages, value);
        }
        
        public bool IncludeMaterialCosts
        {
            get => _includeMaterialCosts;
            set => SetProperty(ref _includeMaterialCosts, value);
        }
        
        public bool IncludeRoomSpecifications
        {
            get => _includeRoomSpecifications;
            set => SetProperty(ref _includeRoomSpecifications, value);
        }
        
        public bool IncludePermitItems
        {
            get => _includePermitItems;
            set => SetProperty(ref _includePermitItems, value);
        }
        
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
        
        public Job CreatedJob
        {
            get => _createdJob;
            private set => SetProperty(ref _createdJob, value);
        }
        
        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                SetProperty(ref _isConverting, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string ConversionStatus
        {
            get => _conversionStatus;
            set => SetProperty(ref _conversionStatus, value);
        }
        
        #endregion
        
        #region Commands
        
        public ICommand ConvertCommand { get; }
        public ICommand CancelCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private bool CanExecuteConvert(object parameter)
        {
            return !string.IsNullOrWhiteSpace(JobNumber) && 
                   !IsConverting &&
                   _sourceEstimate.Status == EstimateStatus.Approved;
        }
        
        private async void ExecuteConvert(object parameter)
        {
            try
            {
                IsConverting = true;
                ConversionStatus = "Starting conversion...";
                
                var options = new ConversionOptions
                {
                    IncludeAllStages = IncludeAllStages,
                    IncludeMaterialCosts = IncludeMaterialCosts,
                    IncludeRoomSpecifications = IncludeRoomSpecifications,
                    IncludePermitItems = IncludePermitItems,
                    JobNumber = JobNumber,
                    Notes = Notes
                };
                
                // Update status
                ConversionStatus = "Creating job...";
                CreatedJob = await _conversionService.ConvertEstimateToJobAsync(_sourceEstimate, options);
                
                if (CreatedJob != null)
                {
                    ConversionStatus = "Conversion completed successfully!";
                    
                    // Mark estimate as converted
                    _sourceEstimate.ConvertedToJobId = CreatedJob.JobId;
                    _sourceEstimate.ConvertedDate = DateTime.Now;
                    _databaseService.SaveEstimate(_sourceEstimate);
                    
                    // Raise completion event
                    ConversionCompleted?.Invoke(this, true);
                }
                else
                {
                    ConversionStatus = "Conversion failed. Please check the logs.";
                }
            }
            catch (Exception ex)
            {
                ConversionStatus = $"Error: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"An error occurred during conversion:\n{ex.Message}",
                    "Conversion Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsConverting = false;
            }
        }
        
        private void ExecuteCancel(object parameter)
        {
            ConversionCompleted?.Invoke(this, false);
        }
        
        #endregion
        
        #region Private Methods
        
        private void GenerateJobNumber()
        {
            // Get the next available job number
            var lastJob = _databaseService.GetLastJobNumber();
            if (!string.IsNullOrEmpty(lastJob) && int.TryParse(lastJob, out int lastNumber))
            {
                JobNumber = (lastNumber + 1).ToString();
            }
            else
            {
                // Start from a default if no jobs exist
                JobNumber = "1001";
            }
        }
        
        #endregion
        
        #region Events
        
        public event EventHandler<bool> ConversionCompleted;
        
        #endregion
    }
}
