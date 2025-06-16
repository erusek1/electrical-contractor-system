using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for AddPropertyDialog.xaml
    /// </summary>
    public partial class AddPropertyDialog : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly PropertyService _propertyService;
        private Property _existingProperty;

        public Property CreatedProperty { get; private set; }

        public AddPropertyDialog(DatabaseService databaseService, List<Customer> customers)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _propertyService = new PropertyService(databaseService);

            // Populate customer combo box
            CustomerComboBox.ItemsSource = customers.OrderBy(c => c.Name);
            
            // Set default values
            StateComboBox.SelectedIndex = 0; // NJ
            PropertyTypeComboBox.SelectedIndex = 0; // Residential
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckForExistingProperty();
        }

        private void CheckForExistingProperty()
        {
            if (CustomerComboBox.SelectedItem == null || 
                string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                ExistingPropertyWarning.Visibility = Visibility.Collapsed;
                _existingProperty = null;
                return;
            }

            var customer = (Customer)CustomerComboBox.SelectedItem;
            _existingProperty = _propertyService.FindExistingProperty(
                customer.CustomerId, 
                AddressTextBox.Text.Trim(), 
                CityTextBox.Text.Trim(), 
                (StateComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString());

            ExistingPropertyWarning.Visibility = _existingProperty != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (CustomerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                CustomerComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("Please enter a street address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                AddressTextBox.Focus();
                return;
            }

            // Check if we should use existing property
            if (_existingProperty != null)
            {
                var result = MessageBox.Show(
                    "A property already exists at this address for this customer. Would you like to use the existing property?",
                    "Property Exists",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CreatedProperty = _existingProperty;
                    
                    if (CreateJobCheckBox.IsChecked == true)
                    {
                        // Open dialog to create job at existing property
                        var jobDialog = new AddJobAtPropertyDialog(_databaseService, _existingProperty);
                        jobDialog.ShowDialog();
                    }
                    
                    DialogResult = true;
                    return;
                }
            }

            try
            {
                // Create new property
                var property = new Property
                {
                    CustomerId = (int)CustomerComboBox.SelectedValue,
                    Address = AddressTextBox.Text.Trim(),
                    City = string.IsNullOrWhiteSpace(CityTextBox.Text) ? null : CityTextBox.Text.Trim(),
                    State = (StateComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                    Zip = string.IsNullOrWhiteSpace(ZipTextBox.Text) ? null : ZipTextBox.Text.Trim(),
                    PropertyType = (PropertyTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Residential",
                    ElectricalPanelInfo = string.IsNullOrWhiteSpace(ElectricalPanelTextBox.Text) ? null : ElectricalPanelTextBox.Text.Trim()
                };

                // Parse optional numeric fields
                if (!string.IsNullOrWhiteSpace(SquareFootageTextBox.Text))
                {
                    if (int.TryParse(SquareFootageTextBox.Text, out int sqft))
                    {
                        property.SquareFootage = sqft;
                    }
                    else
                    {
                        MessageBox.Show("Square footage must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        SquareFootageTextBox.Focus();
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(NumFloorsTextBox.Text))
                {
                    if (int.TryParse(NumFloorsTextBox.Text, out int floors))
                    {
                        property.NumFloors = floors;
                    }
                    else
                    {
                        MessageBox.Show("Number of floors must be a valid number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        NumFloorsTextBox.Focus();
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(YearBuiltTextBox.Text))
                {
                    if (int.TryParse(YearBuiltTextBox.Text, out int year))
                    {
                        if (year < 1800 || year > DateTime.Now.Year)
                        {
                            MessageBox.Show($"Year built must be between 1800 and {DateTime.Now.Year}.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            YearBuiltTextBox.Focus();
                            return;
                        }
                        property.YearBuilt = year;
                    }
                    else
                    {
                        MessageBox.Show("Year built must be a valid year.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        YearBuiltTextBox.Focus();
                        return;
                    }
                }

                // Save property
                _propertyService.CreateProperty(property);
                CreatedProperty = property;

                // Create job if requested
                if (CreateJobCheckBox.IsChecked == true)
                {
                    var jobDialog = new AddJobAtPropertyDialog(_databaseService, property);
                    jobDialog.ShowDialog();
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating property: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
