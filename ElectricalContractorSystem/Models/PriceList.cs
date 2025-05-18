using System.Collections.Generic;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents an item in the price list for electrical services and materials
    /// </summary>
    public class PriceList
    {
        /// <summary>
        /// Database ID of the price list item
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Category of the item (e.g., Lighting, Switches, Outlets)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Code for the item (e.g., 'HH' for high hat, 'O' for outlet)
        /// </summary>
        public string ItemCode { get; set; }

        /// <summary>
        /// Name of the item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Detailed description of the item
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Base cost of the item (before tax and markup)
        /// </summary>
        public decimal BaseCost { get; set; }

        /// <summary>
        /// Tax rate as a percentage (e.g., 6.6 for 6.6%)
        /// </summary>
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// Estimated labor minutes to install/service the item
        /// </summary>
        public int? LaborMinutes { get; set; }

        /// <summary>
        /// Markup percentage (e.g., 15 for 15%)
        /// </summary>
        public decimal? MarkupPercentage { get; set; }

        /// <summary>
        /// Whether the item is active in the price list
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Navigation property to collection of RoomSpecifications using this item
        /// </summary>
        public ICollection<RoomSpecification> RoomSpecifications { get; set; }

        /// <summary>
        /// Calculates the cost with tax
        /// </summary>
        public decimal CostWithTax
        {
            get
            {
                if (TaxRate.HasValue)
                {
                    return BaseCost * (1 + (TaxRate.Value / 100));
                }
                return BaseCost;
            }
        }

        /// <summary>
        /// Calculates the cost with markup
        /// </summary>
        public decimal CostWithMarkup
        {
            get
            {
                if (MarkupPercentage.HasValue)
                {
                    return BaseCost * (1 + (MarkupPercentage.Value / 100));
                }
                return BaseCost;
            }
        }

        /// <summary>
        /// Calculates the total cost with tax and markup
        /// </summary>
        public decimal TotalCost
        {
            get
            {
                decimal taxMultiplier = 1 + ((TaxRate ?? 0) / 100);
                decimal markupMultiplier = 1 + ((MarkupPercentage ?? 0) / 100);
                return BaseCost * taxMultiplier * markupMultiplier;
            }
        }

        /// <summary>
        /// Calculates the labor cost based on the labor minutes and labor rate
        /// </summary>
        public decimal LaborCost(decimal hourlyRate = 75.0m)
        {
            if (LaborMinutes.HasValue)
            {
                return (LaborMinutes.Value / 60.0m) * hourlyRate;
            }
            return 0;
        }

        /// <summary>
        /// Returns a formatted name with code
        /// </summary>
        public string FormattedName
        {
            get
            {
                return $"{ItemCode} - {Name}";
            }
        }
    }
}
