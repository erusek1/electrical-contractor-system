namespace ElectricalContractorSystem.Models
{
    public class EstimateLineItem
    {
        public int LineId { get; set; }
        public int RoomId { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        
        // Aliases for compatibility
        public string Description 
        { 
            get => ItemDescription; 
            set => ItemDescription = value; 
        }
        
        public string ItemName
        {
            get => ItemDescription;
            set => ItemDescription = value;
        }
        
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MaterialCost { get; set; }
        public int LaborMinutes { get; set; }
        public int LineOrder { get; set; }
        public string Notes { get; set; }
        
        // Calculated property
        public decimal TotalPrice => Quantity * UnitPrice;
        
        // Factory method to create from PriceListItem
        public static EstimateLineItem CreateFromPriceListItem(PriceListItem item)
        {
            return new EstimateLineItem
            {
                ItemCode = item.ItemCode,
                ItemDescription = item.Name,
                UnitPrice = item.SellPrice,
                MaterialCost = item.BaseCost,
                LaborMinutes = item.LaborMinutes,
                Quantity = 1,
                LineOrder = 0
            };
        }
    }
}
