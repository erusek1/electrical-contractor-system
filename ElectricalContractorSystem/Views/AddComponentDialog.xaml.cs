using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ElectricalContractorSystem.Views
{
    public partial class AddComponentDialog : Window, INotifyPropertyChanged
    {
        private decimal _quantity = 1;
        private string _notes;
        
        public AddComponentDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            // Focus on quantity textbox and select all
            Loaded += (s, e) =>
            {
                QuantityTextBox.Focus();
                QuantityTextBox.SelectAll();
            };
        }
        
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Quantity must be greater than zero");
                    
                _quantity = value;
                OnPropertyChanged();
            }
        }
        
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }
        
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate quantity
                if (Quantity <= 0)
                {
                    MessageBox.Show("Quantity must be greater than zero.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityTextBox.Focus();
                    return;
                }
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
