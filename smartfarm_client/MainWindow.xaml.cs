using System.Windows;
using ServerConnecting;

namespace smartfarm_client
{
    public partial class MainWindow : Window
    {
        private string ipAddress = "127.0.0.1";
        private int port = 6000;

        private JsonClient _jsonClient = new JsonClient();

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _jsonClient.ConnectAsync(ipAddress, port);
            }
            catch
            {
                MessageBox.Show("서버 접속 실패");
            }
        }
    }
}
