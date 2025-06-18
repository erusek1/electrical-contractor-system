using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class CustomerSelectionViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Customer> _allCustomers;
        private ObservableCollection<Customer> _filteredCustomers;
        private Customer _selectedCustomer;
        private string _searchText;

        public event Action<Customer> CustomerSelected;
        public event Action DialogCancelled;
        public event PropertyChangedEventHandler PropertyChanged;

        public CustomerSelectionViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadCustomers();
            InitializeCommands();
        }

        public ObservableCollection<Customer> FilteredCustomers
        {
            get => _filteredCustomers;
            set
            {
                _filteredCustomers = value;
                OnPropertyChanged();
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                ((RelayCommand)SelectCommand).RaiseCanExecuteChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterCustomers();
            }
        }

        public ICommand SearchCommand { get; private set; }
        public ICommand SelectCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand NewCustomerCommand { get; private set; }

        private void InitializeCommands()
        {
            SearchCommand = new RelayCommand(_ => FilterCustomers());
            SelectCommand = new RelayCommand(_ => SelectCustomer(), _ => SelectedCustomer != null);
            CancelCommand = new RelayCommand(_ => Cancel());
            NewCustomerCommand = new RelayCommand(_ => CreateNewCustomer());
        }

        private void LoadCustomers()
        {
            try
            {
                _allCustomers = new ObservableCollection<Customer>(_databaseService.GetAllCustomers());
                FilteredCustomers = new ObservableCollection<Customer>(_allCustomers);
            }
            catch (Exception ex)
            {
                // Log error and show empty collection
                FilteredCustomers = new ObservableCollection<Customer>();
            }
        }

        private void FilterCustomers()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCustomers = new ObservableCollection<Customer>(_allCustomers);
            }
            else
            {
                var filtered = _allCustomers.Where(c =>
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(c.Address) && c.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.City) && c.City.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.Phone) && c.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                FilteredCustomers = new ObservableCollection<Customer>(filtered);
            }
        }

        private void SelectCustomer()
        {
            if (SelectedCustomer != null)
            {
                CustomerSelected?.Invoke(SelectedCustomer);
            }
        }

        private void Cancel()
        {
            DialogCancelled?.Invoke();
        }

        private void CreateNewCustomer()
        {
            var addCustomerDialog = new AddCustomerDialog(_databaseService);
            if (addCustomerDialog.ShowDialog() == true && addCustomerDialog.NewCustomer != null)
            {
                // Refresh the customer list
                LoadCustomers();
                
                // Select the newly created customer
                SelectedCustomer = _allCustomers.FirstOrDefault(c => c.CustomerId == addCustomerDialog.NewCustomer.CustomerId);
                
                // Auto-select the new customer
                if (SelectedCustomer != null)
                {
                    CustomerSelected?.Invoke(SelectedCustomer);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}