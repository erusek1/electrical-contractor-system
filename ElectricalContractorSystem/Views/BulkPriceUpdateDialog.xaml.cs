using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class BulkPriceUpdateDialog : Window
    {
        private ObservableCollection<BulkPriceUpdateItem> _items;
        private ObservableCollection<Vendor> _vendors;
        private DatabaseService _databaseService;
        
        public BulkPriceUpdateDialog()
        {
            InitializeComponent();
            
            _items = new ObservableCollection<BulkPriceUpdateItem>();
            _vendors = new ObservableCollection<Vendor>();
            
            LoadData();
        }
        
        public List<(int MaterialId, decimal NewPrice, int? VendorId)> PriceUpdates { get; private set; }
        
        private void LoadData()
        {
            try
            {
                _databaseService = new DatabaseService();
                
                // Load vendors
                var vendors = _databaseService.GetAllVendors();
                _vendors.Clear();
                _vendors.Add(new Vendor { VendorId = 0, Name = "(No Vendor)" });
                foreach (var vendor in vendors.OrderBy(v => v.Name))
                {
                    _vendors.Add(vendor);
                }
                
                // Set vendor combobox source
                var vendorColumn = MaterialsGrid.Columns.OfType<DataGridComboBoxColumn>()
                    .FirstOrDefault(c => c.Header.ToString() == "Vendor");
                if (vendorColumn != null)
                {
                    vendorColumn.ItemsSource = _vendors;
                }
                
                // Load materials
                var materials = _databaseService.GetAllMaterials();
                foreach (var material in materials.OrderBy(m => m.Category).ThenBy(m => m.Name))
                {
                    _items.Add(new BulkPriceUpdateItem
                    {
                        MaterialId = material.MaterialId,
                        MaterialCode = material.MaterialCode,
                        Name = material.Name,
                        Category = material.Category,
                        CurrentPrice = material.CurrentPrice,
                        NewPrice = material.CurrentPrice,
                        VendorId = null
                    });
                }
                
                MaterialsGrid.ItemsSource = _items;
                
                // Load categories for filter
                CategoryComboBox.Items.Add("All Categories");
                var categories = materials.Select(m => m.Category).Distinct().OrderBy(c => c);
                foreach (var category in categories)
                {
                    CategoryComboBox.Items.Add(category);
                }
                CategoryComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }
        
        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }
        
        private void ApplyFilter()
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(MaterialsGrid.ItemsSource);
            if (view == null) return;
            
            view.Filter = item =>
            {
                var bulkItem = item as BulkPriceUpdateItem;
                if (bulkItem == null) return false;
                
                // Category filter
                if (CategoryComboBox.SelectedIndex > 0)
                {
                    var selectedCategory = CategoryComboBox.SelectedItem.ToString();
                    if (bulkItem.Category != selectedCategory)
                        return false;
                }
                
                // Text filter
                if (!string.IsNullOrWhiteSpace(FilterTextBox.Text))
                {
                    var filterText = FilterTextBox.Text.ToLower();
                    return bulkItem.MaterialCode.ToLower().Contains(filterText) ||
                           bulkItem.Name.ToLower().Contains(filterText);
                }
                
                return true;
            };
        }
        
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = true;
            }
        }
        
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _items)
            {
                item.IsSelected = false;
            }
        }
        
        private void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _items.Where(i => i.IsSelected && i.NewPrice != i.CurrentPrice).ToList();
            
            if (!selectedItems.Any())
            {
                MessageBox.Show("No items selected or no price changes made.", "No Updates", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var significantChanges = selectedItems.Where(i => i.IsSignificantChange).ToList();
            if (significantChanges.Any())
            {
                var result = MessageBox.Show(
                    $"{significantChanges.Count} items have price changes over 15%. These will trigger immediate alerts.\n\n" +
                    "Do you want to continue?",
                    "Significant Price Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            PriceUpdates = selectedItems.Select(i => 
                (i.MaterialId, i.NewPrice, i.VendorId)).ToList();
                
            DialogResult = true;
        }
    }
    
    public class BulkPriceUpdateItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private decimal _newPrice;
        private int? _vendorId;
        
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal CurrentPrice { get; set; }
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
        
        public decimal NewPrice
        {
            get => _newPrice;
            set
            {
                _newPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PercentageChange));
                OnPropertyChanged(nameof(IsSignificantChange));
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
        
        public decimal PercentageChange => CurrentPrice > 0 ? 
            ((NewPrice - CurrentPrice) / CurrentPrice) * 100 : 0;
            
        public bool IsSignificantChange => Math.Abs(PercentageChange) >= 15;
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
