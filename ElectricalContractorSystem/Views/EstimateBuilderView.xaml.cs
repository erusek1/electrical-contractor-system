using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for EstimateBuilderView.xaml
    /// </summary>
    public partial class EstimateBuilderView : UserControl
    {
        public EstimateBuilderView()
        {
            InitializeComponent();
        }
        
        private void QuickEntryCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Try to execute the quick add command when Enter is pressed
                var viewModel = DataContext as ViewModels.EstimateBuilderViewModel;
                if (viewModel?.QuickAddCommand?.CanExecute(null) == true)
                {
                    viewModel.QuickAddCommand.Execute(null);
                }
            }
        }
    }
}
