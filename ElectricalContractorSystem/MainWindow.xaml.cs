using System;
using System.Linq;
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("New Estimate requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            // Show customer selection dialog
            var viewModel = new CustomerSelectionViewModel(_databaseService);
            var dialog = new CustomerSelectionDialog
            {
                DataContext = viewModel
            };
            
            if (dialog.ShowDialog() == true && dialog.SelectedCustomer != null)
            {
                ShowEstimateBuilder(dialog.SelectedCustomer);
            }
        }
        
        private void OpenEstimate_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Open Estimate requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            // Show estimate selection dialog
            ShowEstimateList();
        }
        
        private void ManageEstimates_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Manage Estimates requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            ShowEstimateList();
        }
        
        private void ConvertToJob_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Convert to Job requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            // Show estimate selection for conversion
            MessageBox.Show("Select an estimate from the Manage Estimates screen to convert it to a job.", 
                "Convert to Job", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            ShowEstimateList();
        }
        
        private void RoomTemplates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Room Templates allow you to create standard room configurations that can be reused.\n\n" +
                "Coming in a future update:\n" +
                "• Create templates for common room types\n" +
                "• Save standard item configurations\n" +
                "• Quick-add rooms from templates", 
                "Room Templates", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        #endregion
        
        #region Job Menu Items
        
        private void JobManagement_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckDatabaseConnection())
            {
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
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Manage Customers requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            // For now, show a simple list of customers
            var customers = _databaseService.GetAllCustomers();
            var customerList = "Current Customers:\n\n";
            foreach (var customer in customers)
            {
                customerList += $"• {customer.Name}\n";
            }
            
            if (customers.Count == 0)
            {
                customerList = "No customers in database yet.\n\nUse 'Add New Customer' to add customers.";
            }
            
            MessageBox.Show(customerList, "Customer List", MessageBoxButton.OK, MessageBoxImage.Information);
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
        }
        
        #endregion
        
        #region Price List Menu Items
        
        private void ManagePriceList_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckDatabaseConnection())
            {
                MessageBox.Show("Manage Price List requires database connection.", 
                    "Database Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            // Show price list items for now
            var items = _databaseService.GetActivePriceListItems();
            var itemList = "Current Price List Items:\n\n";
            
            if (items.Count > 0)
            {
                itemList += "Code | Description | Price\n";
                itemList += "--------------------------------\n";
                foreach (var item in items.Take(20)) // Show first 20 items
                {
                    itemList += $"{item.ItemCode,-6} | {item.Name,-30} | ${item.SellPrice:F2}\n";
                }
                if (items.Count > 20)
                {
                    itemList += $"\n... and {items.Count - 20} more items";
                }
            }
            else
            {
                itemList = "No price list items in database yet.\n\n" +
                    "Import your price list using:\n" +
                    "python migration/import_price_list_from_excel.py";
            }
            
            MessageBox.Show(itemList, "Price List", MessageBoxButton.OK, MessageBoxImage.Information);
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
                "Estimates:\n" +
                "• Use quick codes: 'hh' for high hat, 'O' for outlet, 'S' for switch\n" +
                "• Add rooms and drag items from price list\n" +
                "• Labor and materials calculate automatically\n\n" +
                "Jobs:\n" +
                "• Track projects from estimate to completion\n" +
                "• Enter weekly labor hours with validation\n" +
                "• Track materials by job and stage\n\n" +
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
                "• Estimate builder with room-by-room specifications\n" +
                "• Quick item codes (hh, O, S, 3W, etc.)\n" +
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
        
        private void ShowEstimateBuilder(Customer customer = null)
        {
            var viewModel = new EstimateBuilderViewModel(_databaseService);
            var view = new EstimateBuilderView
            {
                DataContext = viewModel
            };
            
            // Create new estimate with customer
            if (customer != null)
            {
                viewModel.CreateNewEstimate(customer);
            }
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Estimate Builder";
        }
        
        private void ShowEstimateList()
        {
            var viewModel = new EstimateListViewModel(_databaseService);
            var view = new EstimateListView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
            this.Title = "Electrical Contractor Management System - Manage Estimates";
        }
        
        #endregion
    }
}
