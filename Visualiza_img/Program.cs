using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;

namespace Visualiza_img
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange";
            const string BINDING_KEY = "Image.*";

            // Configurar la conexión a RabbitMQ
            var factory = new ConnectionFactory() { HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declarar el intercambiador
                channel.ExchangeDeclare(EXCHANGE, "topic");

                // Crear una cola temporal para el visualizador
                var queueName = channel.QueueDeclare().QueueName;

                // Enlazar la cola al intercambiador con la clave de enrutamiento
                channel.QueueBind(queueName, EXCHANGE, BINDING_KEY);

                Console.WriteLine("[Visualizador] Esperando mensajes...");

                // Configurar el consumidor
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    // Decodificar el mensaje recibido
                    var message = MessageVocabulary.DecodeMessage(ea.Body.ToArray());
                    Console.WriteLine($"[Visualizador] Recibida Imagen: Id:{message.Id} Timestamp: {message.Timestamp} Type: {message.Type} Payload: {message.Payload}");
                };

                // Iniciar el consumo de mensajes
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Presiona [Enter] para salir.");
                Console.ReadLine();
            }
        }
    }
}
