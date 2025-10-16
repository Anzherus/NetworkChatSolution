using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatCommon;

namespace ChatClient
{
    class Program
    {
        private static TcpClient? _tcpClient;
        private static NetworkStream? _stream;
        private static bool _isConnected = false;
        private static string _username = string.Empty;
        private static readonly byte[] _buffer = new byte[ChatProtocol.BUFFER_SIZE];
        private static readonly CancellationTokenSource _cancellationTokenSource = new();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            ShowWelcomeScreen();

            try
            {
                await ConnectToServer();
                if (_isConnected)
                {
                    await StartChat();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
            }
            finally
            {
                await DisconnectFromServer();
            }

            Console.WriteLine("\n👋 До свидания! Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void ShowWelcomeScreen()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    💬 КЛИЕНТ ЧАТА 💬                        ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  Демонстрация работы с сокетами и TCP/IP протоколами        ║");
            Console.WriteLine("║  Интерактивное взаимодействие пользователей в реальном времени ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
        }

        private static async Task ConnectToServer()
        {
            try
            {
                Console.WriteLine("🔌 Подключение к серверу...");
                
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ChatProtocol.DEFAULT_SERVER, ChatProtocol.PORT);
                _stream = _tcpClient.GetStream();
                _isConnected = true;

                Console.WriteLine("✅ Подключение установлено!");
                Console.WriteLine();

                // Аутентификация
                await Authenticate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                _isConnected = false;
            }
        }

        private static async Task Authenticate()
        {
            while (true)
            {
                Console.Write("👤 Введите ваше имя: ");
                var username = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("❌ Имя не может быть пустым!");
                    continue;
                }

                _username = username;

                try
                {
                    // Отправляем запрос аутентификации
                    var authMessage = ChatProtocol.CreateAuthRequest(_username);
                    await SendMessageAsync(authMessage);

                    // Ждем ответ от сервера
                    var response = await ReceiveMessageAsync();
                    if (response == null)
                    {
                        Console.WriteLine("❌ Сервер не отвечает");
                        _isConnected = false;
                        return;
                    }

                    if (response.Command == ChatProtocol.Commands.ERROR)
                    {
                        Console.WriteLine($"❌ {response.Data}");
                        continue;
                    }
                    else if (response.Command == ChatProtocol.Commands.SUCCESS)
                    {
                        Console.WriteLine($"✅ {response.Data}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка аутентификации: {ex.Message}");
                    _isConnected = false;
                    return;
                }
            }
        }

        private static async Task StartChat()
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        🚀 ЧАТ АКТИВЕН 🚀                    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            ShowHelp();

            // Запускаем задачу для получения сообщений
            var receiveTask = Task.Run(ReceiveMessagesLoop);

            // Основной цикл ввода сообщений
            while (_isConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Console.Write($"{_username}> ");
                    var input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input)) continue;

                    if (input.ToLower() == "/exit" || input.ToLower() == "/quit")
                    {
                        break;
                    }

