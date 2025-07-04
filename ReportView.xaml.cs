using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace F.L.A.M.E
{
    public partial class ReportView : UserControl, INotifyPropertyChanged
    {
        private const string DbFilePath = "SensorLog.db";

        public ReportView(int a)
        {
            InitializeComponent();
            LoadGunNames(a);
            this.DataContext = this;

        }

        private const int PageSize = 100; // You can adjust page size
        private int currentPage = 1;
        private int totalPages;
        private List<SensorDataRow> allSensorData = new(); // Full data fetched once
        public string PageInfoText => $"Page {currentPage} of {totalPages}";

        private void LoadGunNames(int n)
        {
            GunComboBox.Items.Add("All Guns");
            for (int i = 0; i < n; i++)  // Adjust range if needed
            {
                GunComboBox.Items.Add($"Gun {i}");
            }
            GunComboBox.SelectedIndex = 0;
        }
        public ObservableCollection<SensorDataRow> SensorDataList { get; set; } = new();

        public class SensorDataRow
        {
            public DateTime Timestamp { get; set; }
            public string GunName { get; set; }
            public double Temperature { get; set; }
            public double Flow { get; set; }
        }


        private async void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates!", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string gunFilter = GunComboBox.SelectedItem?.ToString() ?? "All Guns";
            selectedGun = gunFilter;
            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

            Mouse.OverrideCursor = Cursors.Wait;
            ProgressBarControl.Visibility = Visibility.Visible;

            try
            {
                await Task.Run(() => FetchSensorDataStreaming(gunFilter, start, end));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
            finally
            {
                ProgressBarControl.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
            }
        }
        private async Task FetchSensorDataStreaming(string gunFilter, DateTime start, DateTime end)
        {
            string connectionString = $"Data Source={DbFilePath};Version=3;";

            using SQLiteConnection conn = new(connectionString);
            await conn.OpenAsync();

            string query = "SELECT Timestamp, GunName, Temperature, Flow FROM SensorData WHERE Timestamp >= @start AND Timestamp <= @end";
            if (gunFilter != "All Guns")
                query += " AND GunName = @gunName";

            using SQLiteCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));
            if (gunFilter != "All Guns")
                cmd.Parameters.AddWithValue("@gunName", gunFilter);

            using SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync();

            allSensorData.Clear();
            while (await reader.ReadAsync())
            {
                var row = new SensorDataRow
                {
                    Timestamp = reader.GetDateTime(0),
                    GunName = reader.GetString(1),
                    Temperature = reader.GetDouble(2),
                    Flow = reader.GetDouble(3)
                };
                allSensorData.Add(row);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                totalPages = (allSensorData.Count / PageSize);
                currentPage = 1;
                OnPropertyChanged(nameof(PageInfoText));
                UpdatePagedView();
                //Debug.WriteLine($"[DEBUG] currentPage={currentPage}, totalPages={totalPages}, PageInfoText={PageInfoText}");

            });

        }

        private void UpdatePagedView()
        {
            SensorDataList.Clear();
            int start = (currentPage - 1) * PageSize;
            foreach (var row in allSensorData.Skip(start).Take(PageSize))
            {
                SensorDataList.Add(row);
            }
            OnPropertyChanged(nameof(PageInfoText));
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                --currentPage;
                UpdatePagedView();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                ++currentPage;
                UpdatePagedView();
            }
        }


        public string selectedGun;
        private DataTable FetchSensorData(string gunFilter, DateTime start, DateTime end)
        {
            DataTable dt = new DataTable();

            string connectionString = $"Data Source={DbFilePath};Version=3;";

            using SQLiteConnection conn = new(connectionString);
            conn.Open();

            string query = "SELECT Timestamp, GunName, Temperature, Flow FROM SensorData WHERE Timestamp >= @start AND Timestamp <= @end";
            if (gunFilter != "All Guns")
            {
                query += " AND GunName = @gunName";
            }
                
            Debug.WriteLine($"Fetching from {start:yyyy-MM-dd HH:mm:ss} to {end:yyyy-MM-dd HH:mm:ss}");
            Debug.WriteLine($"Query: {query}");


            using SQLiteCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));
            if (gunFilter != "All Guns")
                cmd.Parameters.AddWithValue("@gunName", gunFilter);

            using SQLiteDataAdapter adapter = new(cmd);
            adapter.Fill(dt);
            return dt;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates!", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get inputs on UI thread first
            string gunFilter = GunComboBox.SelectedItem?.ToString() ?? "All Guns";
            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
            selectedGun = gunFilter;
            Debug.WriteLine($"DOWNLOAD END DATE: {end}");
            ProgressBarControl.Visibility = Visibility.Visible;
            await Task.Delay(800); // Give time for UI to render
            DataTable data;
            try
            {
                data = await Task.Run(() => FetchSensorData(gunFilter, start, end));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ProgressBarControl.Visibility = Visibility.Collapsed;
                return;
            }

            if (data.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                ProgressBarControl.Visibility = Visibility.Collapsed;
                return;
            }

            string defaultFileName = $"Sensor Data [{selectedGun}] ({start:yyyy-MM-dd} to {end:yyyy-MM-dd}).xlsx";

            // Show save dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                Title = "Save Sensor Data As"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using XLWorkbook workbook = new();
                        
                        var sensordata=workbook.Worksheets.Add(data, "SensorData");
                        sensordata.Columns().AdjustToContents(); // Adjust column widths

                        // Generate summary table
                        DataTable summaryTable = new DataTable();
                        summaryTable.Columns.Add("Date");
                        summaryTable.Columns.Add("GunName");
                        summaryTable.Columns.Add("Max Temperature");
                        summaryTable.Columns.Add("Max Temp Time");
                        summaryTable.Columns.Add("Min Temperature");
                        summaryTable.Columns.Add("Min Temp Time");
                        summaryTable.Columns.Add("Max Flow");
                        summaryTable.Columns.Add("Max Flow Time");
                        summaryTable.Columns.Add("Min Flow");
                        summaryTable.Columns.Add("Min Flow Time");

                        var grouped = data.AsEnumerable()
                            .GroupBy(row => new
                            {
                                Date = DateTime.Parse(row["Timestamp"].ToString()).Date,
                                Gun = row["GunName"].ToString()
                            });

                        foreach (var group in grouped)
                        {
                            var rows = group.ToList();

                            var maxTempRow = rows.OrderByDescending(r => Convert.ToDouble(r["Temperature"])).First();
                            var minTempRow = rows.OrderBy(r => Convert.ToDouble(r["Temperature"])).First();
                            var maxFlowRow = rows.OrderByDescending(r => Convert.ToDouble(r["Flow"])).First();
                            var minFlowRow = rows.OrderBy(r => Convert.ToDouble(r["Flow"])).First();

                            summaryTable.Rows.Add(
                                group.Key.Date.ToString("yyyy-MM-dd"),
                                group.Key.Gun,
                                maxTempRow["Temperature"],
                                maxTempRow["Timestamp"],
                                minTempRow["Temperature"],
                                minTempRow["Timestamp"],
                                maxFlowRow["Flow"],
                                maxFlowRow["Timestamp"],
                                minFlowRow["Flow"],
                                minFlowRow["Timestamp"]
                            );
                        }
                        var summarySheet = workbook.Worksheets.Add(summaryTable, "Summary");

                        // Apply colors
                        var headerRow = summarySheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        int rowCount = summarySheet.RowsUsed().Count();
                        for (int i = 2; i <= rowCount; i++) // Start from row 2 (skip header)
                        {
                            summarySheet.Cell(i, 3).Style.Font.FontColor = XLColor.Red;     // Max Temp
                            summarySheet.Cell(i, 5).Style.Font.FontColor = XLColor.Green;   // Min Temp
                            summarySheet.Cell(i, 7).Style.Font.FontColor = XLColor.Red;     // Max Flow
                            summarySheet.Cell(i, 9).Style.Font.FontColor = XLColor.Green;   // Min Flow
                        }

                        summarySheet.Columns().AdjustToContents();
                        workbook.SaveAs(saveFileDialog.FileName);
                    });


                    MessageBox.Show($"File saved: {saveFileDialog.FileName}", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            ProgressBarControl.Visibility = Visibility.Collapsed;
        }


    }
}


