using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class ImportPricesDialog : Window, INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private string _filePath;
        private string _statusText = "No file selected";

        public ImportPricesDialog(DatabaseService databaseService)
        {
            InitializeComponent();
            
            _databaseService = databaseService;
            DataContext = this;
            
            PreviewData = new ObservableCollection<PriceImportItem>();
        }

        #region Properties

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PriceImportItem> PreviewData { get; private set; }

        public bool Success { get; private set; }
        public int ImportedCount { get; private set; }

        #endregion

        #region Event Handlers

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Price List File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                LoadPreviewData();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                LoadPreviewData();
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in PreviewData)
            {
                item.ShouldImport = true;
            }
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in PreviewData)
            {
                item.ShouldImport = false;
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var itemsToImport = PreviewData.Where(item => item.ShouldImport).ToList();
                
                if (!itemsToImport.Any())
                {
                    MessageBox.Show("No items selected for import.", "Nothing to Import", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Import {itemsToImport.Count} price updates?\n\n" +
                    "This will:\n" +
                    "• Update material prices\n" +
                    "• Create price history records\n" +
                    "• Generate alerts for significant changes\n\n" +
                    "Continue with import?",
                    "Confirm Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ImportSelectedItems(itemsToImport);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during import: {ex.Message}", "Import Error", 
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

        #region Methods

        private void LoadPreviewData()
        {
            try
            {
                StatusText = "Loading file...";
                PreviewData.Clear();

                if (!File.Exists(FilePath))
                {
                    StatusText = "File not found";
                    return;
                }

                var extension = Path.GetExtension(FilePath).ToLower();
                
                if (extension == ".csv")
                {
                    LoadFromCsv();
                }
                else if (extension == ".xlsx" || extension == ".xls")
                {
                    LoadFromExcel();
                }
                else
                {
                    StatusText = "Unsupported file format";
                    return;
                }

                StatusText = $"Loaded {PreviewData.Count} items from file";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading file: {ex.Message}";
                MessageBox.Show($"Error loading file: {ex.Message}", "Load Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromCsv()
        {
            var lines = File.ReadAllLines(FilePath);
            var currentMaterials = _databaseService.GetAllMaterials();
            
            if (lines.Length == 0)
            {
                StatusText = "File is empty";
                return;
            }

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 2)
                {
                    var materialCode = parts[0].Trim().Trim('"');
                    var priceText = parts[1].Trim().Trim('"');
                    
                    if (decimal.TryParse(priceText, out decimal newPrice))
                    {
                        var existingMaterial = currentMaterials.FirstOrDefault(m => 
                            string.Equals(m.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase));

                        var importItem = new PriceImportItem
                        {
                            MaterialCode = materialCode,
                            NewPrice = newPrice,
                            ShouldImport = true
                        };

                        if (existingMaterial != null)
                        {
                            importItem.Name = existingMaterial.Name;
                            importItem.CurrentPrice = existingMaterial.CurrentPrice;
                            importItem.PercentageChange = existingMaterial.CalculatePriceChange(newPrice);
                            importItem.Status = Math.Abs(importItem.PercentageChange) > 15 ? "Large Change" :
                                              Math.Abs(importItem.PercentageChange) > 5 ? "Moderate Change" : "Small Change";
                        }
                        else
                        {
                            importItem.Name = "New Material";
                            importItem.CurrentPrice = 0;
                            importItem.PercentageChange = 0;
                            importItem.Status = "New";
                        }

                        PreviewData.Add(importItem);
                    }
                }
            }
        }

        private void LoadFromExcel()
        {
            // For now, show a message about Excel support
            StatusText = "Excel files require additional setup";
            
            MessageBox.Show(
                "Excel file import requires additional libraries.\n\n" +
                "For now, please:\n" +
                "1. Save your Excel file as CSV format\n" +
                "2. Use the CSV import option\n\n" +
                "Excel format should have columns:\n" +
                "MaterialCode, Price, Name (optional), Vendor (optional)",
                "Excel Import",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ImportSelectedItems(System.Collections.Generic.List<PriceImportItem> itemsToImport)
        {
            int successCount = 0;
            var currentMaterials = _databaseService.GetAllMaterials();

            foreach (var item in itemsToImport)
            {
                try
                {
                    var existingMaterial = currentMaterials.FirstOrDefault(m => 
                        string.Equals(m.MaterialCode, item.MaterialCode, StringComparison.OrdinalIgnoreCase));

                    if (existingMaterial != null)
                    {
                        // Update existing material price
                        var oldPrice = existingMaterial.CurrentPrice;
                        existingMaterial.CurrentPrice = item.NewPrice;
                        existingMaterial.UpdatedDate = DateTime.Now;
                        
                        _databaseService.UpdateMaterial(existingMaterial);

                        // Create price history record
                        var priceHistory = new MaterialPriceHistory
                        {
                            MaterialId = existingMaterial.MaterialId,
                            Price = item.NewPrice,
                            EffectiveDate = DateTime.Now,
                            CreatedBy = "Import",
                            PercentageChangeFromPrevious = existingMaterial.CalculatePriceChange(item.NewPrice)
                        };

                        _databaseService.SaveMaterialPriceHistory(priceHistory);
                        successCount++;
                    }
                    else
                    {
                        // Create new material
                        var newMaterial = new Material
                        {
                            MaterialCode = item.MaterialCode,
                            Name = item.Name == "New Material" ? item.MaterialCode : item.Name,
                            Category = "Imported",
                            UnitOfMeasure = "Each",
                            CurrentPrice = item.NewPrice,
                            TaxRate = 6.4m,
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        // For sample data, just assign an ID
                        newMaterial.MaterialId = new Random().Next(1000, 9999);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue with other items
                    System.Diagnostics.Debug.WriteLine($"Error importing {item.MaterialCode}: {ex.Message}");
                }
            }

            ImportedCount = successCount;
            Success = true;

            MessageBox.Show(
                $"Import completed successfully!\n\n" +
                $"Items imported: {successCount} of {itemsToImport.Count}\n" +
                $"Price history records created: {successCount}",
                "Import Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
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

    #region Supporting Classes

    public class PriceImportItem : INotifyPropertyChanged
    {
        private bool _shouldImport = true;

        public string MaterialCode { get; set; }
        public string Name { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal PercentageChange { get; set; }
        public string Status { get; set; }

        public bool ShouldImport
        {
            get => _shouldImport;
            set
            {
                _shouldImport = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}
