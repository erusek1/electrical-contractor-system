namespace ElectricalContractorSystem.Models
{
    public class RoomTemplateItem
    {
        public int TemplateItemId { get; set; }
        public int TemplateId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int DefaultQuantity { get; set; }
        public int DisplayOrder { get; set; }
        public decimal UnitPrice { get; set; }
        public int LaborMinutes { get; set; }

        public RoomTemplateItem()
        {
            DefaultQuantity = 1;
            DisplayOrder = 0;
        }
    }
}
