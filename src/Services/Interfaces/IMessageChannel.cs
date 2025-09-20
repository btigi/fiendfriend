using System;
using System.Threading;
using System.Threading.Tasks;

namespace FiendFriend.Services.Interfaces
{
    public interface IMessageChannel : IDisposable
    {
        string ChannelName { get; }
        bool IsActive { get; }
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string Command { get; set; } = string.Empty;
        public object? Data { get; set; }
        public Func<object, Task>? ResponseCallback { get; set; }
    }
}
