using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.API;
using System;
using System.Collections.Concurrent;

namespace Emqo.NoNameTag.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly ConcurrentDictionary<ulong, (PermissionGroupConfig group, long timestamp)> _permissionCache;
        private const int CacheExpirationSeconds = Constants.PermissionCacheExpirationSeconds;

        public PermissionService(NoNameTagConfiguration config)
        {
            _config = config;
            _permissionCache = new ConcurrentDictionary<ulong, (PermissionGroupConfig, long)>();
        }

        public PermissionGroupConfig GetPlayerPermissionGroup(IRocketPlayer player)
        {
            if (player == null || _config.PermissionGroups == null || _config.PermissionGroups.Count == 0)
                return null;

            var playerId = GetPlayerId(player);

            // 检查缓存
            if (_permissionCache.TryGetValue(playerId, out var cached))
            {
                var elapsedSeconds = (DateTime.UtcNow.Ticks - cached.timestamp) / TimeSpan.TicksPerSecond;
                if (elapsedSeconds < CacheExpirationSeconds)
                {
                    return cached.group;
                }
            }

            // 查询权限
            var result = FindHighestPriorityGroup(player);
            _permissionCache[playerId] = (result, DateTime.UtcNow.Ticks);
            return result;
        }

        private PermissionGroupConfig FindHighestPriorityGroup(IRocketPlayer player)
        {
            PermissionGroupConfig highest = null;

            foreach (var group in _config.PermissionGroups)
            {
                if (!string.IsNullOrEmpty(group.Permission) && player.HasPermission(group.Permission))
                {
                    if (_config.PriorityMode == PriorityMode.FirstMatch)
                        return group;

                    if (highest == null || group.Priority > highest.Priority)
                        highest = group;
                }
            }

            return highest;
        }

        public bool HasAnyPermissionGroup(IRocketPlayer player)
        {
            return GetPlayerPermissionGroup(player) != null;
        }

        public void ClearPlayerCache(ulong steamId)
        {
            _permissionCache.TryRemove(steamId, out _);
        }

        public void ClearAllCache()
        {
            _permissionCache.Clear();
        }

        private ulong GetPlayerId(IRocketPlayer player)
        {
            if (player is Rocket.Unturned.Player.UnturnedPlayer unturnedPlayer)
            {
                return unturnedPlayer.CSteamID.m_SteamID;
            }
            return 0;
        }
    }
}
