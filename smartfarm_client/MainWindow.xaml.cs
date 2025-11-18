using client;
using LiveCharts;
using LiveCharts.Wpf;
using ServerConnecting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace smartfarm_client
{
    public partial class MainWindow : Window
    {
<<<<<<< HEAD
        private string ipAddress = "127.0.0.1";
        private int port = 6000;
=======
        public ChartValues<double> TempValues = new ChartValues<double>();
        public List<DateTime> TimeList = new List<DateTime>();
>>>>>>> origin/UI-version

        private JsonClient _jsonClient = new JsonClient();

        public MainWindow()
        {
            InitializeComponent();

<<<<<<< HEAD
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _jsonClient.ConnectAsync(ipAddress, port);
            }
            catch
=======
            TemperatureChart.Series = new SeriesCollection
>>>>>>> origin/UI-version
            {
                new LineSeries
                {
                    Values = TempValues,
                    StrokeThickness = 3,
                    PointGeometrySize = 0,
                    LineSmoothness = 0,
                    Title = "온도"
                }
            };

            TemperatureChart.AxisY.Add(new Axis
            {
                Title = "Temperature",
                Foreground = Brushes.White,
            });

            TemperatureChart.AxisX.Add(new Axis
            {
                Title = "Time",
                Foreground = Brushes.White,
                Labels = new List<string>()
            });

            if (Connect.ConnectToServer("127.0.0.1", 6000))
                StartReceive();
        }

        private async void StartReceive()
        {
            var ns = Connect.Client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int len = await ns.ReadAsync(buffer, 0, buffer.Length);
                    if (len == 0) break;

                    string data = Encoding.UTF8.GetString(buffer, 0, len).Trim();

                    Application.Current.Dispatcher.Invoke(() => UpdateUI(data));
                }
            }
            catch { }
        }

        private void UpdateUI(string temp)
        {
            if (!double.TryParse(temp, out double t)) return;

            txtCurrentTemp.Text = $"{t:F1} ℃";

            TempValues.Add(t);
            TimeList.Add(DateTime.Now);

            TemperatureChart.AxisX[0].Labels.Add(TimeList[^1].ToString("HH:mm:ss"));

            if (t < 20 || t > 30)
                AlarmPanel.Background = new SolidColorBrush(Colors.DarkRed);
            else
                AlarmPanel.Background = new SolidColorBrush(Color.FromRgb(38, 38, 38));
        }

        private void OpenAnalysisWindow_Click(object sender, RoutedEventArgs e)
        {
            new AnalysisWindow().Show();
        }
    }
}
