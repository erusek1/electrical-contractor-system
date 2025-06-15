using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace ElectricalContractorSystem.ViewModels
{
    public class PriceListManagementViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private ObservableCollection<PriceList> _priceListItems;
        private PriceList _selectedItem;
        private string _searchText;
        private string _selectedCategory = "All";

        public PriceListManagementViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadPriceList();
            InitializeCommands();
            LoadCategories();
        }

        public ObservableCollection<PriceList> PriceListItems
        {
            get => _priceListItems;
            set
            {
                _priceListItems = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public ObservableCollection<PriceList> FilteredItems
        {
            get
            {
                var filtered = PriceListItems.AsEnumerable();

                // Filter by category
                if (SelectedCategory != "All")
                {
                    filtered = filtered.Where(i => i.Category == SelectedCategory);
                }

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(i =>
                        (i.ItemCode != null && i.ItemCode.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (i.Name != null && i.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (i.Description != null && i.Description.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    );
                }

                return new ObservableCollection<PriceList>(filtered);
            }
        }

        public ObservableCollection<string> Categories { get; private set; }

        public PriceList SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsItemSelected));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredItems));
            }
        }

        public bool IsItemSelected => SelectedItem != null;

        // Commands
        public ICommand AddItemCommand { get; private set; }
        public ICommand EditItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand ImportCommand { get; private set; }
        public ICommand CreateAssemblyCommand { get; private set; }

        private void InitializeCommands()
        {
            AddItemCommand = new RelayCommand(AddItem);
            EditItemCommand = new RelayCommand(EditItem, CanEditItem);
            DeleteItemCommand = new RelayCommand(DeleteItem, CanDeleteItem);
            RefreshCommand = new RelayCommand(RefreshItems);
            ExportCommand = new RelayCommand(ExportToExcel);
            ImportCommand = new RelayCommand(ImportFromExcel);
            CreateAssemblyCommand = new RelayCommand(CreateAssembly, CanCreateAssembly);
        }

        private void LoadCategories()
        {
            Categories = new ObservableCollection<string>
            {
                "All",
                "Devices",
                "Lighting",
                "Wire",
                "Service",
                "Other"
            };
        }

        private void LoadPriceList()
        {
            try
            {
                var items = _databaseService.GetAllPriceListItems();
                PriceListItems = new ObservableCollection<PriceList>(items.OrderBy(i => i.Category).ThenBy(i => i.Name));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading price list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                PriceListItems = new ObservableCollection<PriceList>();
            }
        }

        private void AddItem(object parameter)
        {
            var dialog = new PriceListItemDialog();
            
            if (dialog.ShowDialog() == true)
            {
                _databaseService.SavePriceListItem(dialog.PriceListItem);
                LoadPriceList();
            }
        }

        private void EditItem(object parameter)
        {
            if (SelectedItem == null) return;

            var dialog = new PriceListItemDialog(SelectedItem);

            if (dialog.ShowDialog() == true)
            {
                _databaseService.UpdatePriceListItem(dialog.PriceListItem);
                LoadPriceList();
            }
        }

        private bool CanEditItem(object parameter)
        {
            return SelectedItem != null;
        }

        private void DeleteItem(object parameter)
        {
            if (SelectedItem == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedItem.Name}' ({SelectedItem.ItemCode})?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _databaseService.DeletePriceListItem(SelectedItem.ItemId);
                LoadPriceList();
            }
        }

        private bool CanDeleteItem(object parameter)
        {
            return SelectedItem != null;
        }

        private void RefreshItems(object parameter)
        {
            LoadPriceList();
        }

        private void ExportToExcel(object parameter)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"PriceList_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // TODO: Implement Excel export
                    MessageBox.Show("Excel export functionality will be implemented soon.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportFromExcel(object parameter)
        {
            MessageBox.Show(
                "To import price list items:\n\n" +
                "1. Use the Python script in /migration/import_price_list_from_excel.py\n" +
                "2. Ensure your Excel file has columns for:\n" +
                "   - Item Code\n" +
                "   - Description\n" +
                "   - Base Cost\n" +
                "   - Labor Minutes\n\n" +
                "The import will preserve your existing quick codes (hh, O, S, etc.)",
                "Import Instructions",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CreateAssembly(object parameter)
        {
            if (SelectedItem == null) return;

            var dialog = new CreateAssemblyDialog(SelectedItem);
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Create the assembly template
                    var assembly = new AssemblyTemplate
                    {
                        AssemblyCode = dialog.AssemblyCode,
                        Name = dialog.AssemblyName,
                        Description = dialog.AssemblyDescription,
                        Category = SelectedItem.Category ?? "General",
                        RoughMinutes = dialog.RoughMinutes,
                        FinishMinutes = dialog.FinishMinutes,
                        ServiceMinutes = dialog.ServiceMinutes,
                        ExtraMinutes = dialog.ExtraMinutes,
                        IsDefault = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System" // TODO: Get current user
                    };

                    // Save the assembly
                    _databaseService.SaveAssembly(assembly);

                    // Create the material from the price list item
                    var material = new Material
                    {
                        MaterialCode = SelectedItem.ItemCode,
                        Name = SelectedItem.Name,
                        Description = SelectedItem.Description,
                        Category = SelectedItem.Category ?? "General",
                        UnitOfMeasure = "Each",
                        CurrentPrice = SelectedItem.BaseCost,
                        TaxRate = SelectedItem.TaxRate ?? 6.4m,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    // Check if material already exists
                    var existingMaterials = _databaseService.GetAllMaterials();
                    var existingMaterial = existingMaterials.FirstOrDefault(m => m.MaterialCode == material.MaterialCode);
                    
                    if (existingMaterial == null)
                    {
                        // Save new material
                        using (var connection = _databaseService.GetConnection())
                        {
                            connection.Open();
                            var query = @"INSERT INTO Materials (material_code, name, description, category, 
                                        unit_of_measure, current_price, tax_rate, is_active, created_date)
                                        VALUES (@code, @name, @desc, @category, @unit, @price, @tax, @active, @created)";
                            
                            using (var command = new MySql.Data.MySqlClient.MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@code", material.MaterialCode);
                                command.Parameters.AddWithValue("@name", material.Name);
                                command.Parameters.AddWithValue("@desc", material.Description ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@category", material.Category);
                                command.Parameters.AddWithValue("@unit", material.UnitOfMeasure);
                                command.Parameters.AddWithValue("@price", material.CurrentPrice);
                                command.Parameters.AddWithValue("@tax", material.TaxRate);
                                command.Parameters.AddWithValue("@active", material.IsActive);
                                command.Parameters.AddWithValue("@created", material.CreatedDate);
                                
                                command.ExecuteNonQuery();
                                material.MaterialId = (int)command.LastInsertedId;
                            }
                        }
                    }
                    else
                    {
                        material = existingMaterial;
                    }

                    // Create the assembly component linking the material to the assembly
                    var component = new AssemblyComponent
                    {
                        AssemblyId = assembly.AssemblyId,
                        MaterialId = material.MaterialId,
                        Quantity = 1,
                        IsOptional = false
                    };

                    _databaseService.SaveAssemblyComponent(component);

                    MessageBox.Show(
                        $"Assembly '{assembly.Name}' created successfully!\n\n" +
                        $"Code: {assembly.AssemblyCode}\n" +
                        $"Total Labor: {assembly.TotalLaborMinutes} minutes",
                        "Assembly Created",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Open assembly management if desired
                    var result = MessageBox.Show(
                        "Would you like to open the Assembly Management window to view or edit the new assembly?",
                        "Open Assembly Management",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var assemblyManagementView = new AssemblyManagementView(_databaseService);
                        assemblyManagementView.Show();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error creating assembly: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private bool CanCreateAssembly(object parameter)
        {
            return SelectedItem != null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
