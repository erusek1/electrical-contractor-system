using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class CreateVariantDialog : Window, INotifyPropertyChanged
    {
        private string _variantName;
        private int? _roughMinutes;
        private int? _finishMinutes;
        private int? _serviceMinutes;
        private int? _extraMinutes;
        
        public CreateVariantDialog(AssemblyTemplate baseAssembly)
        {
            InitializeComponent();
            DataContext = this;
            
            BaseAssembly = baseAssembly;
            ComponentChanges = new Dictionary<int, decimal>();
            
            // Focus on variant name textbox
            Loaded += (s, e) => VariantNameTextBox.Focus();
        }
        
        public AssemblyTemplate BaseAssembly { get; }
        
        public string VariantName
        {
            get => _variantName;
            set
            {
                _variantName = value;
                OnPropertyChanged();
            }
        }
        
        public int? RoughMinutes
        {
            get => _roughMinutes;
            set
            {
                _roughMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int? FinishMinutes
        {
            get => _finishMinutes;
            set
            {
                _finishMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int? ServiceMinutes
        {
            get => _serviceMinutes;
            set
            {
                _serviceMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public int? ExtraMinutes
        {
            get => _extraMinutes;
            set
            {
                _extraMinutes = value;
                OnPropertyChanged();
            }
        }
        
        public Dictionary<int, decimal> ComponentChanges { get; }
        
        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(VariantName))
            {
                MessageBox.Show("Variant Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                VariantNameTextBox.Focus();
                return;
            }
            
            // If labor minutes are specified, add them to component changes
            // Using negative IDs to indicate labor adjustments
            if (RoughMinutes.HasValue)
                ComponentChanges[-1] = RoughMinutes.Value;
            if (FinishMinutes.HasValue)
                ComponentChanges[-2] = FinishMinutes.Value;
            if (ServiceMinutes.HasValue)
                ComponentChanges[-3] = ServiceMinutes.Value;
            if (ExtraMinutes.HasValue)
                ComponentChanges[-4] = ExtraMinutes.Value;
            
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
