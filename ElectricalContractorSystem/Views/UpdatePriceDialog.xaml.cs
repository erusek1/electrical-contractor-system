using System;
using System.Collections.ObjectModel;
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
        private readonly DatabaseService _databaseService;
        private readonly PricingService _pricingService;
        private readonly Material _material;
        private decimal _newPrice;
        private Vendor _selectedVendor;
        private string _invoiceNumber;
        private string _notes;

        public UpdatePriceDialog(Material material, DatabaseService databaseService)
        {
            InitializeComponent();
            
            _material = material;
            _databaseService = databaseService;
            _pricingService = new PricingService(databaseService);
            
            DataContext = this;
            
            LoadVendors();
            NewPrice = material.CurrentPrice;
            
            NewPriceTextBox.Focus();
        }

        #region Properties

        public string MaterialName => _material.Name;
        public string MaterialCode => _material.MaterialCode;
        public decimal CurrentPrice => _material.CurrentPrice;

        public decimal NewPrice
        {
            get => _newPrice;
            set
            {
                _newPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PercentageChange));
            }
        }

        public decimal PercentageChange
        {
            get
            {
                if (CurrentPrice == 0) return 0;
                return ((NewPrice - CurrentPrice) / CurrentPrice) * 100;
            }
        }

        public ObservableCollection<Vendor> Vendors { get; private set; }

        public Vendor SelectedVendor
        {
            get => _selectedVendor;
            set
            {
                _selectedVendor = value;
                OnPropertyChanged();
            }
        }

        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                _invoiceNumber = value;
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

        public bool Success { get; private set; }

        #endregion

        #region Methods

        private void LoadVendors()
        {
            Vendors = new ObservableCollection<Vendor>();
            
            try
            {
                var vendors = _databaseService.GetAllVendors();
                foreach (var vendor in vendors.OrderBy(v => v.Name))
                {
                    Vendors.Add(vendor);
                }

                // Select preferred vendor if available
                if (_material.PreferredVendorId.HasValue)
                {
                    SelectedVendor = Vendors.FirstOrDefault(v => v.VendorId == _material.PreferredVendorId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vendors: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate new price
                if (NewPrice <= 0)
                {
                    MessageBox.Show("Price must be greater than zero.", "Invalid Price", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPriceTextBox.Focus();
                    return;
                }

                // Create price history entry
                var priceHistory = new MaterialPriceHistory
                {
                    MaterialId = _material.MaterialId,
                    OldPrice = _material.CurrentPrice,
                    NewPrice = NewPrice,
                    PercentageChange = PercentageChange,
                    ChangedBy = Environment.UserName,
                    ChangeDate = DateTime.Now,
                    EffectiveDate = DateTime.Now,
                    VendorId = SelectedVendor?.VendorId,
                    PurchaseOrderNumber = InvoiceNumber,
                    Notes = Notes
                };

                // Determine alert level
                var absChange = Math.Abs(PercentageChange);
                if (absChange >= 15)
                    priceHistory.AlertLevel = PriceChangeAlertLevel.Immediate;
                else if (absChange >= 5)
                    priceHistory.AlertLevel = PriceChangeAlertLevel.Review;
                else
                    priceHistory.AlertLevel = PriceChangeAlertLevel.None;

                // Update material price
                _material.CurrentPrice = NewPrice;
                _material.UpdatedDate = DateTime.Now;
                
                // Save to database
                _databaseService.UpdateMaterial(_material);
                _databaseService.SaveMaterialPriceHistory(priceHistory);

                // Fire price change events if significant
                if (priceHistory.AlertLevel == PriceChangeAlertLevel.Immediate)
                {
                    _pricingService.FireMajorPriceChangeEvent(_material, _material.CurrentPrice, NewPrice, PercentageChange);
                }
                else if (priceHistory.AlertLevel == PriceChangeAlertLevel.Review)
                {
                    _pricingService.FireModeratePriceChangeEvent(_material, _material.CurrentPrice, NewPrice, PercentageChange);
                }

                Success = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating price: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Success = false;
            DialogResult = false;
            Close();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
