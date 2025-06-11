using System.Windows;

namespace ElectricalContractorSystem
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Run diagnostics in debug mode or if requested
            #if DEBUG
            StartupTest.RunDiagnostics();
            #endif
        }
    }
}