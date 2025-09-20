using System.Windows;

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
        }
    }
}
