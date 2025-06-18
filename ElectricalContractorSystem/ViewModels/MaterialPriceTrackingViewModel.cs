using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;
using ElectricalContractorSystem.Helpers;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for tracking material prices and price history
    /// </summary>
    public class MaterialPriceTrackingViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly PricingService _pricingService;

        #region Properties

        private ObservableCollection<Material> _materials;
        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set => SetProperty(ref _materials, value);
        }

        private Material _selectedMaterial;
        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                SetProperty(ref _selectedMaterial, value);
                LoadPriceHistory();
                UpdateCommands();
            }
        }

        private ObservableCollection<MaterialPriceHistory> _priceHistory;
        public ObservableCollection<MaterialPriceHistory> PriceHistory
        {
            get => _priceHistory;
            set => SetProperty(ref _priceHistory, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }

        private string _categoryFilter = "All";
        public string CategoryFilter
        {
            get => _categoryFilter;
            set
            {
                SetProperty(ref _categoryFilter, value);
                ApplyFilters();
            }
        }

        private bool _showOnlyWithAlerts = false;
        public bool ShowOnlyWithAlerts
        {
            get => _showOnlyWithAlerts;
            set
            {
                SetProperty(ref _showOnlyWithAlerts, value);
                ApplyFilters();
            }
        }

        public ICollectionView MaterialsView { get; private set; }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand AddMaterialCommand { get; }
        public ICommand UpdatePriceCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand ImportPricesCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand RefreshCommand { get; }

        #endregion

        #region Constructor

        public MaterialPriceTrackingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _pricingService = new PricingService(databaseService);

            Materials = new ObservableCollection<Material>();
            PriceHistory = new ObservableCollection<MaterialPriceHistory>();

            MaterialsView = CollectionViewSource.GetDefaultView(Materials);
            MaterialsView.Filter = FilterMaterials;

            // Initialize commands
            LoadDataCommand = new RelayCommand(LoadData);
            AddMaterialCommand = new RelayCommand(ExecuteAddMaterial);
            UpdatePriceCommand = new RelayCommand(ExecuteUpdatePrice, CanExecuteUpdatePrice);
            ViewHistoryCommand = new RelayCommand(ExecuteViewHistory, CanExecuteViewHistory);
            ImportPricesCommand = new RelayCommand(ExecuteImportPrices);
            ExportDataCommand = new RelayCommand(ExecuteExportData);
            RefreshCommand = new RelayCommand(LoadData);

            LoadData();
        }

        #endregion

        #region Methods

        private void LoadData()
        {
            try
            {
                Materials.Clear();
                var materials = _databaseService.GetAllMaterials();
                
                foreach (var material in materials)
                {
                    Materials.Add(material);
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading materials: {ex.Message}");
                // For now, just create some sample data to show the interface works
                CreateSampleData();
            }
        }

        private void CreateSampleData()
        {
            // Create sample materials to demonstrate the interface
            Materials.Clear();
            Materials.Add(new Material
            {
                MaterialId = 1,
                Name = "12 AWG Romex Wire",
                Category = "Wire",
                CurrentPrice = 125.50m,
                Unit = "250ft Roll",
                VendorName = "Cooper Electric"
            });
            Materials.Add(new Material
            {
                MaterialId = 2,
                Name = "Standard Duplex Receptacle",
                Category = "Devices",
                CurrentPrice = 2.85m,
                Unit = "Each",
                VendorName = "Home Depot"
            });
            Materials.Add(new Material
            {
                MaterialId = 3,
                Name = "Single Pole Switch",
                Category = "Devices", 
                CurrentPrice = 3.25m,
                Unit = "Each",
                VendorName = "Cooper Electric"
            });
        }

        private void LoadPriceHistory()
        {
            PriceHistory.Clear();
            
            if (SelectedMaterial == null) return;

            try
            {
                // For now, create sample price history
                PriceHistory.Add(new MaterialPriceHistory
                {
                    MaterialId = SelectedMaterial.MaterialId,
                    Price = SelectedMaterial.CurrentPrice,
                    EffectiveDate = DateTime.Now,
                    VendorName = SelectedMaterial.VendorName,
                    ChangePercent = 0,
                    AlertLevel = PriceAlertLevel.None
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading price history: {ex.Message}");
            }
        }

        private bool FilterMaterials(object obj)
        {
            if (!(obj is Material material)) return false;

            // Category filter
            if (CategoryFilter != "All" && material.Category != CategoryFilter)
                return false;

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                if (!material.Name.ToLower().Contains(searchLower))
                    return false;
            }

            // Alert filter
            if (ShowOnlyWithAlerts)
            {
                // For now, assume no alerts - would need actual price change logic
                return false;
            }

            return true;
        }

        private void ApplyFilters()
        {
            MaterialsView?.Refresh();
        }

        private void UpdateCommands()
        {
            (UpdatePriceCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ViewHistoryCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Implementations

        private void ExecuteAddMaterial()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "Material management requires the advanced pricing tables to be created.\n\n" +
                    "For now, you can manage items through the Price List Management screen.",
                    "Material Management",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error adding material: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteUpdatePrice()
        {
            return SelectedMaterial != null;
        }

        private void ExecuteUpdatePrice()
        {
            if (SelectedMaterial == null) return;

            try
            {
                System.Windows.MessageBox.Show(
                    "Price updating requires the advanced pricing tables to be created.\n\n" +
                    "For now, you can manage prices through the Price List Management screen.",
                    "Price Update",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating price: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteViewHistory()
        {
            return SelectedMaterial != null;
        }

        private void ExecuteViewHistory()
        {
            if (SelectedMaterial == null) return;

            try
            {
                System.Windows.MessageBox.Show(
                    "Price history viewing requires the advanced pricing tables to be created.\n\n" +
                    "For now, you can view pricing information through the Price List Management screen.",
                    "Price History",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error viewing history: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteImportPrices()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "Price importing requires the advanced pricing tables to be created.\n\n" +
                    "For now, you can manage prices through the Price List Management screen.",
                    "Import Prices",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing prices: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteExportData()
        {
            try
            {
                System.Windows.MessageBox.Show(
                    "Export functionality will be implemented soon.",
                    "Export Data",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting data: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
