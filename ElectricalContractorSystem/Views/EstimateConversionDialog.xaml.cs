using System.Windows;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    public partial class EstimateConversionDialog : Window
    {
        public EstimateConversionDialog()
        {
            InitializeComponent();
        }
        
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            if (DataContext is EstimateConversionViewModel viewModel)
            {
                viewModel.ConversionCompleted += (sender, result) =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        }
    }
}
