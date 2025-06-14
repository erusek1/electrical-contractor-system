using System;

namespace ElectricalContractorSystem.Models
{
    public class MaterialPriceHistory
    {
        public int PriceHistoryId { get; set; }
        
        public int MaterialId { get; set; }
        
        public decimal Price { get; set; }
        
        public DateTime EffectiveDate { get; set; }
        
        public int? VendorId { get; set; }
        
        public string PurchaseOrderNumber { get; set; }
        
        public decimal? QuantityPurchased { get; set; }
        
        public string Notes { get; set; }
        
        public string CreatedBy { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Material Material { get; set; }
        public virtual Vendor Vendor { get; set; }
        
        // Calculated properties
        public decimal PercentageChangeFromPrevious { get; set; }
        
        // Analysis properties
        public bool IsMinorChange => Math.Abs(PercentageChangeFromPrevious) < 5;
        public bool IsModerateChange => Math.Abs(PercentageChangeFromPrevious) >= 5 && Math.Abs(PercentageChangeFromPrevious) < 15;
        public bool IsMajorChange => Math.Abs(PercentageChangeFromPrevious) >= 15;
        
        // Alert level
        public PriceChangeAlertLevel AlertLevel
        {
            get
            {
                if (IsMajorChange) return PriceChangeAlertLevel.Immediate;
                if (IsModerateChange) return PriceChangeAlertLevel.Review;
                return PriceChangeAlertLevel.None;
            }
        }
    }
    
    public enum PriceChangeAlertLevel
    {
        None,
        Review,
        Immediate
    }
}
