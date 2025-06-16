using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class AddComponentDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<PriceListItem> _allItems;
        private ObservableCollection<PriceListItem> _filteredItems;
        private PriceListItem _selectedItem;
        private decimal _quantity = 1;
        private bool _isOptional;

        public AddComponentDialog()
        {
            InitializeComponent();
            DataContext = this;
            
            _allItems = new ObservableCollection<PriceListItem>();
            _filteredItems = new ObservableCollection<PriceListItem>();
            
            // Focus on search box
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        public List<PriceListItem> AvailableItems
        {
            set
            {
                _allItems = new ObservableCollection<PriceListItem>(value);
                _filteredItems = new ObservableCollection<PriceListItem>(_allItems);
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public ObservableCollection<PriceListItem> FilteredItems
        {
            get => _filteredItems;
            set
            {
                _filteredItems = value;
                OnPropertyChanged();
            }
        }

        public PriceListItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }

        public bool IsOptional
        {
            get => _isOptional;
            set
            {
                _isOptional = value;
                OnPropertyChanged();
            }
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredItems = new ObservableCollection<PriceListItem>(_allItems);
            }
            else
            {
                var filtered = _allItems.Where(m => 
                    m.ItemCode.ToLower().Contains(searchText) || 
                    m.Name.ToLower().Contains(searchText) ||
                    (m.Category != null && m.Category.ToLower().Contains(searchText))).ToList();
                    
                FilteredItems = new ObservableCollection<PriceListItem>(filtered);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Please select an item.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Quantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than 0.", "Validation Error", 
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
