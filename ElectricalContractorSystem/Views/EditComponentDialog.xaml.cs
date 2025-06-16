using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class EditComponentDialog : Window
    {
        public string ComponentName { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal NewQuantity { get; set; }
        public bool IsOptional { get; set; }

        public EditComponentDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initialize NewQuantity with CurrentQuantity
            NewQuantity = CurrentQuantity;
            
            // Focus on quantity textbox
            Loaded += (s, e) => 
            {
                QuantityTextBox.Focus();
                QuantityTextBox.SelectAll();
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewQuantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
