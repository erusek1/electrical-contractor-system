using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class MaterialStage
    {
        public int MaterialStageId { get; set; }
        
        [Required]
        public int ItemId { get; set; }
        
        [Required]
        public string Stage { get; set; }
        
        [Required]
        public decimal MaterialCost { get; set; }
        
        // Navigation property
        public virtual PriceListItem PriceListItem { get; set; }
    }
}