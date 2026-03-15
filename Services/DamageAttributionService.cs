using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Emqo.NoNameTag.Models;
using SDG.Unturned;
using Steamworks;

namespace Emqo.NoNameTag.Services
{
    public sealed class DamageAttributionService : IDamageAttributionService
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromSeconds(5);
        private static readonly string[] AttackerMemberNames = { "killer", "instigator", "attacker", "sender", "source" };
        private static readonly string[] VictimMemberNames = { "player", "victim", "target", "life" };
        private static readonly string[] SteamIdMemberChain = { "steamID", "playerID", "owner", "channel", "player", "life", "CSteamID" };
        private static readonly string[] BleedingMemberNames = { "isBleeding", "bleeding" };
        private readonly StatsSettingsConfig _settings;
        private readonly ConcurrentDictionary<ulong, BleedAttributionRecord> _recentBleedCapableHits;
        private readonly ConcurrentDictionary<ulong, BleedAttributionRecord> _activeBleedSources;
        private long _nextCleanupTicksUtc;

        public DamageAttributionService(StatsSettingsConfig settings = null)
        {
            _settings = settings ?? new StatsSettingsConfig();
            _recentBleedCapableHits = new ConcurrentDictionary<ulong, BleedAttributionRecord>();
            _activeBleedSources = new ConcurrentDictionary<ulong, BleedAttributionRecord>();
        }

        public void TrackDamage(DamagePlayerParameters parameters, bool shouldAllow)
        {
            if (!shouldAllow || !_settings.Enabled)
            {
                return;
            }

            if (!TryResolveSteamId(parameters, AttackerMemberNames, out var attackerSteamId)
                || !TryResolveSteamId(parameters, VictimMemberNames, out var victimSteamId))
            {
                return;
            }

            RecordAttributedHit(attackerSteamId, victimSteamId, parameters.cause, null, null);
        }

        public void RecordAttributedHit(ulong attackerSteamId, ulong victimSteamId, EDeathCause cause, string weaponName, int? distanceMeters = null, DateTimeOffset? occurredAtUtc = null)
        {
            if (!IsTrackablePair(attackerSteamId, victimSteamId))
            {
                return;
            }

            var timestamp = occurredAtUtc ?? DateTimeOffset.UtcNow;
            _recentBleedCapableHits[victimSteamId] = new BleedAttributionRecord
            {
                VictimSteamId = victimSteamId,
                AttackerSteamId = attackerSteamId,
                Cause = cause,
                WeaponName = weaponName,
                DistanceMeters = distanceMeters,
                LastBleedCapableHitAtUtc = timestamp,
                ExpiresAtUtc = timestamp.AddSeconds(_settings.BleedHitRetentionSeconds)
            };
        }

        public void RecordBleedCapableHit(ulong attackerSteamId, ulong victimSteamId, DateTimeOffset? occurredAtUtc = null)
        {
            RecordAttributedHit(attackerSteamId, victimSteamId, EDeathCause.BLEEDING, null, null, occurredAtUtc);
        }

        public void HandleBleedingStateChanged(PlayerLife playerLife)
        {
            if (!_settings.Enabled || playerLife == null)
            {
                return;
            }

            var victimSteamId = playerLife.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
            if (victimSteamId == 0)
            {
                return;
            }

            var isBleeding = TryReadBoolean(playerLife, BleedingMemberNames);
            if (!isBleeding.HasValue)
            {
                return;
            }

            HandleBleedingStateChanged(victimSteamId, isBleeding.Value, DateTimeOffset.UtcNow);
        }

        public bool HandleBleedingStateChanged(ulong victimSteamId, bool isBleeding, DateTimeOffset? observedAtUtc = null)
        {
            if (victimSteamId == 0 || !_settings.Enabled)
            {
                return false;
            }

            var timestamp = observedAtUtc ?? DateTimeOffset.UtcNow;
            TryCleanupExpired(timestamp);

            if (!isBleeding)
            {
                ClearPlayer(victimSteamId);
                return false;
            }

            var hasActiveRecord = _activeBleedSources.TryGetValue(victimSteamId, out var activeRecord) && !activeRecord.IsExpired(timestamp);
            if (!_recentBleedCapableHits.TryGetValue(victimSteamId, out var recentRecord) || recentRecord.IsExpired(timestamp))
            {
                return hasActiveRecord;
            }

            if (hasActiveRecord && recentRecord.LastBleedCapableHitAtUtc <= activeRecord.LastBleedCapableHitAtUtc)
            {
                return true;
            }

            _activeBleedSources[victimSteamId] = new BleedAttributionRecord
            {
                VictimSteamId = victimSteamId,
                AttackerSteamId = recentRecord.AttackerSteamId,
                Cause = recentRecord.Cause,
                WeaponName = recentRecord.WeaponName,
                DistanceMeters = recentRecord.DistanceMeters,
                LastBleedCapableHitAtUtc = recentRecord.LastBleedCapableHitAtUtc,
                BleedBoundAtUtc = timestamp,
                ExpiresAtUtc = timestamp.AddSeconds(_settings.BleedSourceRetentionSeconds)
            };

            _recentBleedCapableHits.TryRemove(victimSteamId, out _);
            return true;
        }

        public bool TryResolveBleedKiller(ulong victimSteamId, out ulong killerSteamId)
        {
            return TryGetBleedKillerSteamId(victimSteamId, out killerSteamId);
        }

