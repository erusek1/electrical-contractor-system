using System;
using System.Windows;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DatabaseService _databaseService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize database service
                _databaseService = new DatabaseService();
                
                // Test database connection
                bool connected = _databaseService.TestConnection();
                if (!connected)
                {
                    MessageBox.Show("Could not connect to the database. Please check your connection settings and try again.",
                        "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Allow the application to continue, but functionality will be limited
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while initializing the application: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources
            _databaseService?.Dispose();
            
            base.OnExit(e);
        }
    }
}
