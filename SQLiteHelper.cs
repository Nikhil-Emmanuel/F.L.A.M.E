using System;
using System.Data.SQLite;
using System.IO;

public static class SQLiteHelper
{
    private static readonly string DbPath = "SensorLog.db";
    private static readonly string ConnectionString = $"Data Source={DbPath};Version=3;";

    static SQLiteHelper()
    {
        if (!File.Exists(DbPath))
        {
            SQLiteConnection.CreateFile(DbPath);
            using var connection = new SQLiteConnection(ConnectionString);
            connection.Open();
            using var command = new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS SensorData (
                    Timestamp TEXT,
                    GunName TEXT,
                    Temperature REAL,
                    Flow REAL
                );", connection);
            command.ExecuteNonQuery();
        }
    }

    public static void InsertData(string gunName, double temperature, double flow)
    {
        string timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("yyyy-MM-dd HH:mm:ss");

        // Round temperature and flow to 2 decimal places
        double roundedTemp = Math.Round(temperature, 2);
        double roundedFlow = Math.Round(flow, 2);

        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();
        using var command = new SQLiteCommand("INSERT INTO SensorData (Timestamp, GunName, Temperature, Flow) VALUES (@t, @g, @temp, @f)", connection);
        command.Parameters.AddWithValue("@t", timestamp);
        command.Parameters.AddWithValue("@g", gunName);
        command.Parameters.AddWithValue("@temp", roundedTemp);
        command.Parameters.AddWithValue("@f", roundedFlow);
        command.ExecuteNonQuery();
    }

}
