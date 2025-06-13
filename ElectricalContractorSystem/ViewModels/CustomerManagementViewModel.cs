using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;
using Microsoft.Win32;
using System.Windows;

namespace ElectricalContractorSystem.ViewModels
{
    public class CustomerManagementViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private string _searchText;
        private string _statusMessage;
        private ICollectionView _filteredCustomers;

        public CustomerManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadCustomers();
            InitializeCommands();
        }

        public ObservableCollection<Customer> Customers
        {
            get { return _customers; }
            set
            {
                _customers = value;
                OnPropertyChanged(nameof(Customers));
            }
        }

        public ICollectionView FilteredCustomers
        {
            get { return _filteredCustomers; }
            set
            {
                _filteredCustomers = value;
                OnPropertyChanged(nameof(FilteredCustomers));
            }
        }

        public Customer SelectedCustomer
        {
            get { return _selectedCustomer; }
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged(nameof(SelectedCustomer));
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilteredCustomers.Refresh();
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        #region Commands

        public ICommand AddCustomerCommand { get; private set; }
        public ICommand SaveCustomerCommand { get; private set; }
        public ICommand DeleteCustomerCommand { get; private set; }
        public ICommand ImportCustomersCommand { get; private set; }
        public ICommand ExportCustomersCommand { get; private set; }
        public ICommand ClearSearchCommand { get; private set; }

        private void InitializeCommands()
        {
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            SaveCustomerCommand = new RelayCommand(ExecuteSaveCustomer);
            DeleteCustomerCommand = new RelayCommand(ExecuteDeleteCustomer);
            ImportCustomersCommand = new RelayCommand(ExecuteImportCustomers);
            ExportCustomersCommand = new RelayCommand(ExecuteExportCustomers);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
        }

        #endregion

        private void LoadCustomers()
        {
            try
            {
                var customerList = _databaseService.GetAllCustomers();
                Customers = new ObservableCollection<Customer>(customerList);
                
                FilteredCustomers = CollectionViewSource.GetDefaultView(Customers);
                FilteredCustomers.Filter = FilterCustomers;
                
                StatusMessage = $"Loaded {Customers.Count} customers";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading customers: {ex.Message}";
            }
        }

        private bool FilterCustomers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var customer = obj as Customer;
            if (customer == null)
                return false;

            var searchLower = SearchText.ToLower();
            return customer.Name.ToLower().Contains(searchLower) ||
                   (customer.Address?.ToLower().Contains(searchLower) ?? false) ||
                   (customer.City?.ToLower().Contains(searchLower) ?? false) ||
                   (customer.Phone?.Contains(searchText) ?? false) ||
                   (customer.Email?.ToLower().Contains(searchLower) ?? false);
        }

        private void ExecuteAddCustomer(object parameter)
        {
            var dialog = new AddCustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                var customer = dialog.Customer;
                _databaseService.SaveCustomer(customer);
                Customers.Add(customer);
                StatusMessage = $"Added customer: {customer.Name}";
            }
        }

        private void ExecuteSaveCustomer(object parameter)
        {
            var customer = parameter as Customer;
            if (customer == null) return;

            try
            {
                _databaseService.SaveCustomer(customer);
                StatusMessage = $"Saved customer: {customer.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteCustomer(object parameter)
        {
            var customer = parameter as Customer;
            if (customer == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete customer '{customer.Name}'?\n\n" +
                "This will not delete any associated jobs or estimates.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // For now, we'll just mark as inactive or remove from list
                    // You may want to implement a soft delete in the database
                    Customers.Remove(customer);
                    StatusMessage = $"Deleted customer: {customer.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteImportCustomers(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Import Customers"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // For CSV import
                    if (Path.GetExtension(openFileDialog.FileName).ToLower() == ".csv")
                    {
                        ImportFromCsv(openFileDialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Excel import will be implemented soon. Please use CSV format for now.", 
                            "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing customers: {ex.Message}", "Import Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportFromCsv(string fileName)
        {
            var lines = File.ReadAllLines(fileName);
            if (lines.Length <= 1)
            {
                MessageBox.Show("CSV file is empty or contains only headers.", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int imported = 0;
            int skipped = 0;

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2) // At minimum need name and some contact info
                {
                    var customer = new Customer
                    {
                        Name = parts[0].Trim(),
                        Address = parts.Length > 1 ? parts[1].Trim() : null,
                        City = parts.Length > 2 ? parts[2].Trim() : null,
                        State = parts.Length > 3 ? parts[3].Trim() : null,
                        Zip = parts.Length > 4 ? parts[4].Trim() : null,
                        Phone = parts.Length > 5 ? parts[5].Trim() : null,
                        Email = parts.Length > 6 ? parts[6].Trim() : null
                    };

                    // Check if customer already exists
                    if (!Customers.Any(c => c.Name.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _databaseService.SaveCustomer(customer);
                        Customers.Add(customer);
                        imported++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }

            StatusMessage = $"Import complete: {imported} added, {skipped} skipped (duplicates)";
        }

        private void ExecuteExportCustomers(object parameter)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Export Customers",
                FileName = $"Customers_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Name,Address,City,State,Zip,Phone,Email");

                    foreach (var customer in Customers)
                    {
                        csv.AppendLine($"{customer.Name},{customer.Address},{customer.City}," +
                                      $"{customer.State},{customer.Zip},{customer.Phone},{customer.Email}");
                    }

                    File.WriteAllText(saveFileDialog.FileName, csv.ToString());
                    StatusMessage = $"Exported {Customers.Count} customers to {Path.GetFileName(saveFileDialog.FileName)}";
                    
                    MessageBox.Show($"Successfully exported {Customers.Count} customers.", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting customers: {ex.Message}", "Export Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteClearSearch(object parameter)
        {
            SearchText = string.Empty;
        }
    }
}