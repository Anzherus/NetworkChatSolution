using System.Text;
using System.Text.Json;

namespace ChatCommon
{
    public class ChatMessage
    {
        public string Command { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }

        public ChatMessage() { }

        public ChatMessage(string command, string data = "", string sender = "System")
        {
            Command = command;
            Data = data;
            Sender = sender;
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public static class ChatProtocol
    {
        public const int PORT = 12345;
        public const string DEFAULT_SERVER = "localhost";
        public const int BUFFER_SIZE = 4096;
        public const string PROTOCOL_VERSION = "1.0";

        public static class Commands
        {
            public const string AUTH_REQUEST = "AUTH_REQ";
            public const string AUTH_RESPONSE = "AUTH_RES";
            public const string MESSAGE = "MSG";
            public const string USER_JOINED = "USER_JOIN";
            public const string USER_LEFT = "USER_LEFT";
            public const string ONLINE_USERS = "ONLINE";
            public const string SERVER_TIME = "TIME";
            public const string HELP = "HELP";
            public const string ERROR = "ERROR";
            public const string SUCCESS = "SUCCESS";
            public const string PING = "PING";
            public const string PONG = "PONG";
        }


        public static byte[] EncodeMessage(ChatMessage message)
        {
            try
            {
                string json = JsonSerializer.Serialize(message);
                byte[] data = Encoding.UTF8.GetBytes(json);

                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                byte[] result = new byte[lengthPrefix.Length + data.Length];

                Buffer.BlockCopy(lengthPrefix, 0, result, 0, lengthPrefix.Length);
                Buffer.BlockCopy(data, 0, result, lengthPrefix.Length, data.Length);

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка кодирования сообщения: {ex.Message}", ex);
            }
        }

        public static ChatMessage? DecodeMessage(byte[] buffer, int bytesRead)
        {
            try
            {
                if (bytesRead < 4) return null;

                int messageLength = BitConverter.ToInt32(buffer, 0);

                if (bytesRead < 4 + messageLength) return null;

                string json = Encoding.UTF8.GetString(buffer, 4, messageLength);
                return JsonSerializer.Deserialize<ChatMessage>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка декодирования сообщения: {ex.Message}");
                return null;
            }
        }

        public static ChatMessage CreateErrorMessage(string errorText)
        {
            return new ChatMessage(Commands.ERROR, errorText, "System");
        }

        public static ChatMessage CreateSuccessMessage(string successText)
        {
            return new ChatMessage(Commands.SUCCESS, successText, "System");
        }

        public static ChatMessage CreateUserMessage(string sender, string message)
        {
            return new ChatMessage(Commands.MESSAGE, message, sender);
        }

        public static ChatMessage CreateUserJoinedMessage(string username)
        {
            return new ChatMessage(Commands.USER_JOINED, $"{username} присоединился к чату", "System");
        }

        public static ChatMessage CreateUserLeftMessage(string username)
        {
            return new ChatMessage(Commands.USER_LEFT, $"{username} покинул чат", "System");
        }

        public static ChatMessage CreatePrivateMessage(string fromUser, string toUser, string message)
        {
            return new ChatMessage(Commands.MESSAGE, $"[Приватно от {fromUser}] {message}", toUser);
        }

        public static ChatMessage CreateAuthRequest(string username)
        {
            return new ChatMessage(Commands.AUTH_REQUEST, username, "Client");
        }
    }
}