using System.Text;
using System.Text.Json;

namespace ImageProcLib.Vocabulary
{
    public static class MessageVocabulary
    {
        // Estructura de un mensaje
        public class Message
        {
            public int Id { get; set; } // Identificador único del mensaje
            public required string Type { get; set; } // Tipo de mensaje (e.g., "Image.Raw", "Image.Result")
            public required string Payload { get; set; } // Contenido del mensaje
            public DateTime Timestamp { get; set; } // Marca de tiempo
        }

        // Codificar un mensaje en formato JSON
        public static byte[] EncodeMessage(Message message)
        {
            try
            {
                string json = JsonSerializer.Serialize(message);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al codificar el mensaje: {ex.Message}");
                throw;
            }
        }

        // Decodificar un mensaje desde formato JSON
        public static Message DecodeMessage(byte[] body)
        {
            try
            {
                string json = Encoding.UTF8.GetString(body);

                // Intentar deserializar el mensaje
        var message = JsonSerializer.Deserialize<Message>(json);

        // Verificar si el resultado es null
        if (message == null)
        {
            throw new Exception("El mensaje deserializado es null.");
        }

        return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al decodificar el mensaje: {ex.Message}");
                throw;
            }
        }

        // Crear un mensaje
        public static Message CreateMessage(int id, string type, string payload)
        {
            return new Message
            {
                Id = id,
                Type = type,
                Payload = payload,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
