using System.Collections.Concurrent;

namespace ImageProcLib.Utilities
{
    public class ImageData
    {
        public required int Id { get; set; } 
        public required byte[] Image { get; set; } 
        public DateTime Timestamp { get; set; } 
        public required string Type { get; set; } 
    }

    public class Image_Sorter
    {
        private readonly ConcurrentDictionary <int, ImageData> _imageBuffer = new();
        private int _expectedId = 0;

        public void AddImage(int id, byte[] image, DateTime timestamp, string type)
        {
            var imageData = new ImageData
            {
                Id = id,
                Image = image,
                Timestamp = timestamp,
                Type = type
            };
            _imageBuffer[id] = imageData;
        }

        public ImageData? GetNextImage()
        {
            if (_imageBuffer.TryRemove(_expectedId, out var imageData))
            {
                _expectedId++;
                return imageData;
            }
            else
            {
                return null; // No hay imagenes
            }
        }
    }
}
