using System;
using System.Text;
using ImageProcessingLib.Vocabulary;
using Xunit;

namespace ImageProcLib.Tests
{
    public class MessageVocabularyTests
    {
        [Fact]
        public void CreateMessage_ShouldReturnValidMessage()
        {
            // Arrange
            int id = 1;
            string type = "Image.Raw";
            string payload = "TestPayload";

            // Act
            var message = MessageVocabulary.CreateMessage(id, type, payload);

            // Assert
            Assert.Equal(id, message.Id);
            Assert.Equal(type, message.Type);
            Assert.Equal(payload, message.Payload);
            Assert.True(message.Timestamp <= DateTime.UtcNow);
        }

        [Fact]
        public void EncodeMessage_ShouldReturnValidByteArray()
        {
            // Arrange
            var message = new MessageVocabulary.Message
            {
                Id = 1,
                Type = "Image.Raw",
                Payload = "TestPayload",
                Timestamp = DateTime.UtcNow
            };

            // Act
            var encodedMessage = MessageVocabulary.EncodeMessage(message);

            // Assert
            Assert.NotNull(encodedMessage);
            Assert.IsType<byte[]>(encodedMessage);
        }

        [Fact]
        public void DecodeMessage_ShouldReturnValidMessage()
        {
            // Arrange
            var originalMessage = new MessageVocabulary.Message
            {
                Id = 1,
                Type = "Image.Raw",
                Payload = "TestPayload",
                Timestamp = DateTime.UtcNow
            };
            var encodedMessage = MessageVocabulary.EncodeMessage(originalMessage);

            // Act
            var decodedMessage = MessageVocabulary.DecodeMessage(encodedMessage);

            // Assert
            Assert.NotNull(decodedMessage);
            Assert.Equal(originalMessage.Id, decodedMessage.Id);
            Assert.Equal(originalMessage.Type, decodedMessage.Type);
            Assert.Equal(originalMessage.Payload, decodedMessage.Payload);
            Assert.Equal(originalMessage.Timestamp, decodedMessage.Timestamp);
        }

        [Fact]
        public void DecodeMessage_ShouldThrowExceptionForInvalidJson()
        {
            // Arrange
            byte[] invalidMessage = Encoding.UTF8.GetBytes("Invalid JSON");

            // Act & Assert
            var exception = Assert.Throws<System.Text.Json.JsonException>(() => MessageVocabulary.DecodeMessage(invalidMessage));
            Assert.Contains("'I' is an invalid start of a value", exception.Message);
        }


        [Fact]
        public void DecodeMessage_ShouldThrowExceptionForNullMessage()
        {
            // Arrange
            byte[] nullMessage = Encoding.UTF8.GetBytes("null");

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => MessageVocabulary.DecodeMessage(nullMessage));
            Assert.Contains("El mensaje deserializado es null", exception.Message);
        }
    }
}
