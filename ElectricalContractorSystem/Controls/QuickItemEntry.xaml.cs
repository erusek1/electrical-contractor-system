using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Controls
{
    public partial class QuickItemEntry : UserControl
    {
        #region Dependency Properties
        
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<PriceListItem>), 
                typeof(QuickItemEntry), new PropertyMetadata(null, OnItemsSourceChanged));
        
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(PriceListItem), 
                typeof(QuickItemEntry), new PropertyMetadata(null));
        
        public static readonly DependencyProperty AddItemCommandProperty =
            DependencyProperty.Register("AddItemCommand", typeof(ICommand), 
                typeof(QuickItemEntry), new PropertyMetadata(null));
        
        public ObservableCollection<PriceListItem> ItemsSource
        {
            get { return (ObservableCollection<PriceListItem>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        
        public PriceListItem SelectedItem
        {
            get { return (PriceListItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }
        
        public ICommand AddItemCommand
        {
            get { return (ICommand)GetValue(AddItemCommandProperty); }
            set { SetValue(AddItemCommandProperty, value); }
        }
        
        #endregion
        
        #region Private Fields
        
        private ObservableCollection<PriceListItem> _filteredItems;
        private int _quantity = 1;
        private string _searchText = "";
        
        #endregion
        
        #region Properties
        
        public ObservableCollection<PriceListItem> FilteredItems
        {
            get => _filteredItems;
            set
            {
                _filteredItems = value;
                OnPropertyChanged(nameof(FilteredItems));
            }
        }
        
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterItems();
            }
        }
        
        #endregion
        
        public QuickItemEntry()
        {
            InitializeComponent();
            DataContext = this;
            _filteredItems = new ObservableCollection<PriceListItem>();
        }
        
        #region Event Handlers
        
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is QuickItemEntry control)
            {
                control.FilterItems();
            }
        }
        
        private void ItemCodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (AutocompleteListBox.SelectedItem != null)
                {
                    SelectCurrentItem();
                }
                else if (FilteredItems.Count == 1)
                {
                    // Auto-select if only one match
                    AutocompleteListBox.SelectedItem = FilteredItems[0];
                    SelectCurrentItem();
                }
                else if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    // Try exact match
                    var exactMatch = ItemsSource?.FirstOrDefault(i => 
                        i.ItemCode.Equals(SearchText, StringComparison.OrdinalIgnoreCase));
                    
                    if (exactMatch != null)
                    {
                        SelectedItem = exactMatch;
                        AddAndReset();
                    }
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Down && AutocompletePopup.IsOpen)
            {
                AutocompleteListBox.Focus();
                if (AutocompleteListBox.Items.Count > 0)
                {
                    AutocompleteListBox.SelectedIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                // Move to quantity on Shift+Tab
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                {
                    QuantityTextBox.Focus();
                    QuantityTextBox.SelectAll();
                    e.Handled = true;
                }
            }
        }
        
        private void QuantityTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                ItemCodeTextBox.Focus();
                ItemCodeTextBox.SelectAll();
                e.Handled = true;
            }
        }
        
        private void AutocompleteListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrentItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                AutocompletePopup.IsOpen = false;
                ItemCodeTextBox.Focus();
                e.Handled = true;
            }
        }
        
        private void AutocompleteListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentItem();
        }
        
        private void ItemCodeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (FilteredItems.Count > 0)
            {
                AutocompletePopup.IsOpen = true;
            }
        }
        
        private void ItemCodeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Delay closing to allow clicks on the listbox
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!AutocompleteListBox.IsKeyboardFocusWithin)
                {
                    AutocompletePopup.IsOpen = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
        
        private void QuantityTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            QuantityTextBox.SelectAll();
        }
        
        #endregion
        
        #region Private Methods
        
        private void FilterItems()
        {
            FilteredItems.Clear();
            
            if (ItemsSource == null || string.IsNullOrWhiteSpace(SearchText))
            {
                AutocompletePopup.IsOpen = false;
                return;
            }
            
            var searchLower = SearchText.ToLower();
            var matches = ItemsSource.Where(item =>
                item.ItemCode.ToLower().StartsWith(searchLower) ||
                item.ItemCode.ToLower().Contains(searchLower) ||
                item.Name.ToLower().Contains(searchLower))
                .OrderBy(item => 
                {
                    // Prioritize exact code matches
                    if (item.ItemCode.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                        return 0;
                    // Then code starts with
                    if (item.ItemCode.StartsWith(SearchText, StringComparison.OrdinalIgnoreCase))
                        return 1;
                    // Then code contains
                    if (item.ItemCode.ToLower().Contains(searchLower))
                        return 2;
                    // Finally name contains
                    return 3;
                })
                .ThenBy(item => item.ItemCode)
                .Take(10); // Limit results for performance
            
            foreach (var item in matches)
            {
                FilteredItems.Add(item);
            }
            
            AutocompletePopup.IsOpen = FilteredItems.Count > 0 && ItemCodeTextBox.IsFocused;
            
            // Auto-select first item
            if (FilteredItems.Count > 0)
            {
                AutocompleteListBox.SelectedIndex = 0;
            }
        }
        
        private void SelectCurrentItem()
        {
            if (AutocompleteListBox.SelectedItem is PriceListItem item)
            {
                SelectedItem = item;
                AddAndReset();
            }
        }
        
        private void AddAndReset()
        {
            // Execute add command if available
            if (AddItemCommand?.CanExecute(new { Item = SelectedItem, Quantity = Quantity }) == true)
            {
                AddItemCommand.Execute(new { Item = SelectedItem, Quantity = Quantity });
                
                // Reset for next entry
                SearchText = "";
                Quantity = 1;
                AutocompletePopup.IsOpen = false;
                ItemCodeTextBox.Focus();
            }
        }
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}
