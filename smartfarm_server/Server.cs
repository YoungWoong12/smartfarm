using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;   // ★ MySql.Data NuGet 패키지 필요

namespace smartfarm_Server
{
    public class Server
    {
        // ===== TCP 관련 필드 =====
        private readonly int _tcpPort;                      // WPF 클라이언트랑 통신할 포트
        private TcpListener _listener;                      // TCP 접속을 받는 리스너
        private readonly List<TcpClient> _clients = new List<TcpClient>();  // 접속 중인 클라이언트 목록 (여러 개 가능)
        private readonly object _lock = new object();       // 클라이언트 리스트 보호용 lock

        // ===== 아두이노 시리얼 관련 필드 =====
        private readonly string _serialPortName;            // 아두이노가 연결된 COM 포트 이름 (예: "COM3")
        private readonly int _baudRate;                     // 아두이노 보드와 맞춘 보레이트 (예: 9600)
        private SerialPort _serialPort;                     // 시리얼 포트 객체

        // ===== DB 관련 필드 =====
        // 본인 PC 환경에 맞게 수정
        private readonly string _connStr =
            "Server=localhost;Database=smartfarm;Uid=root;Pwd=1234;";

        // ===== 생성자: TCP 포트 + 시리얼 설정 =====
        public Server(int tcpPort, string serialPortName, int baudRate = 9600)
        {
            _tcpPort = tcpPort;
            _serialPortName = serialPortName;
            _baudRate = baudRate;
        }

        // ===== 서버 시작: TCP 리슨 + 시리얼 오픈 =====
        public async Task StartAsync()
        {
            // 1) 시리얼 포트 오픈 (아두이노 연결)
            OpenSerialPort();

            // 2) TCP 서버 시작 (클라이언트 접속 대기)
            _listener = new TcpListener(IPAddress.Any, _tcpPort);
            _listener.Start();
            Console.WriteLine($"[SERVER] Tcp 서버 가동  Port={_tcpPort}");

            // 3) 클라이언트 Accept 무한 루프
            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Console.WriteLine("[SERVER] 클라 연결 .");

                lock (_lock)
                {
                    _clients.Add(client);
                }

                _ = HandleClientAsync(client);  // 클라 하나씩 따로 처리
            }
        }

        // ===== 아두이노 시리얼 포트 열기 =====
        private void OpenSerialPort()
        {
            try
            {
                _serialPort = new SerialPort(_serialPortName, _baudRate);
                _serialPort.Encoding = Encoding.UTF8;
                _serialPort.NewLine = "\n";                   // 아두이노에서 println 기준
                _serialPort.DataReceived += SerialDataReceived;// 데이터 들어올 때 이벤트
                _serialPort.Open();

                Console.WriteLine($"[SERVER] 시리얼 연결  {_serialPortName} @ {_baudRate}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] 시리얼 연결 실패 : {ex.Message}");
            }
        }

        // ===== 아두이노에서 데이터 들어왔을 때 호출되는 이벤트 핸들러 =====
        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = _serialPort.ReadLine().Trim();      // 예: "24.6"
                Console.WriteLine($"[SERVER] 온도 : {line}");

                // 1) DB 저장
                if (float.TryParse(line, out float temp))
                {
                    // 센서 하나만 쓰면 "A" 고정, 나중에 여러 개면 구분 문자열 넘기면 됨
                    SaveTemperatureToDB("A", temp);
                }

                // 2) 클라이언트로 실시간 전송
                BroadcastToClients(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] 아두이노 읽기 실패 : {ex.Message}");
            }
        }

        // ===== DB에 현재 온도 저장 =====
        private void SaveTemperatureToDB(string sensor, float temp)
        {
            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql =
                        "INSERT INTO temperature_log(sensor_name, temperature) " +
                        "VALUES(@sensor, @temp)";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sensor", sensor);
                        cmd.Parameters.AddWithValue("@temp", temp);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] DB 저장 실패 : {ex.Message}");
            }
        }

        // ===== 특정 기간 온도 조회 (히스토리 요청 처리용) =====
        // 클라이언트에서 "REQ:SELECT:시작시간|끝시간" 형식으로 보낸다고 가정
        private string QueryTemperature(string start, string end)
        {
            var rows = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(_connStr))
                {
                    conn.Open();
                    string sql =
                        "SELECT sensor_name, temperature, created_at " +
                        "FROM temperature_log " +
                        "WHERE created_at BETWEEN @s AND @e " +
                        "ORDER BY created_at ASC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@s", start);
                        cmd.Parameters.AddWithValue("@e", end);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // "A,24.6,2025-01-08 03:15:00" 이런 식으로 한 줄 구성
                                string row =
                                    $"{reader["sensor_name"]},{reader["temperature"]},{reader["created_at"]}";
                                rows.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] DB 조회 실패 : {ex.Message}");
            }

            // 여러 줄을 '|' 로 이어서 보냄
            return string.Join("|", rows);
        }

        // ===== 접속 중인 모든 WPF 클라이언트에게 메시지 전송 =====
        private void BroadcastToClients(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n"); // 한 줄 단위로 보내기

            lock (_lock)
            {
                List<TcpClient> dead = new List<TcpClient>(); // 끊어진 클라 모아두는 리스트

                foreach (var client in _clients)
                {
                    try
                    {
                        NetworkStream ns = client.GetStream();
                        ns.Write(data, 0, data.Length);
                        ns.Flush();
                    }
                    catch
                    {
                        // 전송 실패한 클라는 나중에 제거
                        dead.Add(client);
                    }
                }

                // 끊어진 클라 정리
                foreach (var d in dead)
                {
                    _clients.Remove(d);
                    try { d.Close(); } catch { }
                }
            }
        }

        // ===== WPF 클라이언트 개별 처리 (요청 수신) =====
        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream ns = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int len = await ns.ReadAsync(buffer, 0, buffer.Length);
                    if (len == 0) break; // 클라 종료

                    string msg = Encoding.UTF8.GetString(buffer, 0, len).Trim();
                    Console.WriteLine($"[SERVER] 클라에서 수신 : {msg}");

                    // ★ 데이터 조회 요청 처리
                    // 포맷 예시: "REQ:SELECT:2025-01-08 00:00:00|2025-01-08 06:00:00"
                    if (msg.StartsWith("REQ:SELECT:"))
                    {
                        string payload = msg.Substring("REQ:SELECT:".Length);
                        string[] range = payload.Split('|');
                        if (range.Length == 2)
                        {
                            string start = range[0];
                            string end = range[1];

                            string result = QueryTemperature(start, end);

                            byte[] outBytes = Encoding.UTF8.GetBytes(result + "\n");
                            await ns.WriteAsync(outBytes, 0, outBytes.Length);
                            await ns.FlushAsync();
                        }
                    }
                    else
                    {
                        // 필요하면 다른 명령 처리
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER] 클라 예외 : {ex.Message}");
            }

            Console.WriteLine("[SERVER] 클라 종료 .");
            lock (_lock)
            {
                _clients.Remove(client);
            }
            client.Close();
        }

        // ===== 서버 종료 시 정리 =====
        public void Stop()
        {
            try
            {
                _listener?.Stop();
            }
            catch { }

            try
            {
                _serialPort?.Close();
            }
            catch { }

            lock (_lock)
            {
                foreach (var c in _clients)
                {
                    try { c.Close(); } catch { }
                }
                _clients.Clear();
            }
        }
    }
}
