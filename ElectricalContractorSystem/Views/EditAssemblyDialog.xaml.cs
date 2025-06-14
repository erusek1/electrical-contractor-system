using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class EditAssemblyDialog : Window
    {
        public EditAssemblyDialog(AssemblyTemplate assembly)
        {
            InitializeComponent();
            
            // Create a copy so we can cancel changes
            Assembly = new AssemblyTemplate
            {
                AssemblyId = assembly.AssemblyId,
                AssemblyCode = assembly.AssemblyCode,
                Name = assembly.Name,
                Category = assembly.Category,
                Description = assembly.Description,
                RoughMinutes = assembly.RoughMinutes,
                FinishMinutes = assembly.FinishMinutes,
                ServiceMinutes = assembly.ServiceMinutes,
                ExtraMinutes = assembly.ExtraMinutes,
                IsDefault = assembly.IsDefault,
                IsActive = assembly.IsActive,
                CreatedDate = assembly.CreatedDate,
                CreatedBy = assembly.CreatedBy
            };
            
            DataContext = this;
            
            // Focus on name textbox
            Loaded += (s, e) => NameTextBox.Focus();
        }
        
        public AssemblyTemplate Assembly { get; }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(Assembly.Name))
            {
                MessageBox.Show("Assembly Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(Assembly.Category))
            {
                MessageBox.Show("Category is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }
            
            DialogResult = true;
        }
    }
}
