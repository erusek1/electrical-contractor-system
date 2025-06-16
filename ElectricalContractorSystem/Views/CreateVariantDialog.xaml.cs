using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class CreateVariantDialog : Window
    {
        public AssemblyTemplate ParentAssembly { get; set; }
        public string VariantName { get; set; }
        public bool UseSameCode { get; set; } = true;
        public string ParentAssemblyName => ParentAssembly?.Name ?? "";

        public CreateVariantDialog(AssemblyTemplate parentAssembly)
        {
            InitializeComponent();
            ParentAssembly = parentAssembly;
            DataContext = this;
            
            // Focus on the name textbox
            Loaded += (s, e) => VariantNameTextBox.Focus();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VariantName))
            {
                MessageBox.Show("Please enter a variant name.", "Validation Error", 
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
