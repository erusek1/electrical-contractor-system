using System;
using System.Collections.Generic;
using System.Linq;
using ElectricalContractorSystem.Models;
using MySql.Data.MySqlClient;

namespace ElectricalContractorSystem.Services
{
    public class PricingService
    {
        private readonly DatabaseService _databaseService;
        
        public PricingService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }
        
        #region Material Price Management
        
        public void UpdateMaterialPrice(int materialId, decimal newPrice, string updatedBy, 
            int? vendorId = null, string purchaseOrderNumber = null, decimal? quantityPurchased = null)
        {
            var material = _databaseService.GetMaterialById(materialId);
            if (material == null) return;
            
            var oldPrice = material.CurrentPrice;
            var percentageChange = material.CalculatePriceChange(newPrice);
            
            // Create price history record
            var priceHistory = new MaterialPriceHistory
            {
                MaterialId = materialId,
                Price = newPrice,
                EffectiveDate = DateTime.Now,
                VendorId = vendorId,
                PurchaseOrderNumber = purchaseOrderNumber,
                QuantityPurchased = quantityPurchased,
                CreatedBy = updatedBy,
                PercentageChangeFromPrevious = percentageChange
            };
            
            _databaseService.SaveMaterialPriceHistory(priceHistory);
            
            // Update material current price
            material.UpdatePrice(newPrice, updatedBy);
            _databaseService.UpdateMaterial(material);
            
            // Check for alerts
            CheckPriceChangeAlerts(material, oldPrice, newPrice, percentageChange);
        }
        
        private void CheckPriceChangeAlerts(Material material, decimal oldPrice, decimal newPrice, decimal percentageChange)
        {
            var absChange = Math.Abs(percentageChange);
            
            if (absChange >= 15)
            {
                // Major change - immediate alert
                OnMajorPriceChange?.Invoke(this, new PriceChangeAlertEventArgs
                {
                    Material = material,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PercentageChange = percentageChange,
                    AlertLevel = PriceChangeAlertLevel.Immediate
                });
            }
            else if (absChange >= 5)
            {
                // Moderate change - review needed
                OnModeratePriceChange?.Invoke(this, new PriceChangeAlertEventArgs
                {
                    Material = material,
                    OldPrice = oldPrice,
                    NewPrice = newPrice,
                    PercentageChange = percentageChange,
                    AlertLevel = PriceChangeAlertLevel.Review
                });
            }
        }

        // Public methods to fire events (for UpdatePriceDialog)
        public void FireMajorPriceChangeEvent(Material material, decimal oldPrice, decimal newPrice, decimal percentageChange)
        {
            OnMajorPriceChange?.Invoke(this, new PriceChangeAlertEventArgs
            {
                Material = material,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                PercentageChange = percentageChange,
                AlertLevel = PriceChangeAlertLevel.Immediate
            });
        }

        public void FireModeratePriceChangeEvent(Material material, decimal oldPrice, decimal newPrice, decimal percentageChange)
        {
            OnModeratePriceChange?.Invoke(this, new PriceChangeAlertEventArgs
            {
                Material = material,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                PercentageChange = percentageChange,
                AlertLevel = PriceChangeAlertLevel.Review
            });
        }
        
        public List<MaterialPriceHistory> GetPriceHistory(int materialId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return _databaseService.GetMaterialPriceHistory(materialId, startDate, endDate);
        }
        
        public decimal GetAveragePrice(int materialId, int days)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var history = GetPriceHistory(materialId, startDate);
            
            if (history.Any())
            {
                return history.Average(h => h.Price);
            }
            
