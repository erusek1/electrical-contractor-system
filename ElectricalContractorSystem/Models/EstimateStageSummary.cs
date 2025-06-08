namespace ElectricalContractorSystem.Models
{
    public class EstimateStageSummary
    {
        public int SummaryId { get; set; }
        public int EstimateId { get; set; }
        public string StageName { get; set; }
        public int TotalLaborMinutes { get; set; }
        public decimal TotalLaborHours { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public int StageOrder { get; set; }
    }
}
