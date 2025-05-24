using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Windows;

namespace ElectricalContractorSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"].ConnectionString;
                
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Simple test query
                    using (var command = new MySqlCommand("SELECT VERSION()", connection))
                    {
                        var version = command.ExecuteScalar();
                        MessageBox.Show($"Database connection successful!\nMySQL Version: {version}", 
                                      "Connection Test", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed:\n{ex.Message}", 
                              "Connection Test", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
    }
}