using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace smartfarm_client
{
    public enum Category
    {
        Temp = 1,
        Energy = 2
    }

    public class MyData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class JsonClient
    {
        private TcpClient _client;

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            Console.WriteLine("[CLIENT] 연결.");

            // 서버에서 오는 JSON 계속 받는 루프 시작
            _ = ReceiveLoop();
        }

        private async Task ReceiveLoop()
        {
            NetworkStream ns = _client.GetStream();
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int bytes = await ns.ReadAsync(buffer, 0, buffer.Length);
                if (bytes == 0) break;   // 서버 끊김

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytes));

                // \n 단위로 메시지 자르기
                while (sb.ToString().Contains("\n"))
                {
                    string all = sb.ToString();
                    int idx = all.IndexOf("\n");
                    string json = all.Substring(0, idx);
                    sb.Remove(0, idx + 1);

                    // JSON → MyData 로 파싱
                    var data = JsonConvert.DeserializeObject<MyData>(json);

                    Console.WriteLine($"[CLIENT] JSON 받음 → Id={data.Id}, Name={data.Name}");
                    if (float.TryParse(data.Name, out float temp))
                    {
                        Console.WriteLine($"[CLIENT] Temp(float) = {temp}");
                    }
                    else
                    {
                        Console.WriteLine("[CLIENT] 온도값 파싱 실패");
                    }
                    // TODO: 여기서 WPF UI 갱신해도 됨 (라벨에 표시 등)
                }
            }
        }
    }
}