        public bool TryGetBleedAttribution(ulong victimSteamId, out BleedAttributionRecord record)
        {
            var timestamp = DateTimeOffset.UtcNow;
            TryCleanupExpired(timestamp);

            if (_activeBleedSources.TryGetValue(victimSteamId, out record) && !record.IsExpired(timestamp))
            {
                return true;
            }

            record = null;
            return false;
        }

        public bool TryGetBleedKillerSteamId(ulong victimSteamId, out ulong killerSteamId)
        {
            if (TryGetBleedAttribution(victimSteamId, out var record))
            {
                killerSteamId = record.AttackerSteamId;
                return true;
            }

            killerSteamId = 0;
            return false;
        }

        public bool TryGetRecentAttribution(ulong victimSteamId, out BleedAttributionRecord record)
        {
            var timestamp = DateTimeOffset.UtcNow;
            TryCleanupExpired(timestamp);

            if (_recentBleedCapableHits.TryGetValue(victimSteamId, out record) && !record.IsExpired(timestamp))
            {
                return true;
            }

            if (_activeBleedSources.TryGetValue(victimSteamId, out record) && !record.IsExpired(timestamp))
            {
                return true;
            }

            record = null;
            return false;
        }

        public bool TryGetRecentKillerSteamId(ulong victimSteamId, out ulong killerSteamId)
        {
            if (TryGetRecentAttribution(victimSteamId, out var activeRecord))
            {
                killerSteamId = activeRecord.AttackerSteamId;
                return true;
            }

            killerSteamId = 0;
            return false;
        }

        public void ClearPlayer(ulong victimSteamId)
        {
            ClearVictim(victimSteamId);
        }

        public void ClearVictim(ulong victimSteamId)
        {
            if (victimSteamId == 0)
            {
                return;
            }

            _recentBleedCapableHits.TryRemove(victimSteamId, out _);
            _activeBleedSources.TryRemove(victimSteamId, out _);
        }

        public void ClearExpired(DateTimeOffset? nowUtc = null)
        {
            var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
            RemoveExpiredEntries(_recentBleedCapableHits, timestamp);
            RemoveExpiredEntries(_activeBleedSources, timestamp);
        }

        public void ClearAll()
        {
            _recentBleedCapableHits.Clear();
            _activeBleedSources.Clear();
        }

        private static void RemoveExpiredEntries(ConcurrentDictionary<ulong, BleedAttributionRecord> source, DateTimeOffset timestamp)
        {
            foreach (var entry in source)
            {
                if (entry.Value.IsExpired(timestamp))
                {
                    source.TryRemove(entry.Key, out _);
                }
            }
        }

        private void TryCleanupExpired(DateTimeOffset timestamp)
        {
            var nowTicks = timestamp.UtcDateTime.Ticks;
            var nextCleanupTicks = Interlocked.Read(ref _nextCleanupTicksUtc);
            if (nowTicks < nextCleanupTicks)
            {
                return;
            }

            if (Interlocked.CompareExchange(
                    ref _nextCleanupTicksUtc,
                    nowTicks + CleanupInterval.Ticks,
                    nextCleanupTicks) != nextCleanupTicks)
            {
                return;
            }

            ClearExpired(timestamp);
        }

        private static bool IsTrackablePair(ulong attackerSteamId, ulong victimSteamId)
        {
            return attackerSteamId != 0 && victimSteamId != 0 && attackerSteamId != victimSteamId;
        }

        private static bool TryResolveSteamId(object source, string[] preferredMembers, out ulong steamId)
        {
            steamId = 0;
            if (source == null)
            {
                return false;
            }

            foreach (var memberName in preferredMembers)
            {
                if (!TryReadMemberValue(source, memberName, out var candidate))
                {
                    continue;
                }

                if (TryExtractSteamId(candidate, 0, out steamId))
                {
                    return true;
                }
            }

            return TryExtractSteamId(source, 0, out steamId);
        }

        private static bool TryExtractSteamId(object value, int depth, out ulong steamId)
        {
            steamId = 0;
            if (value == null || depth > 4)
            {
                return false;
            }

            if (value is CSteamID cSteamId)
            {
                steamId = cSteamId.m_SteamID;
                return steamId != 0;
            }

            if (value is ulong unsignedLong)
            {
                steamId = unsignedLong;
                return steamId != 0;
            }

            if (value is long signedLong && signedLong > 0)
            {
                steamId = (ulong)signedLong;
                return true;
            }

            foreach (var memberName in SteamIdMemberChain)
            {
                if (!TryReadMemberValue(value, memberName, out var nestedValue))
                {
                    continue;
                }

                if (TryExtractSteamId(nestedValue, depth + 1, out steamId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool? TryReadBoolean(object source, string[] memberNames)
        {
            foreach (var memberName in memberNames)
            {
                if (!TryReadMemberValue(source, memberName, out var value))
                {
                    continue;
                }

                if (value is bool boolValue)
                {
                    return boolValue;
                }
            }

            return null;
        }

        private static bool TryReadMemberValue(object source, string memberName, out object value)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            value = null;

            if (source == null || string.IsNullOrWhiteSpace(memberName))
            {
                return false;
            }

            var type = source.GetType();
            var property = type.GetProperty(memberName, Flags);
            if (property != null)
            {
                value = property.GetValue(source, null);
                return true;
            }

            var field = type.GetField(memberName, Flags);
            if (field != null)
            {
                value = field.GetValue(source);
                return true;
            }

            return false;
        }
    }
}
