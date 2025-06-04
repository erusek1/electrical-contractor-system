using System;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for NewJobDialog.xaml
    /// </summary>
    public partial class NewJobDialog : Window
    {
        public Job NewJob { get; private set; }

        public NewJobDialog()
        {
            InitializeComponent();
            
            // Set default job number (in real implementation, this would be auto-generated from database)
            JobNumberTextBox.Text = GenerateNextJobNumber();
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(JobNumberTextBox.Text))
                {
                    MessageBox.Show("Job Number is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    JobNumberTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
                {
                    MessageBox.Show("Customer Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CustomerNameTextBox.Focus();
                    return;
                }

                // Parse estimate if provided
                decimal estimate = 0;
                if (!string.IsNullOrWhiteSpace(EstimateTextBox.Text))
                {
                    if (!decimal.TryParse(EstimateTextBox.Text, out estimate))
                    {
                        MessageBox.Show("Please enter a valid estimate amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        EstimateTextBox.Focus();
                        return;
                    }
                }

                // Create the new job object
                NewJob = new Job
                {
                    JobNumber = JobNumberTextBox.Text.Trim(),
                    JobName = CustomerNameTextBox.Text.Trim(),
                    Address = AddressTextBox.Text.Trim(),
                    Status = ((System.Windows.Controls.ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString(),
                    CreateDate = DateTime.Now,
                    TotalEstimate = estimate > 0 ? estimate : (decimal?)null,
                    TotalActual = 0,
                    Customer = new Customer
                    {
                        Name = CustomerNameTextBox.Text.Trim()
                    }
                };

                // Set dialog result and close
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating job: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Generate the next job number (simplified version for demo)
        /// In the real implementation, this would query the database for the highest job number
        /// </summary>
        private string GenerateNextJobNumber()
        {
            // For demo purposes, just use a number based on current year and month
            var now = DateTime.Now;
            var baseNumber = (now.Year - 2020) * 1000 + now.Month * 10 + now.Day % 10;
            return baseNumber.ToString();
        }
    }
}
