using System.Windows;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for BulkStatusUpdateDialog.xaml
    /// </summary>
    public partial class BulkStatusUpdateDialog : Window
    {
        public BulkStatusUpdateDialogViewModel ViewModel { get; private set; }

        public BulkStatusUpdateDialog()
        {
            InitializeComponent();
            ViewModel = new BulkStatusUpdateDialogViewModel();
            DataContext = ViewModel;
            
            // Subscribe to close events
            ViewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose(object sender, bool? dialogResult)
        {
            DialogResult = dialogResult;
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (ViewModel != null)
            {
                ViewModel.RequestClose -= OnRequestClose;
            }
        }
    }
}