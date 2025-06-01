using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;

namespace F.L.A.M.E
{
    public class GunDataEventArgs : EventArgs
    {
        public int GunIndex { get; }
        public float Temperature { get; }
        public float Flow { get; }

        public GunDataEventArgs(int gunIndex, float temperature, float flow)
        {
            GunIndex = gunIndex;
            Temperature = temperature;
            Flow = flow;
        }
    }

    public class PlcReader
    {
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(5);
        private Dictionary<int, (float Temperature, float FlowRate)> _lastSensorValues = new();

        public static PlcReader SharedInstance { get; } = new PlcReader();

        private const string JsonFilePath = "sensor_data.json";
        private readonly System.Timers.Timer pollTimer;
        private int pollInterval = 2000;
        private bool isReading = false;
        public bool IsConnected { get; private set; } = false;
        public event EventHandler<GunDataEventArgs>? OnGunDataUpdated;

        private PlcReader()
        {
            pollTimer = new System.Timers.Timer(pollInterval);
            pollTimer.Elapsed += PollTimerElapsed;
            pollTimer.AutoReset = true;
        }

        public void StartMockPolling()
        {
            pollTimer.Start();
        }

        public void StopMockPolling()
        {
            pollTimer.Stop();
        }

        private async void PollTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (isReading) return;
            isReading = true;
            try
            {
                await ReadJsonAndUpdateAsync();
            }
            finally
            {
                isReading = false;
            }
        }

        private async Task ReadJsonAndUpdateAsync()
        {
            try
            {
                if (!File.Exists(JsonFilePath))
                {
                    IsConnected = false;
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(JsonFilePath);
                var sensorList = JsonSerializer.Deserialize<List<SensorData>>(jsonContent);

                if (sensorList == null || sensorList.Count == 0)
                {
                    IsConnected = false;
                    return;
                }

                bool anyChanged = false;

                foreach (var sensor in sensorList)
                {
                    var index = sensor.GunIndex;
                    var currentTemp = sensor.Temperature;
                    var currentFlow = sensor.FlowRate;

                    // Check if value changed since last update
                    if (!_lastSensorValues.TryGetValue(index, out var last) ||
                        last.Temperature != currentTemp || last.FlowRate != currentFlow)
                    {
                        _lastSensorValues[index] = (currentTemp, currentFlow);
                        OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(index, currentTemp, currentFlow));
                        anyChanged = true;
                    }
                }

                if (anyChanged)
                {
                    _lastUpdateTime = DateTime.Now;
                    IsConnected = true;
                }
                else
                {
                    // If no data change for more than timeout, mark as disconnected
                    if (DateTime.Now - _lastUpdateTime > timeout)
                    {
                        IsConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading sensor_data.json: {ex.Message}");
                IsConnected = false;
            }
        }


        private class SensorData
        {
            [JsonPropertyName("gunIndex")]
            public int GunIndex { get; set; }

            [JsonPropertyName("timestamp")]
            public string Timestamp { get; set; } = string.Empty;

            [JsonPropertyName("flowRate")]
            public float FlowRate { get; set; }

            [JsonPropertyName("temperature")]
            public float Temperature { get; set; }
        }
    }
}


/*using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace F.L.A.M.E
{
    public class GunDataEventArgs : EventArgs
    {
        public int GunIndex { get; }
        public float Temperature { get; }
        public float Flow { get; }

        public GunDataEventArgs(int gunIndex, float temperature, float flow)
        {
            GunIndex = gunIndex;
            Temperature = temperature;
            Flow = flow;
        }
    }

    public class PlcReader
    {
        public static PlcReader SharedInstance { get; } = new PlcReader();

        private string ipAddress => SettingsView.IPaddress;
        private int pollInterval = 1500;

        public bool IsConnected { get; private set; } = false;
        public event EventHandler<GunDataEventArgs>? OnGunDataUpdated;

        private Dictionary<int, (string TempTag, string FlowTag)> CreateGunTagMap()
        {
            var dict = new Dictionary<int, (string TempTag, string FlowTag)>();
            int count = SettingsView.GunsCount;
            for (int i = 1; i < count; i++)
            {
                dict[i] = ($"Sensor_Temp_Flow[{i}].Temp_Out", $"Sensor_Temp_Flow[{i}].Flow_Out");
            }
            return dict;
        }

        private Dictionary<int, (string TempTag, string FlowTag)> gunTagMap => CreateGunTagMap();

        private float? TryReadTag(string tagName)
        {
            MessageBox.Show($"Reading tag '{tagName}' from PLC at {ipAddress}...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            var tag = new Tag<RealPlcMapper,float>
            {
                Name = tagName,
                Gateway = "192.168.1.1",
                Path = "1,0",
                PlcType = PlcType.ControlLogix,
                Protocol = Protocol.ab_eip,
                Timeout = TimeSpan.FromSeconds(5)
            };

            try
            {
                tag.Read();
                MessageBox.Show($"Tag Value'{tagName}':{tag.Value}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return tag.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error reading tag '{tagName}': {ex.Message}");
                return null;
            }
        }

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            int i = 0;
            while (i < 5 && !cancellationToken.IsCancellationRequested)
            {
                bool anySuccessful = false;

                foreach (var (gunIndex, (tempTag, flowTag)) in gunTagMap)
                {
                    MessageBox.Show($"Reading data FOR Gun {gunIndex}...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    var temp = TryReadTag(tempTag);
                    var flow = TryReadTag(flowTag);

                    if (temp.HasValue && flow.HasValue)
                    {
                        MessageBox.Show($"Gun {gunIndex}: Temp={temp.Value}°C, Flow={flow.Value} L/min", "Gun Data", MessageBoxButton.OK, MessageBoxImage.Information);

                        OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp.Value, flow.Value));
                        anySuccessful = true;
                        i++;
                    }
                }

                IsConnected = anySuccessful;
                await Task.Delay(pollInterval, cancellationToken);
            }

            IsConnected = false;
        }
        private readonly Random _random = new();
        private void SimulateMockData(int gunIndex)
        {
            float temp = (float)(_random.NextDouble() * (50 - 15) + 15); // 15 to 50 °C
            float flow = (float)(_random.NextDouble() * (30 - 5) + 5);   // 5 to 30 L/min
            OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp, flow));
            IsConnected = true;
        }
    }
}
*/