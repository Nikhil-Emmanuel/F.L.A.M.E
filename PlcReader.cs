using libplctag;
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
            for (int i = 0; i < count; i++)
            {
                dict[i] = ($"Sensor_Temp_Flow[{i}].Temp_Out", $"Sensor_Temp_Flow[{i}].Flow_Out");
            }
            return dict;
        }

        private Dictionary<int, (string TempTag, string FlowTag)> gunTagMap => CreateGunTagMap();

        private async Task<float?> TryReadTagAsync(string tagName)
        {
            var tag = new Tag<RealPlcMapper, float>
            {
                Name = tagName,
                Gateway = ipAddress,
                Path = "1,0",
                PlcType = PlcType.ControlLogix,
                Protocol = Protocol.ab_eip,
                Timeout = TimeSpan.FromSeconds(2)
            };

            try
            {
                await tag.ReadAsync();
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
            int i=0;
            while (i<5 && !cancellationToken.IsCancellationRequested)
            {
                bool anySuccessful = false;

                foreach (var (gunIndex, (tempTag, flowTag)) in gunTagMap)
                {
                    //SimulateMockData(gunIndex); // Simulate data for testing
                    //anySuccessful = true;
                    //continue;
                    MessageBox.Show($"Reading data for Gun {gunIndex}...", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    var tempTask = TryReadTagAsync(tempTag);
                    var flowTask = TryReadTagAsync(flowTag);

                    await Task.WhenAll(tempTask, flowTask);

                   // if (tempTask.Result.HasValue && flowTask.Result.HasValue)
                    //{
                        float temp = tempTask.Result.Value;
                        float flow = flowTask.Result.Value;
                        MessageBox.Show($"Gun {gunIndex}: Temp={temp}°C, Flow={flow} L/min", "Gun Data", MessageBoxButton.OK, MessageBoxImage.Information);
                        i++;

                    OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp, flow));
                        anySuccessful = true;
                    //}
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
