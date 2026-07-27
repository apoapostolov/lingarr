using Microsoft.Data.Sqlite;

namespace Lingarr.Server.Services;

/// <summary>
/// Periodically checkpoints Hangfire's SQLite WAL so it does not grow without bound
/// (observed multi‑GB WAL files with few jobs on Bedroom).
/// </summary>
public sealed class HangfireSqliteMaintenanceService : BackgroundService
{
    private readonly ILogger<HangfireSqliteMaintenanceService> _logger;
    private readonly string? _sqlitePath;
    private readonly TimeSpan _interval;

    public HangfireSqliteMaintenanceService(ILogger<HangfireSqliteMaintenanceService> logger)
    {
        _logger = logger;
        var dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION")?.ToLowerInvariant() ?? "sqlite";
        if (dbConnection is "mysql" or "postgres" or "postgresql")
        {
            _sqlitePath = null;
        }
        else
        {
            _sqlitePath = Environment.GetEnvironmentVariable("DB_HANGFIRE_SQLITE_PATH")
                          ?? "/app/config/Hangfire.db";
        }

        var minutes = 15;
        if (int.TryParse(Environment.GetEnvironmentVariable("HANGFIRE_WAL_CHECKPOINT_MINUTES"), out var configured)
            && configured > 0)
        {
            minutes = configured;
        }

        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_sqlitePath == null)
        {
            _logger.LogDebug("Hangfire SQLite maintenance disabled (non-SQLite Hangfire storage).");
            return;
        }

        // First checkpoint shortly after start (jobs may have opened WAL already).
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CheckpointWal(_sqlitePath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Hangfire SQLite WAL checkpoint failed for {Path}", _sqlitePath);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CheckpointWal(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var walPath = path + "-wal";
        long walBytesBefore = 0;
        if (File.Exists(walPath))
        {
            walBytesBefore = new FileInfo(walPath).Length;
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=120000;";
        command.ExecuteNonQuery();
        command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        using var reader = command.ExecuteReader();
        // result columns: busy, log, checkpointed
        var busy = 0;
        var log = 0;
        var checkpointed = 0;
        if (reader.Read())
        {
            busy = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            log = reader.FieldCount > 1 && !reader.IsDBNull(1) ? reader.GetInt32(1) : 0;
            checkpointed = reader.FieldCount > 2 && !reader.IsDBNull(2) ? reader.GetInt32(2) : 0;
        }

        long walBytesAfter = File.Exists(walPath) ? new FileInfo(walPath).Length : 0;
        if (walBytesBefore > 8 * 1024 * 1024 || walBytesAfter > 8 * 1024 * 1024 || busy != 0)
        {
            _logger.LogInformation(
                "Hangfire WAL checkpoint: path={Path} busy={Busy} log={Log} checkpointed={Checkpointed} walBefore={Before} walAfter={After}",
                path, busy, log, checkpointed, walBytesBefore, walBytesAfter);
        }
        else
        {
            _logger.LogDebug(
                "Hangfire WAL checkpoint: path={Path} busy={Busy} walAfter={After}",
                path, busy, walBytesAfter);
        }
    }

    /// <summary>
    /// If Hangfire.db cannot be opened as SQLite, quarantine the files so Hangfire can recreate a clean store.
    /// </summary>
    public static bool TryRecoverCorruptDatabase(string sqliteDbPath, ILogger logger)
    {
        try
        {
            if (!File.Exists(sqliteDbPath))
            {
                return false;
            }

            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = sqliteDbPath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Shared
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = command.ExecuteScalar()?.ToString();
            if (string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            logger.LogError(
                "Hangfire SQLite integrity_check failed ({Result}). Quarantining database files so a fresh store can be created.",
                result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Hangfire SQLite database at {Path} is unreadable. Quarantining files for recreation.",
                sqliteDbPath);
        }

        Quarantine(sqliteDbPath, logger);
        return true;
    }

    private static void Quarantine(string sqliteDbPath, ILogger logger)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var source = sqliteDbPath + suffix;
            if (!File.Exists(source))
            {
                continue;
            }

            var dest = $"{sqliteDbPath}.corrupt-{stamp}{suffix}";
            try
            {
                File.Move(source, dest, overwrite: true);
                logger.LogWarning("Quarantined Hangfire file {Source} -> {Dest}", source, dest);
            }
            catch (Exception moveEx)
            {
                logger.LogError(moveEx, "Failed to quarantine Hangfire file {Source}", source);
            }
        }
    }
}
