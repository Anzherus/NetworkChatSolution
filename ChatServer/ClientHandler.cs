using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatCommon;

namespace ChatServer
{
    public class ClientHandler
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly byte[] _buffer = new byte[ChatProtocol.BUFFER_SIZE];
        private bool _isConnected = false;
        private string _username = string.Empty;

        public string Username => _username;
        public bool IsConnected => _isConnected;

        public ClientHandler(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _stream = _tcpClient.GetStream();
        }

        public async Task StartAsync()
        {
            try
            {
                _isConnected = true;
                Console.WriteLine($"🔗 Новое подключение от {_tcpClient.Client.RemoteEndPoint}");

                // Ожидаем аутентификацию
                await HandleAuthentication();

                if (_isConnected)
                {
                    // Добавляем клиента в список
                    await Program.AddClient(this);

                    // Основной цикл обработки сообщений
                    await HandleMessages();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки клиента {_username}: {ex.Message}");
            }
            finally
            {
                await DisconnectAsync();
            }
        }

        private async Task HandleAuthentication()
        {
            try
            {
                var message = await ReceiveMessageAsync();
                if (message == null || message.Command != ChatProtocol.Commands.AUTH_REQUEST)
                {
                    await SendMessageAsync(ChatProtocol.CreateErrorMessage("Требуется аутентификация"));
                    _isConnected = false;
                    return;
                }

                _username = message.Data.Trim();
                if (string.IsNullOrEmpty(_username))
                {
                    await SendMessageAsync(ChatProtocol.CreateErrorMessage("Имя пользователя не может быть пустым"));
                    _isConnected = false;
                    return;
                }

                // Проверяем, не занято ли имя
                if (Program.GetOnlineUsers().Contains(_username))
                {
                    await SendMessageAsync(ChatProtocol.CreateErrorMessage("Пользователь с таким именем уже подключен"));
                    _isConnected = false;
                    return;
                }

                // Отправляем подтверждение аутентификации
                await SendMessageAsync(ChatProtocol.CreateSuccessMessage($"Добро пожаловать, {_username}!"));
                
                // Отправляем список онлайн пользователей
                var onlineUsers = Program.GetOnlineUsers();
                var onlineMessage = new ChatMessage(ChatProtocol.Commands.ONLINE_USERS, string.Join(",", onlineUsers), "System");
                await SendMessageAsync(onlineMessage);

                Console.WriteLine($"✅ Пользователь '{_username}' успешно аутентифицирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка аутентификации: {ex.Message}");
                _isConnected = false;
            }
        }

        private async Task HandleMessages()
        {
            while (_isConnected && _tcpClient.Connected)
            {
                try
                {
                    var message = await ReceiveMessageAsync();
                    if (message == null)
                    {
                        // Соединение закрыто
                        break;
                    }

                    await ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обработки сообщения от {_username}: {ex.Message}");
                    break;
                }
            }
        }

        private async Task ProcessMessage(ChatMessage message)
        {
            switch (message.Command)
            {
                case ChatProtocol.Commands.MESSAGE:
                    await HandleChatMessage(message);
                    break;
                case ChatProtocol.Commands.PING:
                    await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.PONG, "pong", "Server"));
                    break;
                case ChatProtocol.Commands.HELP:
                    await SendHelpMessage();
                    break;
                default:
                    Console.WriteLine($"⚠️  Неизвестная команда от {_username}: {message.Command}");
                    break;
            }
        }

        private async Task HandleChatMessage(ChatMessage message)
        {
            // Проверяем, не является ли это приватным сообщением
            if (message.Data.StartsWith("/pm "))
            {
                await HandlePrivateMessage(message);
                return;
            }

            // Проверяем команды
            if (message.Data.StartsWith("/"))
            {
                await HandleCommand(message);
                return;
            }

            // Обычное сообщение в чат
            var chatMessage = new ChatMessage(ChatProtocol.Commands.MESSAGE, message.Data, _username);
            await Program.BroadcastMessage(chatMessage);
            
            Console.WriteLine($"💬 {_username}: {message.Data}");
        }

        private async Task HandlePrivateMessage(ChatMessage message)
        {
            var parts = message.Data.Split(' ', 3);
            if (parts.Length < 3)
            {
                await SendMessageAsync(ChatProtocol.CreateErrorMessage("Использование: /pm <пользователь> <сообщение>"));
                return;
            }

            var targetUser = parts[1];
            var privateMessage = parts[2];

            if (targetUser == _username)
            {
                await SendMessageAsync(ChatProtocol.CreateErrorMessage("Нельзя отправить сообщение самому себе"));
                return;
            }

            await Program.SendPrivateMessage(_username, targetUser, privateMessage);
            Console.WriteLine($"🔒 {_username} -> {targetUser}: {privateMessage}");
        }

        private async Task HandleCommand(ChatMessage message)
        {
            var command = message.Data.ToLower();
            
            switch (command)
            {
                case "/help":
                    await SendHelpMessage();
                    break;
                case "/users":
                case "/online":
                    await SendOnlineUsers();
                    break;
                case "/time":
                    await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.SERVER_TIME, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Server"));
                    break;
                default:
                    await SendMessageAsync(ChatProtocol.CreateErrorMessage($"Неизвестная команда: {command}"));
                    break;
            }
        }

        private async Task SendHelpMessage()
        {
            var helpText = "📋 Доступные команды:\n" +
                          "  /help - показать эту справку\n" +
                          "  /users или /online - список онлайн пользователей\n" +
                          "  /time - показать время сервера\n" +
                          "  /pm <пользователь> <сообщение> - отправить приватное сообщение\n" +
                          "  /ping - проверить соединение\n" +
                          "\n💡 Просто введите текст для отправки сообщения в общий чат";

            await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.HELP, helpText, "Server"));
        }

        private async Task SendOnlineUsers()
        {
            var users = Program.GetOnlineUsers();
            var usersText = users.Count == 0 ? "Нет подключенных пользователей" : string.Join(", ", users);
            await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.ONLINE_USERS, usersText, "Server"));
        }

        public async Task SendMessageAsync(ChatMessage message)
        {
            if (!_isConnected || !_tcpClient.Connected) return;

            try
            {
                var data = ChatProtocol.EncodeMessage(message);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки сообщения клиенту {_username}: {ex.Message}");
                _isConnected = false;
            }
        }

        private async Task<ChatMessage?> ReceiveMessageAsync()
        {
            try
            {
                int totalBytesRead = 0;
                int bytesRead;

                // Читаем длину сообщения (4 байта)
                while (totalBytesRead < 4)
                {
                    bytesRead = await _stream.ReadAsync(_buffer, totalBytesRead, 4 - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        return null; // Соединение закрыто
                    }
                    totalBytesRead += bytesRead;
                }

                int messageLength = BitConverter.ToInt32(_buffer, 0);
                if (messageLength <= 0 || messageLength > ChatProtocol.BUFFER_SIZE - 4)
                {
                    throw new InvalidOperationException($"Недопустимая длина сообщения: {messageLength}");
                }

                // Читаем само сообщение
                totalBytesRead = 0;
                while (totalBytesRead < messageLength)
                {
                    bytesRead = await _stream.ReadAsync(_buffer, 4 + totalBytesRead, messageLength - totalBytesRead);
                    if (bytesRead == 0)
                    {
                        return null; // Соединение закрыто
                    }
                    totalBytesRead += bytesRead;
                }

                return ChatProtocol.DecodeMessage(_buffer, 4 + messageLength);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка чтения сообщения от {_username}: {ex.Message}");
                return null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!_isConnected) return;

            _isConnected = false;
            
            try
            {
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отключения клиента {_username}: {ex.Message}");
            }
        }
    }
}
