using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Data;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Helpers;

namespace ElectricalContractorSystem.ViewModels
{
    /// <summary>
    /// ViewModel for material price tracking and management
    /// </summary>
    public class MaterialPriceTrackingViewModel : ViewModelBase
    {
        #region Fields
        
        private readonly DatabaseService _databaseService;
        private readonly PricingService _pricingService;
        
        private ObservableCollection<Material> _materialList;
        private ObservableCollection<MaterialPriceHistory> _priceHistoryList;
        private ObservableCollection<MaterialPriceAlert> _priceAlerts;
        private ObservableCollection<BulkPurchaseRecommendation> _bulkPurchaseRecommendations;
        
        private Material _selectedMaterial;
        private MaterialPriceHistory _selectedPriceHistory;
        private string _searchText;
        private bool _isLoading;
        private string _statusMessage;
        private DateTime _historyStartDate;
        private DateTime _historyEndDate;
        private bool _showOnlyAlertsMode;
        private string _selectedTimeFrame;
        private decimal _alertThreshold;
        
        // Edit mode fields
        private bool _isEditingPrice;
        private decimal _newPrice;
        private string _priceChangeReason;
        private string _vendorName;
        private string _invoiceNumber;
        private decimal? _quantityPurchased;
        
        // Filter fields
        private string _selectedCategory;
        private PriceChangeAlertLevel? _selectedAlertLevel;
        private CollectionViewSource _materialsViewSource;
        private CollectionViewSource _historyViewSource;
        
        #endregion

        #region Properties

        /// <summary>
        /// Collection of all materials for display
        /// </summary>
        public ObservableCollection<Material> MaterialList
        {
            get { return _materialList; }
            set { SetProperty(ref _materialList, value); }
        }

        /// <summary>
        /// Collection of price history for selected material
        /// </summary>
        public ObservableCollection<MaterialPriceHistory> PriceHistoryList
        {
            get { return _priceHistoryList; }
            set { SetProperty(ref _priceHistoryList, value); }
        }

        /// <summary>
        /// Collection of active price alerts
        /// </summary>
        public ObservableCollection<MaterialPriceAlert> PriceAlerts
        {
            get { return _priceAlerts; }
            set { SetProperty(ref _priceAlerts, value); }
        }

        /// <summary>
        /// Collection of bulk purchase recommendations
        /// </summary>
        public ObservableCollection<BulkPurchaseRecommendation> BulkPurchaseRecommendations
        {
            get { return _bulkPurchaseRecommendations; }
            set { SetProperty(ref _bulkPurchaseRecommendations, value); }
        }

