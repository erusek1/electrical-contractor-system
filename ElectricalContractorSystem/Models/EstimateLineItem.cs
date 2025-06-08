namespace ElectricalContractorSystem.Models
{
    public class EstimateLineItem
    {
        public int LineId { get; set; }
        public int RoomId { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MaterialCost { get; set; }
        public int LaborMinutes { get; set; }
        public int LineOrder { get; set; }
        public string Notes { get; set; }
        
        // Calculated property
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
