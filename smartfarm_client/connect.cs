using System;
using System.Net.Sockets;

namespace ServerConnecting
{
    public static class Connect
    {
        // 서버와 연결된 TCP 클라이언트 객체
        private static TcpClient _client;

        // 외부에서 TcpClient를 쓸 수 있도록 제공(읽기전용)
        public static TcpClient Client => _client;

        // -----------------------------
        // 서버 접속 시도 (성공/실패 반환)
        // -----------------------------
        public static bool ConnectToServer(string ipAddress, int port)
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(ipAddress, port);  // 서버 접속

                return true;                      // 성공
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 실패 : {ex.Message}");
                return false;                     // 실패
            }
        }

        // -----------------------------
        // 서버 연결 종료
        // -----------------------------
        public static void Close()
        {
            try
            {
                _client?.Close();
            }
            catch { }
        }
    }
}