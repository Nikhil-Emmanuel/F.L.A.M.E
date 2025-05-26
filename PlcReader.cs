/*using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlcTag;

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
        private readonly string ipAddress = "192.168.1.1";
        private readonly int pollInterval = 1000; // 1 second

        private readonly Dictionary<int, (string TempTag, string FlowTag)> gunTagMap = new()
        {
            { 0, ("Sensor_Temp_Flow[0].Temp_Out", "Sensor_Temp_Flow[0].Flow_Out") },
            { 1, ("Sensor_Temp_Flow[1].Temp_Out", "Sensor_Temp_Flow[1].Flow_Out") },
            { 2, ("Sensor_Temp_Flow[2].Temp_Out", "Sensor_Temp_Flow[2].Flow_Out") },
            { 3, ("Sensor_Temp_Flow[3].Temp_Out", "Sensor_Temp_Flow[3].Flow_Out") },
            { 4, ("Sensor_Temp_Flow[4].Temp_Out", "Sensor_Temp_Flow[4].Flow_Out") },
            { 5, ("Sensor_Temp_Flow[5].Temp_Out", "Sensor_Temp_Flow[5].Flow_Out") },
            { 6, ("Sensor_Temp_Flow[6].Temp_Out", "Sensor_Temp_Flow[6].Flow_Out") },
            
        };

        private PlcController? controller;
        private readonly Dictionary<string, Tag<float>> tagCache = new();

        public event EventHandler<GunDataEventArgs>? OnGunDataUpdated;

        public async Task StartPollingAsync(CancellationToken cancellationToken)
        {
            controller = new PlcController(ipAddress, "0", CPUType.LGX);

            // Create and connect all tags
            foreach (var (tempTag, flowTag) in gunTagMap.Values)
            {
                var temp = controller.CreateTag<float>(tempTag);
                var flow = controller.CreateTag<float>(flowTag);
                temp.Connect();
                flow.Connect();
                tagCache[tempTag] = temp;
                tagCache[flowTag] = flow;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var gun in gunTagMap)
                {
                    int gunIndex = gun.Key;
                    string tempTagName = gun.Value.TempTag;
                    string flowTagName = gun.Value.FlowTag;

                    Tag<float> tempTag = tagCache[tempTagName];
                    Tag<float> flowTag = tagCache[flowTagName];

                    tempTag.Read();
                    flowTag.Read();

                    float temp = tempTag.LastValueRead;
                    float flow = flowTag.LastValueRead;

                    OnGunDataUpdated?.Invoke(this, new GunDataEventArgs(gunIndex, temp, flow));
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            foreach (var tag in tagCache.Values)
            {
                tag.Disconnect();
            }
        }
    }
}
*/