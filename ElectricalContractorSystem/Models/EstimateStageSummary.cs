namespace ElectricalContractorSystem.Models
{
    public class EstimateStageSummary
    {
        public int SummaryId { get; set; }
        public int EstimateId { get; set; }
        public string Stage { get; set; }
        public decimal LaborHours { get; set; }
        public decimal MaterialCost { get; set; }
        public int StageOrder { get; set; }
        
        // Additional properties for compatibility
        public string StageName 
        { 
            get => Stage; 
            set => Stage = value; 
        }
        
        public decimal TotalLaborHours 
        { 
            get => LaborHours; 
            set => LaborHours = value; 
        }
        
        public decimal TotalMaterialCost 
        { 
            get => MaterialCost; 
            set => MaterialCost = value; 
        }
    }
}
