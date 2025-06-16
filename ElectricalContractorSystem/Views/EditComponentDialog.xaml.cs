using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class EditComponentDialog : Window
    {
        public AssemblyComponent Component { get; set; }
        public string ComponentName => $"{Component?.ItemCode} - {Component?.ItemName}";
        public decimal Quantity { get; set; }
        public string Notes { get; set; }

        public EditComponentDialog(AssemblyComponent component)
        {
            InitializeComponent();
            Component = component;
            Quantity = component.Quantity;
            Notes = component.Notes;
            DataContext = this;
            
            // Focus on the quantity textbox
            Loaded += (s, e) => QuantityTextBox.Focus();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than zero.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Component.Quantity = Quantity;
            Component.Notes = Notes;

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
