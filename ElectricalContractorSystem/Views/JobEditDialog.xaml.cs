using System.Windows;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for JobEditDialog.xaml
    /// </summary>
    public partial class JobEditDialog : Window
    {
        public JobEditDialogViewModel ViewModel { get; private set; }

        public JobEditDialog(int jobId)
        {
            InitializeComponent();
            ViewModel = new JobEditDialogViewModel(jobId);
            DataContext = ViewModel;
            
            // Subscribe to close events
            ViewModel.RequestClose += OnRequestClose;
        }

        public JobEditDialog() : this(0)
        {
            // For new job creation
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