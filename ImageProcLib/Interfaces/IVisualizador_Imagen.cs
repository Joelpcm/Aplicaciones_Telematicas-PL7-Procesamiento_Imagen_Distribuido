
namespace ImageProcLib.Interfaces
{
     public interface IVisualizador_Imagen
    {
        void ImageVisualize(int id, DateTime timestamp, string type, byte[] payload);
    }
}
