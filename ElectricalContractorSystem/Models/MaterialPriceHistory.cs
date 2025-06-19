using System;
using System.ComponentModel;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Tracks material price changes over time with alert level analysis
    /// </summary>
    public class MaterialPriceHistory : INotifyPropertyChanged
    {
        #region Fields
        
        private int _historyId;
        private int _materialId;
        private decimal _oldPrice;
        private decimal _newPrice;
        private decimal _price;
        private decimal _percentageChange;
        private decimal _percentageChangeFromPrevious;
        private string _changedBy;
        private string _createdBy;
        private DateTime _changeDate;
        private DateTime _effectiveDate;
        private int? _vendorId;
        private string _invoiceNumber;
        private string _purchaseOrderNumber;
        private decimal? _quantityPurchased;
        private string _notes;
        
        #endregion

        #region Properties

        public int HistoryId
        {
            get { return _historyId; }
            set
            {
                if (_historyId != value)
                {
                    _historyId = value;
                    OnPropertyChanged(nameof(HistoryId));
                }
            }
        }
        
        public int MaterialId
        {
            get { return _materialId; }
            set
            {
                if (_materialId != value)
                {
                    _materialId = value;
                    OnPropertyChanged(nameof(MaterialId));
                }
            }
        }
        
        public decimal OldPrice
        {
            get { return _oldPrice; }
            set
            {
                if (_oldPrice != value)
                {
                    _oldPrice = value;
                    OnPropertyChanged(nameof(OldPrice));
                    UpdatePercentageChange();
                }
            }
        }
        
        public decimal NewPrice
        {
            get { return _newPrice; }
            set
            {
                if (_newPrice != value)
                {
                    _newPrice = value;
                    OnPropertyChanged(nameof(NewPrice));
                    UpdatePercentageChange();
                }
            }
        }
        
        public decimal Price
        {
            get { return _price; }
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged(nameof(Price));
                    // Update NewPrice if they should be the same
                    if (_newPrice != value)
                    {
                        _newPrice = value;
                        OnPropertyChanged(nameof(NewPrice));
                        UpdatePercentageChange();
                    }
                }
            }
        }
        
        public decimal PercentageChange
        {
            get { return _percentageChange; }
            set
            {
                if (_percentageChange != value)
                {
                    _percentageChange = value;
                    OnPropertyChanged(nameof(PercentageChange));
                    OnPropertyChanged(nameof(AlertLevel));
                    OnPropertyChanged(nameof(IsMinorChange));
                    OnPropertyChanged(nameof(IsModerateChange));
                    OnPropertyChanged(nameof(IsMajorChange));
                }
            }
        }
        
        public decimal PercentageChangeFromPrevious
        {
            get { return _percentageChangeFromPrevious; }
            set
            {
                if (_percentageChangeFromPrevious != value)
                {
                    _percentageChangeFromPrevious = value;
                    OnPropertyChanged(nameof(PercentageChangeFromPrevious));
                }
            }
        }
        
        public string ChangedBy
        {
            get { return _changedBy; }
            set
            {
                if (_changedBy != value)
                {
                    _changedBy = value;
                    OnPropertyChanged(nameof(ChangedBy));
                }
            }
        }
        
        public string CreatedBy
        {
            get { return _createdBy; }
            set
            {
                if (_createdBy != value)
                {
                    _createdBy = value;
                    OnPropertyChanged(nameof(CreatedBy));
                }
            }
        }
        
        public DateTime ChangeDate
        {
            get { return _changeDate; }
            set
            {
                if (_changeDate != value)
                {
                    _changeDate = value;
                    OnPropertyChanged(nameof(ChangeDate));
                    OnPropertyChanged(nameof(FormattedChangeDate));
                }
            }
        }
        
        public DateTime EffectiveDate
        {
            get { return _effectiveDate; }
            set
            {
                if (_effectiveDate != value)
                {
                    _effectiveDate = value;
                    OnPropertyChanged(nameof(EffectiveDate));
                    OnPropertyChanged(nameof(FormattedEffectiveDate));
                }
            }
        }
        
        public int? VendorId
        {
            get { return _vendorId; }
            set
            {
                if (_vendorId != value)
                {
                    _vendorId = value;
                    OnPropertyChanged(nameof(VendorId));
                }
            }
        }
        
        public string InvoiceNumber
        {
            get { return _invoiceNumber; }
            set
            {
                if (_invoiceNumber != value)
                {
                    _invoiceNumber = value;
                    OnPropertyChanged(nameof(InvoiceNumber));
                }
            }
        }
        
        public string PurchaseOrderNumber
        {
            get { return _purchaseOrderNumber; }
            set
            {
                if (_purchaseOrderNumber != value)
                {
                    _purchaseOrderNumber = value;
                    OnPropertyChanged(nameof(PurchaseOrderNumber));
                }
            }
        }
        
        public decimal? QuantityPurchased
        {
            get { return _quantityPurchased; }
            set
            {
                if (_quantityPurchased != value)
                {
                    _quantityPurchased = value;
                    OnPropertyChanged(nameof(QuantityPurchased));
                }
            }
        }
        
        public string Notes
        {
            get { return _notes; }
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged(nameof(Notes));
                }
            }
        }

        #endregion

        #region Navigation Properties
        
        public virtual Material Material { get; set; }
        public virtual Vendor Vendor { get; set; }
        
        #endregion

        #region Computed Properties

        /// <summary>
        /// Formatted change date for display
        /// </summary>
        public string FormattedChangeDate
        {
            get { return ChangeDate.ToString("MM/dd/yyyy HH:mm"); }
        }

        /// <summary>
        /// Formatted effective date for display
        /// </summary>
        public string FormattedEffectiveDate
        {
            get { return EffectiveDate.ToString("MM/dd/yyyy"); }
        }

        /// <summary>
        /// Indicates if this is a minor price change (less than 5%)
        /// </summary>
        public bool IsMinorChange => Math.Abs(PercentageChange) < 5;

        /// <summary>
        /// Indicates if this is a moderate price change (5-15%)
        /// </summary>
        public bool IsModerateChange => Math.Abs(PercentageChange) >= 5 && Math.Abs(PercentageChange) < 15;

        /// <summary>
        /// Indicates if this is a major price change (15% or more)
        /// </summary>
        public bool IsMajorChange => Math.Abs(PercentageChange) >= 15;
        
        /// <summary>
        /// Alert level based on percentage change - READ ONLY COMPUTED PROPERTY
        /// </summary>
        public PriceChangeAlertLevel AlertLevel
        {
            get
            {
                if (IsMajorChange) return PriceChangeAlertLevel.Immediate;
                if (IsModerateChange) return PriceChangeAlertLevel.Review;
                return PriceChangeAlertLevel.None;
            }
        }

        /// <summary>
        /// Color indicator for UI display based on alert level
        /// </summary>
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
                    case PriceChangeAlertLevel.None:
                    default:
                        return "#10B981"; // Green
                }
            }
        }

        /// <summary>
        /// Text description of the alert level
        /// </summary>
        public string AlertDescription
        {
            get
            {
                switch (AlertLevel)
                {
                    case PriceChangeAlertLevel.Immediate:
                        return "Immediate Alert";
                    case PriceChangeAlertLevel.Review:
                        return "Review Required";
                    case PriceChangeAlertLevel.None:
                    default:
                        return "Normal Change";
                }
            }
        }

        /// <summary>
        /// Direction of price change for display
        /// </summary>
        public string PriceDirection
        {
            get
            {
                if (PercentageChange > 0) return "↑ Increase";
                if (PercentageChange < 0) return "↓ Decrease";
                return "→ No Change";
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public MaterialPriceHistory()
        {
            ChangeDate = DateTime.Now;
            EffectiveDate = DateTime.Now;
        }

        /// <summary>
        /// Constructor with basic price information
        /// </summary>
        public MaterialPriceHistory(int materialId, decimal oldPrice, decimal newPrice)
        {
            MaterialId = materialId;
            OldPrice = oldPrice;
            NewPrice = newPrice;
            Price = newPrice;
            ChangeDate = DateTime.Now;
            EffectiveDate = DateTime.Now;
            UpdatePercentageChange();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the percentage change calculation
        /// </summary>
        private void UpdatePercentageChange()
        {
            if (OldPrice != 0)
            {
                var change = ((NewPrice - OldPrice) / OldPrice) * 100;
                PercentageChange = Math.Round(change, 2);
            }
            else
            {
                PercentageChange = 0;
            }
        }

        /// <summary>
        /// Creates a new price history entry
        /// </summary>
        public static MaterialPriceHistory CreateEntry(int materialId, decimal oldPrice, decimal newPrice, 
            string changedBy, int? vendorId = null, string purchaseOrderNumber = null, decimal? quantityPurchased = null)
        {
            return new MaterialPriceHistory(materialId, oldPrice, newPrice)
            {
                ChangedBy = changedBy,
                CreatedBy = changedBy,
                VendorId = vendorId,
                PurchaseOrderNumber = purchaseOrderNumber,
                QuantityPurchased = quantityPurchased
            };
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"Material {MaterialId}: {OldPrice:C} → {NewPrice:C} ({PercentageChange:+0.##;-0.##;0}%)";
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
    
    /// <summary>
    /// Alert levels for price changes
    /// </summary>
    public enum PriceChangeAlertLevel
    {
        None,
        Review,
        Immediate
    }
}