/*using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        public string selectedGun;

        private async void ViewButton_Click(object sender, RoutedEventArgs e)
        {
             // Give time for UI to render
            string gunFilter = GunComboBox.SelectedItem?.ToString() ?? "All Guns";
            selectedGun = gunFilter; // Store the selected gun for later use
            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates !", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ProgressBarControl.Visibility = Visibility.Visible;
            await Task.Delay(800);

            var data = await Task.Run(()=> FetchSensorData(selectedGun,start,end));
            var summary = await Task.Run(() => FetchSummaryData(selectedGun,start,end));
            Dispatcher.Invoke(() =>
            {
                DataGrid.ItemsSource = data.DefaultView;
                SummaryGrid.ItemsSource = summary.DefaultView;
            });
            ProgressBarControl.Visibility = Visibility.Collapsed;
        }

        
        private DataTable FetchSensorData(string gunFilter, DateTime start, DateTime end)
        {
            DataTable dt = new DataTable();

            //string gunFilter = GunComboBox.SelectedItem?.ToString() ?? "All Guns";
            selectedGun = gunFilter; // Store the selected gun for later use

            string connectionString = $"Data Source={DbFilePath};Version=3;";
            using SQLiteConnection conn = new(connectionString);
            conn.Open();

            string query = "SELECT Timestamp, GunName, Temperature, Flow FROM SensorData WHERE Timestamp BETWEEN @start AND @end";

            if (selectedGun != "All Guns")
            {
                query += " AND GunName = @gunName";
            }

            using SQLiteCommand cmd = new(query, conn);
            //DateTime start = StartDatePicker.SelectedDate.Value.Date;
            //DateTime end = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1); // end of day
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));

            if (selectedGun != "All Guns")
            {
                cmd.Parameters.AddWithValue("@gunName", selectedGun);
            }
            using SQLiteDataAdapter adapter = new(cmd);
            adapter.Fill(dt);
            return dt;
        }

        //Added for Summary Report
        private DataTable FetchSummaryData(string gunFilter, DateTime start, DateTime end)
        {
            selectedGun = gunFilter; // Store the selected gun for later use
            DataTable dt = new DataTable();
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Gun Name", typeof(string));
            dt.Columns.Add("Max Temp (°C)", typeof(double));
            dt.Columns.Add("Min Temp (°C)", typeof(double));
            dt.Columns.Add("Max Flow (L/min)", typeof(double));
            dt.Columns.Add("Min Flow (L/min)", typeof(double));

            string summaryDbPath = "SummaryStats.db";
            string connectionString = $"Data Source={summaryDbPath};Version=3;";

            using SQLiteConnection conn = new(connectionString);
            conn.Open();

           // DateTime start = StartDatePicker.SelectedDate.Value.Date;
            //DateTime end = EndDatePicker.SelectedDate.Value.Date;

            string query = @"
        SELECT 
            Date,
            GunId,
            TempMax,
            TempMin,
            FlowMax,
            FlowMin
        FROM GunSummary
        WHERE Date BETWEEN @start AND @end";

            if (selectedGun != "All Guns")
                query += " AND GunId = @gun";

            using SQLiteCommand cmd = new(query, conn);
            cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd"));

            if (selectedGun != "All Guns")
                cmd.Parameters.AddWithValue("@gun", selectedGun);

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = dt.NewRow();
                row["Date"] = reader.GetString(0);
                row["Gun Name"] = reader.GetString(1);
                row["Max Temp (°C)"] = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                row["Min Temp (°C)"] = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                row["Max Flow (L/min)"] = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                row["Min Flow (L/min)"] = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);
                dt.Rows.Add(row);
            }

            return dt;
        }

         private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = StartDatePicker.SelectedDate.Value.Date;
            DateTime end = EndDatePicker.SelectedDate.Value.Date;
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates !", "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
                       
            var data = FetchSensorData(selectedGun,start,end);
            var summary = FetchSummaryData(selectedGun,start,end);

            if (data.Rows.Count == 0 && summary.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }
            string defaultFileName = $"Sensor Data [{selectedGun}] ({start} to {end}).xlsx";

            // Open Save File Dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = defaultFileName,
                Title = "Save Sensor Data As"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ProgressBarControl.Visibility = Visibility.Visible;
                await Task.Run(() =>
                {
                    using XLWorkbook workbook = new();
                    workbook.Worksheets.Add(data, "SensorData");
                    workbook.Worksheets.Add(summary, "SummaryStats");
                    workbook.SaveAs(saveFileDialog.FileName);
                });
                Dispatcher.Invoke(() =>
                {
                    ProgressBarControl.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"File saved: {saveFileDialog.FileName}", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }

    }
}
*/