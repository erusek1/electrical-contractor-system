using System;
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
        private DatabaseService _databaseService;
        private bool _isDatabaseConnected = false;
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            InitializeHomeScreenButtons();
        }
        
        private void InitializeDatabaseConnection()
        {
            try
            {
                _databaseService = new DatabaseService();
                _isDatabaseConnected = _databaseService.TestConnection();
                
                if (!_isDatabaseConnected)
                {
                    ShowDatabaseError("Unable to connect to the database. Some features will be unavailable.");
                }
            }
            catch (Exception ex)
            {
                _isDatabaseConnected = false;
                ShowDatabaseError($"Database initialization failed: {ex.Message}");
            }
        }
        
        private void InitializeHomeScreenButtons()
        {
            // Wire up the home screen buttons
            var newEstimateBtn = this.FindName("NewEstimateButton") as System.Windows.Controls.Button;
            if (newEstimateBtn != null)
            {
                newEstimateBtn.Click += NewEstimate_Click;
            }
            
            var manageEstimatesBtn = this.FindName("ManageEstimatesButton") as System.Windows.Controls.Button;
            if (manageEstimatesBtn != null)
            {
                manageEstimatesBtn.Click += ManageEstimates_Click;
            }
            
            var jobManagementBtn = this.FindName("JobManagementButton") as System.Windows.Controls.Button;
            if (jobManagementBtn != null)
            {
                jobManagementBtn.Click += JobManagement_Click;
            }
            
            var weeklyLaborBtn = this.FindName("WeeklyLaborEntryButton") as System.Windows.Controls.Button;
            if (weeklyLaborBtn != null)
            {
                weeklyLaborBtn.Click += WeeklyLaborEntry_Click;
            }
        }
        
        private void ShowDatabaseError(string message)
        {
            MessageBox.Show(
                $"{message}\n\nTo fix database issues:\n" +
                "1. Ensure MySQL is installed and running\n" +
                "2. Run the database script in /database/electrical_contractor_db.sql\n" +
                "3. Update App.config with your MySQL credentials\n\n" +
                "You can still explore the interface, but data won't be saved.",
                "Database Connection Issue",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        
        private bool CheckDatabaseConnection()
        {
            if (!_isDatabaseConnected)
            {
                var result = MessageBox.Show(
                    "This feature requires a database connection. Would you like to retry connecting?",
                    "Database Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    InitializeDatabaseConnection();
                }
            }
            return _isDatabaseConnected;
        }
        
        #region Estimate Menu Items
        
        private void NewEstimate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement estimate functionality
            MessageBox.Show("Estimate functionality will be implemented in a future update.\n\n" +
                "This will allow you to:\n" +
                "• Create new estimates with room-by-room specifications\n" +
                "• Use quick codes like 'hh' for items\n" +
                "• Calculate labor and material costs automatically\n" +
                "• Convert approved estimates to jobs", 
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
            if (!CheckDatabaseConnection())
            {
                // Show a demo view even without database
                MessageBox.Show("Job Management requires database connection.\n\n" +
                    "Features include:\n" +
                    "• Track all jobs with status\n" +
                    "• Filter by active/completed\n" +
                    "• Quick access to job details\n" +
                    "• Monitor job profitability",
                    "Database Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Weekly Labor Entry requires database connection.\n\n" +
                    "Features include:\n" +
                    "• Enter hours by employee, job, and stage\n" +
                    "• Weekly view with 40-hour validation\n" +
                    "• Quick entry grid interface\n" +
                    "• Automatic hour calculations",
                    "Database Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Material Entry requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Job Cost Tracking requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Import Jobs requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Add Customer requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
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
            MessageBox.Show("Price List Management interface coming soon!\n\n" +
                "This will allow you to:\n" +
                "• Add and edit items with codes\n" +
                "• Set labor minutes per item\n" +
                "• Configure material costs and markup\n" +
                "• Import/export from Excel", 
                "Coming Soon", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        private void ImportPriceList_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("To import your price list:\n\n" +
                "1. Ensure MySQL database is set up\n" +
                "2. Place your Excel price list in the migration folder\n" +
                "3. Run: python migration/import_price_list_from_excel.py\n\n" +
                "The script will import items with codes, prices, and labor minutes.", 
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
            MessageBox.Show("User Guide\n\n" +
                "Quick Start:\n" +
                "1. Set up MySQL database using the script in /database\n" +
                "2. Import your existing data using migration scripts\n" +
                "3. Use Job Management to track projects\n" +
                "4. Enter labor hours weekly for accurate tracking\n" +
                "5. Track materials by job and stage\n\n" +
                "For detailed instructions, see /docs folder", 
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
                "Features:\n" +
                "• Job tracking and management\n" +
                "• Weekly labor entry with validation\n" +
                "• Material tracking by job and stage\n" +
                "• Cost analysis and profitability tracking\n\n" +
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
            if (!CheckDatabaseConnection()) return;
            
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
