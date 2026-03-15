using System;
using Emqo.NoNameTag.Models;

namespace Emqo.NoNameTag.Services
{
    public interface IPlayerStatsService : IDisposable
    {
        void EnsurePlayer(ulong steamId);

        PlayerStatsRecord GetPlayerStats(ulong steamId);

        void RecordDeath(ulong victimSteamId, ulong? killerSteamId = null);

        void RecordPlayerDeath(ulong victimSteamId, ulong killerSteamId = 0);

        int GetCurrentKillstreak(ulong steamId);

        void ResetCurrentKillstreak(ulong steamId);

        void ClearCurrentKillstreak(ulong steamId);

        void ClearSession();
    }
}
