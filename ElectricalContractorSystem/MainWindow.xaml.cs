using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Views;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        
        public MainWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
        }
        
        #region Estimate Menu Items
        
        private void NewEstimate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement estimate functionality
            MessageBox.Show("Estimate functionality will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void OpenEstimate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement estimate functionality
            MessageBox.Show("Estimate functionality will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void ManageEstimates_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement estimate functionality
            MessageBox.Show("Estimate functionality will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void ConvertToJob_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement estimate functionality
            MessageBox.Show("Estimate functionality will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void RoomTemplates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Room Templates management will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Job Menu Items
        
        private void JobManagement_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new JobManagementViewModel(_databaseService);
            var view = new JobManagementView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Job Management";
        }
        
        private void WeeklyLaborEntry_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new WeeklyLaborEntryViewModel(_databaseService);
            var view = new WeeklyLaborEntryView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Weekly Labor Entry";
        }
        
        private void MaterialEntry_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new MaterialEntryViewModel(_databaseService);
            var view = new MaterialEntryView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Material Entry";
        }
        
        private void JobCostTracking_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new JobCostTrackingViewModel(_databaseService);
            var view = new JobCostTrackingView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Job Cost Tracking";
        }
        
        private void ImportJobs_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportJobsWindow();
            var viewModel = new ImportJobsViewModel(_databaseService, importWindow);
            importWindow.DataContext = viewModel;
            importWindow.ShowDialog();
        }
        
        #endregion
        
        #region Customer Menu Items
        
        private void ManageCustomers_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Customer Management interface coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement AddCustomerDialog
            MessageBox.Show("Add Customer dialog will be implemented in a future update.", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            
            /* Commented out until AddCustomerDialog is implemented
            var dialog = new AddCustomerDialog();
            if (dialog.ShowDialog() == true)
            {
                var customer = dialog.Customer;
                _databaseService.SaveCustomer(customer);
                MessageBox.Show($"Customer '{customer.Name}' added successfully!", 
                    "Customer Added", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            */
        }
        
        #endregion
        
        #region Price List Menu Items
        
        private void ManagePriceList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Price List Management interface coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void ImportPriceList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Use the migration script: python migration/import_price_list_from_excel.py", 
                "Import Price List", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void ExportPriceList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export Price List feature coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Reports Menu Items
        
        private void JobProfitability_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Job Profitability Report coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void EmployeeHours_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Employee Hours Report coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void MaterialUsage_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Material Usage Report coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void CustomReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Custom Report Builder coming soon!", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Help Menu Items
        
        private void UserGuide_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("See docs/Estimating_System_User_Guide.md for user guide.", 
                "User Guide", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void KeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Keyboard Shortcuts:\n\n" +
                "Ctrl+N - New Estimate\n" +
                "Ctrl+O - Open Estimate\n" +
                "Ctrl+S - Save\n" +
                "F1 - Help\n" +
                "Alt+F4 - Exit", 
                "Keyboard Shortcuts", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Electrical Contractor Management System\n" +
                "Version 1.0\n\n" +
                "Developed for efficient estimate creation and job management.\n\n" +
                "© 2025 Erik Rusek Electric", 
                "About", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Other Menu Items
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        #endregion
        
        #region Helper Methods
        
        private void ShowJobDetails(Job job)
        {
            var viewModel = new JobDetailsViewModel(_databaseService);
            viewModel.CurrentJob = job;
            
            var view = new JobDetailsView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = $"Electrical Contractor Management System - Job #{job.JobNumber}";
        }
        
        #endregion
    }
}
