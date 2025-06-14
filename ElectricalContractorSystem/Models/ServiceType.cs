using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class ServiceType
    {
        public int ServiceTypeId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public decimal LaborMultiplier { get; set; } = 1.00m;
        
        public decimal MinimumHours { get; set; } = 1.00m;
        
        public decimal DriveTimeIncluded { get; set; } = 0.00m;
        
        public bool IsActive { get; set; } = true;
        
        // Calculated properties
        public decimal CalculateRate(decimal baseHourlyRate)
        {
            return baseHourlyRate * LaborMultiplier;
        }
        
        public string DisplayText => $"{Name} ({LaborMultiplier:0.0}x)";
    }
}
