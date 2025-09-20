namespace FiendFriend.Configuration
{
    public class CommunicationSettings
    {
        public NamedPipeSettings NamedPipe { get; set; } = new();
        public WebServerSettings WebServer { get; set; } = new();
    }

    public class NamedPipeSettings
    {
        public bool Enabled { get; set; } = true;
        public string PipeName { get; set; } = "FiendFriend_IPC";
    }

    public class WebServerSettings
    {
        public bool Enabled { get; set; } = false;
        public int Port { get; set; } = 8080;
        public string Host { get; set; } = "localhost";
    }
}
