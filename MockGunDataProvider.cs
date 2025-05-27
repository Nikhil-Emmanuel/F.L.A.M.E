/*using System;
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

    public class MockGunDataProvider
    {
        private readonly Random _random = new();
        private readonly System.Timers.Timer _timer;
        private readonly Dictionary<int, GunDataEventArgs> _latestData = new();


        public event EventHandler<GunDataEventArgs>? OnGunDataUpdated;

        public MockGunDataProvider(int intervalMs = 2000)
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += TimerElapsed;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            for (int i = 0; i < 8; i++)
            {
                float temp = (float)(_random.NextDouble() * 50.0);
                float flow = (float)(_random.NextDouble() * 30.0);
                var gunData = new GunDataEventArgs(i, temp, flow);
                OnGunDataUpdated?.Invoke(this, gunData);
                _latestData[i] = gunData;
            }
        }
        public List<GunDataEventArgs> GetLatestData()
        {
            return _latestData.Values.ToList();
        }
     }
}
*/