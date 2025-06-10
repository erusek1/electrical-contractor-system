using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    public class PermitItemType
    {
        public int PermitTypeId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
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