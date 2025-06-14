using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ElectricalContractorSystem.Helpers;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.ViewModels
{
    public class MaterialPriceTrackingViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<Material> _materials;
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
        
        public ObservableCollection<string> Categories { get; }
        
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
        
        public ICommand RefreshCommand { get; }
        public ICommand UpdatePriceCommand { get; }
        public ICommand ViewHistoryCommand { get; }
        public ICommand BulkUpdateCommand { get; }
        public ICommand ImportPricesCommand { get; }
        public ICommand ExportPricesCommand { get; }
        public ICommand DismissAlertCommand { get; }
        public ICommand ApplyPriceChangeCommand { get; }
        public ICommand ViewTrendsCommand { get; }
        
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
            
            var dialog = new Views.UpdatePriceDialog(SelectedMaterial);
            if (dialog.ShowDialog() == true)
            {
                _pricingService.UpdateMaterialPrice(
                    SelectedMaterial.MaterialId,
                    dialog.NewPrice,
                    Environment.UserName,
                    dialog.VendorId,
                    dialog.PurchaseOrderNumber,
                    dialog.QuantityPurchased);
                    
                LoadData();
                LoadPriceHistory();
            }
        }
        
        private bool CanExecuteViewHistory(object parameter)
        {
            return SelectedMaterial != null;
        }
        
        private void ExecuteViewHistory(object parameter)
        {
            if (SelectedMaterial == null) return;
            
            var dialog = new Views.PriceHistoryDialog(SelectedMaterial, PriceHistory);
            dialog.ShowDialog();
        }
        
        private void ExecuteBulkUpdate(object parameter)
        {
            var dialog = new Views.BulkPriceUpdateDialog();
            if (dialog.ShowDialog() == true)
            {
                var updates = dialog.PriceUpdates;
                
                foreach (var update in updates)
                {
                    _pricingService.UpdateMaterialPrice(
                        update.MaterialId,
                        update.NewPrice,
                        Environment.UserName,
                        update.VendorId);
                }
                
                LoadData();
                
                System.Windows.MessageBox.Show(
                    $"Updated prices for {updates.Count} materials.",
                    "Bulk Update Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        
        private void ExecuteImportPrices(object parameter)
        {
            System.Windows.MessageBox.Show(
                "To import prices from Excel:\n\n" +
                "1. Ensure your Excel file has columns for:\n" +
                "   - Material Code\n" +
                "   - Price\n" +
                "   - Vendor (optional)\n" +
                "   - Date (optional)\n\n" +
                "2. Run the import script:\n" +
                "   python migration/import_prices_from_excel.py\n\n" +
                "The script will update prices and create history records.",
                "Import Prices",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        
        private void ExecuteExportPrices(object parameter)
        {
            // TODO: Implement export to Excel
            System.Windows.MessageBox.Show("Export prices feature coming soon!", "Export", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
                    $"Apply {alert.PercentageChange:+0.0;-0.0}% price change for {alert.MaterialName} to all active estimates?",
                    "Apply Price Change",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                    
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // TODO: Implement applying price changes to estimates
                    PriceAlerts.Remove(alert);
                    OnPropertyChanged(nameof(AlertCount));
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
            
            var dialog = new Views.PriceTrendsDialog(SelectedMaterial, _pricingService);
            dialog.ShowDialog();
        }
        
        #endregion
        
        #region Private Methods
        
        private void LoadData()
        {
            // Load materials
            var materials = _databaseService.GetAllMaterials();
            Materials.Clear();
            foreach (var material in materials.OrderBy(m => m.Category).ThenBy(m => m.Name))
            {
                Materials.Add(material);
            }
            
            // Load categories
            Categories.Clear();
            Categories.Add("All Categories");
            var categories = materials.Select(m => m.Category).Distinct().OrderBy(c => c);
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
            
            // Load price alerts
            LoadPriceAlerts();
            
            ApplyFilters();
        }
        
        private void LoadPriceHistory()
        {
            if (SelectedMaterial == null)
            {
                PriceHistory.Clear();
                return;
            }
            
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
        
        private void LoadPriceAlerts()
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
                Average30DayPrice = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 30);
                Average90DayPrice = _pricingService.GetAveragePrice(SelectedMaterial.MaterialId, 90);
                
                var trend = _pricingService.AnalyzePriceTrend(SelectedMaterial.MaterialId);
                PriceTrend = trend.ToString();
                
                var recommendation = _pricingService.GetBulkPurchaseRecommendation(SelectedMaterial.MaterialId);
                BulkPurchaseRecommendation = recommendation?.Recommendation;
            }
            
            OnPropertyChanged(nameof(Average30DayPrice));
            OnPropertyChanged(nameof(Average90DayPrice));
            OnPropertyChanged(nameof(PriceTrend));
            OnPropertyChanged(nameof(BulkPurchaseRecommendation));
        }
        
        private void ApplyFilters()
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
            
            // Update the view
            // Note: In a real implementation, you'd have a FilteredMaterials collection
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
