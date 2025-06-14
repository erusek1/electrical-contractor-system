using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class UpdatePriceDialog : Window, INotifyPropertyChanged
    {
        private decimal _newPrice;
        private int? _vendorId;
        private string _purchaseOrderNumber;
        private decimal? _quantityPurchased;
        
        public UpdatePriceDialog(Material material)
        {
            InitializeComponent();
            DataContext = this;
            
            Material = material;
            NewPrice = material.CurrentPrice;
            
            // Load vendors
            LoadVendors();
            
            // Update price change info when new price changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NewPrice))
                {
                    UpdatePriceChangeInfo();
                }
            };
            
            UpdatePriceChangeInfo();
            
            // Focus on new price textbox
            Loaded += (s, e) => NewPriceTextBox.Focus();
        }
        
        public Material Material { get; }
        
        public decimal NewPrice
        {
            get => _newPrice;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Price cannot be negative");
                    
                _newPrice = value;
                OnPropertyChanged();
            }
        }
        
        public int? VendorId
        {
            get => _vendorId;
            set
            {
                _vendorId = value;
                OnPropertyChanged();
            }
        }
        
        public string PurchaseOrderNumber
        {
            get => _purchaseOrderNumber;
            set
            {
                _purchaseOrderNumber = value;
                OnPropertyChanged();
            }
        }
        
        public decimal? QuantityPurchased
        {
            get => _quantityPurchased;
            set
            {
                _quantityPurchased = value;
                OnPropertyChanged();
            }
        }
        
        public System.Collections.ObjectModel.ObservableCollection<Vendor> Vendors { get; } = 
            new System.Collections.ObjectModel.ObservableCollection<Vendor>();
        
        private void LoadVendors()
        {
            try
            {
                // Get database service instance
                var dbService = Application.Current.MainWindow.DataContext as DatabaseService;
                if (dbService == null)
                {
                    // Try to create a new instance
                    dbService = new DatabaseService();
                }
                
                var vendors = dbService.GetAllVendors();
                Vendors.Clear();
                
                // Add empty option
                Vendors.Add(new Vendor { VendorId = 0, Name = "(No Vendor)" });
                
                foreach (var vendor in vendors.OrderBy(v => v.Name))
                {
                    Vendors.Add(vendor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vendors: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdatePriceChangeInfo()
        {
            if (Material == null) return;
            
            var currentPrice = Material.CurrentPrice;
            var change = NewPrice - currentPrice;
            var percentChange = currentPrice > 0 ? (change / currentPrice) * 100 : 0;
            
            string changeText = $"Change: ${change:F2} ({percentChange:+0.0;-0.0}%)";
            
            if (Math.Abs(percentChange) >= 15)
            {
                changeText += "\n⚠️ Major price change! This will trigger an immediate alert.";
                PriceChangeText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (Math.Abs(percentChange) >= 5)
            {
                changeText += "\n⚠️ Moderate price change. This will be flagged for review.";
                PriceChangeText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                PriceChangeText.Foreground = System.Windows.Media.Brushes.Black;
            }
            
            PriceChangeText.Text = changeText;
        }
        
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NewPrice == Material.CurrentPrice)
                {
                    MessageBox.Show("New price is the same as current price.", "No Change", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating price: {ex.Message}", "Error", 
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
