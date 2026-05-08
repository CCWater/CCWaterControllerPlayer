using System.IO;
using CCWaterControllerPlayer.Models;
using Microsoft.Data.Sqlite;

namespace CCWaterControllerPlayer.Services;

public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseService(string? dbPath = null)
    {
        dbPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CCWaterControllerPlayer", "data.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();
        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS TrackRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT '',
                GameName TEXT NOT NULL DEFAULT '',
                WeaponName TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                TriggerStartTicks INTEGER NOT NULL,
                TriggerEndTicks INTEGER NOT NULL,
                SampleCount INTEGER NOT NULL,
                SamplingRateHz INTEGER NOT NULL,
                TrackedStick INTEGER NOT NULL,
                Notes TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                SnapshotData BLOB NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_TrackRecords_CreatedAt ON TrackRecords(CreatedAt);
        ";
        await cmd.ExecuteNonQueryAsync();

        try
        {
            var alterCmd = _connection!.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE TrackRecords ADD COLUMN Name TEXT NOT NULL DEFAULT ''";
            await alterCmd.ExecuteNonQueryAsync();
        }
        catch { }

        try
        {
            var alterCmd = _connection!.CreateCommand();
            alterCmd.CommandText = "ALTER TABLE TrackRecords ADD COLUMN Status INTEGER NOT NULL DEFAULT 0";
            await alterCmd.ExecuteNonQueryAsync();
        }
        catch { }
    }

    public async Task<long> SaveTrackRecordAsync(TrackRecord record)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TrackRecords (Name, GameName, WeaponName, CreatedAt, TriggerStartTicks, TriggerEndTicks, 
                SampleCount, SamplingRateHz, TrackedStick, Notes, Status, SnapshotData)
            VALUES (@name, '', '', @createdAt, @triggerStart, @triggerEnd, 
                @sampleCount, @samplingRate, @trackedStick, @notes, @status, @snapshotData);
            SELECT last_insert_rowid();
        ";

        cmd.Parameters.AddWithValue("@name", record.Name);
        cmd.Parameters.AddWithValue("@createdAt", record.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@triggerStart", record.TriggerStartTicks);
        cmd.Parameters.AddWithValue("@triggerEnd", record.TriggerEndTicks);
        cmd.Parameters.AddWithValue("@sampleCount", record.SampleCount);
        cmd.Parameters.AddWithValue("@samplingRate", record.SamplingRateHz);
        cmd.Parameters.AddWithValue("@trackedStick", (int)record.TrackedStick);
        cmd.Parameters.AddWithValue("@notes", (object?)record.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", (int)record.Status);
        cmd.Parameters.AddWithValue("@snapshotData", SerializeSnapshots(record.Snapshots));

        var result = await cmd.ExecuteScalarAsync();
        record.Id = Convert.ToInt64(result);
        return record.Id;
    }

    public async Task UpdateRecordNameAsync(long id, string name)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE TrackRecords SET Name = @name WHERE Id = @id";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateRecordStatusAsync(long id, RecordStatus status)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE TrackRecords SET Status = @status WHERE Id = @id";
        cmd.Parameters.AddWithValue("@status", (int)status);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteRecordsByStatusAsync(RecordStatus status)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "DELETE FROM TrackRecords WHERE Status = @status";
        cmd.Parameters.AddWithValue("@status", (int)status);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<TrackRecord>> GetTrackRecordsAsync(string? gameName = null, string? weaponName = null)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, CreatedAt, TriggerStartTicks, TriggerEndTicks, SampleCount, SamplingRateHz, TrackedStick, Notes, Status FROM TrackRecords ORDER BY CreatedAt DESC";

        var records = new List<TrackRecord>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(ReadTrackRecordMeta(reader));
        }
        return records;
    }

    public async Task LoadSnapshotsAsync(TrackRecord record)
    {
        if (record.Snapshots.Count > 0) return;
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT SnapshotData FROM TrackRecords WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", record.Id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var blob = (byte[])reader.GetValue(0);
            record.Snapshots = DeserializeSnapshots(blob);
        }
    }

    public async Task<TrackRecord?> GetTrackRecordByIdAsync(long id)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM TrackRecords WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadTrackRecord(reader);
        return null;
    }

    public async Task DeleteTrackRecordAsync(long id)
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "DELETE FROM TrackRecords WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<TrackRecord?> GetLatestRecordAsync()
    {
        var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT * FROM TrackRecords ORDER BY CreatedAt DESC LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadTrackRecord(reader);
        return null;
    }

    private static TrackRecord ReadTrackRecordMeta(SqliteDataReader reader)
    {
        int statusOrdinal = -1;
        try { statusOrdinal = reader.GetOrdinal("Status"); } catch { }

        return new TrackRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            TriggerStartTicks = reader.GetInt64(reader.GetOrdinal("TriggerStartTicks")),
            TriggerEndTicks = reader.GetInt64(reader.GetOrdinal("TriggerEndTicks")),
            SampleCount = reader.GetInt32(reader.GetOrdinal("SampleCount")),
            SamplingRateHz = reader.GetInt32(reader.GetOrdinal("SamplingRateHz")),
            TrackedStick = (StickSide)reader.GetInt32(reader.GetOrdinal("TrackedStick")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            Status = statusOrdinal >= 0 && !reader.IsDBNull(statusOrdinal) 
                ? (RecordStatus)reader.GetInt32(statusOrdinal) 
                : RecordStatus.Temporary
        };
    }

    private static TrackRecord ReadTrackRecord(SqliteDataReader reader)
    {
        int statusOrdinal = -1;
        try { statusOrdinal = reader.GetOrdinal("Status"); } catch { }

        var record = new TrackRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            TriggerStartTicks = reader.GetInt64(reader.GetOrdinal("TriggerStartTicks")),
            TriggerEndTicks = reader.GetInt64(reader.GetOrdinal("TriggerEndTicks")),
            SampleCount = reader.GetInt32(reader.GetOrdinal("SampleCount")),
            SamplingRateHz = reader.GetInt32(reader.GetOrdinal("SamplingRateHz")),
            TrackedStick = (StickSide)reader.GetInt32(reader.GetOrdinal("TrackedStick")),
            Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes")),
            Status = statusOrdinal >= 0 && !reader.IsDBNull(statusOrdinal) 
                ? (RecordStatus)reader.GetInt32(statusOrdinal) 
                : RecordStatus.Temporary
        };

        var blob = (byte[])reader.GetValue(reader.GetOrdinal("SnapshotData"));
        record.Snapshots = DeserializeSnapshots(blob);
        return record;
    }

    private static byte[] SerializeSnapshots(List<ControllerSnapshot> snapshots)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(snapshots.Count);
        foreach (var s in snapshots)
        {
            writer.Write(s.TimestampTicks);
            writer.Write(s.LeftStick.X);
            writer.Write(s.LeftStick.Y);
            writer.Write(s.RightStick.X);
            writer.Write(s.RightStick.Y);
            writer.Write(s.Triggers.Left);
            writer.Write(s.Triggers.Right);
            writer.Write(s.Buttons);
        }

        return ms.ToArray();
    }

    private static List<ControllerSnapshot> DeserializeSnapshots(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        int count = reader.ReadInt32();
        var snapshots = new List<ControllerSnapshot>(count);

        for (int i = 0; i < count; i++)
        {
            snapshots.Add(new ControllerSnapshot
            {
                TimestampTicks = reader.ReadInt64(),
                LeftStick = new StickPosition(reader.ReadSingle(), reader.ReadSingle()),
                RightStick = new StickPosition(reader.ReadSingle(), reader.ReadSingle()),
                Triggers = new TriggerState { Left = reader.ReadSingle(), Right = reader.ReadSingle() },
                Buttons = reader.ReadUInt32()
            });
        }

        return snapshots;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
