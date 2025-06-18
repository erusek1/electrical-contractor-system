using System.Windows;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    public partial class EstimateConversionDialog : Window
    {
        public Job ConvertedJob { get; private set; }
        public bool DialogResult { get; private set; }

        public EstimateConversionDialog(Estimate estimate, DatabaseService databaseService)
        {
            InitializeComponent();
            var viewModel = new EstimateConversionViewModel(estimate, databaseService);
            viewModel.JobCreated += OnJobCreated;
            viewModel.DialogCancelled += OnDialogCancelled;
            DataContext = viewModel;
        }

        private void OnJobCreated(Job job)
        {
            ConvertedJob = job;
            DialogResult = true;
            Close();
        }

        private void OnDialogCancelled()
        {
            DialogResult = false;
            Close();
        }
    }
}