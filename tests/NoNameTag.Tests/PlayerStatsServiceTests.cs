using System;
using System.IO;
using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Services;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class PlayerStatsServiceTests
    {
        [Fact]
        public void ReleasePlayer_FlushesDirtyStatsAndKeepsCurrentKillstreak()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "NoNameTag.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            var databasePath = Path.Combine(tempDirectory, "stats.litedb");

            try
            {
                var settings = new StatsSettingsConfig
                {
                    Enabled = true,
                    DatabaseRelativePath = databasePath
                };

                using (var service = new PlayerStatsService(settings))
                {
                    const ulong victimSteamId = 76561198000000001UL;
                    const ulong killerSteamId = 76561198000000002UL;

                    service.RecordPlayerDeath(victimSteamId, killerSteamId);
                    Assert.Equal(1, service.GetCurrentKillstreak(killerSteamId));

                    service.ReleasePlayer(killerSteamId);

                    var reloadedKillerStats = service.GetPlayerStats(killerSteamId);
                    Assert.Equal(1, reloadedKillerStats.TotalKills);
                    Assert.Equal(1, reloadedKillerStats.CurrentKillstreak);
                }
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
