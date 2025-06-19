using System;

namespace ElectricalContractorSystem.Models
{
    /// <summary>
    /// Represents the reason for a price change in materials or assemblies
    /// </summary>
    public enum PriceChangeReason
    {
        /// <summary>
        /// Vendor increased their prices
        /// </summary>
        VendorIncrease = 0,

        /// <summary>
        /// Vendor decreased their prices
        /// </summary>
        VendorDecrease = 1,

        /// <summary>
        /// Market forces drove price up
        /// </summary>
        MarketIncrease = 2,

        /// <summary>
        /// Market forces drove price down
        /// </summary>
        MarketDecrease = 3,

        /// <summary>
        /// Seasonal price adjustment (up)
        /// </summary>
        SeasonalIncrease = 4,

        /// <summary>
        /// Seasonal price adjustment (down)
        /// </summary>
        SeasonalDecrease = 5,

        /// <summary>
        /// Manual price correction by user
        /// </summary>
        ManualCorrection = 6,

        /// <summary>
        /// Bulk purchase discount received
        /// </summary>
        BulkDiscount = 7,

        /// <summary>
        /// Volume pricing tier reached
        /// </summary>
        VolumePricing = 8,

        /// <summary>
        /// Promotional pricing from vendor
        /// </summary>
        Promotional = 9,

        /// <summary>
        /// Contract pricing negotiated
        /// </summary>
        ContractPricing = 10,

        /// <summary>
        /// Cost of materials increased
        /// </summary>
        MaterialCostIncrease = 11,

        /// <summary>
        /// Transportation/shipping costs changed
        /// </summary>
        ShippingCostChange = 12,

        /// <summary>
        /// Currency exchange rate impact
        /// </summary>
        CurrencyFluctuation = 13,

        /// <summary>
        /// Tariff or tax changes
        /// </summary>
        TariffChange = 14,

        /// <summary>
        /// Supply shortage driving prices up
        /// </summary>
        SupplyShortage = 15,

        /// <summary>
        /// Oversupply driving prices down
        /// </summary>
        Oversupply = 16,

        /// <summary>
        /// Manufacturer changed pricing
        /// </summary>
        ManufacturerChange = 17,

        /// <summary>
        /// Product discontinued, final pricing
        /// </summary>
        ProductDiscontinued = 18,

        /// <summary>
        /// New product launch pricing
        /// </summary>
        NewProductLaunch = 19,

        /// <summary>
        /// Competitive pressure adjustment
        /// </summary>
        CompetitivePressure = 20,

        /// <summary>
        /// Error correction from previous entry
        /// </summary>
        ErrorCorrection = 21,

        /// <summary>
        /// System automatically updated price
        /// </summary>
        SystemUpdate = 22,

        /// <summary>
        /// Annual price review adjustment
        /// </summary>
        AnnualReview = 23,

        /// <summary>
        /// Emergency pricing due to crisis
        /// </summary>
        Emergency = 24,

        /// <summary>
        /// Other reason not listed
        /// </summary>
        Other = 25
    }

