using System.Windows;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    public partial class AssemblyEditDialog : Window
    {
        public AssemblyEditDialog()
        {
            InitializeComponent();
            
            // Handle ViewModel close request
            if (DataContext is AssemblyEditViewModel viewModel)
            {
                viewModel.RequestClose += (s, e) =>
                {
                    DialogResult = viewModel.DialogResult;
                    Close();
                };
            }
        }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Subscribe to RequestClose event when DataContext is set
            if (DataContext is AssemblyEditViewModel viewModel)
            {
                viewModel.RequestClose += (s, args) =>
                {
                    DialogResult = viewModel.DialogResult;
                    Close();
                };
            }
        }
    }
}
