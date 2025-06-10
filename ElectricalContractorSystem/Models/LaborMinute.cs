namespace ElectricalContractorSystem.Models
{
    public class LaborMinute
    {
        public int LaborId { get; set; }
        public int ItemId { get; set; }
        public string Stage { get; set; }
        public int Minutes { get; set; }
        
        // Navigation property
        public virtual PriceListItem PriceListItem { get; set; }
    }
}