    /// <summary>
    /// Extension methods for PriceChangeReason enum
    /// </summary>
    public static class PriceChangeReasonExtensions
    {
        /// <summary>
        /// Gets the display name for the price change reason
        /// </summary>
        /// <param name="reason">The price change reason</param>
        /// <returns>Display-friendly reason name</returns>
        public static string GetDisplayName(this PriceChangeReason reason)
        {
            return reason switch
            {
                PriceChangeReason.VendorIncrease => "Vendor Price Increase",
                PriceChangeReason.VendorDecrease => "Vendor Price Decrease",
                PriceChangeReason.MarketIncrease => "Market Price Increase",
                PriceChangeReason.MarketDecrease => "Market Price Decrease",
                PriceChangeReason.SeasonalIncrease => "Seasonal Increase",
                PriceChangeReason.SeasonalDecrease => "Seasonal Decrease",
                PriceChangeReason.ManualCorrection => "Manual Correction",
                PriceChangeReason.BulkDiscount => "Bulk Purchase Discount",
                PriceChangeReason.VolumePricing => "Volume Pricing",
                PriceChangeReason.Promotional => "Promotional Pricing",
                PriceChangeReason.ContractPricing => "Contract Pricing",
                PriceChangeReason.MaterialCostIncrease => "Material Cost Increase",
                PriceChangeReason.ShippingCostChange => "Shipping Cost Change",
                PriceChangeReason.CurrencyFluctuation => "Currency Fluctuation",
                PriceChangeReason.TariffChange => "Tariff/Tax Change",
                PriceChangeReason.SupplyShortage => "Supply Shortage",
                PriceChangeReason.Oversupply => "Oversupply",
                PriceChangeReason.ManufacturerChange => "Manufacturer Change",
                PriceChangeReason.ProductDiscontinued => "Product Discontinued",
                PriceChangeReason.NewProductLaunch => "New Product Launch",
                PriceChangeReason.CompetitivePressure => "Competitive Pressure",
                PriceChangeReason.ErrorCorrection => "Error Correction",
                PriceChangeReason.SystemUpdate => "System Update",
                PriceChangeReason.AnnualReview => "Annual Review",
                PriceChangeReason.Emergency => "Emergency Pricing",
                PriceChangeReason.Other => "Other",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the category of the price change reason
        /// </summary>
        /// <param name="reason">The price change reason</param>
        /// <returns>Category name</returns>
        public static string GetCategory(this PriceChangeReason reason)
        {
            return reason switch
            {
                PriceChangeReason.VendorIncrease or PriceChangeReason.VendorDecrease => "Vendor",
                PriceChangeReason.MarketIncrease or PriceChangeReason.MarketDecrease => "Market",
                PriceChangeReason.SeasonalIncrease or PriceChangeReason.SeasonalDecrease => "Seasonal",
                PriceChangeReason.BulkDiscount or PriceChangeReason.VolumePricing or PriceChangeReason.Promotional or PriceChangeReason.ContractPricing => "Discount",
                PriceChangeReason.MaterialCostIncrease or PriceChangeReason.ShippingCostChange => "Cost",
                PriceChangeReason.CurrencyFluctuation or PriceChangeReason.TariffChange => "External",
                PriceChangeReason.SupplyShortage or PriceChangeReason.Oversupply => "Supply",
                PriceChangeReason.ManufacturerChange or PriceChangeReason.ProductDiscontinued or PriceChangeReason.NewProductLaunch => "Product",
                PriceChangeReason.ManualCorrection or PriceChangeReason.ErrorCorrection or PriceChangeReason.SystemUpdate => "Administrative",
                PriceChangeReason.CompetitivePressure or PriceChangeReason.AnnualReview => "Strategic",
                PriceChangeReason.Emergency => "Emergency",
                PriceChangeReason.Other => "Other",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Determines if the reason indicates a price increase
        /// </summary>
        /// <param name="reason">The price change reason</param>
        /// <returns>True if typically indicates increase, false otherwise</returns>
        public static bool IndicatesIncrease(this PriceChangeReason reason)
        {
            return reason == PriceChangeReason.VendorIncrease ||
                   reason == PriceChangeReason.MarketIncrease ||
                   reason == PriceChangeReason.SeasonalIncrease ||
                   reason == PriceChangeReason.MaterialCostIncrease ||
                   reason == PriceChangeReason.SupplyShortage ||
                   reason == PriceChangeReason.TariffChange ||
                   reason == PriceChangeReason.Emergency;
        }

        /// <summary>
        /// Determines if the reason indicates a price decrease
        /// </summary>
        /// <param name="reason">The price change reason</param>
        /// <returns>True if typically indicates decrease, false otherwise</returns>
        public static bool IndicatesDecrease(this PriceChangeReason reason)
        {
            return reason == PriceChangeReason.VendorDecrease ||
                   reason == PriceChangeReason.MarketDecrease ||
                   reason == PriceChangeReason.SeasonalDecrease ||
                   reason == PriceChangeReason.BulkDiscount ||
                   reason == PriceChangeReason.VolumePricing ||
                   reason == PriceChangeReason.Promotional ||
                   reason == PriceChangeReason.Oversupply;
        }

        /// <summary>
        /// Determines if the reason requires documentation
        /// </summary>
        /// <param name="reason">The price change reason</param>
        /// <returns>True if requires documentation, false otherwise</returns>
        public static bool RequiresDocumentation(this PriceChangeReason reason)
        {
            return reason == PriceChangeReason.ManualCorrection ||
                   reason == PriceChangeReason.ContractPricing ||
                   reason == PriceChangeReason.ErrorCorrection ||
                   reason == PriceChangeReason.Emergency ||
                   reason == PriceChangeReason.Other;
        }
    }
}