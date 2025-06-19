using System;
using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class Material
    {
        public int MaterialId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string MaterialCode { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        [Required]
        [StringLength(20)]
        public string UnitOfMeasure { get; set; } = "Each";
        
        [Required]
        public decimal CurrentPrice { get; set; }
        
        public decimal TaxRate { get; set; } = 6.4m;
        
        public int MinStockLevel { get; set; } = 0;
        
        public int MaxStockLevel { get; set; } = 0;
        
        public int? PreferredVendorId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
        
        public string CreatedBy { get; set; } // ADDED: Missing property for AddMaterialDialog
        
        // Navigation properties
        public virtual Vendor PreferredVendor { get; set; }
        
        // Calculated properties
        public decimal PriceWithTax => CurrentPrice * (1 + TaxRate / 100);
        
        // Methods
        public void UpdatePrice(decimal newPrice, string updatedBy)
        {
            if (newPrice != CurrentPrice)
            {
                CurrentPrice = newPrice;
                UpdatedDate = DateTime.Now;
                
                // This would trigger creation of price history record
                OnPriceChanged(newPrice, updatedBy);
            }
        }
        
        public decimal CalculatePriceChange(decimal newPrice)
        {
            if (CurrentPrice == 0) return 0;
            return ((newPrice - CurrentPrice) / CurrentPrice) * 100;
        }
        
        public bool IsPriceChangeSignificant(decimal newPrice, decimal threshold = 5.0m)
        {
            return Math.Abs(CalculatePriceChange(newPrice)) >= threshold;
        }
        
        // Event for price changes
        public event EventHandler<PriceChangedEventArgs> PriceChanged;
        
        protected virtual void OnPriceChanged(decimal newPrice, string updatedBy)
        {
            PriceChanged?.Invoke(this, new PriceChangedEventArgs 
            { 
                OldPrice = CurrentPrice, 
                NewPrice = newPrice,
                ChangedBy = updatedBy,
                ChangeDate = DateTime.Now
            });
        }
    }
    
    public class PriceChangedEventArgs : EventArgs
    {
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangeDate { get; set; }
        
        public decimal PercentageChange => 
            OldPrice == 0 ? 0 : ((NewPrice - OldPrice) / OldPrice) * 100;
    }
}
