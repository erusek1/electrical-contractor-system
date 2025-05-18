using System;
using System.Windows;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // The ViewModel is set in XAML, but we can also access it here
            var viewModel = (MainViewModel)DataContext;
            
            // Set the title based on company name (could be loaded from config)
            Title = "Erik Rusek Electric - Contractor System";
        }
    }
}
