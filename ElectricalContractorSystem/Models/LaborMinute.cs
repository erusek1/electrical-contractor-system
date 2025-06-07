using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class LaborMinute
    {
        public int LaborId { get; set; }
        
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        public string Stage { get; set; }
        
        [Required]
        public int Minutes { get; set; }
        
        // Navigation property
        public virtual PriceListItem PriceListItem { get; set; }
    }
}