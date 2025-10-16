using System.Net;
using System.Net.Sockets;

namespace ChatCommon
{
    public static class NetworkHelper
    {
        public static string GetLocalIPAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                foreach (IPAddress address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения IP адреса: {ex.Message}");
            }

            return "127.0.0.1";
        }

        public static bool IsPortAvailable(int port)
        {
            try
            {
                using var tester = new TcpListener(IPAddress.Loopback, port);
                tester.Start();
                tester.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int FindFreePort(int startPort = 12345)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1;
        }

        public static bool IsValidIPAddress(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }
    }
}