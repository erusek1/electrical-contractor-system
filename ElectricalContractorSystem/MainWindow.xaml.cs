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
        
        private void NewEstimate_Click(object sender, RoutedEventArgs e)
        {
            // Create a new customer or select existing
            var customer = new Customer
            {
                Name = "Test Customer",
                Address = "123 Main St",
                City = "Point Pleasant",
                State = "NJ",
                Zip = "08742"
            };
            
            // Create the view model and view
            var viewModel = new EstimateBuilderViewModel(_databaseService);
            viewModel.CreateNewEstimate(customer);
            
            var view = new EstimateBuilderView
            {
                DataContext = viewModel
            };
            
            MainContent.Content = view;
        }
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}