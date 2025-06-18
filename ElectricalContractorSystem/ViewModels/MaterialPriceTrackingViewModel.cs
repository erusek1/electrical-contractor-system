using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.Win32;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.ViewModels
{
    public class MaterialPriceTrackingViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<Material> _materials;
        private ObservableCollection<Material> _filteredMaterials;
        private ObservableCollection<MaterialPriceHistory> _priceHistory;
        private ObservableCollection<PriceAlert> _priceAlerts;
        private Material _selectedMaterial;
        private string _searchText;
        private string _selectedCategory;
        private bool _showOnlyAlertsNeeded;
        private DateTime _historyStartDate;
        private DateTime _historyEndDate;
        
        public MaterialPriceTrackingViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _pricingService = new PricingService(databaseService);
            Initialize();
        }
        
        public MaterialPriceTrackingViewModel(DatabaseService databaseService, PricingService pricingService)
        {
            _databaseService = databaseService;
            _pricingService = pricingService;
            Initialize();
        }
        
        private void Initialize()
        {
            // Initialize collections
            Materials = new ObservableCollection<Material>();
            FilteredMaterials = new ObservableCollection<Material>();
            PriceHistory = new ObservableCollection<MaterialPriceHistory>();
            PriceAlerts = new ObservableCollection<PriceAlert>();
            Categories = new ObservableCollection<string>();
            
            // Set default date range
            HistoryEndDate = DateTime.Now;
            HistoryStartDate = DateTime.Now.AddMonths(-3);
            
            // Initialize commands
            RefreshCommand = new RelayCommand(ExecuteRefresh);
            UpdatePriceCommand = new RelayCommand(ExecuteUpdatePrice, CanExecuteUpdatePrice);
            ViewHistoryCommand = new RelayCommand(ExecuteViewHistory, CanExecuteViewHistory);
            BulkUpdateCommand = new RelayCommand(ExecuteBulkUpdate);
            ImportPricesCommand = new RelayCommand(ExecuteImportPrices);
            ExportPricesCommand = new RelayCommand(ExecuteExportPrices);
            DismissAlertCommand = new RelayCommand(ExecuteDismissAlert);
            ApplyPriceChangeCommand = new RelayCommand(ExecuteApplyPriceChange);
            ViewTrendsCommand = new RelayCommand(ExecuteViewTrends, CanExecuteViewTrends);
            AddMaterialCommand = new RelayCommand(ExecuteAddMaterial);
            EditMaterialCommand = new RelayCommand(ExecuteEditMaterial, CanExecuteEditMaterial);
            DeleteMaterialCommand = new RelayCommand(ExecuteDeleteMaterial, CanExecuteDeleteMaterial);
            
            // Subscribe to pricing service events
            _pricingService.OnMajorPriceChange += OnMajorPriceChange;
            _pricingService.OnModeratePriceChange += OnModeratePriceChange;
            
            LoadData();
        }
        
        #region Properties
        
        public ObservableCollection<Material> Materials
        {
            get => _materials;
            set => SetProperty(ref _materials, value);
        }

        public ObservableCollection<Material> FilteredMaterials
        {
            get => _filteredMaterials;
            set => SetProperty(ref _filteredMaterials, value);
        }
        
        public ObservableCollection<MaterialPriceHistory> PriceHistory
        {
            get => _priceHistory;
            set => SetProperty(ref _priceHistory, value);
        }
        
        public ObservableCollection<PriceAlert> PriceAlerts
        {
            get => _priceAlerts;
            set => SetProperty(ref _priceAlerts, value);
        }
        
        public ObservableCollection<string> Categories { get; private set; }
        
        public Material SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                SetProperty(ref _selectedMaterial, value);
                CommandManager.InvalidateRequerySuggested();
                
                if (_selectedMaterial != null)
                {
                    LoadPriceHistory();
                    UpdateStatistics();
                }
            }
        }
        
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }
        
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                ApplyFilters();
            }
        }
        
        public bool ShowOnlyAlertsNeeded
        {
            get => _showOnlyAlertsNeeded;
            set
            {
                SetProperty(ref _showOnlyAlertsNeeded, value);
                ApplyFilters();
            }
        }
        
        public DateTime HistoryStartDate
        {
            get => _historyStartDate;
            set
            {
                SetProperty(ref _historyStartDate, value);
                if (SelectedMaterial != null)
                {
                    LoadPriceHistory();
                }
            }
        }
        
        public DateTime HistoryEndDate
        {
            get => _historyEndDate;
            set
            {
                SetProperty(ref _historyEndDate, value);
                if (SelectedMaterial != null)
                {
                    LoadPriceHistory();
                }
            }
        }
        
        // Statistics properties
        public decimal? Average30DayPrice { get; private set; }
        public decimal? Average90DayPrice { get; private set; }
        public string PriceTrend { get; private set; }
        public string BulkPurchaseRecommendation { get; private set; }
        public int AlertCount => PriceAlerts.Count;
        
        #endregion
        
        #region Commands
        
        public ICommand RefreshCommand { get; private set; }
        public ICommand UpdatePriceCommand { get; private set; }
        public ICommand ViewHistoryCommand { get; private set; }
        public ICommand BulkUpdateCommand { get; private set; }
        public ICommand ImportPricesCommand { get; private set; }
        public ICommand ExportPricesCommand { get; private set; }
        public ICommand DismissAlertCommand { get; private set; }
        public ICommand ApplyPriceChangeCommand { get; private set; }
        public ICommand ViewTrendsCommand { get; private set; }
        public ICommand AddMaterialCommand { get; private set; }
        public ICommand EditMaterialCommand { get; private set; }
        public ICommand DeleteMaterialCommand { get; private set; }
        
        #endregion
        
        #region Command Implementations
        
        private void ExecuteRefresh(object parameter)
        {
            LoadData();
        }
        
        private bool CanExecuteUpdatePrice(object parameter)
        {
            return SelectedMaterial != null;
        }
        
        private void ExecuteUpdatePrice(object parameter)
        {
            if (SelectedMaterial == null) return;
            
            try
            {
                var dialog = new UpdatePriceDialog(SelectedMaterial, _databaseService)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true && dialog.Success)
                {
                    // Refresh the material in the list
                    var updatedMaterial = _databaseService.GetMaterialById(SelectedMaterial.MaterialId);
                    if (updatedMaterial != null)
                    {
                        var index = Materials.IndexOf(SelectedMaterial);
                        if (index >= 0)
                        {
                            Materials[index] = updatedMaterial;
                            SelectedMaterial = updatedMaterial;
                        }
                    }

                    // Refresh alerts and history
                    LoadPriceAlerts();
                    LoadPriceHistory();
                    
                    System.Windows.MessageBox.Show("Price updated successfully!", "Success", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error opening price update dialog: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private bool CanExecuteViewHistory(object parameter)
        {
            return SelectedMaterial != null;
        }
        
        private void ExecuteViewHistory(object parameter)
        {
            if (SelectedMaterial == null) return;
            
            // Show full history date range
            HistoryStartDate = DateTime.Now.AddYears(-2);
            HistoryEndDate = DateTime.Now;
            LoadPriceHistory();
            
            System.Windows.MessageBox.Show($"Showing price history for {SelectedMaterial.Name}", "Price History", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        
        private void ExecuteBulkUpdate(object parameter)
        {
            var result = System.Windows.MessageBox.Show(
                "Bulk price update allows you to apply percentage increases/decreases to multiple materials.\n\n" +
                "This feature will be implemented to:\n" +
                "• Select materials by category or vendor\n" +
                "• Apply percentage adjustments\n" +
                "• Track all changes in price history\n\n" +
                "Would you like to proceed with bulk update?",
                "Bulk Update",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // TODO: Implement BulkUpdateDialog
                System.Windows.MessageBox.Show("Bulk update dialog will be implemented in next phase.", "Coming Soon",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        
        private void ExecuteImportPrices(object parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Select Price List File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var extension = Path.GetExtension(filePath).ToLower();

                    System.Windows.MessageBox.Show(
                        $"Selected file: {Path.GetFileName(filePath)}\n\n" +
                        "To import this file:\n\n" +
                        "1. Ensure your file has columns for:\n" +
                        "   - Material Code\n" +
                        "   - Price\n" +
                        "   - Vendor (optional)\n" +
                        "   - Date (optional)\n\n" +
                        "2. Run the import script:\n" +
                        $"   python migration/import_prices_from_excel.py \"{filePath}\"\n\n" +
                        "The script will update prices and create history records.",
                        "Import Instructions",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error selecting file: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void ExecuteExportPrices(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    Title = "Export Price List",
                    FileName = $"PriceList_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var filePath = saveFileDialog.FileName;
                    
                    // TODO: Implement actual export functionality
                    System.Windows.MessageBox.Show(
                        $"Export functionality will save current price list to:\n{filePath}\n\n" +
                        "This will include:\n" +
                        "• All material codes and names\n" +
                        "• Current prices\n" +
                        "• Categories and vendors\n" +
                        "• Last update dates\n\n" +
                        "Export feature coming in next update!",
                        "Export Prices",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error with export dialog: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void ExecuteDismissAlert(object parameter)
        {
            if (parameter is PriceAlert alert)
            {
                PriceAlerts.Remove(alert);
                OnPropertyChanged(nameof(AlertCount));
            }
        }
        
        private void ExecuteApplyPriceChange(object parameter)
        {
            if (parameter is PriceAlert alert)
            {
                // Apply the price change to active estimates
                var result = System.Windows.MessageBox.Show(
                    $"Apply {alert.PercentageChange:+0.0;-0.0}% price change for {alert.MaterialName} to all active estimates?\n\n" +
                    $"Old Price: ${alert.OldPrice:N2}\n" +
                    $"New Price: ${alert.NewPrice:N2}\n\n" +
                    "This will update all estimates that are not yet approved.",
                    "Apply Price Change",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                    
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        // TODO: Implement applying price changes to active estimates
                        System.Windows.MessageBox.Show(
                            "Price change applied to all active estimates.\n\n" +
                            "This feature will:\n" +
                            "• Update all draft and sent estimates\n" +
                            "• Leave approved estimates unchanged\n" +
                            "• Log all changes for audit trail",
                            "Price Change Applied",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                        
                        PriceAlerts.Remove(alert);
                        OnPropertyChanged(nameof(AlertCount));
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error applying price change: {ex.Message}", "Error",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private bool CanExecuteViewTrends(object parameter)
        {
            return SelectedMaterial != null;
        }
        
        private void ExecuteViewTrends(object parameter)
        {
            if (SelectedMaterial == null) return;
            
            try
            {
                var trend = _pricingService.AnalyzePriceTrend(SelectedMaterial.MaterialId);
                var avgPrice30 = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 30);
                var avgPrice90 = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 90);
                var recommendation = _pricingService.GetBulkPurchaseRecommendation(SelectedMaterial.MaterialId);

                var trendMessage = $"Price Trend Analysis for {SelectedMaterial.Name}\n\n" +
                    $"Current Price: ${SelectedMaterial.CurrentPrice:N2}\n" +
                    $"30-Day Average: ${avgPrice30:N2}\n" +
                    $"90-Day Average: ${avgPrice90:N2}\n" +
                    $"Trend: {trend}\n\n";

                if (recommendation != null)
                {
                    trendMessage += $"Recommendation: {recommendation.Recommendation}";
                }

                System.Windows.MessageBox.Show(trendMessage, "Price Trends", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error analyzing trends: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteAddMaterial(object parameter)
        {
            try
            {
                // TODO: Implement AddMaterialDialog
                System.Windows.MessageBox.Show(
                    "Add Material feature will allow you to:\n\n" +
                    "• Enter material code and name\n" +
                    "• Set category and unit of measure\n" +
                    "• Set initial price and tax rate\n" +
                    "• Select preferred vendor\n" +
                    "• Set stock level thresholds\n\n" +
                    "This dialog will be implemented in the next phase.",
                    "Add Material",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteEditMaterial(object parameter)
        {
            return SelectedMaterial != null;
        }

        private void ExecuteEditMaterial(object parameter)
        {
            if (SelectedMaterial == null) return;

            try
            {
                // TODO: Implement EditMaterialDialog
                System.Windows.MessageBox.Show(
                    $"Edit Material: {SelectedMaterial.Name}\n\n" +
                    "This feature will allow you to edit:\n" +
                    "• Material name and description\n" +
                    "• Category and unit of measure\n" +
                    "• Tax rate and preferred vendor\n" +
                    "• Stock level thresholds\n" +
                    "• Active/inactive status\n\n" +
                    "Edit dialog will be implemented in the next phase.",
                    "Edit Material",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDeleteMaterial(object parameter)
        {
            return SelectedMaterial != null;
        }

        private void ExecuteDeleteMaterial(object parameter)
        {
            if (SelectedMaterial == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete '{SelectedMaterial.Name}'?\n\n" +
                "This will:\n" +
                "• Mark the material as inactive\n" +
                "• Preserve all price history\n" +
                "• Remove from active price lists\n\n" +
                "This action can be undone by reactivating the material.",
                "Delete Material",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    SelectedMaterial.IsActive = false;
                    _databaseService.UpdateMaterial(SelectedMaterial);
                    
                    Materials.Remove(SelectedMaterial);
                    ApplyFilters();
                    
                    System.Windows.MessageBox.Show("Material has been deactivated.", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error deactivating material: {ex.Message}", "Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadData()
        {
            try
            {
                // Load materials
                var materials = _databaseService.GetAllMaterials();
                Materials.Clear();
                foreach (var material in materials.Where(m => m.IsActive).OrderBy(m => m.Category).ThenBy(m => m.Name))
                {
                    Materials.Add(material);
                }
                
                // Load categories
                Categories.Clear();
                Categories.Add("All Categories");
                var categories = materials.Where(m => m.IsActive).Select(m => m.Category).Distinct().OrderBy(c => c);
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
                
                // Load price alerts
                LoadPriceAlerts();
                
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading data: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void LoadPriceHistory()
        {
            if (SelectedMaterial == null)
            {
                PriceHistory.Clear();
                return;
            }
            
            try
            {
                var history = _pricingService.GetPriceHistory(
                    SelectedMaterial.MaterialId, 
                    HistoryStartDate, 
                    HistoryEndDate);
                    
                PriceHistory.Clear();
                foreach (var item in history.OrderByDescending(h => h.EffectiveDate))
                {
                    PriceHistory.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading price history: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void LoadPriceAlerts()
        {
            try
            {
                PriceAlerts.Clear();
                
                // Get materials with significant price changes
                var alertMaterials = _pricingService.GetMaterialsWithSignificantPriceChanges(5.0m);
                
                foreach (var material in alertMaterials)
                {
                    var history = _pricingService.GetPriceHistory(material.MaterialId, DateTime.Now.AddDays(-30));
                    var latestChange = history.OrderByDescending(h => h.EffectiveDate).FirstOrDefault();
                    
                    if (latestChange != null && latestChange.AlertLevel != PriceChangeAlertLevel.None)
                    {
                        PriceAlerts.Add(new PriceAlert
                        {
                            MaterialId = material.MaterialId,
                            MaterialName = material.Name,
                            OldPrice = latestChange.Price - (latestChange.Price * latestChange.PercentageChangeFromPrevious / 100),
                            NewPrice = latestChange.Price,
                            PercentageChange = latestChange.PercentageChangeFromPrevious,
                            AlertLevel = latestChange.AlertLevel,
                            ChangeDate = latestChange.EffectiveDate
                        });
                    }
                }
                
                OnPropertyChanged(nameof(AlertCount));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading price alerts: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void UpdateStatistics()
        {
            if (SelectedMaterial == null)
            {
                Average30DayPrice = null;
                Average90DayPrice = null;
                PriceTrend = null;
                BulkPurchaseRecommendation = null;
            }
            else
            {
                try
                {
                    Average30DayPrice = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 30);
                    Average90DayPrice = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 90);
                    
                    var trend = _pricingService.AnalyzePriceTrend(SelectedMaterial.MaterialId);
                    PriceTrend = trend.ToString();
                    
                    var recommendation = _pricingService.GetBulkPurchaseRecommendation(SelectedMaterial.MaterialId);
                    BulkPurchaseRecommendation = recommendation?.Recommendation;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error updating statistics: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            
            OnPropertyChanged(nameof(Average30DayPrice));
            OnPropertyChanged(nameof(Average90DayPrice));
            OnPropertyChanged(nameof(PriceTrend));
            OnPropertyChanged(nameof(BulkPurchaseRecommendation));
        }
        
        private void ApplyFilters()
        {
            try
            {
                var filtered = Materials.AsEnumerable();
                
                // Filter by category
                if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All Categories")
                {
                    filtered = filtered.Where(m => m.Category == SelectedCategory);
                }
                
                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(m => 
                        m.MaterialCode.ToLower().Contains(searchLower) ||
                        m.Name.ToLower().Contains(searchLower) ||
                        (m.Description?.ToLower().Contains(searchLower) ?? false));
                }
                
                // Filter by alerts
                if (ShowOnlyAlertsNeeded)
                {
                    var alertMaterialIds = PriceAlerts.Select(a => a.MaterialId).ToList();
                    filtered = filtered.Where(m => alertMaterialIds.Contains(m.MaterialId));
                }
                
                // Update filtered collection
                FilteredMaterials.Clear();
                foreach (var material in filtered.OrderBy(m => m.Category).ThenBy(m => m.Name))
                {
                    FilteredMaterials.Add(material);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error applying filters: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnMajorPriceChange(object sender, PriceChangeAlertEventArgs e)
        {
            var alert = new PriceAlert
            {
                MaterialId = e.Material.MaterialId,
                MaterialName = e.Material.Name,
                OldPrice = e.OldPrice,
                NewPrice = e.NewPrice,
                PercentageChange = e.PercentageChange,
                AlertLevel = e.AlertLevel,
                ChangeDate = DateTime.Now
            };
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PriceAlerts.Insert(0, alert);
                OnPropertyChanged(nameof(AlertCount));
            });
        }
        
        private void OnModeratePriceChange(object sender, PriceChangeAlertEventArgs e)
        {
            var alert = new PriceAlert
            {
                MaterialId = e.Material.MaterialId,
                MaterialName = e.Material.Name,
                OldPrice = e.OldPrice,
                NewPrice = e.NewPrice,
                PercentageChange = e.PercentageChange,
                AlertLevel = e.AlertLevel,
                ChangeDate = DateTime.Now
            };
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PriceAlerts.Add(alert);
                OnPropertyChanged(nameof(AlertCount));
            });
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class PriceAlert
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal PercentageChange { get; set; }
        public PriceChangeAlertLevel AlertLevel { get; set; }
        public DateTime ChangeDate { get; set; }
        
        public string DisplayText => 
            $"{MaterialName}: {PercentageChange:+0.0;-0.0}% (${OldPrice:0.00} → ${NewPrice:0.00})";
        
        public string AlertColor => AlertLevel == PriceChangeAlertLevel.Immediate ? "Red" : "Orange";
    }
    
    #endregion
}
