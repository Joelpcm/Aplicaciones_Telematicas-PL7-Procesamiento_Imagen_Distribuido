using System.Collections.Concurrent;

namespace ImageProcLib.Utilities
{
    public class Ordenacion_Imagenes
    {
        private readonly ConcurrentDictionary <int, string> _imageBuffer = new();
        private int _expectedId = 0;

        public void AddImage(int id, string image)
        {
            _imageBuffer[id] = image;
        }

        public string GetNextImage()
        {
            if (_imageBuffer.TryRemove(_expectedId, out var image))
            {
                _expectedId++;
                return image;
            }
            else
            {
                return "-1"; // No hay imágenes en orden
            }
        }
    }
}
