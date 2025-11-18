using System;
using System.Net.Sockets;

namespace ServerConnecting
{
    public static class Connect
    {
        private static TcpClient _client;
        public static TcpClient Client => _client;

        public static bool ConnectToServer(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ip, port);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                return false;
            }
        }

        public static void Close()
        {
            try { _client?.Close(); } catch { }
        }
    }
}
