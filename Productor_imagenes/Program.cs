using RabbitMQ.Client;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;
using System.Runtime.ConstrainedExecution;
using ImageProcLib.Interfaces;
using OpenCvSharp;
using System.Diagnostics;

namespace Productor_imagenes
{

    internal class WebcamImageSource : IFuente_Imagen
    {
        private readonly VideoCapture _capture;

        public WebcamImageSource()
        {
            _capture = new VideoCapture(0); // Usar la cámara web
            if (!_capture.IsOpened())
            {
                throw new Exception("No se pudo abrir la cámara.");
            }
        }

        public byte[] GetNextImage()
        {
            using var frame = new Mat();
            _capture.Read(frame);

            if (frame.Empty())
            {
                throw new Exception("No se pudo capturar un frame de la cámara.");
            }

            // Codificar el frame como JPEG


            return frame.ToBytes(".jpg");
        }
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange";
            const string ROUTING_KEY = "Image.Raw";

            // Configuración de la conexión a RabbitMQ
            var factory = new ConnectionFactory() 
            { 
                HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP 
            };

            // Usar la interfaz IFuente_Imagen para obtener imágenes
            var imageSource = new WebcamImageSource();

            // Establecer la conexión con RabbitMQ
            using (var connection = factory.CreateConnection())
            {

                // Crear un canal para interactuar con RabbitMQ
                using (var channel = connection.CreateModel())
                {
                    // Declarar un intercambio de tipo "topic"
                    channel.ExchangeDeclare(EXCHANGE, "topic");

                    int frameId = 0;
                    while (true) // Capturar frames continuamente
                    {
                        try
                        {
                            // Obtener el siguiente frame
                            byte[] imageBytes = imageSource.GetNextImage();

                            // Crear un mensaje con el frame
                            var message = MessageVocabulary.CreateMessage(frameId++, ROUTING_KEY, imageBytes);
                            var body = MessageVocabulary.EncodeMessage(message);

                            // Publicar el mensaje
                            channel.BasicPublish(EXCHANGE, ROUTING_KEY, null, body);
                            Console.WriteLine($"[Productor] Enviado Frame: Id:{message.Id} Timestamp:{message.Timestamp}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Productor] Error al capturar el frame: {ex.Message}");
                            break;
                        }
                    }
                }
            }
        }
    }
}
