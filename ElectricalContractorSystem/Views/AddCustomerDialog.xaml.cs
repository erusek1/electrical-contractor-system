using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for AddCustomerDialog.xaml
    /// </summary>
    public partial class AddCustomerDialog : Window
    {
        public Customer Customer { get; private set; }
        
        public AddCustomerDialog()
        {
            InitializeComponent();
            Customer = new Customer();
            DataContext = this;
            
            // Focus on name field
            Loaded += (s, e) => NameTextBox.Focus();
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(Customer.Name))
            {
                MessageBox.Show("Customer name is required.", "Validation Error", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }
            
            DialogResult = true;
        }
    }
}