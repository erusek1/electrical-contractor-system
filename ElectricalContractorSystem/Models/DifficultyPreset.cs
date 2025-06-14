using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class DifficultyPreset
    {
        public int PresetId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Category { get; set; }
        
        public decimal RoughMultiplier { get; set; } = 1.00m;
        public decimal FinishMultiplier { get; set; } = 1.00m;
        public decimal ServiceMultiplier { get; set; } = 1.00m;
        public decimal ExtraMultiplier { get; set; } = 1.00m;
        
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        
        // Display properties
        public string DisplayText => 
            $"{Name} (R: {RoughMultiplier:P0}, F: {FinishMultiplier:P0})";
        
        public string FullDisplayText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                
                if (RoughMultiplier != 1.00m)
                    parts.Add($"Rough: {(RoughMultiplier > 1 ? "+" : "")}{(RoughMultiplier - 1) * 100:0}%");
                    
                if (FinishMultiplier != 1.00m)
                    parts.Add($"Finish: {(FinishMultiplier > 1 ? "+" : "")}{(FinishMultiplier - 1) * 100:0}%");
                    
                if (ServiceMultiplier != 1.00m)
                    parts.Add($"Service: {(ServiceMultiplier > 1 ? "+" : "")}{(ServiceMultiplier - 1) * 100:0}%");
                    
                if (ExtraMultiplier != 1.00m)
                    parts.Add($"Extra: {(ExtraMultiplier > 1 ? "+" : "")}{(ExtraMultiplier - 1) * 100:0}%");
                
                return parts.Count > 0 ? string.Join(", ", parts) : "No adjustment";
            }
        }
        
        // Check if this preset has any effect
        public bool HasAdjustment => 
            RoughMultiplier != 1.00m || 
            FinishMultiplier != 1.00m || 
            ServiceMultiplier != 1.00m || 
            ExtraMultiplier != 1.00m;
    }
}
