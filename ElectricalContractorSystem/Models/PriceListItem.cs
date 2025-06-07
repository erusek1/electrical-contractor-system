using System;
using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class PriceListItem
    {
        public int ItemId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string ItemCode { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public decimal BasePrice { get; set; }
        
        public decimal TaxRate { get; set; } = 0.064m;
        
        public decimal TotalPrice => BasePrice * (1 + TaxRate);
        
        [StringLength(20)]
        public string Unit { get; set; } = "each";
        
        [StringLength(50)]
        public string Category { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<LaborMinute> LaborMinutes { get; set; }
        public virtual ICollection<MaterialStage> MaterialStages { get; set; }
        
        public PriceListItem()
        {
            LaborMinutes = new HashSet<LaborMinute>();
            MaterialStages = new HashSet<MaterialStage>();
        }
        
        // Helper method to get labor minutes for a specific stage
        public int GetLaborMinutes(string stage)
        {
            var labor = LaborMinutes.FirstOrDefault(l => l.Stage == stage);
            return labor?.Minutes ?? 0;
        }
        
        // Helper method to get material cost for a specific stage
        public decimal GetMaterialCost(string stage)
        {
            var material = MaterialStages.FirstOrDefault(m => m.Stage == stage);
            return material?.MaterialCost ?? 0;
        }
    }
}