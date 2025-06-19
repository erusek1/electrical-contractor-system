using System.Windows;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Dialog for converting estimates to jobs
    /// FIXED: Constructor signature and property access issues
    /// </summary>
    public partial class EstimateConversionDialog : Window
    {
        public Job CreatedJob { get; private set; }
        public new bool DialogResult { get; private set; }

        public EstimateConversionDialog(DatabaseService databaseService, Estimate estimate)
        {
            InitializeComponent();
            var viewModel = new EstimateConversionViewModel(estimate, databaseService);
            viewModel.JobCreated += OnJobCreated;
            viewModel.DialogCancelled += OnDialogCancelled;
            DataContext = viewModel;
        }

        private void OnJobCreated(Job job)
        {
            CreatedJob = job;
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