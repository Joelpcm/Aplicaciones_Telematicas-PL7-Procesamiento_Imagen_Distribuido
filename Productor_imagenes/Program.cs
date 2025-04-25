using RabbitMQ.Client;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;
using System.Runtime.ConstrainedExecution;

namespace Productor_imagenes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange";
            const string ROUTING_KEY = "Image.Raw";

            // Configuración de la conexión a RabbitMQ
            var factory = new ConnectionFactory() { HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP };

            // Establecer la conexión con RabbitMQ
            using (var connection = factory.CreateConnection())
            {

                // Crear un canal para interactuar con RabbitMQ
                using (var channel = connection.CreateModel())
                {
                    // Declarar un intercambio de tipo "topic"
                    channel.ExchangeDeclare(EXCHANGE, "topic");

                    // Generador de números aleatorios para simular imágenes
                    var random = new Random();

                    // Publicar 10 mensajes simulando imágenes
                    for (int i = 0; i < 10; i++)
                    {
                        // Crear un mensaje de texto simulando una imagen
                        string payload = $"Image_{random.Next(1, 100)}";

                        // Crear un objeto de mensaje con un identificador, tipo y contenido
                        var message = MessageVocabulary.CreateMessage(i, ROUTING_KEY, payload);

                        // Codificar el mensaje en formato JSON para enviarlo a RabbitMQ
                        var body = MessageVocabulary.EncodeMessage(message);

                        // Publicar el mensaje
                        channel.BasicPublish(EXCHANGE, ROUTING_KEY, null, body);
                        Console.WriteLine($"[Productor] Enviado: {payload}");
                    }
                }
            }
        }
    }
}
