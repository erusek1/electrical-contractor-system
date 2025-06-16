using System;

namespace ElectricalContractorSystem.Models
{
    public class MaterialPriceHistory
    {
        public int HistoryId { get; set; }
        
        public int MaterialId { get; set; }
        
        public decimal OldPrice { get; set; }
        
        public decimal NewPrice { get; set; }
        
        public decimal PercentageChange { get; set; }
        
        public string ChangedBy { get; set; }
        
        public DateTime ChangeDate { get; set; }
        
        public int? VendorId { get; set; }
        
        public string InvoiceNumber { get; set; }
        
        public decimal? QuantityPurchased { get; set; }
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual Material Material { get; set; }
        public virtual Vendor Vendor { get; set; }
        
        // Analysis properties
        public bool IsMinorChange => Math.Abs(PercentageChange) < 5;
        public bool IsModerateChange => Math.Abs(PercentageChange) >= 5 && Math.Abs(PercentageChange) < 15;
        public bool IsMajorChange => Math.Abs(PercentageChange) >= 15;
        
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
