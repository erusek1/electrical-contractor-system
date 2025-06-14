using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class CustomerManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private string _searchText;

        public CustomerManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadCustomers();
            InitializeCommands();
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredCustomers));
            }
        }

        public ObservableCollection<Customer> FilteredCustomers
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return Customers;

                var filtered = Customers.Where(c =>
                    c.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (c.Email != null && c.Email.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (c.Phone != null && c.Phone.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (c.City != null && c.City.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                );

                return new ObservableCollection<Customer>(filtered);
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomerSelected));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredCustomers));
            }
        }

        public bool IsCustomerSelected => SelectedCustomer != null;

        // Commands
        public ICommand AddCustomerCommand { get; private set; }
        public ICommand EditCustomerCommand { get; private set; }
        public ICommand DeleteCustomerCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        private void InitializeCommands()
        {
            AddCustomerCommand = new RelayCommand(AddCustomer);
            EditCustomerCommand = new RelayCommand(EditCustomer, CanEditCustomer);
            DeleteCustomerCommand = new RelayCommand(DeleteCustomer, CanDeleteCustomer);
            RefreshCommand = new RelayCommand(RefreshCustomers);
        }

        private void LoadCustomers()
        {
            try
            {
                var customers = _databaseService.GetAllCustomers();
                Customers = new ObservableCollection<Customer>(customers.OrderBy(c => c.Name));
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error loading customers: {ex.Message}");
                Customers = new ObservableCollection<Customer>();
            }
        }

        private void AddCustomer(object parameter)
        {
            var dialog = new AddCustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                var customer = dialog.Customer;
                _databaseService.SaveCustomer(customer);
                LoadCustomers();
            }
        }

        private void EditCustomer(object parameter)
        {
            if (SelectedCustomer == null) return;

            var dialog = new AddCustomerDialog();
            dialog.Customer = new Customer
            {
                CustomerId = SelectedCustomer.CustomerId,
                Name = SelectedCustomer.Name,
                Address = SelectedCustomer.Address,
                City = SelectedCustomer.City,
                State = SelectedCustomer.State,
                Zip = SelectedCustomer.Zip,
                Email = SelectedCustomer.Email,
                Phone = SelectedCustomer.Phone,
                Notes = SelectedCustomer.Notes
            };

            dialog.Title = "Edit Customer";

            if (dialog.ShowDialog() == true)
            {
                var customer = dialog.Customer;
                _databaseService.UpdateCustomer(customer);
                LoadCustomers();
            }
        }

        private bool CanEditCustomer(object parameter)
        {
            return SelectedCustomer != null;
        }

        private void DeleteCustomer(object parameter)
        {
            if (SelectedCustomer == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete customer '{SelectedCustomer.Name}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                _databaseService.DeleteCustomer(SelectedCustomer.CustomerId);
                LoadCustomers();
            }
        }

        private bool CanDeleteCustomer(object parameter)
        {
            return SelectedCustomer != null;
        }

        private void RefreshCustomers(object parameter)
        {
            LoadCustomers();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}