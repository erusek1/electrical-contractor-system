using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class AddComponentDialog : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private List<PriceListItem> _allItems;
        private List<PriceListItem> _filteredItems;
        private PriceListItem _selectedItem;
        private string _searchText = "";
        private decimal _quantity = 1;

        public AddComponentDialog(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            DataContext = this;
            
            LoadPriceListItems();
            
            // Focus on search box
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        public List<PriceListItem> FilteredPriceListItems
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
                OnPropertyChanged(nameof(CanAdd));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAdd));
            }
        }

        public bool CanAdd => SelectedItem != null && Quantity > 0;

        private void LoadPriceListItems()
        {
            _allItems = _databaseService.GetAllPriceListItems();
            FilteredPriceListItems = _allItems;
        }

        private void FilterItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPriceListItems = _allItems;
            }
            else
            {
                var searchLower = SearchText.ToLower();
                FilteredPriceListItems = _allItems.Where(i => 
                    i.ItemCode.ToLower().Contains(searchLower) ||
                    i.Name.ToLower().Contains(searchLower) ||
                    (i.Category?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CanAdd)
            {
                AddButton_Click(sender, null);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
