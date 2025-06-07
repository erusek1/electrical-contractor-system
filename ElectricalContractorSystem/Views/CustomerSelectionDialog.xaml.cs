using System.Windows;
using System.Windows.Input;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.ViewModels;

namespace ElectricalContractorSystem.Views
{
    public partial class CustomerSelectionDialog : Window
    {
        public Customer SelectedCustomer => (DataContext as CustomerSelectionViewModel)?.SelectedCustomer;
        
        public CustomerSelectionDialog()
        {
            InitializeComponent();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCustomer != null)
            {
                DialogResult = true;
                Close();
            }
        }
        
        // Handle the select command result
        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            if (DataContext is CustomerSelectionViewModel viewModel)
            {
                viewModel.CloseRequested += (sender, result) =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        }
    }
}
