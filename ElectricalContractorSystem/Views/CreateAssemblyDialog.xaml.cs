using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class CreateAssemblyDialog : Window, INotifyPropertyChanged
    {
        private string _assemblyCode;
        private string _assemblyName;
        private string _category;
        private string _description;
        private int _roughMinutes;
        private int _finishMinutes;
        private int _serviceMinutes;
        private int _extraMinutes;
        private bool _isDefault;
        
        public CreateAssemblyDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            // Focus on code textbox
            Loaded += (s, e) => CodeTextBox.Focus();
        }
        
        public AssemblyTemplate Assembly { get; private set; }
        
        public string AssemblyCode
        {
            get => _assemblyCode;
            set
            {
                _assemblyCode = value?.ToLower(); // Always lowercase for consistency
                OnPropertyChanged();
            }
        }
        
        public string AssemblyName
        {
            get => _assemblyName;
            set
            {
                _assemblyName = value;
                OnPropertyChanged();
            }
        }
        
        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }
        
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }
        
        public int RoughMinutes
        {
            get => _roughMinutes;
            set
            {
                _roughMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int FinishMinutes
        {
            get => _finishMinutes;
            set
            {
                _finishMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int ServiceMinutes
        {
            get => _serviceMinutes;
            set
            {
                _serviceMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int ExtraMinutes
        {
            get => _extraMinutes;
            set
            {
                _extraMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsDefault
        {
            get => _isDefault;
            set
            {
                _isDefault = value;
                OnPropertyChanged();
            }
        }
        
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(AssemblyCode))
            {
                MessageBox.Show("Assembly Code is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(AssemblyName))
            {
                MessageBox.Show("Assembly Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(Category))
            {
                MessageBox.Show("Category is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }
            
            // Create the assembly
            Assembly = new AssemblyTemplate
            {
                AssemblyCode = AssemblyCode,
                Name = AssemblyName,
                Category = Category,
                Description = Description,
                RoughMinutes = RoughMinutes,
                FinishMinutes = FinishMinutes,
                ServiceMinutes = ServiceMinutes,
                ExtraMinutes = ExtraMinutes,
                IsDefault = IsDefault,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            
            DialogResult = true;
        }
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}
