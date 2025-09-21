using System.Windows;
using System.Linq;

namespace FiendFriend
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Ensure only one instance can run
            var mutex = new System.Threading.Mutex(true, "FiendFriend", out bool createdNew);
            if (!createdNew)
            {
                Current.Shutdown();
                return;
            }

            string? configFile = ParseConfigFileArgument(e.Args);
            
            var mainWindow = new MainWindow(configFile);
            mainWindow.Show();
        }

        private string? ParseConfigFileArgument(string[] args)
        {
            return args.FirstOrDefault(arg => arg.StartsWith('-') && arg.EndsWith(".json"))?.Substring(1);
        }
    }
}
