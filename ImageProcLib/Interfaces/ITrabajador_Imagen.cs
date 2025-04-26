namespace ImageProcessingLib.Interfaces
{

    // El que tiene que procesar la iamgen es el trabajador
    public interface ITrabajador_Imagen
    {
        byte[] ImageProcess(byte[] imageBytes);
    }
}

