using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for PropertyManagementView.xaml
    /// </summary>
    public partial class PropertyManagementView : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly PropertyService _propertyService;
        private ObservableCollection<CustomerWithProperties> _customersWithProperties;
        private Property _selectedProperty;
        private List<Customer> _allCustomers;

        public PropertyManagementView(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _propertyService = new PropertyService(databaseService);
            _customersWithProperties = new ObservableCollection<CustomerWithProperties>();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                StatusText.Text = "Loading data...";
                _customersWithProperties.Clear();

                // Load all customers
                _allCustomers = _databaseService.GetAllCustomers();
                
                // For each customer, load their properties
                foreach (var customer in _allCustomers)
                {
                    var properties = _propertyService.GetCustomerProperties(customer.CustomerId);
                    
                    if (properties.Count > 0)
                    {
                        var customerWithProps = new CustomerWithProperties
                        {
                            CustomerId = customer.CustomerId,
                            Name = customer.Name,
                            Properties = new ObservableCollection<Property>(properties)
                        };
                        _customersWithProperties.Add(customerWithProps);
                    }
                }

                CustomerTreeView.ItemsSource = _customersWithProperties;
                UpdateStatusBar();
                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error loading data";
            }
        }

        private void UpdateStatusBar()
        {
            CustomerCountText.Text = $"{_customersWithProperties.Count} Customers";
            PropertyCountText.Text = $"{_customersWithProperties.Sum(c => c.Properties.Count)} Properties";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void AddPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPropertyDialog(_databaseService, _allCustomers);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                
                // Select the newly added property
                var newProperty = dialog.CreatedProperty;
                if (newProperty != null)
                {
                    SelectProperty(newProperty);
                }
            }
        }

        private void EditPropertyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProperty == null) return;

            var dialog = new EditPropertyDialog(_databaseService, _selectedProperty);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
                SelectProperty(_selectedProperty);
            }
        }

        private void AddJobButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProperty == null) return;

            var dialog = new AddJobAtPropertyDialog(_databaseService, _selectedProperty);
            if (dialog.ShowDialog() == true)
            {
                LoadPropertyJobs();
                StatusText.Text = "New job created successfully";
            }
        }

        private void CustomerTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Property property)
            {
                _selectedProperty = property;
                ShowPropertyDetails(property);
                LoadPropertyJobs();
            }
            else
            {
                _selectedProperty = null;
                HidePropertyDetails();
            }
        }

        private void ShowPropertyDetails(Property property)
        {
            PropertyDetailsPanel.Visibility = Visibility.Visible;
            JobsHeaderPanel.Visibility = Visibility.Visible;
            JobsDataGrid.Visibility = Visibility.Visible;
            NoSelectionPanel.Visibility = Visibility.Collapsed;

            PropertyAddressText.Text = property.FullAddress;
            PropertyCustomerText.Text = property.Customer?.Name ?? "Unknown Customer";
            PropertyTypeText.Text = property.PropertyType;
            SquareFootageText.Text = property.SquareFootage?.ToString("N0") ?? "N/A";
            YearBuiltText.Text = property.YearBuilt?.ToString() ?? "N/A";
            NumFloorsText.Text = property.NumFloors?.ToString() ?? "N/A";
            ElectricalPanelText.Text = string.IsNullOrWhiteSpace(property.ElectricalPanelInfo) ? "No information available" : property.ElectricalPanelInfo;
        }

        private void HidePropertyDetails()
        {
            PropertyDetailsPanel.Visibility = Visibility.Collapsed;
            JobsHeaderPanel.Visibility = Visibility.Collapsed;
            JobsDataGrid.Visibility = Visibility.Collapsed;
            NoSelectionPanel.Visibility = Visibility.Visible;
        }

        private void LoadPropertyJobs()
        {
            if (_selectedProperty == null) return;

            try
            {
                var jobs = _propertyService.GetPropertyJobs(_selectedProperty.PropertyId);
                JobsDataGrid.ItemsSource = jobs;
                
                TotalJobsText.Text = jobs.Count.ToString();
                ActiveJobsText.Text = jobs.Count(j => j.Status != "Complete").ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading jobs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectProperty(Property property)
        {
            // Find and expand the customer node
            foreach (var customer in _customersWithProperties)
            {
                if (customer.CustomerId == property.CustomerId)
                {
                    // Find the property in the collection
                    var prop = customer.Properties.FirstOrDefault(p => p.PropertyId == property.PropertyId);
                    if (prop != null)
                    {
                        // This is a bit tricky with TreeView, might need to use a different approach
                        // For now, just show the details
                        _selectedProperty = prop;
                        ShowPropertyDetails(prop);
                        LoadPropertyJobs();
                    }
                    break;
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                CustomerTreeView.ItemsSource = _customersWithProperties;
            }
            else
            {
                var filtered = new ObservableCollection<CustomerWithProperties>();
                
                foreach (var customer in _customersWithProperties)
                {
                    var matchingProperties = customer.Properties
                        .Where(p => p.Address.ToLower().Contains(searchText) ||
                                   p.City?.ToLower().Contains(searchText) == true ||
                                   customer.Name.ToLower().Contains(searchText))
                        .ToList();
                    
                    if (matchingProperties.Any() || customer.Name.ToLower().Contains(searchText))
                    {
                        var filteredCustomer = new CustomerWithProperties
                        {
                            CustomerId = customer.CustomerId,
                            Name = customer.Name,
                            Properties = new ObservableCollection<Property>(matchingProperties.Any() ? matchingProperties : customer.Properties)
                        };
                        filtered.Add(filteredCustomer);
                    }
                }
                
                CustomerTreeView.ItemsSource = filtered;
            }
        }

        private void JobsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (JobsDataGrid.SelectedItem is Job job)
            {
                // Open job details - you can implement this to open the job in the main job management view
                MessageBox.Show($"Opening job {job.JobNumber} - {job.JobName}", "Job Details", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Implement navigation to job details
            }
        }
    }

    /// <summary>
    /// Helper class to represent a customer with their properties
    /// </summary>
    public class CustomerWithProperties
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Property> Properties { get; set; }
        
        public string PropertyCountText
        {
            get { return $"{Properties?.Count ?? 0} Properties"; }
        }
    }
}
