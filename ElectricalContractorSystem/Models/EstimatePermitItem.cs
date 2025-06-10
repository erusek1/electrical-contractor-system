namespace ElectricalContractorSystem.Models
{
    public class EstimatePermitItem
    {
        public int PermitItemId { get; set; }
        public int EstimateId { get; set; }
        public int PermitTypeId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual Estimate Estimate { get; set; }
        public virtual PermitItemType PermitType { get; set; }
    }
}