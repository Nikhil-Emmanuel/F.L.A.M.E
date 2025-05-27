using F.L.A.M.E;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class BackgroundLogger
{
    private readonly PlcReader _plcReader;
    private readonly CancellationTokenSource _cts = new();

    // Thread-safe dictionary to hold the latest values
    private readonly ConcurrentDictionary<int, GunDataEventArgs> _latestData = new();

    public BackgroundLogger(PlcReader plcReader)
    {
        _plcReader = plcReader;
        _plcReader.OnGunDataUpdated += HandlePlcDataUpdate;
    }

    private void HandlePlcDataUpdate(object? sender, GunDataEventArgs e)
    {
        _latestData[e.GunIndex] = e;
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                foreach (var entry in _latestData.Values)
                {
                    string gunName = $"Gun {entry.GunIndex}";
                    SQLiteHelper.InsertData(gunName, entry.Temperature, entry.Flow);
                }

                await Task.Delay(10000, _cts.Token); // Log every 10 seconds
            }
        });
    }

    public void Stop()
    {
        _cts.Cancel();
    }
}
