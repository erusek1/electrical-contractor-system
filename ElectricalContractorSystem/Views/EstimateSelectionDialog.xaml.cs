using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    public partial class EstimateSelectionDialog : Window
    {
        public Estimate SelectedEstimate => (DataContext as EstimateSelectionViewModel)?.SelectedEstimate;
        
        public EstimateSelectionDialog()
        {
            InitializeComponent();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as EstimateSelectionViewModel;
            if (viewModel?.SelectedEstimate != null)
            {
                // For conversion to job, only allow approved estimates
                if (!viewModel.ShowOnlyApproved || viewModel.SelectedEstimate.Status == EstimateStatus.Approved)
                {
                    DialogResult = true;
                    Close();
                }
            }
        }
        
        // Handle the select command result
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            if (DataContext is EstimateSelectionViewModel viewModel)
            {
                viewModel.CloseRequested += (sender, result) =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        }
    }
}
