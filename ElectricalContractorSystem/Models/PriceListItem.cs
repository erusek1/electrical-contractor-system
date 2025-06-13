namespace ElectricalContractorSystem.Models
{
    public class PriceListItem
    {
        public int ItemId { get; set; }
        public string Category { get; set; }
        public string ItemCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal BaseCost { get; set; }
        public decimal TaxRate { get; set; }
        public int LaborMinutes { get; set; }
        public decimal MarkupPercentage { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
        
        // Stage-specific labor minutes
        public int? RoughMinutes { get; set; }
        public int? FinishMinutes { get; set; }
        public int? ServiceMinutes { get; set; }
        public int? ExtraMinutes { get; set; }

        public PriceListItem()
        {
            TaxRate = 0.064m; // Default 6.4%
            MarkupPercentage = 22m; // Default 22%
            IsActive = true;
        }

        public decimal CalculateTotalPrice(int quantity = 1, bool includeTax = true, bool includeMarkup = true)
        {
            decimal baseTotal = BaseCost * quantity;
            
            if (includeMarkup && MarkupPercentage > 0)
            {
                baseTotal *= (1 + MarkupPercentage / 100);
            }

            if (includeTax && TaxRate > 0)
            {
                baseTotal *= (1 + TaxRate);
            }

            return baseTotal;
        }
        
        // Calculated property to get sell price (with markup but no tax)
        public decimal SellPrice 
        { 
            get
            {
                return BaseCost * (1 + MarkupPercentage / 100);
            }
        }
    }
}
