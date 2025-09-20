using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using FiendFriend.Services.Interfaces;

namespace FiendFriend.Services.Core
{
    public class ImageService : IImageController
    {
        private readonly MainWindow _mainWindow;
        private readonly string _spritePath;
        private readonly Random _random = new();
        
        public ImageService(MainWindow mainWindow, string spritePath)
        {
            _mainWindow = mainWindow;
            _spritePath = spritePath;
        }
        
        public async Task LoadRandomImagesAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _mainWindow.LoadRandomImages();
            });
        }
        
        public async Task SetBaseImageAsync(string filename)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var basePath = Path.Combine(_spritePath, "bases", filename);
                if (File.Exists(basePath))
                {
                    _mainWindow.BaseImage.Source = new BitmapImage(new Uri(basePath));
                }
                else
                {
                    throw new FileNotFoundException($"Base image not found: {filename}");
                }
            });
        }
        
        public async Task SetFaceImageAsync(string filename)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var facePath = Path.Combine(_spritePath, "faces", filename);
                if (File.Exists(facePath))
                {
                    _mainWindow.FaceImage.Source = new BitmapImage(new Uri(facePath));
                }
                else
                {
                    throw new FileNotFoundException($"Face image not found: {filename}");
                }
            });
        }
        
        public async Task SetBothImagesAsync(string baseFilename, string faceFilename)
        {
            await SetBaseImageAsync(baseFilename);
            await SetFaceImageAsync(faceFilename);
        }
        
        public async Task<string[]> GetAvailableBaseImagesAsync()
        {
            return await Task.Run(() =>
            {
                var basesPath = Path.Combine(_spritePath, "bases");
                if (!Directory.Exists(basesPath))
                    return Array.Empty<string>();
                
                return Directory.GetFiles(basesPath, "*.png")
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToArray();
            });
        }
        
        public async Task<string[]> GetAvailableFaceImagesAsync()
        {
            return await Task.Run(() =>
            {
                var facesPath = Path.Combine(_spritePath, "faces");
                if (!Directory.Exists(facesPath))
                    return Array.Empty<string>();
                
                return Directory.GetFiles(facesPath, "*.png")
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToArray();
            });
        }
        
        public async Task<(string baseImage, string faceImage)> GetCurrentImagesAsync()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var baseImage = _mainWindow.BaseImage.Source switch
                {
                    BitmapImage bitmap => Path.GetFileName(bitmap.UriSource?.LocalPath ?? ""),
                    _ => ""
                };
                
                var faceImage = _mainWindow.FaceImage.Source switch
                {
                    BitmapImage bitmap => Path.GetFileName(bitmap.UriSource?.LocalPath ?? ""),
                    _ => ""
                };
                
                return (baseImage, faceImage);
            });
        }
    }
}