        /// <summary>
        /// Currently selected material
        /// </summary>
        public Material SelectedMaterial
        {
            get { return _selectedMaterial; }
            set
            {
                if (SetProperty(ref _selectedMaterial, value))
                {
                    LoadPriceHistoryForSelectedMaterial();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Currently selected price history entry
        /// </summary>
        public MaterialPriceHistory SelectedPriceHistory
        {
            get { return _selectedPriceHistory; }
            set { SetProperty(ref _selectedPriceHistory, value); }
        }

        /// <summary>
        /// Search/filter text
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                SetProperty(ref _isLoading, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        /// <summary>
        /// History start date filter
        /// </summary>
        public DateTime HistoryStartDate
        {
            get { return _historyStartDate; }
            set
            {
                if (SetProperty(ref _historyStartDate, value))
                {
                    LoadPriceHistoryForSelectedMaterial();
                }
            }
        }

        /// <summary>
        /// History end date filter
        /// </summary>
        public DateTime HistoryEndDate
        {
            get { return _historyEndDate; }
            set
            {
                if (SetProperty(ref _historyEndDate, value))
                {
                    LoadPriceHistoryForSelectedMaterial();
                }
            }
        }

        /// <summary>
        /// Show only materials with active alerts
        /// </summary>
        public bool ShowOnlyAlertsMode
        {
            get { return _showOnlyAlertsMode; }
            set
            {
                if (SetProperty(ref _showOnlyAlertsMode, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Selected time frame for history display
        /// </summary>
        public string SelectedTimeFrame
        {
            get { return _selectedTimeFrame; }
            set
            {
                if (SetProperty(ref _selectedTimeFrame, value))
                {
                    UpdateHistoryDateRange();
                }
            }
        }

        /// <summary>
        /// Alert threshold percentage
        /// </summary>
        public decimal AlertThreshold
        {
            get { return _alertThreshold; }
            set { SetProperty(ref _alertThreshold, value); }
        }

        /// <summary>
        /// Price editing mode indicator
        /// </summary>
        public bool IsEditingPrice
        {
            get { return _isEditingPrice; }
            set
            {
                SetProperty(ref _isEditingPrice, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// New price being entered
        /// </summary>
        public decimal NewPrice
        {
            get { return _newPrice; }
            set { SetProperty(ref _newPrice, value); }
        }

        /// <summary>
        /// Reason for price change
        /// </summary>
        public string PriceChangeReason
        {
            get { return _priceChangeReason; }
            set { SetProperty(ref _priceChangeReason, value); }
        }

        /// <summary>
        /// Vendor name for price update
        /// </summary>
        public string VendorName
        {
            get { return _vendorName; }
            set { SetProperty(ref _vendorName, value); }
        }

        /// <summary>
        /// Invoice number for price update
        /// </summary>
        public string InvoiceNumber
        {
            get { return _invoiceNumber; }
            set { SetProperty(ref _invoiceNumber, value); }
        }

        /// <summary>
        /// Quantity purchased for price update
        /// </summary>
        public decimal? QuantityPurchased
        {
            get { return _quantityPurchased; }
            set { SetProperty(ref _quantityPurchased, value); }
        }

        /// <summary>
        /// Selected category filter
        /// </summary>
        public string SelectedCategory
        {
            get { return _selectedCategory; }
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Selected alert level filter
        /// </summary>
        public PriceChangeAlertLevel? SelectedAlertLevel
        {
            get { return _selectedAlertLevel; }
            set
            {
                if (SetProperty(ref _selectedAlertLevel, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Filtered view of materials
        /// </summary>
        public ICollectionView MaterialsView
        {
            get { return _materialsViewSource?.View; }
        }

        /// <summary>
        /// Filtered view of price history
        /// </summary>
        public ICollectionView HistoryView
        {
            get { return _historyViewSource?.View; }
        }

        /// <summary>
        /// Available time frame options
        /// </summary>
        public List<string> TimeFrameOptions { get; } = new List<string>
        {
            "Last 7 Days",
            "Last 30 Days",
            "Last 90 Days",
            "Last 6 Months",
            "Last Year",
            "All Time"
        };

        /// <summary>
        /// Available material categories
        /// </summary>
        public List<string> MaterialCategories { get; private set; }

        /// <summary>
        /// Available alert levels
        /// </summary>
        public List<PriceChangeAlertLevel> AlertLevels { get; } = new List<PriceChangeAlertLevel>
        {
            PriceChangeAlertLevel.None,
            PriceChangeAlertLevel.Review,
            PriceChangeAlertLevel.Immediate
        };

        #endregion

        #region Commands

        private RelayCommand _loadMaterialsCommand;
        public RelayCommand LoadMaterialsCommand
        {
            get { return _loadMaterialsCommand ?? (_loadMaterialsCommand = new RelayCommand(LoadMaterials, CanLoadMaterials)); }
        }

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get { return _refreshCommand ?? (_refreshCommand = new RelayCommand(RefreshData, CanRefresh)); }
        }

        private RelayCommand _editPriceCommand;
        public RelayCommand EditPriceCommand
        {
            get { return _editPriceCommand ?? (_editPriceCommand = new RelayCommand(StartEditPrice, CanEditPrice)); }
        }

        private RelayCommand _savePriceCommand;
        public RelayCommand SavePriceCommand
        {
            get { return _savePriceCommand ?? (_savePriceCommand = new RelayCommand(SavePriceChange, CanSavePrice)); }
        }

        private RelayCommand _cancelEditCommand;
        public RelayCommand CancelEditCommand
        {
            get { return _cancelEditCommand ?? (_cancelEditCommand = new RelayCommand(CancelEdit, CanCancelEdit)); }
        }

        private RelayCommand _clearAlertsCommand;
        public RelayCommand ClearAlertsCommand
        {
            get { return _clearAlertsCommand ?? (_clearAlertsCommand = new RelayCommand(ClearAlerts, CanClearAlerts)); }
        }

        private RelayCommand _generateRecommendationsCommand;
        public RelayCommand GenerateRecommendationsCommand
        {
            get { return _generateRecommendationsCommand ?? (_generateRecommendationsCommand = new RelayCommand(GenerateBulkPurchaseRecommendations, CanGenerateRecommendations)); }
        }

        private RelayCommand _exportHistoryCommand;
        public RelayCommand ExportHistoryCommand
        {
            get { return _exportHistoryCommand ?? (_exportHistoryCommand = new RelayCommand(ExportPriceHistory, CanExportHistory)); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of MaterialPriceTrackingViewModel
        /// </summary>
        public MaterialPriceTrackingViewModel()
        {
            _databaseService = new DatabaseService();
            _pricingService = new PricingService(_databaseService);
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance with dependency injection
        /// </summary>
        public MaterialPriceTrackingViewModel(DatabaseService databaseService, PricingService pricingService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
            Initialize();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the ViewModel
        /// </summary>
        private void Initialize()
        {
            MaterialList = new ObservableCollection<Material>();
            PriceHistoryList = new ObservableCollection<MaterialPriceHistory>();
            PriceAlerts = new ObservableCollection<MaterialPriceAlert>();
            BulkPurchaseRecommendations = new ObservableCollection<BulkPurchaseRecommendation>();

            // Initialize collection views for filtering
            _materialsViewSource = new CollectionViewSource { Source = MaterialList };
            _historyViewSource = new CollectionViewSource { Source = PriceHistoryList };

            // Set default values
            HistoryEndDate = DateTime.Now;
            HistoryStartDate = DateTime.Now.AddDays(-30);
            SelectedTimeFrame = "Last 30 Days";
            AlertThreshold = 5.0m;
            StatusMessage = "Ready";

            // Subscribe to pricing service events
            _pricingService.OnMajorPriceChange += OnMajorPriceChangeReceived;
            _pricingService.OnModeratePriceChange += OnModeratePriceChangeReceived;

            // Load initial data
            LoadMaterials();
            LoadMaterialCategories();
        }

        #endregion

        #region Command Methods

        #region Load Operations

        private bool CanLoadMaterials()
        {
            return !IsLoading;
        }

        private void LoadMaterials()
        {
            SafeExecute(() =>
            {
                IsLoading = true;
                StatusMessage = "Loading materials...";

                var materials = _databaseService.GetAllMaterials();
                
                MaterialList.Clear();
                foreach (var material in materials)
                {
                    MaterialList.Add(material);
                }

                LoadPriceAlerts();
                ApplyFilters();

                StatusMessage = $"Loaded {MaterialList.Count} materials";
            }, "Failed to load materials");
            
            IsLoading = false;
        }

        private bool CanRefresh()
        {
            return !IsLoading;
        }

        private void RefreshData()
        {
            LoadMaterials();
            if (SelectedMaterial != null)
            {
                LoadPriceHistoryForSelectedMaterial();
            }
        }

        #endregion

        #region Price Management

        private bool CanEditPrice()
        {
            return !IsLoading && !IsEditingPrice && SelectedMaterial != null;
        }

        private void StartEditPrice()
        {
            if (SelectedMaterial == null) return;

            IsEditingPrice = true;
            NewPrice = SelectedMaterial.CurrentPrice;
            PriceChangeReason = "";
            VendorName = "";
            InvoiceNumber = "";
            QuantityPurchased = null;
            StatusMessage = "Editing price...";
        }

        private bool CanSavePrice()
        {
            return IsEditingPrice && SelectedMaterial != null && NewPrice > 0;
        }

        private void SavePriceChange()
        {
            SafeExecute(() =>
            {
                if (SelectedMaterial == null || NewPrice <= 0) return;

                IsLoading = true;
                StatusMessage = "Saving price change...";

                // Find vendor if name provided
                int? vendorId = null;
                if (!string.IsNullOrWhiteSpace(VendorName))
                {
                    var vendors = _databaseService.GetAllVendors();
                    var vendor = vendors.FirstOrDefault(v => v.Name.Equals(VendorName, StringComparison.OrdinalIgnoreCase));
                    vendorId = vendor?.VendorId;
                }

                // Update price through pricing service
                _pricingService.UpdateMaterialPrice(
                    SelectedMaterial.MaterialId,
                    NewPrice,
                    Environment.UserName,
                    vendorId,
                    InvoiceNumber,
                    QuantityPurchased
                );

                // Refresh data
                var updatedMaterial = _databaseService.GetMaterialById(SelectedMaterial.MaterialId);
                if (updatedMaterial != null)
                {
                    var index = MaterialList.IndexOf(SelectedMaterial);
                    if (index >= 0)
                    {
                        MaterialList[index] = updatedMaterial;
                        SelectedMaterial = updatedMaterial;
                    }
                }

                LoadPriceHistoryForSelectedMaterial();
                LoadPriceAlerts();

                IsEditingPrice = false;
                StatusMessage = "Price updated successfully";
            }, "Failed to save price change");

            IsLoading = false;
        }

        private bool CanCancelEdit()
        {
            return IsEditingPrice;
        }

        private void CancelEdit()
        {
            IsEditingPrice = false;
            NewPrice = 0;
            PriceChangeReason = "";
            VendorName = "";
            InvoiceNumber = "";
            QuantityPurchased = null;
            StatusMessage = "Price edit cancelled";
        }

        #endregion

        #region Alert Management

        private bool CanClearAlerts()
        {
            return PriceAlerts?.Count > 0;
        }

        private void ClearAlerts()
        {
            SafeExecute(() =>
            {
                PriceAlerts.Clear();
                StatusMessage = "Price alerts cleared";
            }, "Failed to clear alerts");
        }

        #endregion

        #region Recommendations

        private bool CanGenerateRecommendations()
        {
            return !IsLoading;
        }

        private void GenerateBulkPurchaseRecommendations()
        {
            SafeExecute(() =>
            {
                IsLoading = true;
                StatusMessage = "Generating bulk purchase recommendations...";

                BulkPurchaseRecommendations.Clear();

                foreach (var material in MaterialList)
                {
                    var recommendation = _pricingService.GetBulkPurchaseRecommendation(material.MaterialId);
                    if (recommendation != null)
                    {
                        BulkPurchaseRecommendations.Add(recommendation);
                    }
                }

                StatusMessage = $"Generated {BulkPurchaseRecommendations.Count} recommendations";
            }, "Failed to generate recommendations");

            IsLoading = false;
        }

        #endregion

        #region Export

        private bool CanExportHistory()
        {
            return PriceHistoryList?.Count > 0;
        }

        private void ExportPriceHistory()
        {
            SafeExecute(() =>
            {
                // TODO: Implement Excel export functionality
                StatusMessage = "Export feature coming soon...";
            }, "Failed to export price history");
        }

        #endregion

        #endregion

        #region Helper Methods

        private void LoadPriceHistoryForSelectedMaterial()
        {
            if (SelectedMaterial == null) return;

            SafeExecute(() =>
            {
                var history = _pricingService.GetPriceHistory(
                    SelectedMaterial.MaterialId,
                    HistoryStartDate,
                    HistoryEndDate
                );

                PriceHistoryList.Clear();
                foreach (var entry in history.OrderByDescending(h => h.EffectiveDate))
                {
                    PriceHistoryList.Add(entry);
                }

                _historyViewSource.Source = PriceHistoryList;
            }, "Failed to load price history");
        }

        private void LoadPriceAlerts()
        {
            SafeExecute(() =>
            {
                PriceAlerts.Clear();

                var materialsWithAlerts = _pricingService.GetMaterialsWithSignificantPriceChanges(AlertThreshold);
                
                foreach (var material in materialsWithAlerts)
                {
                    var recentHistory = _pricingService.GetPriceHistory(material.MaterialId, DateTime.Now.AddDays(-7));
                    var significantChanges = recentHistory.Where(h => Math.Abs(h.PercentageChangeFromPrevious) >= AlertThreshold);

                    foreach (var change in significantChanges)
                    {
                        PriceAlerts.Add(new MaterialPriceAlert
                        {
                            Material = material,
                            PriceHistory = change,
                            AlertLevel = change.AlertLevel,
                            AlertDate = change.ChangeDate,
                            Message = $"{material.Name}: {change.PriceDirection} {Math.Abs(change.PercentageChangeFromPrevious):F1}%"
                        });
                    }
                }
            }, "Failed to load price alerts");
        }

        private void LoadMaterialCategories()
        {
            SafeExecute(() =>
            {
                var materials = _databaseService.GetAllMaterials();
                MaterialCategories = materials.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();
                OnPropertyChanged(nameof(MaterialCategories));
            }, "Failed to load material categories");
        }

        private void UpdateHistoryDateRange()
        {
            switch (SelectedTimeFrame)
            {
                case "Last 7 Days":
                    HistoryStartDate = DateTime.Now.AddDays(-7);
                    break;
                case "Last 30 Days":
                    HistoryStartDate = DateTime.Now.AddDays(-30);
                    break;
                case "Last 90 Days":
                    HistoryStartDate = DateTime.Now.AddDays(-90);
                    break;
                case "Last 6 Months":
                    HistoryStartDate = DateTime.Now.AddMonths(-6);
                    break;
                case "Last Year":
                    HistoryStartDate = DateTime.Now.AddYears(-1);
                    break;
                case "All Time":
                    HistoryStartDate = DateTime.MinValue;
                    break;
            }
            
            HistoryEndDate = DateTime.Now;
        }

        private void ApplyFilters()
        {
            if (_materialsViewSource?.View == null) return;

            _materialsViewSource.View.Filter = item =>
            {
                if (!(item is Material material)) return false;

                // Search text filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    if (!material.Name.ToLower().Contains(searchLower) &&
                        !material.MaterialCode.ToLower().Contains(searchLower) &&
                        !material.Category.ToLower().Contains(searchLower))
                    {
                        return false;
                    }
                }

                // Category filter
                if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All Categories")
                {
                    if (!material.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Alerts mode filter
                if (ShowOnlyAlertsMode)
                {
                    var hasAlerts = PriceAlerts.Any(a => a.Material.MaterialId == material.MaterialId);
                    if (!hasAlerts) return false;
                }

                return true;
            };

            _materialsViewSource.View.Refresh();
        }

        #endregion

        #region Event Handlers

        private void OnMajorPriceChangeReceived(object sender, PriceChangeAlertEventArgs e)
        {
            SafeExecute(() =>
            {
                PriceAlerts.Add(new MaterialPriceAlert
                {
                    Material = e.Material,
                    AlertLevel = e.AlertLevel,
                    AlertDate = DateTime.Now,
                    Message = $"MAJOR CHANGE: {e.Material.Name} changed {e.PercentageChange:+0.##;-0.##;0}% (${e.OldPrice:F2} → ${e.NewPrice:F2})"
                });

                StatusMessage = $"Major price change alert: {e.Material.Name}";
            });
        }

        private void OnModeratePriceChangeReceived(object sender, PriceChangeAlertEventArgs e)
        {
            SafeExecute(() =>
            {
                PriceAlerts.Add(new MaterialPriceAlert
                {
                    Material = e.Material,
                    AlertLevel = e.AlertLevel,
                    AlertDate = DateTime.Now,
                    Message = $"REVIEW: {e.Material.Name} changed {e.PercentageChange:+0.##;-0.##;0}% (${e.OldPrice:F2} → ${e.NewPrice:F2})"
                });

                StatusMessage = $"Price change review needed: {e.Material.Name}";
            });
        }

        #endregion

        #region Cleanup

        protected override void OnDispose()
        {
            // Unsubscribe from events
            if (_pricingService != null)
            {
                _pricingService.OnMajorPriceChange -= OnMajorPriceChangeReceived;
                _pricingService.OnModeratePriceChange -= OnModeratePriceChangeReceived;
            }

            base.OnDispose();
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Material price alert for UI display
    /// </summary>
    public class MaterialPriceAlert
    {
        public Material Material { get; set; }
        public MaterialPriceHistory PriceHistory { get; set; }
        public PriceChangeAlertLevel AlertLevel { get; set; }
        public DateTime AlertDate { get; set; }
        public string Message { get; set; }

        public string AlertColor
        {
            get
            {
                switch (AlertLevel)
                {
                    case PriceChangeAlertLevel.Immediate:
                        return "#EF4444"; // Red
                    case PriceChangeAlertLevel.Review:
                        return "#F59E0B"; // Orange
                    default:
                        return "#10B981"; // Green
                }
            }
        }

        public string FormattedAlertDate => AlertDate.ToString("MM/dd/yyyy HH:mm");
    }

    #endregion
}