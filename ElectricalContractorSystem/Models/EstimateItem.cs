namespace ElectricalContractorSystem.Models
{
    public class EstimateItem
    {
        public int ItemId { get; set; }
        public int RoomId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MaterialCost { get; set; }
        public int LaborMinutes { get; set; }
        public decimal TotalPrice { get; set; }
        public int LineOrder { get; set; }

        public void CalculateTotal()
        {
            MaterialCost = UnitPrice * Quantity;
            TotalPrice = MaterialCost; // This will be adjusted by tax and markup at the estimate level
        }
    }
}
