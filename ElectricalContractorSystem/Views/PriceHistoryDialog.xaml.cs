using System.Collections.ObjectModel;
using System.Windows;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class PriceHistoryDialog : Window
    {
        public PriceHistoryDialog(Material material, ObservableCollection<MaterialPriceHistory> priceHistory)
        {
            InitializeComponent();
            DataContext = this;
            
            Material = material;
            PriceHistory = priceHistory;
        }
        
        public Material Material { get; }
        public ObservableCollection<MaterialPriceHistory> PriceHistory { get; }
    }
}
