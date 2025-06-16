using System.Windows;
using ElectricalContractorSystem.Models;
using ElectricalContractorSystem.Services;

namespace ElectricalContractorSystem.Views
{
    /// <summary>
    /// Interaction logic for EditPropertyDialog.xaml
    /// </summary>
    public partial class EditPropertyDialog : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly Property _property;

        public EditPropertyDialog(DatabaseService databaseService, Property property)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _property = property;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
