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
        
        // Calculated properties
        public decimal TotalCost => Quantity * (PriceListItem?.SellPrice ?? 0);
        
        public string DisplayText => $"{Quantity} x {PriceListItem?.Name ?? "Unknown"}";
        
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