                    await ProcessUserInput(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обработки ввода: {ex.Message}");
                }
            }

            _cancellationTokenSource.Cancel();
            await receiveTask;
        }

        private static async Task ProcessUserInput(string input)
        {
            if (input.StartsWith("/"))
            {
                await HandleCommand(input);
            }
            else
            {
                // Обычное сообщение
                var message = ChatProtocol.CreateUserMessage(_username, input);
                await SendMessageAsync(message);
            }
        }

        private static async Task HandleCommand(string command)
        {
            var parts = command.Split(' ', 2);
            var cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "/help":
                    ShowHelp();
                    break;
                case "/users":
                case "/online":
                    await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.HELP, "/users", _username));
                    break;
                case "/time":
                    await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.HELP, "/time", _username));
                    break;
                case "/ping":
                    await SendMessageAsync(new ChatMessage(ChatProtocol.Commands.PING, "ping", _username));
                    break;
                case "/pm":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("❌ Использование: /pm <пользователь> <сообщение>");
                        return;
                    }
                    var message = ChatProtocol.CreateUserMessage(_username, command);
                    await SendMessageAsync(message);
                    break;
                default:
                    Console.WriteLine("❌ Неизвестная команда. Введите /help для справки");
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("📋 Доступные команды:");
            Console.WriteLine("  /help - показать эту справку");
            Console.WriteLine("  /users или /online - список онлайн пользователей");
            Console.WriteLine("  /time - показать время сервера");
            Console.WriteLine("  /pm <пользователь> <сообщение> - отправить приватное сообщение");
            Console.WriteLine("  /ping - проверить соединение");
            Console.WriteLine("  /exit или /quit - выйти из чата");
            Console.WriteLine();
            Console.WriteLine("💡 Просто введите текст для отправки сообщения в общий чат");
            Console.WriteLine();
        }

        private static async Task ReceiveMessagesLoop()
        {
            while (_isConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var message = await ReceiveMessageAsync();
                    if (message == null)
                    {
                        Console.WriteLine("\n❌ Соединение с сервером потеряно");
                        _isConnected = false;
                        break;
                    }

                    DisplayMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Ошибка получения сообщения: {ex.Message}");
                    _isConnected = false;
                    break;
                }
            }
        }

        private static void DisplayMessage(ChatMessage message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var color = GetMessageColor(message.Command, message.Sender);

            Console.ForegroundColor = color;

            switch (message.Command)
            {
                case ChatProtocol.Commands.MESSAGE:
                    if (message.Sender == _username)
                    {
                        Console.WriteLine($"[{timestamp}] Вы: {message.Data}");
                    }
                    else if (message.Sender == "System")
                    {
                        Console.WriteLine($"🔔 [{timestamp}] {message.Data}");
                    }
                    else
                    {
                        Console.WriteLine($"[{timestamp}] {message.Sender}: {message.Data}");
                    }
                    break;

                case ChatProtocol.Commands.USER_JOINED:
                    Console.WriteLine($"🟢 [{timestamp}] {message.Data}");
                    break;

                case ChatProtocol.Commands.USER_LEFT:
                    Console.WriteLine($"🔴 [{timestamp}] {message.Data}");
                    break;

                case ChatProtocol.Commands.ONLINE_USERS:
                    Console.WriteLine($"👥 [{timestamp}] Онлайн: {message.Data}");
                    break;

                case ChatProtocol.Commands.SERVER_TIME:
                    Console.WriteLine($"🕐 [{timestamp}] Время сервера: {message.Data}");
                    break;

                case ChatProtocol.Commands.ERROR:
                    Console.WriteLine($"❌ [{timestamp}] Ошибка: {message.Data}");
                    break;

                case ChatProtocol.Commands.SUCCESS:
                    Console.WriteLine($"✅ [{timestamp}] {message.Data}");
                    break;

                case ChatProtocol.Commands.PONG:
                    Console.WriteLine($"🏓 [{timestamp}] Pong от сервера");
                    break;

                case ChatProtocol.Commands.HELP:
                    Console.WriteLine($"\n📋 [{timestamp}] Справка от сервера:");
                    Console.WriteLine(message.Data);
                    break;

                default:
                    Console.WriteLine($"[{timestamp}] {message.Sender}: {message.Data}");
                    break;
            }

            Console.ResetColor();
        }

        private static ConsoleColor GetMessageColor(string command, string sender)
        {
            return command switch
            {
                ChatProtocol.Commands.ERROR => ConsoleColor.Red,
                ChatProtocol.Commands.SUCCESS => ConsoleColor.Green,
                ChatProtocol.Commands.USER_JOINED => ConsoleColor.Green,
                ChatProtocol.Commands.USER_LEFT => ConsoleColor.Red,
                ChatProtocol.Commands.ONLINE_USERS => ConsoleColor.Cyan,
                ChatProtocol.Commands.SERVER_TIME => ConsoleColor.Yellow,
                ChatProtocol.Commands.HELP => ConsoleColor.Magenta,
                ChatProtocol.Commands.PONG => ConsoleColor.Blue,
                _ when sender == _username => ConsoleColor.Gray,
                _ when sender == "System" => ConsoleColor.Yellow,
                _ => ConsoleColor.White
            };
        }

        private static async Task SendMessageAsync(ChatMessage message)
        {
            if (!_isConnected || _stream == null) return;

            try
            {
                var data = ChatProtocol.EncodeMessage(message);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки сообщения: {ex.Message}");
                _isConnected = false;
            }
        }

        private static async Task<ChatMessage?> ReceiveMessageAsync()
        {
            if (!_isConnected || _stream == null) return null;

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
                Console.WriteLine($"❌ Ошибка чтения сообщения: {ex.Message}");
                return null;
            }
        }

        private static async Task DisconnectFromServer()
        {
            _isConnected = false;
            _cancellationTokenSource.Cancel();

            try
            {
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отключения: {ex.Message}");
            }
        }
    }
}
