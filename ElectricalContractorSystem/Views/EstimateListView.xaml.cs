using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem.Views
{
    public partial class EstimateListView : UserControl
    {
        public EstimateListView()
        {
            InitializeComponent();
            
            // Wire up events when ViewModel is set
            DataContextChanged += OnDataContextChanged;
        }
        
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is EstimateListViewModel oldViewModel)
            {
                // Unsubscribe from old events
                oldViewModel.EstimateCreated -= OnEstimateCreated;
                oldViewModel.EstimateEditRequested -= OnEstimateEditRequested;
            }
            
            if (e.NewValue is EstimateListViewModel newViewModel)
            {
                // Subscribe to new events
                newViewModel.EstimateCreated += OnEstimateCreated;
                newViewModel.EstimateEditRequested += OnEstimateEditRequested;
            }
        }
        
        private void OnEstimateCreated(EstimateBuilderViewModel estimateBuilderViewModel)
        {
            // Navigate to estimate builder
            NavigateToEstimateBuilder(estimateBuilderViewModel);
        }
        
        private void OnEstimateEditRequested(EstimateBuilderViewModel estimateBuilderViewModel)
        {
            // Navigate to estimate builder
            NavigateToEstimateBuilder(estimateBuilderViewModel);
        }
        
        private void NavigateToEstimateBuilder(EstimateBuilderViewModel viewModel)
        {
            // Find the MainWindow's content area
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                var view = new EstimateBuilderView
                {
                    DataContext = viewModel
                };
                
                // Navigate by setting the main content
                var contentControl = mainWindow.FindName("MainContent") as ContentControl;
                if (contentControl != null)
                {
                    contentControl.Content = view;
                    mainWindow.Title = $"Electrical Contractor Management System - {viewModel.EstimateTitle}";
                }
            }
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
