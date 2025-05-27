using PlcTag;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly string ipAddress = SettingsView.IPaddress;
        private readonly int pollInterval = 1500; // 1 second


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

        private PlcController? controller;
        private readonly Dictionary<string, Tag<float>> tagCache = new();
        public bool IsConnected { get; private set; } = false;

        public event EventHandler<GunDataEventArgs>? OnGunDataUpdated;

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            controller = new PlcController(ipAddress, "0", CPUType.LGX);
            try
            {
                Console.WriteLine("BEFORE LOOOOOOPPPPPPP");
                // Create and connect all tags
                foreach (var (tempTag, flowTag) in gunTagMap.Values)
                {
                    var temp = controller.CreateTag<float>(tempTag);
                    var flow = controller.CreateTag<float>(flowTag);
                    //temp.Connect();
                    //flow.Connect();
                    tagCache[tempTag] = temp;
                    tagCache[flowTag] = flow;
                }
                IsConnected = true;
            }
            catch (TagOperationException ex)
            {
                Console.WriteLine($"Error connecting to PLC: {ex.Message}");
                IsConnected = false;
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var gun in gunTagMap)
                {
                    int gunIndex = gun.Key;
                    string tempTagName = gun.Value.TempTag;
                    string flowTagName = gun.Value.FlowTag;
                    try
                    {
                        // ✅ Comment the following two lines to use real PLC data
                        SimulateMockData(gunIndex); 
                        continue;

                        Tag<float> tempTag = tagCache[tempTagName];
                        Tag<float> flowTag = tagCache[flowTagName];

                        tempTag.Read();
                        flowTag.Read();

                        float temp = tempTag.LastValueRead;
                        float flow = flowTag.LastValueRead;

                        OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp, flow));
                        IsConnected = true;
                    }
                    catch
                    {
                        IsConnected = false;
                    }
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            foreach (var tag in tagCache.Values)
            {
                tag.Disconnect();
            }
            IsConnected = false;
        }
        private readonly Random _random = new();

        private void SimulateMockData(int gunIndex)
        {
            float temp = (float)(_random.NextDouble() * (50 - 15) + 15); // Random temp between 15°C and 50°C
            float flow = (float)(_random.NextDouble() * (30 - 5) + 5);   // Random flow between 5 and 30 L/min

            OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp, flow));
            IsConnected = true;
        }

    }
}
