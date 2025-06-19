using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Dialog for adding new customers
    /// FIXED: Constructor signature to accept DatabaseService and actually save customer
    /// </summary>
    public partial class AddCustomerDialog : Window
    {
        private readonly DatabaseService _databaseService;
        
        public Customer NewCustomer { get; private set; }
        public Customer Customer { get; set; }
        
        public AddCustomerDialog(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService ?? throw new System.ArgumentNullException(nameof(databaseService));
            Customer = new Customer();
            DataContext = this;
            
            // Focus on name field when dialog loads
            Loaded += (s, e) => NameTextBox.Focus();
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Customer.Name))
                {
                    MessageBox.Show("Customer name is required.", "Validation Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }
                
                // Save customer to database
                var customerId = _databaseService.AddCustomer(Customer);
                Customer.CustomerId = customerId;
                
                // Set the result for the calling code
                NewCustomer = Customer;
                DialogResult = true;
                
                MessageBox.Show("Customer saved successfully!", "Success", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving customer: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error in AddCustomerDialog.SaveButton_Click: {ex.Message}");
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}