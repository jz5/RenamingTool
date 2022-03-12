using System.Windows;

namespace RenamingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR LICENSE KEY");
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var window = new MainWindow(e.Args);
            window.Show();
        }
    }
}
