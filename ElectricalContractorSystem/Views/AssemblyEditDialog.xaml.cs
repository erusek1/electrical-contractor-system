using System.Windows;
using System.Windows.Input;

namespace ElectricalContractorSystem.Views
{
    public partial class AssemblyEditDialog : Window
    {
        public AssemblyEditDialog()
        {
            InitializeComponent();
        }

        private void MaterialsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click adds the material to components
            if (DataContext is ViewModels.AssemblyEditViewModel viewModel)
            {
                viewModel.AddSelectedMaterialCommand.Execute(null);
            }
        }
    }
}