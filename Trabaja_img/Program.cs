using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;
using System.Threading;

namespace Trabaja_img
{
    internal class Program
    {
        static void Main(string[] args)
        {

            const string QUEUE = "ImageWorkQueue";
            const string EXCHANGE = "ImageExchange";
            const string ROUTING_KEY = "Image.Result";

            // Configuración de la conexión a RabbitMQ
            var factory = new ConnectionFactory() { HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP };

            // Establecer la conexión con RabbitMQ
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declarar la cola de trabajo (por si no existe)
                channel.QueueDeclare(QUEUE, durable: true, exclusive: false, autoDelete: false, arguments: null);

                Console.WriteLine("[Trabajador] Esperando mensajes...");

                // Configurar el consumidor para recibir mensajes
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    // Decodificar el mensaje recibido
                    var message = MessageVocabulary.DecodeMessage(ea.Body.ToArray());
                    Console.WriteLine($"[Trabajador] Recibida Imagen: Id:{message.Id} Timestamp: {message.Timestamp} Type: {message.Type} Payload: {message.Payload}");

                    // Simular el procesamiento de la imagen
                    SimulateProcessing(message.Id, message.Timestamp,message.Type ,message.Payload);

                    // Crear un mensaje de resultado
                    var resultMessage = MessageVocabulary.CreateMessage(
                        message.Id,
                        ROUTING_KEY,
                        $"{message.Payload}_Processed"
                    );

                    // Codificar el mensaje de resultado
                    var body = MessageVocabulary.EncodeMessage(resultMessage);

                    // Publicar el resultado en el intercambio
                    channel.BasicPublish(EXCHANGE, ROUTING_KEY, null, body);
                    Console.WriteLine($"[Trabajador] Resultado publicada Imagen: Id:{message.Id} Timestamp: {message.Timestamp} Type: {message.Type} Payload: {message.Payload}");
                };

                // Iniciar el consumo de mensajes
                channel.BasicConsume(queue: QUEUE, autoAck: true, consumer: consumer);

                Console.WriteLine("Presiona [Enter] para salir.");
                Console.ReadLine();
            }
        }

        // Método para simular el procesamiento de una imagen
        private static void SimulateProcessing(int id, DateTime timestamp,string type, string payload)
        {
            Console.WriteLine($"[Trabajador] Procesando: Id:{id} Timestamp: {timestamp} Type: {type} Payload: {payload}");
            Thread.Sleep(2000); // Simula un procesamiento de 2 segundos
            Console.WriteLine($"[Trabajador] Procesamiento completado: Id:{id} Timestamp: {timestamp} Type: {type} Payload: {payload}");
        }
    }
}