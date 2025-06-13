namespace ElectricalContractorSystem.Models
{
    public class LaborMinute
    {
        public int LaborId { get; set; }
        public int ItemId { get; set; }
        public string Stage { get; set; }
        public int Minutes { get; set; }
        
        // Static properties for stage-specific minutes (used for compatibility)
        public static int RoughMinutes { get; set; }
        public static int FinishMinutes { get; set; }
        public static int ServiceMinutes { get; set; }
        public static int ExtraMinutes { get; set; }
        
        // Navigation property
        public virtual PriceListItem PriceListItem { get; set; }
    }
}
