using System;
using Emqo.NoNameTag.Models;
using SDG.Unturned;

namespace Emqo.NoNameTag.Services
{
    public interface IDamageAttributionService
    {
        void TrackDamage(DamagePlayerParameters parameters, bool shouldAllow);

        void RecordAttributedHit(ulong attackerSteamId, ulong victimSteamId, EDeathCause cause, string weaponName, int? distanceMeters = null, DateTimeOffset? occurredAtUtc = null);

        void RecordBleedCapableHit(ulong attackerSteamId, ulong victimSteamId, DateTimeOffset? occurredAtUtc = null);

        void HandleBleedingStateChanged(PlayerLife playerLife);

        bool HandleBleedingStateChanged(ulong victimSteamId, bool isBleeding, DateTimeOffset? observedAtUtc = null);

        bool TryResolveBleedKiller(ulong victimSteamId, out ulong killerSteamId);

        bool TryGetBleedKillerSteamId(ulong victimSteamId, out ulong killerSteamId);

        bool TryGetRecentKillerSteamId(ulong victimSteamId, out ulong killerSteamId);

        bool TryGetBleedAttribution(ulong victimSteamId, out BleedAttributionRecord record);

        bool TryGetRecentAttribution(ulong victimSteamId, out BleedAttributionRecord record);

        void ClearPlayer(ulong victimSteamId);

        void ClearVictim(ulong victimSteamId);

        void ClearExpired(DateTimeOffset? nowUtc = null);

        void ClearAll();
    }
}
