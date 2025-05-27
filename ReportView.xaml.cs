using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace F.L.A.M.E
{
    public partial class ReportView : UserControl
    {
        private const string DbFilePath = "SensorLog.db";

        public ReportView(int a)
        {
            InitializeComponent();
            LoadGunNames(a);
        }

        private void LoadGunNames(int n)
        {
            GunComboBox.Items.Add("All Guns");
            for (int i = 0; i < n; i++)  // Adjust range if needed
            {
                GunComboBox.Items.Add($"Gun {i}");
            }
            GunComboBox.SelectedIndex = 0;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates !","Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = FetchSensorData();
            DataGrid.ItemsSource = data.DefaultView;
        }

        private DataTable FetchSensorData()
        {
            DataTable dt = new DataTable();

            string gunFilter = GunComboBox.SelectedItem?.ToString() ?? "All Guns";
            

            string connectionString = $"Data Source={DbFilePath};Version=3;";
            using SQLiteConnection conn = new(connectionString);
            conn.Open();

            string query = "SELECT Timestamp, GunName, Temperature, Flow FROM SensorData WHERE Timestamp BETWEEN @start AND @end";

            if (gunFilter != "All Guns")
            {
                query += " AND GunName = @gunName";
            }

            using SQLiteCommand cmd = new(query, conn);
            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1); // end of day
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

            if (gunFilter != "All Guns")
            {
                cmd.Parameters.AddWithValue("@gunName", gunFilter);
            }

            using SQLiteDataAdapter adapter = new(cmd);
            adapter.Fill(dt);
            return dt;
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var data = FetchSensorData();
            if (data.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            string start = StartDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "start";
            string end = EndDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "end";
            string defaultFileName = $"Sensor Data ({start} to {end}).xlsx";

            // Open Save File Dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                Title = "Save Sensor Data As"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using XLWorkbook workbook = new();
                workbook.Worksheets.Add(data, "SensorData");
                workbook.SaveAs(saveFileDialog.FileName);

                MessageBox.Show($"File saved: {saveFileDialog.FileName}","Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}
