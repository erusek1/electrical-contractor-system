namespace ElectricalContractorSystem.Models
{
    public class AssemblyComponent
    {
        public int ComponentId { get; set; }
        
        public int AssemblyId { get; set; }
        
        // Changed from MaterialId to PriceListItemId
        public int PriceListItemId { get; set; }
        
        public decimal Quantity { get; set; } = 1;
        
        public bool IsOptional { get; set; } = false;
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual AssemblyTemplate Assembly { get; set; }
        public virtual PriceListItem PriceListItem { get; set; }
        
        // Additional properties for easier access (not stored in DB)
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Calculated properties
        public decimal TotalCost => Quantity * (PriceListItem?.SellPrice ?? UnitPrice);
        
        public string DisplayText => $"{Quantity} x {ItemName ?? PriceListItem?.Name ?? "Unknown"}";
        
        // Backward compatibility properties for migration
        public int MaterialId 
        { 
            get => PriceListItemId; 
            set => PriceListItemId = value; 
        }
        
        public Material Material 
        { 
            get => null; // No longer used
            set { } // Ignore sets
        }
    }
}
