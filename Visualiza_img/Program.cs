using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using ImageProcLib.Interfaces;
using ImageProcLib.Utilities;
using OpenCvSharp;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace Visualiza_img
{
    internal class WindowImageVisualizer : IVisualizador_Imagen
    {
        private const string WindowName = "Image Visualizer";
        private bool _windowInitialized = false;
        private readonly ConcurrentQueue<(int id, DateTime timestamp, string type, Mat image)> _imageQueue = new();
        private readonly Thread _uiThread;

        public WindowImageVisualizer()
        {
            // Inicializar la ventana en el hilo
            _uiThread = new Thread(UILoop)
            {
                IsBackground = true,
                Name = "OpenCV UI Thread"
            };
            _uiThread.Start();

            Console.WriteLine("[Visualizador] Window initialized");
        }

        private void UILoop()
        {
            try
            {
                // Inicializar ventana en este hilo
                Cv2.NamedWindow(WindowName, WindowFlags.AutoSize | WindowFlags.KeepRatio);
                _windowInitialized = true;

                Mat? lastImage = null;

                while (true)
                {
                    // Procesar imágenes en cola
                    if (_imageQueue.TryDequeue(out var imageData))
                    {
                        var (id, timestamp, type, image) = imageData;

                        // Mostrar información en la imagen
                        Cv2.PutText(image, $"ID: {id} | {timestamp:HH:mm:ss.fff}", new Point(10, 20),
                                    HersheyFonts.HersheySimplex, 0.5, Scalar.White);
                        Cv2.PutText(image, $"Type: {type}", new Point(10, 40),
                                    HersheyFonts.HersheySimplex, 0.5, Scalar.White);

                        // Mostrar la imagen
                        Cv2.ImShow(WindowName, image);
                        lastImage = image;

                        Console.WriteLine($"[Visualizador] Showing image: Id:{id} Timestamp:{timestamp}");
                    }
                    else if (lastImage != null)
                    {
                        // Mantener mostrando la última imagen mientras no hay nueva
                        Cv2.ImShow(WindowName, lastImage);
                    }

                    // Procesar eventos de GUI
                    int key = Cv2.WaitKey(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Visualizador] Error en el hilo de UI: {ex.Message}");
            }
            finally
            {
                if (_windowInitialized)
                {
                    Cv2.DestroyWindow(WindowName);
                }
            }
        }


        public void ImageVisualize(int id, DateTime timestamp, string type, byte[] payload)
        {
            try
            {
                // Convertir a OpenCV Mat
                Mat image = Cv2.ImDecode(payload, ImreadModes.Color);

                if (image.Empty())
                {
                    Console.WriteLine($"[Visualizador] Error: Image {id} is empty");
                    return;
                }

                // Poner en cola para mostrar en el hilo de UI
                _imageQueue.Enqueue((id, timestamp, type, image));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Visualizador] Error al procesar imagen: {ex.Message}");
            }
        }   
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            const string EXCHANGE = "ImageExchange";
            const string BINDING_KEY = "Image.*";

            // Configurar la conexión a RabbitMQ
            var factory = new ConnectionFactory() 
            { 
                HostName = ImageProcLib.Constants.Constants.RabbitMQ_Server_IP 
            };

            // Crear el visualizador de imágenes
            var imageVisualizer = new WindowImageVisualizer();

            // Crear el ordenador de imagenes
            var imageSorter = new Image_Sorter();

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

                    // Agregar la imagen al ordenador
                    if (message.Type == "Image.Result")
                    {
                        imageSorter.AddImage(message.Id, message.Payload, message.Timestamp, message.Type);
                    }

                    // Intentar mostrar las imágenes en orden
                    ImageData? nextImage;
                    if ((nextImage = imageSorter.GetNextImage()) != null)
                    {
                        imageVisualizer.ImageVisualize(nextImage.Id, nextImage.Timestamp, nextImage.Type, nextImage.Image);
                    }
                };

                // Iniciar el consumo de mensajes
                channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

                Console.WriteLine("Presiona [Enter] para salir.");
                Console.ReadLine();
            }
        }
    }
}
