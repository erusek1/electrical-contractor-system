using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class EstimatePermitItem
    {
        public int PermitItemId { get; set; }
        
        [Required]
        public int EstimateId { get; set; }
        
        [Required]
        public int PermitTypeId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual Estimate Estimate { get; set; }
        public virtual PermitItemType PermitType { get; set; }
    }
}