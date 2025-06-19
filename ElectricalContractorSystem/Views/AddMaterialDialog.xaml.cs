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
    /// <summary>
    /// Dialog for adding new materials to the system
    /// FIXED: Renamed conflicting properties to avoid inheritance conflicts
    /// - Name → MaterialName (conflicts with FrameworkElement.Name)
    /// - IsActive → IsMaterialActive (conflicts with Window.IsActive)
    /// </summary>
    public partial class AddMaterialDialog : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private string _materialCode;
        private string _materialName;
        private string _description;
        private string _category = "Devices";
        private string _unitOfMeasure = "Each";
        private decimal _currentPrice;
        private decimal _taxRate = 6.4m;
        private int _minStockLevel;
        private int _maxStockLevel;
        private bool _isMaterialActive = true;
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

        /// <summary>
        /// Material code/SKU for the new material
        /// </summary>
        public string MaterialCode
        {
            get => _materialCode;
            set
            {
                _materialCode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        /// <summary>
        /// Material name - RENAMED to avoid conflict with FrameworkElement.Name
        /// </summary>
        public string MaterialName
        {
            get => _materialName;
            set
            {
                _materialName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        /// <summary>
        /// Material description
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Material category
        /// </summary>
        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Unit of measure (Each, Foot, Box, etc.)
        /// </summary>
        public string UnitOfMeasure
        {
            get => _unitOfMeasure;
            set
            {
                _unitOfMeasure = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Current price of the material
        /// </summary>
        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                _currentPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        /// <summary>
        /// Tax rate percentage
        /// </summary>
        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                _taxRate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Minimum stock level for alerts
        /// </summary>
        public int MinStockLevel
        {
            get => _minStockLevel;
            set
            {
                _minStockLevel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Maximum stock level for alerts
        /// </summary>
        public int MaxStockLevel
        {
            get => _maxStockLevel;
            set
            {
                _maxStockLevel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether material is active - RENAMED to avoid conflict with Window.IsActive
        /// </summary>
        public bool IsMaterialActive
        {
            get => _isMaterialActive;
            set
            {
                _isMaterialActive = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Available vendors for selection
        /// </summary>
        public ObservableCollection<Vendor> Vendors { get; private set; }

        /// <summary>
        /// Selected preferred vendor
        /// </summary>
        public Vendor SelectedVendor
        {
            get => _selectedVendor;
            set
            {
                _selectedVendor = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The newly created material (populated after successful save)
        /// </summary>
        public Material NewMaterial { get; private set; }

        /// <summary>
        /// Indicates if the dialog was completed successfully
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Computed property to determine if save is allowed
        /// </summary>
        public bool CanSave
        {
            get
            {
                return !string.IsNullOrWhiteSpace(MaterialCode) &&
                       !string.IsNullOrWhiteSpace(MaterialName) &&
                       CurrentPrice >= 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load available vendors from database
        /// </summary>
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

        /// <summary>
        /// Validate input and create new material
        /// </summary>
        private bool ValidateInput()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(MaterialCode))
            {
                MessageBox.Show("Material Code is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaterialCodeTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(MaterialName))
            {
                MessageBox.Show("Material Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaterialNameTextBox.Focus();
                return false;
            }

            if (CurrentPrice < 0)
            {
                MessageBox.Show("Price must be greater than or equal to zero.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return false;
            }

            if (TaxRate < 0 || TaxRate > 100)
            {
                MessageBox.Show("Tax rate must be between 0 and 100 percent.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TaxRateTextBox.Focus();
                return false;
            }

            if (MinStockLevel < 0)
            {
                MessageBox.Show("Minimum stock level cannot be negative.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MinStockTextBox.Focus();
                return false;
            }

            if (MaxStockLevel < 0)
            {
                MessageBox.Show("Maximum stock level cannot be negative.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MaxStockTextBox.Focus();
                return false;
            }

            if (MaxStockLevel > 0 && MinStockLevel > MaxStockLevel)
            {
                MessageBox.Show("Minimum stock level cannot be greater than maximum stock level.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MinStockTextBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create the new material object
        /// </summary>
        private void CreateMaterial()
        {
            NewMaterial = new Material
            {
                MaterialCode = MaterialCode.Trim(),
                Name = MaterialName.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Category = Category,
                UnitOfMeasure = UnitOfMeasure,
                CurrentPrice = CurrentPrice,
                TaxRate = TaxRate,
                MinStockLevel = MinStockLevel,
                MaxStockLevel = MaxStockLevel,
                PreferredVendorId = SelectedVendor?.VendorId,
                IsActive = IsMaterialActive,
                CreatedDate = DateTime.Now,
                CreatedBy = "User" // In a real system, this would be the current user
            };

            // Note: MaterialId will be assigned by the database when saved
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Save button click handler
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                CreateMaterial();

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

        /// <summary>
        /// Cancel button click handler
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Success = false;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Handle Enter key press for quick save
        /// </summary>
        private void AddMaterialDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && CanSave)
            {
                SaveButton_Click(sender, new RoutedEventArgs());
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, new RoutedEventArgs());
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}