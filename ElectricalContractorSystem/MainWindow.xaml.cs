using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Windows;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Test database connection on startup
            Services.DatabaseConnectionTest.TestConnection();
            
            // Set up main view model
            this.DataContext = new MainViewModel();
        }

        // Menu event handlers for testing
        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            Services.DatabaseConnectionTest.TestConnection();
        }
        
        private void CreateSampleData_Click(object sender, RoutedEventArgs e)
        {
            Services.DatabaseConnectionTest.CreateSampleData();
        }
        
        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Electrical Contractor System\n" +
                "Version 1.0\n\n" +
                "Erik Rusek Electric\n" +
                "Database-driven business management system\n\n" +
                "Replacing Excel with modern technology!",
                "About ERE System",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
