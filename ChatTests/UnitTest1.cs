using ChatCommon;
using System.Text;

namespace ChatTests
{
    public class ChatProtocolTests
    {
        [Fact]
        public void TestMessageCreation()
        {
            // Тест создания сообщения
            var message = new ChatMessage("TEST", "Hello World", "TestUser");
            
            Assert.Equal("TEST", message.Command);
            Assert.Equal("Hello World", message.Data);
            Assert.Equal("TestUser", message.Sender);
            Assert.NotNull(message.Timestamp);
        }

        [Fact]
        public void TestMessageEncodingDecoding()
        {
            // Тест кодирования и декодирования сообщения
            var originalMessage = new ChatMessage("MSG", "Test message", "TestUser");
            
            var encodedData = ChatProtocol.EncodeMessage(originalMessage);
            Assert.NotNull(encodedData);
            Assert.True(encodedData.Length > 0);
            
            var decodedMessage = ChatProtocol.DecodeMessage(encodedData, encodedData.Length);
            Assert.NotNull(decodedMessage);
            Assert.Equal(originalMessage.Command, decodedMessage.Command);
            Assert.Equal(originalMessage.Data, decodedMessage.Data);
            Assert.Equal(originalMessage.Sender, decodedMessage.Sender);
        }

        [Fact]
        public void TestErrorMessageCreation()
        {
            var errorMessage = ChatProtocol.CreateErrorMessage("Test error");
            
            Assert.Equal(ChatProtocol.Commands.ERROR, errorMessage.Command);
            Assert.Equal("Test error", errorMessage.Data);
            Assert.Equal("System", errorMessage.Sender);
        }

        [Fact]
        public void TestSuccessMessageCreation()
        {
            var successMessage = ChatProtocol.CreateSuccessMessage("Test success");
            
            Assert.Equal(ChatProtocol.Commands.SUCCESS, successMessage.Command);
            Assert.Equal("Test success", successMessage.Data);
            Assert.Equal("System", successMessage.Sender);
        }

        [Fact]
        public void TestUserMessageCreation()
        {
            var userMessage = ChatProtocol.CreateUserMessage("TestUser", "Hello");
            
            Assert.Equal(ChatProtocol.Commands.MESSAGE, userMessage.Command);
            Assert.Equal("Hello", userMessage.Data);
            Assert.Equal("TestUser", userMessage.Sender);
        }

        [Fact]
        public void TestUserJoinedMessageCreation()
        {
            var joinedMessage = ChatProtocol.CreateUserJoinedMessage("NewUser");
            
            Assert.Equal(ChatProtocol.Commands.USER_JOINED, joinedMessage.Command);
            Assert.True(joinedMessage.Data.Contains("NewUser"));
            Assert.Equal("System", joinedMessage.Sender);
        }

        [Fact]
        public void TestUserLeftMessageCreation()
        {
            var leftMessage = ChatProtocol.CreateUserLeftMessage("OldUser");
            
            Assert.Equal(ChatProtocol.Commands.USER_LEFT, leftMessage.Command);
            Assert.True(leftMessage.Data.Contains("OldUser"));
            Assert.Equal("System", leftMessage.Sender);
        }

        [Fact]
        public void TestPrivateMessageCreation()
        {
            var privateMessage = ChatProtocol.CreatePrivateMessage("FromUser", "ToUser", "Secret message");
            
            Assert.Equal(ChatProtocol.Commands.MESSAGE, privateMessage.Command);
            Assert.True(privateMessage.Data.Contains("FromUser"));
            Assert.True(privateMessage.Data.Contains("Secret message"));
            Assert.Equal("ToUser", privateMessage.Sender);
        }

        [Fact]
        public void TestAuthRequestCreation()
        {
            var authRequest = ChatProtocol.CreateAuthRequest("TestUser");
            
            Assert.Equal(ChatProtocol.Commands.AUTH_REQUEST, authRequest.Command);
            Assert.Equal("TestUser", authRequest.Data);
            Assert.Equal("Client", authRequest.Sender);
        }

        [Fact]
        public void TestProtocolConstants()
        {
            // Тест констант протокола
            Assert.Equal(12345, ChatProtocol.PORT);
            Assert.Equal("localhost", ChatProtocol.DEFAULT_SERVER);
            Assert.Equal(4096, ChatProtocol.BUFFER_SIZE);
            Assert.Equal("1.0", ChatProtocol.PROTOCOL_VERSION);
        }

        [Fact]
        public void TestCommandConstants()
        {
            // Тест констант команд
            Assert.Equal("AUTH_REQ", ChatProtocol.Commands.AUTH_REQUEST);
            Assert.Equal("AUTH_RES", ChatProtocol.Commands.AUTH_RESPONSE);
            Assert.Equal("MSG", ChatProtocol.Commands.MESSAGE);
            Assert.Equal("USER_JOIN", ChatProtocol.Commands.USER_JOINED);
            Assert.Equal("USER_LEFT", ChatProtocol.Commands.USER_LEFT);
            Assert.Equal("ONLINE", ChatProtocol.Commands.ONLINE_USERS);
            Assert.Equal("TIME", ChatProtocol.Commands.SERVER_TIME);
            Assert.Equal("HELP", ChatProtocol.Commands.HELP);
            Assert.Equal("ERROR", ChatProtocol.Commands.ERROR);
            Assert.Equal("SUCCESS", ChatProtocol.Commands.SUCCESS);
            Assert.Equal("PING", ChatProtocol.Commands.PING);
            Assert.Equal("PONG", ChatProtocol.Commands.PONG);
        }

        [Fact]
        public void TestLargeMessageHandling()
        {
            // Тест обработки больших сообщений
            var largeText = new string('A', 1000);
            var message = new ChatMessage("MSG", largeText, "TestUser");
            
            var encodedData = ChatProtocol.EncodeMessage(message);
            var decodedMessage = ChatProtocol.DecodeMessage(encodedData, encodedData.Length);
            
            Assert.NotNull(decodedMessage);
            Assert.Equal(largeText, decodedMessage.Data);
        }

        [Fact]
        public void TestUnicodeMessageHandling()
        {
            // Тест обработки Unicode сообщений
            var unicodeText = "Привет! 🌟 Тест с эмодзи и кириллицей";
            var message = new ChatMessage("MSG", unicodeText, "TestUser");
            
            var encodedData = ChatProtocol.EncodeMessage(message);
            var decodedMessage = ChatProtocol.DecodeMessage(encodedData, encodedData.Length);
            
            Assert.NotNull(decodedMessage);
            Assert.Equal(unicodeText, decodedMessage.Data);
        }

        [Fact]
        public void TestEmptyMessageHandling()
        {
            // Тест обработки пустых сообщений
            var emptyMessage = new ChatMessage("", "", "");
            
            var encodedData = ChatProtocol.EncodeMessage(emptyMessage);
            var decodedMessage = ChatProtocol.DecodeMessage(encodedData, encodedData.Length);
            
            Assert.NotNull(decodedMessage);
            Assert.Equal("", decodedMessage.Command);
            Assert.Equal("", decodedMessage.Data);
            Assert.Equal("", decodedMessage.Sender);
        }
    }
}