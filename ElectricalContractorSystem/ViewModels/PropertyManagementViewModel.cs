using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class PropertyManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private ObservableCollection<Property> _properties;
        private Property _selectedProperty;
        private string _searchText;
        private bool _isLoading;

        public PropertyManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            
            Customers = new ObservableCollection<Customer>();
            Properties = new ObservableCollection<Property>();
            
            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            AddPropertyCommand = new RelayCommand(AddProperty, () => SelectedCustomer != null);
            EditPropertyCommand = new RelayCommand(EditProperty, () => SelectedProperty != null);
            DeletePropertyCommand = new RelayCommand(DeleteProperty, () => SelectedProperty != null);
            AddJobAtPropertyCommand = new RelayCommand(AddJobAtProperty, () => SelectedProperty != null);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            
            // Load initial data
            LoadDataCommand.Execute(null);
        }

        #region Properties

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    LoadPropertiesForCustomer();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ObservableCollection<Property> Properties
        {
            get => _properties;
            set => SetProperty(ref _properties, value);
        }

        public Property SelectedProperty
        {
            get => _selectedProperty;
            set
            {
                if (SetProperty(ref _selectedProperty, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterCustomers();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand AddPropertyCommand { get; }
        public ICommand EditPropertyCommand { get; }
        public ICommand DeletePropertyCommand { get; }
        public ICommand AddJobAtPropertyCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Methods

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                
                var customers = await System.Threading.Tasks.Task.Run(() => _databaseService.GetAllCustomers());
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers.Clear();
                    foreach (var customer in customers)
                    {
                        Customers.Add(customer);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadPropertiesForCustomer()
        {
            if (SelectedCustomer == null)
            {
                Properties.Clear();
                return;
            }

            try
            {
                // Load properties for the selected customer
                var properties = _databaseService.GetPropertiesForCustomer(SelectedCustomer.CustomerId);
                
                Properties.Clear();
                foreach (var property in properties)
                {
                    Properties.Add(property);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading properties: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void FilterCustomers()
        {
            // This would implement filtering based on SearchText
            // For now, just reload all customers
            if (string.IsNullOrEmpty(SearchText))
            {
                LoadDataCommand.Execute(null);
            }
        }

        private void AddProperty()
        {
            try
            {
                var dialog = new AddPropertyDialog();
                var viewModel = new AddPropertyDialogViewModel(_databaseService, SelectedCustomer);
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true && viewModel.Property != null)
                {
                    _databaseService.SaveProperty(viewModel.Property);
                    LoadPropertiesForCustomer();
                    
                    MessageBox.Show("Property added successfully!", 
                        "Success", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding property: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void EditProperty()
        {
            try
            {
                var dialog = new EditPropertyDialog();
                var viewModel = new EditPropertyDialogViewModel(_databaseService, SelectedProperty);
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true)
                {
                    _databaseService.UpdateProperty(SelectedProperty);
                    LoadPropertiesForCustomer();
                    
                    MessageBox.Show("Property updated successfully!", 
                        "Success", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing property: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void DeleteProperty()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete the property at {SelectedProperty.Address}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _databaseService.DeleteProperty(SelectedProperty.PropertyId);
                    Properties.Remove(SelectedProperty);
                    SelectedProperty = null;
                    
                    MessageBox.Show("Property deleted successfully!", 
                        "Success", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting property: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void AddJobAtProperty()
        {
            try
            {
                var dialog = new AddJobAtPropertyDialog();
                var viewModel = new AddJobAtPropertyDialogViewModel(_databaseService, SelectedProperty, SelectedCustomer);
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true && viewModel.Job != null)
                {
                    _databaseService.SaveJob(viewModel.Job);
                    
                    MessageBox.Show($"Job #{viewModel.Job.JobNumber} created successfully!", 
                        "Success", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating job: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }

    // Helper ViewModels for dialogs

    public class AddPropertyDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly Customer _customer;
        private Property _property;

        public AddPropertyDialogViewModel(DatabaseService databaseService, Customer customer)
        {
            _databaseService = databaseService;
            _customer = customer;
            Property = new Property
            {
                CustomerId = customer.CustomerId,
                Customer = customer
            };
        }

        public Property Property
        {
            get => _property;
            set => SetProperty(ref _property, value);
        }
    }

    public class EditPropertyDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private Property _property;

        public EditPropertyDialogViewModel(DatabaseService databaseService, Property property)
        {
            _databaseService = databaseService;
            Property = property;
        }

        public Property Property
        {
            get => _property;
            set => SetProperty(ref _property, value);
        }
    }

    public class AddJobAtPropertyDialogViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly Property _property;
        private readonly Customer _customer;
        private Job _job;

        public AddJobAtPropertyDialogViewModel(DatabaseService databaseService, Property property, Customer customer)
        {
            _databaseService = databaseService;
            _property = property;
            _customer = customer;
            
            Job = new Job
            {
                CustomerId = customer.CustomerId,
                Customer = customer,
                PropertyId = property.PropertyId,
                Property = property,
                Address = property.Address,
                City = property.City,
                State = property.State,
                Zip = property.Zip,
                CreateDate = DateTime.Now,
                Status = JobStatus.Estimate
            };
        }

        public Job Job
        {
            get => _job;
            set => SetProperty(ref _job, value);
        }

        public Property Property => _property;
        public Customer Customer => _customer;
    }
}