            // If no history, return current price
            var material = _databaseService.GetMaterialById(materialId);
            return material?.CurrentPrice ?? 0;
        }
        
        public List<Material> GetMaterialsWithSignificantPriceChanges(decimal threshold = 5.0m)
        {
            var materials = new List<Material>();
            var allMaterials = _databaseService.GetAllMaterials();
            
            foreach (var material in allMaterials)
            {
                var history = GetPriceHistory(material.MaterialId, DateTime.Now.AddDays(-30));
                if (history.Any(h => Math.Abs(h.PercentageChangeFromPrevious) >= threshold))
                {
                    materials.Add(material);
                }
            }
            
            return materials;
        }
        
        #endregion
        
        #region Assembly Pricing
        
        public decimal CalculateAssemblyMaterialCost(int assemblyId)
        {
            var assembly = _databaseService.GetAssemblyById(assemblyId);
            if (assembly == null) return 0;
            
            return assembly.TotalMaterialCost;
        }
        
        public decimal CalculateAssemblyTotalCost(int assemblyId, decimal hourlyRate, 
            ServiceType serviceType = null, decimal materialMarkup = 22.0m)
        {
            var assembly = _databaseService.GetAssemblyById(assemblyId);
            if (assembly == null) return 0;
            
            return assembly.CalculateTotalCost(hourlyRate, serviceType, materialMarkup);
        }
        
        public List<AssemblyTemplate> GetAssemblyVariants(string assemblyCode)
        {
            return _databaseService.GetAssemblyVariants(assemblyCode);
        }
        
        #endregion
        
        #region Price Predictions and Analysis
        
        public PriceTrend AnalyzePriceTrend(int materialId, int days = 90)
        {
            var history = GetPriceHistory(materialId, DateTime.Now.AddDays(-days));
            if (!history.Any()) return PriceTrend.Stable;
            
            var prices = history.OrderBy(h => h.EffectiveDate).Select(h => h.Price).ToList();
            if (prices.Count < 2) return PriceTrend.Stable;
            
            // Simple linear regression to determine trend
            var n = prices.Count;
            var sumX = Enumerable.Range(0, n).Sum();
            var sumY = prices.Sum();
            var sumXY = prices.Select((p, i) => i * p).Sum();
            var sumX2 = Enumerable.Range(0, n).Select(i => i * i).Sum();
            
            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            
            // Determine trend based on slope
            var avgPrice = prices.Average();
            var trendPercentage = (slope / avgPrice) * 100;
            
            if (trendPercentage > 1) return PriceTrend.Increasing;
            if (trendPercentage < -1) return PriceTrend.Decreasing;
            return PriceTrend.Stable;
        }
        
        public BulkPurchaseRecommendation GetBulkPurchaseRecommendation(int materialId)
        {
            var material = _databaseService.GetMaterialById(materialId);
            if (material == null) return null;
            
            var trend = AnalyzePriceTrend(materialId);
            var avgPrice30Day = GetAveragePrice(materialId, 30);
            var avgPrice90Day = GetAveragePrice(materialId, 90);
            
            var recommendation = new BulkPurchaseRecommendation
            {
                Material = material,
                CurrentPrice = material.CurrentPrice,
                Average30DayPrice = avgPrice30Day,
                Average90DayPrice = avgPrice90Day,
                PriceTrend = trend
            };
            
            // Recommendation logic
            if (trend == PriceTrend.Increasing && material.CurrentPrice < avgPrice30Day)
            {
                recommendation.Recommendation = "Buy Now - Price is below 30-day average and trending up";
                recommendation.RecommendedAction = PurchaseAction.BuyNow;
            }
            else if (trend == PriceTrend.Decreasing)
            {
                recommendation.Recommendation = "Wait - Price is trending down";
                recommendation.RecommendedAction = PurchaseAction.Wait;
            }
            else
            {
                recommendation.Recommendation = "Buy as needed - Price is stable";
                recommendation.RecommendedAction = PurchaseAction.Normal;
            }
            
            return recommendation;
        }
        
        #endregion
        
        #region Events
        
        public event EventHandler<PriceChangeAlertEventArgs> OnMajorPriceChange;
        public event EventHandler<PriceChangeAlertEventArgs> OnModeratePriceChange;
        
        #endregion
    }
    
    #region Supporting Classes
    
    public class PriceChangeAlertEventArgs : EventArgs
    {
        public Material Material { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal PercentageChange { get; set; }
        public PriceChangeAlertLevel AlertLevel { get; set; }
    }
    
    public enum PriceTrend
    {
        Increasing,
        Decreasing,
        Stable
    }
    
    public class BulkPurchaseRecommendation
    {
        public Material Material { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Average30DayPrice { get; set; }
        public decimal Average90DayPrice { get; set; }
        public PriceTrend PriceTrend { get; set; }
        public string Recommendation { get; set; }
        public PurchaseAction RecommendedAction { get; set; }
    }
    
    public enum PurchaseAction
    {
        BuyNow,
        Wait,
        Normal
    }
    
    #endregion
}
