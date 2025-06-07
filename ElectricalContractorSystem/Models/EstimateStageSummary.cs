using System.ComponentModel.DataAnnotations;

namespace ElectricalContractorSystem.Models
{
    public class EstimateStageSummary
    {
        public int SummaryId { get; set; }
        
        [Required]
        public int EstimateId { get; set; }
        
        [Required]
        public string Stage { get; set; }
        
        public decimal EstimatedHours { get; set; }
        
        public decimal EstimatedMaterial { get; set; }
        
        // Navigation property
        public virtual Estimate Estimate { get; set; }
    }
}