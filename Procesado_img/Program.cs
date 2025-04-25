using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using ImageProcessingLib.Interfaces;

namespace Procesado_img
{
    internal class SimpleImageProcessor : IProcesador_Imagen
    {
        public string ImageProcess(string image)
        {
            image = image+"_ProcessedByProcessor";
            return image;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange"; 
            const string QUEUE = "ImageWorkQueue";  
            const string ROUTING_KEY = "Image.Raw"; 

            // Configuración de la conexión a RabbitMQ
            var factory = new ConnectionFactory() { HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP };

            // Crear una instancia del procesador de imágenes que implementa la interfaz IProcesador_Imagen
            var imageProcessor = new SimpleImageProcessor();

            // Establecer la conexión con RabbitMQ
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declarar el intercambio de tipo "topic"
                channel.ExchangeDeclare(EXCHANGE, "topic");

                // Declarar la cola de trabajo
                channel.QueueDeclare(QUEUE, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Crear una cola temporal para recibir mensajes del intercambio
                var queueName = channel.QueueDeclare().QueueName;

                // Enlazar la cola temporal al intercambio con la clave de enrutamiento
                channel.QueueBind(queueName, EXCHANGE, ROUTING_KEY);

                Console.WriteLine("[Procesador] Esperando mensajes...");

                // Configurar el consumidor para recibir mensajes
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    // Decodificar el mensaje recibido
                    var message = MessageVocabulary.DecodeMessage(ea.Body.ToArray());
                    Console.WriteLine($"[Procesador] Recibido Imagen: Id:{message.Id} Timestamp: {message.Timestamp} Type: {message.Type} Payload: {message.Payload}");

                    // Procesar la imagen
                    string processedImage = imageProcessor.ImageProcess(message.Payload);

                    // Crea el mensaje con la imagen procesada
                    var processedMessage = MessageVocabulary.CreateMessage(message.Id, ROUTING_KEY, processedImage);

                    // Enviar el mensaje a la cola de trabajo
                    var body = MessageVocabulary.EncodeMessage(processedMessage);
                    channel.BasicPublish("", QUEUE, null, body);
                    Console.WriteLine($"[Procesador] Enviado a la cola de trabajo: Id:{processedMessage.Id} Timestamp: {processedMessage.Timestamp} Type: {processedMessage.Type} Payload: {processedMessage.Payload}");
                };

                // Iniciar el consumo de mensajes
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Presiona [Enter] para salir.");
                Console.ReadLine();
            }
        }
    }
}
