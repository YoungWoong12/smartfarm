using LiveCharts;
using LiveCharts.Wpf;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace smartfarm_client
{
    public partial class AnalysisWindow : Window
    {
        public class SensorStats
        {
            public string Sensor { get; set; }
            public double Min { get; set; }
            public double Max { get; set; }
            public double Avg { get; set; }
        }

        public AnalysisWindow()
        {
            InitializeComponent();
        }

        private void BtnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (dpStart.SelectedDate == null || dpEnd.SelectedDate == null)
            {
                MessageBox.Show("날짜를 선택하세요.");
                return;
            }

            string start = dpStart.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00");
            string end = dpEnd.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59");

            var A = new List<double>();
            var B = new List<double>();
            var C = new List<double>();

            string connStr = "Server=localhost;Database=smartfarm;Uid=root;Pwd=1234;";

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT sensor_name, temperature FROM temperature_log WHERE created_at BETWEEN @s AND @e";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@s", start);
                    cmd.Parameters.AddWithValue("@e", end);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string sensor = reader["sensor_name"].ToString();
                            double temp = Convert.ToDouble(reader["temperature"]);

                            if (sensor == "A") A.Add(temp);
                            if (sensor == "B") B.Add(temp);
                            if (sensor == "C") C.Add(temp);
                        }
                    }
                }
            }

            var result = new List<SensorStats>
            {
                MakeStats("A", A),
                MakeStats("B", B),
                MakeStats("C", C)
            };

            dgResult.ItemsSource = result;

            pieA.Series = MakePie(CalcRate(A));
            pieB.Series = MakePie(CalcRate(B));
            pieC.Series = MakePie(CalcRate(C));
        }

        private SeriesCollection MakePie(double rate)
        {
            return new SeriesCollection
            {
                new PieSeries { Title = "OK", Values = new ChartValues<double>{ rate }, Fill = System.Windows.Media.Brushes.LightGreen },
                new PieSeries { Title = "NG", Values = new ChartValues<double>{ 100 - rate }, Fill = System.Windows.Media.Brushes.IndianRed }
            };
        }

        private SensorStats MakeStats(string name, List<double> list)
        {
            if (list.Count == 0)
                return new SensorStats { Sensor = name, Min = 0, Max = 0, Avg = 0 };

            return new SensorStats
            {
                Sensor = name,
                Min = list.Min(),
                Max = list.Max(),
                Avg = list.Average()
            };
        }

        private double CalcRate(List<double> list)
        {
            if (list.Count == 0) return 0;
            int ok = list.Count(v => v >= 20 && v <= 30);
            return Math.Round(ok * 100.0 / list.Count, 1);
        }
    }
}
