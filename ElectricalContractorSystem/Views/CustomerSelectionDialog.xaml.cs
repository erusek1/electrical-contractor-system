using System.Windows;
using ElectricalContractorSystem.ViewModels;
using ElectricalContractorSystem.Services;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Views
{
    public partial class CustomerSelectionDialog : Window
    {
        public Customer SelectedCustomer { get; private set; }
        public bool DialogResult { get; private set; }

        public CustomerSelectionDialog(DatabaseService databaseService)
        {
            InitializeComponent();
            var viewModel = new CustomerSelectionViewModel(databaseService);
            viewModel.CustomerSelected += OnCustomerSelected;
            viewModel.DialogCancelled += OnDialogCancelled;
            DataContext = viewModel;
        }

        private void OnCustomerSelected(Customer customer)
        {
            SelectedCustomer = customer;
            DialogResult = true;
            Close();
        }

        private void OnDialogCancelled()
        {
            DialogResult = false;
            Close();
        }
    }
}