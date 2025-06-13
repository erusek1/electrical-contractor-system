using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class EstimateListViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Estimate> _estimates;
        private Estimate _selectedEstimate;
        private string _searchText;
        private EstimateStatus? _statusFilter;
        
        public EstimateListViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            Estimates = new ObservableCollection<Estimate>();
            
            // Initialize commands
            NewEstimateCommand = new RelayCommand(ExecuteNewEstimate);
            EditEstimateCommand = new RelayCommand(ExecuteEditEstimate, CanExecuteEditEstimate);
            DuplicateEstimateCommand = new RelayCommand(ExecuteDuplicateEstimate, CanExecuteEditEstimate);
            DeleteEstimateCommand = new RelayCommand(ExecuteDeleteEstimate, CanExecuteDeleteEstimate);
            ConvertToJobCommand = new RelayCommand(ExecuteConvertToJob, CanExecuteConvertToJob);
            ViewPdfCommand = new RelayCommand(ExecuteViewPdf, CanExecuteEditEstimate);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            
            LoadEstimates();
        }
        
        #region Properties
        
        public ObservableCollection<Estimate> Estimates
        {
            get => _estimates;
            set => SetProperty(ref _estimates, value);
        }
        
        public Estimate SelectedEstimate
        {
            get => _selectedEstimate;
            set
            {
                SetProperty(ref _selectedEstimate, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }
        
        public EstimateStatus? StatusFilter
        {
            get => _statusFilter;
            set
            {
                SetProperty(ref _statusFilter, value);
                ApplyFilters();
            }
        }
        
        // Statistics
        public int TotalEstimates => Estimates.Count;
        public int DraftCount => Estimates.Count(e => e.Status == EstimateStatus.Draft);
        public int SentCount => Estimates.Count(e => e.Status == EstimateStatus.Sent);
        public int ApprovedCount => Estimates.Count(e => e.Status == EstimateStatus.Approved);
        public decimal TotalValue => Estimates.Sum(e => e.TotalPrice); // Fixed: Changed from TotalCost to TotalPrice
        
        #endregion
        
        #region Commands
        
        public ICommand NewEstimateCommand { get; }
        public ICommand EditEstimateCommand { get; }
        public ICommand DuplicateEstimateCommand { get; }
        public ICommand DeleteEstimateCommand { get; }
        public ICommand ConvertToJobCommand { get; }
        public ICommand ViewPdfCommand { get; }
        public ICommand RefreshCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private void ExecuteNewEstimate(object parameter)
        {
            // Show customer selection dialog
            var customerDialog = new CustomerSelectionDialog();
            var customerViewModel = new CustomerSelectionViewModel(_databaseService);
            customerDialog.DataContext = customerViewModel;
            
            if (customerDialog.ShowDialog() == true)
            {
                var customer = customerViewModel.SelectedCustomer;
                if (customer != null)
                {
                    // Open estimate builder with new estimate
                    var estimateBuilder = new EstimateBuilderView();
                    var builderViewModel = new EstimateBuilderViewModel(_databaseService);
                    builderViewModel.CreateNewEstimate(customer);
                    estimateBuilder.DataContext = builderViewModel;
                    
                    // Show as dialog or navigate to view
                    EstimateCreated?.Invoke(builderViewModel);
                }
            }
        }
        
        private bool CanExecuteEditEstimate(object parameter)
        {
            return SelectedEstimate != null;
        }
        
        private void ExecuteEditEstimate(object parameter)
        {
            if (SelectedEstimate != null)
            {
                // Open estimate builder with selected estimate
                var estimateBuilder = new EstimateBuilderView();
                var builderViewModel = new EstimateBuilderViewModel(_databaseService);
                builderViewModel.CurrentEstimate = SelectedEstimate;
                estimateBuilder.DataContext = builderViewModel;
                
                // Show as dialog or navigate to view
                EstimateEditRequested?.Invoke(builderViewModel);
            }
        }
        
        private void ExecuteDuplicateEstimate(object parameter)
        {
            if (SelectedEstimate != null)
            {
                var newEstimate = SelectedEstimate.CreateNewVersion();
                _databaseService.SaveEstimate(newEstimate);
                LoadEstimates();
                
                // Select the new estimate
                SelectedEstimate = Estimates.FirstOrDefault(e => e.EstimateId == newEstimate.EstimateId);
            }
        }
        
        private bool CanExecuteDeleteEstimate(object parameter)
        {
            return SelectedEstimate != null && SelectedEstimate.Status == EstimateStatus.Draft;
        }
        
        private void ExecuteDeleteEstimate(object parameter)
        {
            if (SelectedEstimate != null && 
                System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete estimate {SelectedEstimate.EstimateNumber}?",
                    "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
            {
                _databaseService.DeleteEstimate(SelectedEstimate.EstimateId);
                LoadEstimates();
            }
        }
        
        private bool CanExecuteConvertToJob(object parameter)
        {
            return SelectedEstimate != null && SelectedEstimate.Status == EstimateStatus.Approved;
        }
        
        private void ExecuteConvertToJob(object parameter)
        {
            if (SelectedEstimate != null)
            {
                // Show conversion dialog
                var conversionDialog = new EstimateConversionDialog();
                var conversionViewModel = new EstimateConversionViewModel(_databaseService, SelectedEstimate);
                conversionDialog.DataContext = conversionViewModel;
                
                if (conversionDialog.ShowDialog() == true)
                {
                    LoadEstimates(); // Refresh to show updated status
                    ConversionCompleted?.Invoke(conversionViewModel.CreatedJob);
                }
            }
        }
        
        private void ExecuteViewPdf(object parameter)
        {
            if (SelectedEstimate != null)
            {
                // TODO: Generate and show PDF
                System.Windows.MessageBox.Show("PDF generation will be implemented in Phase 10", "Coming Soon");
            }
        }
        
        private void ExecuteRefresh(object parameter)
        {
            LoadEstimates();
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadEstimates()
        {
            var estimates = _databaseService.GetAllEstimates();
            
            Estimates.Clear();
            foreach (var estimate in estimates.OrderByDescending(e => e.CreatedDate))
            {
                // Load related customer data
                if (estimate.CustomerId > 0)
                {
                    estimate.Customer = _databaseService.GetCustomerById(estimate.CustomerId);
                }
                Estimates.Add(estimate);
            }
            
            // Update statistics
            OnPropertyChanged(nameof(TotalEstimates));
            OnPropertyChanged(nameof(DraftCount));
            OnPropertyChanged(nameof(SentCount));
            OnPropertyChanged(nameof(ApprovedCount));
            OnPropertyChanged(nameof(TotalValue));
            
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            // This would typically filter a separate FilteredEstimates collection
            // For simplicity, we're filtering the main collection
            // In production, you'd want to maintain both full and filtered collections
        }
        
        #endregion
        
        #region Events
        
        public event Action<EstimateBuilderViewModel> EstimateCreated;
        public event Action<EstimateBuilderViewModel> EstimateEditRequested;
        public event Action<Job> ConversionCompleted;
        
        #endregion
    }
}
