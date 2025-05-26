using F.L.A.M.E;
using System;
using System.Threading;
using System.Threading.Tasks;

public class BackgroundLogger
{
    private readonly MockGunDataProvider _provider;
    private readonly CancellationTokenSource _cts = new();

    public BackgroundLogger(MockGunDataProvider provider)
    {
        _provider = provider;
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var dataList = _provider.GetLatestData();
                foreach (var data in dataList)
                {
                    string gunName = $"Gun {data.GunIndex}";
                    SQLiteHelper.InsertData(gunName, data.Temperature, data.Flow);
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
