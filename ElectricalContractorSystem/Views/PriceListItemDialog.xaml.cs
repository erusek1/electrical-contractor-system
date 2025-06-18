using System;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class PriceListItemDialog : Window
    {
        public PriceListItem PriceListItem { get; private set; }
        public bool IsEditMode { get; private set; }

        public PriceListItemDialog(PriceListItem item = null)
        {
            InitializeComponent();
            
            IsEditMode = item != null;
            
            if (IsEditMode)
            {
                Title = "Edit Price List Item";
                PriceListItem = item;
                LoadItemData();
            }
            else
            {
                Title = "Add Price List Item";
                PriceListItem = new PriceListItem
                {
                    IsActive = true,
                    TaxRate = 6.625m, // Default NJ tax rate
                    MarkupPercentage = 22.0m // Default markup
                };
                
                // Set default values in UI
                TaxRateTextBox.Text = "6.625";
                MarkupPercentageTextBox.Text = "22.0";
            }
        }

        private void LoadItemData()
        {
            CategoryTextBox.Text = PriceListItem.Category;
            ItemCodeTextBox.Text = PriceListItem.ItemCode;
            NameTextBox.Text = PriceListItem.Name;
            DescriptionTextBox.Text = PriceListItem.Description;
            BaseCostTextBox.Text = PriceListItem.BaseCost.ToString("F2");
            TaxRateTextBox.Text = PriceListItem.TaxRate.ToString("F3");
            LaborMinutesTextBox.Text = PriceListItem.LaborMinutes.ToString();
            MarkupPercentageTextBox.Text = PriceListItem.MarkupPercentage.ToString("F2");
            IsActiveCheckBox.IsChecked = PriceListItem.IsActive;
            NotesTextBox.Text = PriceListItem.Notes;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(CategoryTextBox.Text))
            {
                MessageBox.Show("Category is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemCodeTextBox.Text))
            {
                MessageBox.Show("Item Code is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ItemCodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            // Validate numeric fields
            if (!decimal.TryParse(BaseCostTextBox.Text, out decimal baseCost) || baseCost < 0)
            {
                MessageBox.Show("Base Cost must be a valid positive number.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BaseCostTextBox.Focus();
                return;
            }

            decimal taxRate = 0;
            if (!string.IsNullOrWhiteSpace(TaxRateTextBox.Text))
            {
                if (!decimal.TryParse(TaxRateTextBox.Text, out taxRate) || taxRate < 0)
                {
                    MessageBox.Show("Tax Rate must be a valid positive number.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TaxRateTextBox.Focus();
                    return;
                }
            }

            int laborMinutes = 0;
            if (!string.IsNullOrWhiteSpace(LaborMinutesTextBox.Text))
            {
                if (!int.TryParse(LaborMinutesTextBox.Text, out laborMinutes) || laborMinutes < 0)
                {
                    MessageBox.Show("Labor Minutes must be a valid positive integer.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    LaborMinutesTextBox.Focus();
                    return;
                }
            }

            decimal markupPercentage = 0;
            if (!string.IsNullOrWhiteSpace(MarkupPercentageTextBox.Text))
            {
                if (!decimal.TryParse(MarkupPercentageTextBox.Text, out markupPercentage) || markupPercentage < 0)
                {
                    MessageBox.Show("Markup Percentage must be a valid positive number.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    MarkupPercentageTextBox.Focus();
                    return;
                }
            }

            // Update or create the price list item
            if (!IsEditMode)
            {
                PriceListItem = new PriceListItem();
            }

            PriceListItem.Category = CategoryTextBox.Text.Trim();
            PriceListItem.ItemCode = ItemCodeTextBox.Text.Trim();
            PriceListItem.Name = NameTextBox.Text.Trim();
            PriceListItem.Description = DescriptionTextBox.Text?.Trim();
            PriceListItem.BaseCost = baseCost;
            PriceListItem.TaxRate = taxRate;
            PriceListItem.LaborMinutes = laborMinutes;
            PriceListItem.MarkupPercentage = markupPercentage;
            PriceListItem.IsActive = IsActiveCheckBox.IsChecked ?? true;
            PriceListItem.Notes = NotesTextBox.Text?.Trim();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
