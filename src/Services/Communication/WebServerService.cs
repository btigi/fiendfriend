using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FiendFriend.Models;
using FiendFriend.Services.Interfaces;

namespace FiendFriend.Services.Communication
{
    public class WebServerService : IMessageChannel
    {
        private readonly string _host;
        private readonly int _port;
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        
        public string ChannelName => "WebServer";
        public bool IsActive { get; private set; }
        
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        
        public WebServerService(string host, int port)
        {
            _host = host;
            _port = port;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsActive) return;
            
            _listener = new HttpListener();
            
            var prefix = _host == "*" || _host == "+" 
                ? $"http://+:{_port}/" 
                : $"http://{_host}:{_port}/";
            
            _listener.Prefixes.Add(prefix);
            
            try
            {
                _listener.Start();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _listenerTask = Task.Run(async () => await ListenForRequestsAsync(_cancellationTokenSource.Token));
                IsActive = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start web server on {prefix}: {ex.Message}", ex);
            }
            
            await Task.CompletedTask;
        }
        
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive) return;
            
            IsActive = false;
            _cancellationTokenSource?.Cancel();
            
            _listener?.Stop();
            _listener?.Close();
            
            if (_listenerTask != null)
            {
                await _listenerTask;
            }
            
            _cancellationTokenSource?.Dispose();
        }
        
        private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(async () => await HandleRequestAsync(context), cancellationToken);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebServer error: {ex.Message}");
                }
            }
        }
        
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                ImageChangeRequest? imageRequest = null;
                
                if (request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var requestBody = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrWhiteSpace(requestBody))
                    {
                        imageRequest = JsonSerializer.Deserialize<ImageChangeRequest>(requestBody);
                    }
                }
                else if (request.HttpMethod == "GET")
                {
                    var path = request.Url?.AbsolutePath?.TrimStart('/');
                    
                    imageRequest = path switch
                    {
                        "random" => new ImageChangeRequest { Command = "random", Random = true },
                        "status" => new ImageChangeRequest { Command = "status" },
                        "list" => new ImageChangeRequest { Command = "list" },
                        _ => null
                    };
                }
                
                if (imageRequest == null)
                {
                    await SendErrorResponseAsync(response, 400, "Invalid request");
                    return;
                }
                
                var responseCallback = new Func<object, Task>(async (responseData) =>
                {
                    try
                    {
                        var responseJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        
                        var buffer = Encoding.UTF8.GetBytes(responseJson);
                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        response.StatusCode = 200;
                        
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WebServer response error: {ex.Message}");
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
                Console.WriteLine($"WebServer request handling error: {ex.Message}");
                await SendErrorResponseAsync(response, 500, "Internal server error");
            }
        }
        
        private async Task SendErrorResponseAsync(HttpListenerResponse response, int statusCode, string message)
        {
            try
            {
                var errorResponse = new { error = message, statusCode };
                var errorJson = JsonSerializer.Serialize(errorResponse);
                var buffer = Encoding.UTF8.GetBytes(errorJson);
                
                response.StatusCode = statusCode;
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending error response: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            try
            {
                IsActive = false;
                _cancellationTokenSource?.Cancel();
                
                if (_listener != null)
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                    _listener = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing WebServerService: {ex.Message}");
            }
            finally
            {
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing WebServerService cancellation token: {ex.Message}");
                }
                
                GC.SuppressFinalize(this);
            }
        }
    }
}
