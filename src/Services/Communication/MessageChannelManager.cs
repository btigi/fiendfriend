using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FiendFriend.Configuration;
using FiendFriend.Models;
using FiendFriend.Services.Interfaces;

namespace FiendFriend.Services.Communication
{
    public class MessageChannelManager : IDisposable
    {
        private readonly IImageController _imageController;
        private readonly List<IMessageChannel> _channels = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        public MessageChannelManager(IImageController imageController)
        {
            _imageController = imageController;
        }
        
        public async Task InitializeAsync(CommunicationSettings settings)
        {
            if (settings.NamedPipe.Enabled)
            {
                var namedPipeService = new NamedPipeService(settings.NamedPipe.PipeName);
                await AddChannelAsync(namedPipeService);
            }
            
            if (settings.WebServer.Enabled)
            {
                var webServerService = new WebServerService(settings.WebServer.Host, settings.WebServer.Port);
                await AddChannelAsync(webServerService);
            }
        }
        
        private async Task AddChannelAsync(IMessageChannel channel)
        {
            try
            {
                channel.MessageReceived += OnMessageReceived;
                await channel.StartAsync(_cancellationTokenSource.Token);
                _channels.Add(channel);
                
                Console.WriteLine($"Started communication channel: {channel.ChannelName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start channel {channel.ChannelName}: {ex.Message}");
                channel.Dispose();
            }
        }
        
        private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            try
            {
                var response = await ProcessMessageAsync(e.Command, e.Data);
                
                if (e.ResponseCallback != null)
                {
                    await e.ResponseCallback(response);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new ImageChangeResponse
                {
                    Success = false,
                    Message = ex.Message
                };
                
                if (e.ResponseCallback != null)
                {
                    await e.ResponseCallback(errorResponse);
                }
            }
        }
        
        private async Task<ImageChangeResponse> ProcessMessageAsync(string command, object? data)
        {
            var response = new ImageChangeResponse();
            
            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "random":
                        await _imageController.LoadRandomImagesAsync();
                        var (currentBase, currentFace) = await _imageController.GetCurrentImagesAsync();
                        response.Success = true;
                        response.Message = "Loaded random images";
                        response.CurrentBaseImage = currentBase;
                        response.CurrentFaceImage = currentFace;
                        break;
                        
                    case "setbase":
                        if (data is ImageChangeRequest request && !string.IsNullOrEmpty(request.BaseImage))
                        {
                            await _imageController.SetBaseImageAsync(request.BaseImage);
                            response.Success = true;
                            response.Message = $"Set base image to {request.BaseImage}";
                            response.CurrentBaseImage = request.BaseImage;
                        }
                        else
                        {
                            throw new ArgumentException("BaseImage parameter is required");
                        }
                        break;
                        
                    case "setface":
                        if (data is ImageChangeRequest faceRequest && !string.IsNullOrEmpty(faceRequest.FaceImage))
                        {
                            await _imageController.SetFaceImageAsync(faceRequest.FaceImage);
                            response.Success = true;
                            response.Message = $"Set face image to {faceRequest.FaceImage}";
                            response.CurrentFaceImage = faceRequest.FaceImage;
                        }
                        else
                        {
                            throw new ArgumentException("FaceImage parameter is required");
                        }
                        break;
                        
                    case "setboth":
                        if (data is ImageChangeRequest bothRequest && 
                            !string.IsNullOrEmpty(bothRequest.BaseImage) && 
                            !string.IsNullOrEmpty(bothRequest.FaceImage))
                        {
                            await _imageController.SetBothImagesAsync(bothRequest.BaseImage, bothRequest.FaceImage);
                            response.Success = true;
                            response.Message = $"Set images to {bothRequest.BaseImage} and {bothRequest.FaceImage}";
                            response.CurrentBaseImage = bothRequest.BaseImage;
                            response.CurrentFaceImage = bothRequest.FaceImage;
                        }
                        else
                        {
                            throw new ArgumentException("Both BaseImage and FaceImage parameters are required");
                        }
                        break;
                        
                    case "status":
                        var (statusBase, statusFace) = await _imageController.GetCurrentImagesAsync();
                        response.Success = true;
                        response.Message = "Current status";
                        response.CurrentBaseImage = statusBase;
                        response.CurrentFaceImage = statusFace;
                        break;
                        
                    case "list":
                        var baseImages = await _imageController.GetAvailableBaseImagesAsync();
                        var faceImages = await _imageController.GetAvailableFaceImagesAsync();
                        response.Success = true;
                        response.Message = "Available images";
                        response.AvailableBaseImages = baseImages;
                        response.AvailableFaceImages = faceImages;
                        break;
                        
                    default:
                        throw new ArgumentException($"Unknown command: {command}");
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            
            return response;
        }
        
        public IEnumerable<(string Name, bool IsActive)> GetChannelStatus()
        {
            return _channels.Select(c => (c.ChannelName, c.IsActive));
        }
        
        public async Task StopAllChannelsAsync()
        {
            _cancellationTokenSource.Cancel();
            
            var stopTasks = _channels.Select(channel => channel.StopAsync());
            await Task.WhenAll(stopTasks);
        }
        
        public void Dispose()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }
                
                foreach (var channel in _channels)
                {
                    try
                    {
                        channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing channel {channel.ChannelName}: {ex.Message}");
                    }
                }
                
                _channels.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during MessageChannelManager disposal: {ex.Message}");
            }
            finally
            {
                try
                {
                    _cancellationTokenSource.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing cancellation token source: {ex.Message}");
                }
                
                GC.SuppressFinalize(this);
            }
        }
    }
}
