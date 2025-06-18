using System.Windows.Controls;

namespace ElectricalContractorSystem.Views
{
    public partial class MaterialPriceTrackingView : UserControl
    {
        public MaterialPriceTrackingView()
        {
            InitializeComponent();
            
            // Set up test data if no DataContext is provided
            if (DataContext == null)
            {
                this.Loaded += MaterialPriceTrackingView_Loaded;
            }
        }

        private void MaterialPriceTrackingView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // If DataContext is still null after loading, show a helpful message
            if (DataContext == null)
            {
                System.Windows.MessageBox.Show(
                    "MaterialPriceTrackingView loaded without DataContext. " +
                    "This view should be opened through the main menu to work properly.",
                    "Configuration Issue",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
