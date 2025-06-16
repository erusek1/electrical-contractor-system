using System.Windows;

namespace ElectricalContractorSystem.Views
{
    public partial class AssemblyEditDialog : Window
    {
        public AssemblyEditDialog()
        {
            InitializeComponent();
            
            // Set up dialog result binding
            DataContextChanged += (s, e) =>
            {
                if (DataContext is ViewModels.AssemblyEditViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.DialogResult))
                        {
                            DialogResult = vm.DialogResult;
                            if (DialogResult.HasValue)
                            {
                                Close();
                            }
                        }
                    };
                }
            };
        }
    }
}
