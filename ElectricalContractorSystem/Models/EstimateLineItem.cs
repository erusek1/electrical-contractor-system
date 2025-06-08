namespace ElectricalContractorSystem.Models
{
    public class EstimateLineItem
    {
        public int LineItemId { get; set; }
        public int EstimateId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int LaborMinutes { get; set; }
        public int LineOrder { get; set; }
    }
}
