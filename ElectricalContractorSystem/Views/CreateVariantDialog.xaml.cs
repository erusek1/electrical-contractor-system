using System.Windows;

namespace ElectricalContractorSystem.Views
{
    public partial class CreateVariantDialog : Window
    {
        public string VariantName { get; set; }
        public string ParentAssemblyName { get; set; }

        public CreateVariantDialog(string parentAssemblyName)
        {
            InitializeComponent();
            ParentAssemblyName = parentAssemblyName;
            DataContext = this;
            
            // Focus on the variant name textbox
            Loaded += (s, e) => VariantNameTextBox.Focus();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VariantName))
            {
                MessageBox.Show("Please enter a name for the variant.", "Validation Error", 
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
