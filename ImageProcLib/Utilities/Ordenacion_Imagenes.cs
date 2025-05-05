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

    // Clase encargada de gestionar y ordenar imágenes en base a su ID.
    public class Image_Sorter
    {
        // Almacena las imágenes en un diccionario donde la clave es el ID de la imagen
        private readonly ConcurrentDictionary <int, ImageData> _imageBuffer = new();
        // ID esperado de la siguiente imagen
        private int _expectedId = 0;

        // Agrega una nueva imagen a la lista de imágenes
        public void AddImage(int id, byte[] image, DateTime timestamp, string type)
        {
            var imageData = new ImageData
            {
                Id = id,
                Image = image,
                Timestamp = timestamp,
                Type = type
            };

            // Almacena la imagen utilizando su ID como clave.
            _imageBuffer[id] = imageData;
        }

        // Obtiene la siguiente imagen en orden según el ID esperado
        public ImageData? GetNextImage()
        {
            // Intenta obtener la imagen correspondiente al ID esperado.
            if (_imageBuffer.TryRemove(_expectedId, out var imageData))
            {
                // Si se encuentra la imagen, se incrementa el ID esperado para la siguiente imagen.
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
