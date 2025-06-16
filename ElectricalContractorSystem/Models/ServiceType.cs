using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class ServiceType
    {
        public int ServiceTypeId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Code { get; set; }
        
        public string Description { get; set; }
        
        public decimal LaborMultiplier { get; set; } = 1.00m;
        
        public decimal BaseHourlyRate { get; set; } = 85.00m;
        
        public decimal MinimumHours { get; set; } = 1.00m;
        
        public decimal DriveTimeIncluded { get; set; } = 0.00m;
        
        public bool IsActive { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
        
        // Calculated properties
        public decimal CalculateRate(decimal baseHourlyRate)
        {
            return baseHourlyRate * LaborMultiplier;
        }
        
        public string DisplayText => $"{Name} ({LaborMultiplier:0.0}x)";
    }
}
