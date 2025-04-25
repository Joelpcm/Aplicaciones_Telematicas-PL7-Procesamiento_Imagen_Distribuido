using RabbitMQ.Client;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;

namespace Productor_imagenes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange";
            const string ROUTING_KEY = "Image.Raw";

            var factory = new ConnectionFactory() { HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(EXCHANGE, "topic");

                    var random = new Random();
                    for (int i = 0; i < 10; i++)
                    {
                        string payload = $"Image_{random.Next(1, 100)}";
                        var message = MessageVocabulary.CreateMessage(i, ROUTING_KEY, payload);
                        var body = MessageVocabulary.EncodeMessage(message);

                        channel.BasicPublish(EXCHANGE, ROUTING_KEY, null, body);
                        Console.WriteLine($"[Productor] Enviado: {payload}");
                    }
                }
            }
        }
    }
}
