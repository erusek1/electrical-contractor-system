namespace ElectricalContractorSystem.Models
{
    public class MaterialStage
    {
        public int MaterialStageId { get; set; }
        public int ItemId { get; set; }
        public string Stage { get; set; }
        public decimal MaterialCost { get; set; }
        
        // Navigation property
        public virtual PriceListItem PriceListItem { get; set; }
    }
}