using System;
using System.Collections.Concurrent;
using System.IO;
using System.Timers;
using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using LiteDB;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    public sealed class PlayerStatsService : IPlayerStatsService
    {
        private const string CollectionName = "player_stats";
        private const int FlushIntervalSeconds = 30;
        private const int DirtyFlushThreshold = 32;

        private readonly StatsSettingsConfig _settings;
        private readonly string _databasePath;
        private readonly ConcurrentDictionary<ulong, int> _currentKillstreaks;
        private readonly ConcurrentDictionary<ulong, PlayerStatsDocument> _cachedStats;
        private readonly ConcurrentDictionary<ulong, byte> _dirtySteamIds;
        private readonly object _syncRoot;
        private readonly Timer _flushTimer;
        private bool _disposed;

        public PlayerStatsService(StatsSettingsConfig settings = null)
        {
            _settings = settings ?? new StatsSettingsConfig();
            _currentKillstreaks = new ConcurrentDictionary<ulong, int>();
            _cachedStats = new ConcurrentDictionary<ulong, PlayerStatsDocument>();
            _dirtySteamIds = new ConcurrentDictionary<ulong, byte>();
            _syncRoot = new object();
            _databasePath = ResolveDatabasePath(_settings);

            if (!_settings.Enabled)
                return;

            EnsureDatabase();

            _flushTimer = new Timer(FlushIntervalSeconds * 1000.0);
            _flushTimer.Elapsed += OnFlushTimerElapsed;
            _flushTimer.AutoReset = true;
            _flushTimer.Start();
        }

        public void EnsurePlayer(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId) || !_settings.Enabled)
                return;

            GetOrLoadRecord(steamId);
        }

        public PlayerStatsRecord GetPlayerStats(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId))
                return new PlayerStatsRecord();

            if (!_settings.Enabled)
                return CreateStatsRecord(steamId, 0, 0);

            var record = GetOrLoadRecord(steamId);
            return CreateStatsRecord(steamId, record.TotalKills, record.TotalDeaths);
        }

        public void RecordDeath(ulong victimSteamId, ulong? killerSteamId = null)
        {
            RecordPlayerDeath(victimSteamId, killerSteamId ?? 0);
        }

        public void RecordPlayerDeath(ulong victimSteamId, ulong killerSteamId = 0)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(victimSteamId))
                return;

            ResetCurrentKillstreak(victimSteamId);

            if (!_settings.Enabled)
            {
                if (ShouldIncrementKiller(killerSteamId, victimSteamId))
                    _currentKillstreaks.AddOrUpdate(killerSteamId, 1, (_, current) => current + 1);

                return;
            }

            lock (_syncRoot)
            {
                var victimRecord = GetOrLoadRecordCore(victimSteamId);
                victimRecord.TotalDeaths++;
                victimRecord.UpdatedAtUtc = DateTime.UtcNow;
                MarkDirty(victimSteamId);

                if (ShouldIncrementKiller(killerSteamId, victimSteamId))
                {
                    var killerRecord = GetOrLoadRecordCore(killerSteamId);
                    killerRecord.TotalKills++;
                    killerRecord.UpdatedAtUtc = DateTime.UtcNow;
                    MarkDirty(killerSteamId);
                    _currentKillstreaks.AddOrUpdate(killerSteamId, 1, (_, current) => current + 1);
                }
            }

            if (_dirtySteamIds.Count >= DirtyFlushThreshold)
            {
                FlushDirtyRecords();
            }
        }

        public int GetCurrentKillstreak(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId))
                return 0;

            return _currentKillstreaks.TryGetValue(steamId, out var streak) ? streak : 0;
        }

        public void ResetCurrentKillstreak(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId))
                return;

            _currentKillstreaks[steamId] = 0;
        }

        public void ClearCurrentKillstreak(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId))
                return;

            _currentKillstreaks.TryRemove(steamId, out _);
        }

        public void ClearSession()
        {
            EnsureNotDisposed();
            _currentKillstreaks.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _flushTimer?.Stop();
            _flushTimer?.Dispose();
            FlushDirtyRecords();
            _disposed = true;
            _currentKillstreaks.Clear();
            _cachedStats.Clear();
            _dirtySteamIds.Clear();
        }

        private void EnsureDatabase()
        {
            try
            {
                using (var database = OpenDatabase())
                {
                    var collection = GetCollection(database);
                    collection.EnsureIndex(x => x.SteamId, unique: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to initialize player stats database", LogCategory.Plugin);
                throw;
            }
        }

        private LiteDatabase OpenDatabase()
        {
            return new LiteDatabase($"Filename={_databasePath};Connection=shared");
        }

        private void OnFlushTimerElapsed(object sender, ElapsedEventArgs e)
        {
            FlushDirtyRecords();
        }

        private PlayerStatsDocument GetOrLoadRecord(ulong steamId)
        {
            EnsureNotDisposed();

            if (!IsValidSteamId(steamId))
                return CreateDefaultDocument(steamId);

            lock (_syncRoot)
            {
                return GetOrLoadRecordCore(steamId);
            }
        }

        private PlayerStatsDocument GetOrLoadRecordCore(ulong steamId)
        {
            if (_cachedStats.TryGetValue(steamId, out var cachedRecord))
                return cachedRecord;

            using (var database = OpenDatabase())
            {
                var collection = GetCollection(database);
                var loadedRecord = collection.FindById((long)steamId) ?? CreateDefaultDocument(steamId);
                _cachedStats[steamId] = loadedRecord;
                return loadedRecord;
            }
        }

        private void MarkDirty(ulong steamId)
        {
            if (IsValidSteamId(steamId))
                _dirtySteamIds[steamId] = 0;
        }

        private void FlushDirtyRecords()
        {
            if (_disposed || !_settings.Enabled || _dirtySteamIds.IsEmpty)
                return;

            lock (_syncRoot)
            {
                if (_dirtySteamIds.IsEmpty)
                    return;

                try
                {
                    using (var database = OpenDatabase())
                    {
                        var collection = GetCollection(database);
                        foreach (var dirtySteamId in _dirtySteamIds.Keys)
                        {
                            if (!_cachedStats.TryGetValue(dirtySteamId, out var record))
                            {
                                _dirtySteamIds.TryRemove(dirtySteamId, out _);
                                continue;
                            }

                            collection.Upsert(record);
                            _dirtySteamIds.TryRemove(dirtySteamId, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Failed to flush player stats cache", LogCategory.Plugin);
                }
            }
        }

        private static ILiteCollection<PlayerStatsDocument> GetCollection(LiteDatabase database)
        {
            return database.GetCollection<PlayerStatsDocument>(CollectionName);
        }

        private static PlayerStatsDocument CreateDefaultDocument(ulong steamId)
        {
            return new PlayerStatsDocument
            {
                Id = (long)steamId,
                SteamId = steamId,
                TotalKills = 0,
                TotalDeaths = 0,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private PlayerStatsRecord CreateStatsRecord(ulong steamId, int totalKills, int totalDeaths)
        {
            return new PlayerStatsRecord
            {
                SteamId = steamId,
                TotalKills = totalKills,
                TotalDeaths = totalDeaths,
                CurrentKillstreak = GetCurrentKillstreak(steamId)
            };
        }

        private static bool ShouldIncrementKiller(ulong killerSteamId, ulong victimSteamId)
        {
            return IsValidSteamId(killerSteamId) && killerSteamId != victimSteamId;
        }

        private static bool IsValidSteamId(ulong steamId)
        {
            return steamId != 0;
        }

        private static string ResolveDatabasePath(StatsSettingsConfig settings)
        {
            var relativePath = string.IsNullOrWhiteSpace(settings.DatabaseRelativePath)
                ? "Data/nonametag.litedb"
                : settings.DatabaseRelativePath;

            var normalizedRelativePath = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var assemblyDirectory = Path.GetDirectoryName(typeof(PlayerStatsService).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(assemblyDirectory, normalizedRelativePath);
            var directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return path;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PlayerStatsService));
        }

        private sealed class PlayerStatsDocument
        {
            [BsonId]
            public long Id { get; set; }

            public ulong SteamId { get; set; }

            public int TotalKills { get; set; }

            public int TotalDeaths { get; set; }

            public DateTime UpdatedAtUtc { get; set; }
        }
    }
}
