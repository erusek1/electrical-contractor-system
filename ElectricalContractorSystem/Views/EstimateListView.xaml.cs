using System.Windows.Controls;
using System.Windows.Input;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    public partial class EstimateListView : UserControl
    {
        public EstimateListView()
        {
            InitializeComponent();
        }
        
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && DataContext is EstimateListViewModel viewModel)
            {
                viewModel.EditEstimateCommand.Execute(row.DataContext);
            }
        }
    }
}
