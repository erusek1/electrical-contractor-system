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
    public partial class AddMaterialDialog : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private string _materialCode;
        private string _name;
        private string _description;
        private string _category = "Devices";
        private string _unitOfMeasure = "Each";
        private decimal _currentPrice;
        private decimal _taxRate = 6.4m;
        private int _minStockLevel;
        private int _maxStockLevel;
        private bool _isActive = true;
        private Vendor _selectedVendor;

        public AddMaterialDialog(DatabaseService databaseService)
        {
            InitializeComponent();
            
            _databaseService = databaseService;
            DataContext = this;
            
            LoadVendors();
            MaterialCodeTextBox.Focus();
        }

        #region Properties

        public string MaterialCode
        {
            get => _materialCode;
            set
            {
                _materialCode = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
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

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }

        public string UnitOfMeasure
        {
            get => _unitOfMeasure;
            set
            {
                _unitOfMeasure = value;
                OnPropertyChanged();
            }
        }

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                _currentPrice = value;
                OnPropertyChanged();
            }
        }

        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = value;
                OnPropertyChanged();
            }
        }

        public int MinStockLevel
        {
            get => _minStockLevel;
            set
            {
                _minStockLevel = value;
                OnPropertyChanged();
            }
        }

        public int MaxStockLevel
        {
            get => _maxStockLevel;
            set
            {
                _maxStockLevel = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
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

        public Material NewMaterial { get; private set; }
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vendors: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(MaterialCode))
                {
                    MessageBox.Show("Material Code is required.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    MaterialCodeTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(Name))
                {
                    MessageBox.Show("Name is required.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    NameTextBox.Focus();
                    return;
                }

                if (CurrentPrice < 0)
                {
                    MessageBox.Show("Price must be greater than or equal to zero.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceTextBox.Focus();
                    return;
                }

                // Create new material
                NewMaterial = new Material
                {
                    MaterialCode = MaterialCode.Trim(),
                    Name = Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    Category = Category,
                    UnitOfMeasure = UnitOfMeasure,
                    CurrentPrice = CurrentPrice,
                    TaxRate = TaxRate,
                    MinStockLevel = MinStockLevel,
                    MaxStockLevel = MaxStockLevel,
                    PreferredVendorId = SelectedVendor?.VendorId,
                    IsActive = IsActive,
                    CreatedDate = DateTime.Now
                };

                // Add to sample data (since we're working with sample data)
                NewMaterial.MaterialId = new Random().Next(100, 999);

                Success = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating material: {ex.Message}", "Error", 
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
