using System;
using System.Threading.Tasks;

namespace smartfarm_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // ===== 여기서 TCP 포트 + 아두이노 COM 포트 설정 =====
            int tcpPort = 6000;          // WPF 클라랑 맞춰야 하는 포트
            string comPort = "COM3";     // 실제 아두이노가 연결된 포트로 바꿔줘야 함 (예: COM4, COM5 등)

            Server server = new Server(tcpPort, comPort);

            // 비동기로 서버 시작
            Task.Run(async () => await server.StartAsync()).GetAwaiter().GetResult();

            // (필요하면 종료 키 대기 로직 넣을 수도 있음)
            Console.WriteLine("서버 종료");
        }
    }
}
