using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChatCommon;

namespace ChatServer
{
    class Program
    {
        private static TcpListener? _server;
        private static readonly Dictionary<string, ClientHandler> _clients = new();
        private static readonly object _clientsLock = new();
        private static bool _isRunning = false;
        private static readonly CancellationTokenSource _cancellationTokenSource = new();

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    🚀 СЕРВЕР ЧАТА 🚀                        ║");
            Console.WriteLine("║                                                              ║");
            Console.WriteLine("║  Демонстрация работы с сокетами и TCP/IP протоколами        ║");
            Console.WriteLine("║  Поддержка множественных клиентов и интерактивного чата     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            try
            {
                await StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка сервера: {ex.Message}");
            }
            finally
            {
                await StopServer();
            }
        }

        private static async Task StartServer()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, ChatProtocol.PORT);
                _server.Start();
                _isRunning = true;

                Console.WriteLine($"✅ Сервер запущен на порту {ChatProtocol.PORT}");
                Console.WriteLine($"🌐 IP адрес: {GetLocalIPAddress()}");
                Console.WriteLine($"📡 Ожидание подключений...");
                Console.WriteLine();
                Console.WriteLine("💡 Команды сервера:");
                Console.WriteLine("   /help - показать справку");
                Console.WriteLine("   /clients - список подключенных клиентов");
                Console.WriteLine("   /stop - остановить сервер");
                Console.WriteLine("   /broadcast <сообщение> - отправить сообщение всем");
                Console.WriteLine();

                // Запускаем обработку команд сервера в отдельном потоке
                _ = Task.Run(HandleServerCommands);

                // Основной цикл принятия подключений
                while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await _server.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleNewClient(tcpClient));
                    }
                    catch (ObjectDisposedException)
                    {
                        // Сервер был остановлен
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                        {
                            Console.WriteLine($"⚠️  Ошибка при принятии подключения: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                throw;
            }
        }

        private static async Task HandleNewClient(TcpClient tcpClient)
        {
            ClientHandler? clientHandler = null;
            try
            {
                clientHandler = new ClientHandler(tcpClient);
                await clientHandler.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки клиента: {ex.Message}");
            }
            finally
            {
                if (clientHandler != null)
                {
                    await RemoveClient(clientHandler);
                }
            }
        }

        public static async Task AddClient(ClientHandler clientHandler)
        {
            lock (_clientsLock)
            {
                _clients[clientHandler.Username] = clientHandler;
            }

            Console.WriteLine($"✅ Пользователь '{clientHandler.Username}' подключился");
            Console.WriteLine($"📊 Всего подключено: {_clients.Count}");

            // Уведомляем всех о новом пользователе
            await BroadcastMessage(ChatProtocol.CreateUserJoinedMessage(clientHandler.Username));
        }

        public static async Task RemoveClient(ClientHandler clientHandler)
        {
            bool wasRemoved = false;
            lock (_clientsLock)
            {
                wasRemoved = _clients.Remove(clientHandler.Username);
            }

            if (wasRemoved)
            {
                Console.WriteLine($"❌ Пользователь '{clientHandler.Username}' отключился");
                Console.WriteLine($"📊 Всего подключено: {_clients.Count}");

                // Уведомляем всех об отключении пользователя
                await BroadcastMessage(ChatProtocol.CreateUserLeftMessage(clientHandler.Username));
            }
        }

        public static async Task BroadcastMessage(ChatMessage message)
        {
            List<ClientHandler> clientsToNotify;
            lock (_clientsLock)
            {
                clientsToNotify = new List<ClientHandler>(_clients.Values);
            }

            var tasks = clientsToNotify.Select(client => client.SendMessageAsync(message));
            await Task.WhenAll(tasks);
        }

        public static async Task SendPrivateMessage(string fromUser, string toUser, string message)
        {
            ClientHandler? targetClient;
            lock (_clientsLock)
            {
                _clients.TryGetValue(toUser, out targetClient);
            }

            if (targetClient != null)
            {
                var privateMessage = ChatProtocol.CreatePrivateMessage(fromUser, toUser, message);
                await targetClient.SendMessageAsync(privateMessage);
            }
        }

        public static List<string> GetOnlineUsers()
        {
            lock (_clientsLock)
            {
                return new List<string>(_clients.Keys);
            }
        }

        private static async Task HandleServerCommands()
        {
            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input)) continue;

                    var parts = input.Split(' ', 2);
                    var command = parts[0].ToLower();

                    switch (command)
                    {
                        case "/help":
                            ShowHelp();
                            break;
                        case "/clients":
                            ShowClients();
                            break;
                        case "/stop":
                            await StopServer();
                            break;
                        case "/broadcast":
                            if (parts.Length > 1)
                            {
                                await BroadcastMessage(ChatProtocol.CreateUserMessage("Server", parts[1]));
                                Console.WriteLine($"📢 Сообщение отправлено всем клиентам");
                            }
                            else
                            {
                                Console.WriteLine("❌ Использование: /broadcast <сообщение>");
                            }
                            break;
                        default:
                            Console.WriteLine($"❌ Неизвестная команда: {command}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка обработки команды: {ex.Message}");
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("📋 Доступные команды сервера:");
            Console.WriteLine("   /help - показать эту справку");
            Console.WriteLine("   /clients - список подключенных клиентов");
            Console.WriteLine("   /stop - остановить сервер");
            Console.WriteLine("   /broadcast <сообщение> - отправить сообщение всем клиентам");
            Console.WriteLine();
        }

        private static void ShowClients()
        {
            var users = GetOnlineUsers();
            Console.WriteLine();
            Console.WriteLine($"👥 Подключенные клиенты ({users.Count}):");
            if (users.Count == 0)
            {
                Console.WriteLine("   Нет подключенных клиентов");
            }
            else
            {
                foreach (var user in users)
                {
                    Console.WriteLine($"   • {user}");
                }
            }
            Console.WriteLine();
        }

        private static async Task StopServer()
        {
            if (!_isRunning) return;

            Console.WriteLine();
            Console.WriteLine("🛑 Остановка сервера...");

            _isRunning = false;
            _cancellationTokenSource.Cancel();

            // Отключаем всех клиентов
            List<ClientHandler> clientsToDisconnect;
            lock (_clientsLock)
            {
                clientsToDisconnect = new List<ClientHandler>(_clients.Values);
            }

            foreach (var client in clientsToDisconnect)
            {
                await client.DisconnectAsync();
            }

            _server?.Stop();
            Console.WriteLine("✅ Сервер остановлен");
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }
    }
}
