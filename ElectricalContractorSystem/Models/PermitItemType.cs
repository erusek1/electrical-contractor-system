using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class PermitItemType
    {
        public int PermitTypeId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Description { get; set; }
        
        [StringLength(20)]
        public string Unit { get; set; } = "each";
        
        public string Notes { get; set; }
        
        // Navigation properties
        public virtual ICollection<EstimatePermitItem> EstimatePermitItems { get; set; }
        
        public PermitItemType()
        {
            EstimatePermitItems = new HashSet<EstimatePermitItem>();
        }
    }
}