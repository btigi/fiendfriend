using System.Text.Json.Serialization;

namespace FiendFriend.Models
{
    public class ImageChangeRequest
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;
        
        [JsonPropertyName("baseImage")]
        public string? BaseImage { get; set; }
        
        [JsonPropertyName("faceImage")]
        public string? FaceImage { get; set; }
        
        [JsonPropertyName("random")]
        public bool Random { get; set; } = false;
    }
    
    public class ImageChangeResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("currentBaseImage")]
        public string? CurrentBaseImage { get; set; }
        
        [JsonPropertyName("currentFaceImage")]
        public string? CurrentFaceImage { get; set; }
        
        [JsonPropertyName("availableBaseImages")]
        public string[]? AvailableBaseImages { get; set; }
        
        [JsonPropertyName("availableFaceImages")]
        public string[]? AvailableFaceImages { get; set; }
    }
}
