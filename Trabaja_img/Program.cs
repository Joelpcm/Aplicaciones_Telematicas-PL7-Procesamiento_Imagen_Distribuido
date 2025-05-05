using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageProcLib.Vocabulary;
using ImageProcLib.Constants;
using System;
using System.Text;
using System.Threading;
using ImageProcessingLib.Interfaces;
using OpenCvSharp;

namespace Trabaja_img
{
    internal class Program
    {
        // Procesamiento de imagen para detectar caras
        internal class FaceDetectionProcessor : ITrabajador_Imagen
        {
            private readonly CascadeClassifier _faceClassifier;

            public  FaceDetectionProcessor()
            {
                // Cargar el archivo de clasificador de caras
                string haarCascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");

                // Verificar que el archivo existe
                if (!File.Exists(haarCascadePath))
                {
                    throw new FileNotFoundException(
                        "No se encontró el archivo de clasificación de caras. " +
                        "Por favor, descargue 'haarcascade_frontalface_default.xml' manualmente desde " +
                        "https://github.com/opencv/opencv/blob/master/data/haarcascades/haarcascade_frontalface_default.xml " +
                        "y colóquelo en el directorio de la aplicación: " + AppDomain.CurrentDomain.BaseDirectory);
                }

                _faceClassifier = new CascadeClassifier(haarCascadePath);

                if (_faceClassifier.Empty())
                {
                    throw new Exception("Error al cargar el detector de caras. Verifique que el archivo existe.");
                }

                Console.WriteLine("[Trabajador] Detector de caras inicializado correctamente");
            }

            public byte[] ImageProcess(byte[] imageBytes)
            {
                try
                {
                    // Lee la imagen desde bytes
                    using var srcImage = Cv2.ImDecode(imageBytes, ImreadModes.Color);

                    if (srcImage.Empty())
                    {
                        Console.WriteLine("[Trabajador] Error: No se pudo leer la imagen");
                        return imageBytes; // Devolver la imagen original en caso de error
                    }

                    // Convertir a escala de grises para la detección de caras
                    using var grayImage = new Mat();
                    Cv2.CvtColor(srcImage, grayImage, ColorConversionCodes.BGR2GRAY);

                    // Usar el clasificador para detectar caras
                    Rect[] faces = _faceClassifier.DetectMultiScale(
                        grayImage,
                        scaleFactor: 1.1,
                        minNeighbors: 5,
                        minSize: new Size(30, 30)
                    );

                    // Crear una copia para dibujar los resultados
                    using var resultImage = srcImage.Clone();

                    // Dibujar rectángulos alrededor de las caras detectadas
                    foreach (var face in faces)
                    {
                        Cv2.Rectangle(
                            resultImage,
                            new Point(face.X, face.Y),
                            new Point(face.X + face.Width, face.Y + face.Height),
                            Scalar.Red,
                            2
                        );
                    }

                    // Agregar texto con información
                    Cv2.PutText(
                        resultImage,
                        $"Caras detectadas: {faces.Length}",
                        new Point(300, 30),
                        HersheyFonts.HersheySimplex,
                        1.0,
                        Scalar.Black,
                        2
                    );

                    // Convertir la imagen procesada a bytes
                    return resultImage.ToBytes(".jpg");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Trabajador] Error al procesar la imagen: {ex.Message}");
                    return imageBytes; // Devolver la imagen original en caso de error
                }
            }
        }

        static void Main(string[] args)
        {

            const string QUEUE = "ImageWorkQueue";
            const string EXCHANGE = "ImageExchange";
            const string ROUTING_KEY = "Image.Result";

            // Crear el procesador de detección de caras
            var faceProcessor = new FaceDetectionProcessor();

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

                    try
                    {
                        // Decodificar el mensaje recibido
                        var message = MessageVocabulary.DecodeMessage(ea.Body.ToArray());
                        Console.WriteLine($"[Trabajador] Recibida Imagen: Id:{message.Id} Timestamp: {message.Timestamp} Type: {message.Type} Payload: {message.Payload}");

                        // Procesar la imagen solo si el payload es un array de bytes
                        if (message.Payload is byte[] imageBytes)
                        {
                            Console.WriteLine($"[Trabajador] Procesando imagen de {imageBytes.Length} bytes...");

                            // Medir el tiempo de procesamiento
                            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                            // Detectar caras en la imagen
                            byte[] processedImage = faceProcessor.ImageProcess(imageBytes);

                            stopwatch.Stop();
                            Console.WriteLine($"[Trabajador] Detección de caras completada en {stopwatch.ElapsedMilliseconds}ms");

                            // Crear un mensaje con el resultado procesado
                            var resultMessage = MessageVocabulary.CreateMessage(
                                message.Id,
                                ROUTING_KEY,
                                processedImage
                            );

                            // Codificar el mensaje de resultado
                            var body = MessageVocabulary.EncodeMessage(resultMessage);

                            // Publicar el resultado en el intercambio
                            channel.BasicPublish(EXCHANGE, ROUTING_KEY, null, body);
                            Console.WriteLine($"[Trabajador] Resultado publicado: Id:{resultMessage.Id} Timestamp:{resultMessage.Timestamp} Type:{resultMessage.Type}");
                        }
                        else
                        {
                            Console.WriteLine("[Trabajador] Error: El payload no es un array de bytes");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Trabajador] Error al procesar el mensaje: {ex.Message}");
                    }
                };

                // Iniciar el consumo de mensajes
                channel.BasicConsume(queue: QUEUE, autoAck: true, consumer: consumer);

                Console.WriteLine("Presiona [Enter] para salir.");
                Console.ReadLine();
            }
        }  
    }
}