using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class EstimateSelectionViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Estimate> _estimates;
        private ObservableCollection<Estimate> _filteredEstimates;
        private Estimate _selectedEstimate;
        private string _searchText;
        private EstimateStatus? _statusFilter;
        
        public EstimateSelectionViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            Estimates = new ObservableCollection<Estimate>();
            FilteredEstimates = new ObservableCollection<Estimate>();
            
            // Initialize commands
            SelectCommand = new RelayCommand(ExecuteSelect, CanExecuteSelect);
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            
            LoadEstimates();
        }
        
        #region Properties
        
        public ObservableCollection<Estimate> Estimates
        {
            get => _estimates;
            set => SetProperty(ref _estimates, value);
        }
        
        public ObservableCollection<Estimate> FilteredEstimates
        {
            get => _filteredEstimates;
            set => SetProperty(ref _filteredEstimates, value);
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
                FilterEstimates();
            }
        }
        
        public EstimateStatus? StatusFilter
        {
            get => _statusFilter;
            set
            {
                SetProperty(ref _statusFilter, value);
                FilterEstimates();
            }
        }
        
        // Show only approved estimates by default when converting to jobs
        public bool ShowOnlyApproved { get; set; } = false;
        
        #endregion
        
        #region Commands
        
        public ICommand SelectCommand { get; }
        public ICommand RefreshCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private bool CanExecuteSelect(object parameter)
        {
            return SelectedEstimate != null && 
                   (!ShowOnlyApproved || SelectedEstimate.Status == EstimateStatus.Approved);
        }
        
        private void ExecuteSelect(object parameter)
        {
            CloseRequested?.Invoke(this, true);
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
            
            FilterEstimates();
        }
        
        private void FilterEstimates()
        {
            var query = Estimates.AsEnumerable();
            
            // Apply status filter
            if (StatusFilter.HasValue)
            {
                query = query.Where(e => e.Status == StatusFilter.Value);
            }
            
            // Apply "approved only" filter for job conversion
            if (ShowOnlyApproved)
            {
                query = query.Where(e => e.Status == EstimateStatus.Approved);
            }
            
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(e => 
                    e.EstimateNumber.ToLower().Contains(searchLower) ||
                    e.JobName.ToLower().Contains(searchLower) ||
                    (e.Customer?.Name.ToLower().Contains(searchLower) ?? false) ||
                    (e.Address?.ToLower().Contains(searchLower) ?? false));
            }
            
            FilteredEstimates.Clear();
            foreach (var estimate in query)
            {
                FilteredEstimates.Add(estimate);
            }
        }
        
        #endregion
        
        #region Events
        
        public event EventHandler<bool> CloseRequested;
        
        #endregion
    }
}
