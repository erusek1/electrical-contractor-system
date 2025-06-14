using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class PriceTrendsDialog : Window
    {
        private readonly PricingService _pricingService;
        
        public PriceTrendsDialog(Material material, PricingService pricingService)
        {
            InitializeComponent();
            DataContext = this;
            
            Material = material;
            _pricingService = pricingService;
            
            LoadTrendData();
        }
        
        public Material Material { get; }
        public decimal CurrentPrice => Material.CurrentPrice;
        public decimal? Average30Day { get; private set; }
        public decimal? Average90Day { get; private set; }
        public string Trend { get; private set; }
        public string BulkPurchaseRecommendation { get; private set; }
        
        private void LoadTrendData()
        {
            // Calculate averages
            Average30Day = _pricingService.GetAveragePrice(Material.MaterialId, 30);
            Average90Day = _pricingService.GetAveragePrice(Material.MaterialId, 90);
            
            // Get trend
            var trend = _pricingService.AnalyzePriceTrend(Material.MaterialId);
            Trend = trend.ToString();
            
            // Get bulk purchase recommendation
            var recommendation = _pricingService.GetBulkPurchaseRecommendation(Material.MaterialId);
            BulkPurchaseRecommendation = recommendation?.Recommendation ?? "No recommendation available at this time.";
            
            // Update bindings
            DataContext = null;
            DataContext = this;
        }
    }
}
