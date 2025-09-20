using System.Threading.Tasks;

namespace FiendFriend.Services.Interfaces
{
    public interface IImageController
    {
        Task LoadRandomImagesAsync();
        Task SetBaseImageAsync(string filename);
        Task SetFaceImageAsync(string filename);
        Task SetBothImagesAsync(string baseFilename, string faceFilename);
        Task<string[]> GetAvailableBaseImagesAsync();
        Task<string[]> GetAvailableFaceImagesAsync();
        Task<(string baseImage, string faceImage)> GetCurrentImagesAsync();
    }
}
