using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FiendFriend.Models;
using FiendFriend.Services.Interfaces;

namespace FiendFriend.Services.Communication
{
    public class NamedPipeService : IMessageChannel
    {
        private readonly string _pipeName;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        
        public string ChannelName => "NamedPipe";
        public bool IsActive { get; private set; }
        
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        
        public NamedPipeService(string pipeName)
        {
            _pipeName = pipeName;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsActive) return;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listenerTask = Task.Run(async () => await ListenForConnectionsAsync(_cancellationTokenSource.Token));
            IsActive = true;
            
            await Task.CompletedTask;
        }
        
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive) return;
            
            IsActive = false;
            _cancellationTokenSource?.Cancel();
            
            _pipeServer?.Close();
            _pipeServer?.Dispose();
            
            if (_listenerTask != null)
            {
                await _listenerTask;
            }
            
            _cancellationTokenSource?.Dispose();
        }
        
        private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);
                    
                    _ = Task.Run(async () => await HandleClientAsync(_pipeServer, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"NamedPipe error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        
        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(pipe, Encoding.UTF8);
                using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
                
                var request = await reader.ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(request))
                    return;
                
                var imageRequest = JsonSerializer.Deserialize<ImageChangeRequest>(request);
                if (imageRequest == null) return;
                
                var responseCallback = new Func<object, Task>(async (response) =>
                {
                    try
                    {
                        var responseJson = JsonSerializer.Serialize(response);
                        await writer.WriteAsync(responseJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"NamedPipe response error: {ex.Message}");
                    }
                });
                
                var eventArgs = new MessageReceivedEventArgs
                {
                    Command = imageRequest.Command,
                    Data = imageRequest,
                    ResponseCallback = responseCallback
                };
                
                MessageReceived?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NamedPipe client handling error: {ex.Message}");
            }
            finally
            {
                pipe.Close();
            }
        }
        
        public void Dispose()
        {
            try
            {
                IsActive = false;
                _cancellationTokenSource?.Cancel();
                
                if (_pipeServer != null)
                {
                    _pipeServer.Close();
                    _pipeServer.Dispose();
                    _pipeServer = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing NamedPipeService: {ex.Message}");
            }
            finally
            {
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing NamedPipeService cancellation token: {ex.Message}");
                }
                
                GC.SuppressFinalize(this);
            }
        }
    }
}
