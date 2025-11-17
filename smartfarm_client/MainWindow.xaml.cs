using System.Windows;
using ServerConnecting;

namespace smartfarm_client
{
    public partial class MainWindow : Window
    {
        // 접속할 서버 정보
        private string ipAddress = "127.0.0.1";
        private int port = 6000;

        public MainWindow()
        {
            InitializeComponent();

            // --------------------------------------
            // 프로그램 실행 시 서버 접속 시도
            // --------------------------------------
            bool ok = Connect.ConnectToServer(ipAddress, port);

            // 접속 실패 시 메시지 출력
            if (!ok)
            {
                MessageBox.Show("서버 접속 실패");
            }
        }
    }
}
