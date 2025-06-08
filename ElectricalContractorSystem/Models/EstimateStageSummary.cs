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
    }
}
