using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class CustomerSelectionViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Customer> _filteredCustomers;
        private Customer _selectedCustomer;
        private string _searchText;
        
        public CustomerSelectionViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            
            Customers = new ObservableCollection<Customer>();
            FilteredCustomers = new ObservableCollection<Customer>();
            
            // Initialize commands
            SelectCommand = new RelayCommand(ExecuteSelect, CanExecuteSelect);
            NewCustomerCommand = new RelayCommand(ExecuteNewCustomer);
            
            LoadCustomers();
        }
        
        #region Properties
        
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }
        
        public ObservableCollection<Customer> FilteredCustomers
        {
            get => _filteredCustomers;
            set => SetProperty(ref _filteredCustomers, value);
        }
        
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                SetProperty(ref _selectedCustomer, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterCustomers();
            }
        }
        
        #endregion
        
        #region Commands
        
        public ICommand SelectCommand { get; }
        public ICommand NewCustomerCommand { get; }
        
        #endregion
        
        #region Command Implementations
        
        private bool CanExecuteSelect(object parameter)
        {
            return SelectedCustomer != null;
        }
        
        private void ExecuteSelect(object parameter)
        {
            CloseRequested?.Invoke(this, true);
        }
        
        private void ExecuteNewCustomer(object parameter)
        {
            var dialog = new AddCustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                var customer = dialog.Customer;
                _databaseService.SaveCustomer(customer);
                LoadCustomers();
                
                // Select the newly added customer
                SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                
                // Auto-close with the new customer selected
                if (SelectedCustomer != null)
                {
                    CloseRequested?.Invoke(this, true);
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadCustomers()
        {
            var customers = _databaseService.GetAllCustomers();
            
            Customers.Clear();
            foreach (var customer in customers.OrderBy(c => c.Name))
            {
                Customers.Add(customer);
            }
            
            FilterCustomers();
        }
        
        private void FilterCustomers()
        {
            var query = Customers.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(c => 
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.Address?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Phone?.ToLower().Contains(searchLower) ?? false) ||
                    (c.Email?.ToLower().Contains(searchLower) ?? false));
            }
            
            FilteredCustomers.Clear();
            foreach (var customer in query)
            {
                FilteredCustomers.Add(customer);
            }
        }
        
        #endregion
        
        #region Events
        
        public event EventHandler<bool> CloseRequested;
        
        #endregion
    }
}
