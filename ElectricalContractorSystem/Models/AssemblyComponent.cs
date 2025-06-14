namespace ElectricalContractorSystem.Models
{
    public class AssemblyComponent
    {
        public int ComponentId { get; set; }
        
        public int AssemblyId { get; set; }
        
        public int MaterialId { get; set; }
        
        public decimal Quantity { get; set; } = 1;
        
        public bool IsOptional { get; set; } = false;
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual AssemblyTemplate Assembly { get; set; }
        public virtual Material Material { get; set; }
        
        // Calculated properties
        public decimal TotalCost => Quantity * (Material?.PriceWithTax ?? 0);
        
        public string DisplayText => $"{Quantity} x {Material?.Name ?? "Unknown"}";
    }
}
