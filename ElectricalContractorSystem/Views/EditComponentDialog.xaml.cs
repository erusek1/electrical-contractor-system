using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class EditComponentDialog : Window
    {
        public AssemblyComponent Component { get; set; }
        public decimal Quantity { get; set; }
        public bool IsOptional { get; set; }

        public EditComponentDialog(AssemblyComponent component)
        {
            InitializeComponent();
            
            Component = component;
            Quantity = component.Quantity;
            IsOptional = component.IsOptional;
            
            DataContext = this;
            
            // Focus on quantity textbox
            Loaded += (s, e) => QuantityTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update the component
            Component.Quantity = Quantity;
            Component.IsOptional = IsOptional;

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
