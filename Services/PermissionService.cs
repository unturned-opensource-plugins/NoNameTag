using Emqo.NoNameTag.Models;
using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emqo.NoNameTag.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly Dictionary<ulong, (PermissionGroupConfig group, long timestamp)> _permissionCache;
        private const int CacheExpirationSeconds = 300; // 5分钟缓存

        public PermissionService(NoNameTagConfiguration config)
        {
            _config = config;
            _permissionCache = new Dictionary<ulong, (PermissionGroupConfig, long)>();
        }

        public PermissionGroupConfig GetPlayerPermissionGroup(IRocketPlayer player)
        {
            if (player == null || _config.PermissionGroups == null || _config.PermissionGroups.Count == 0)
                return null;

            var playerId = GetPlayerId(player);

            // 检查缓存
            if (_permissionCache.TryGetValue(playerId, out var cached))
            {
                var elapsedSeconds = (DateTime.UtcNow.Ticks - cached.timestamp) / 10000000;
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
            var matchingGroups = new List<PermissionGroupConfig>();

            foreach (var group in _config.PermissionGroups)
            {
                if (!string.IsNullOrEmpty(group.Permission) && player.HasPermission(group.Permission))
                {
                    matchingGroups.Add(group);
                }
            }

            if (matchingGroups.Count == 0)
                return null;

            if (_config.PriorityMode == PriorityMode.FirstMatch)
            {
                return matchingGroups.First();
            }
            else // HighestPriority
            {
                return matchingGroups.OrderByDescending(g => g.Priority).First();
            }
        }

        public bool HasAnyPermissionGroup(IRocketPlayer player)
        {
            return GetPlayerPermissionGroup(player) != null;
        }

        public void ClearPlayerCache(ulong steamId)
        {
            _permissionCache.Remove(steamId);
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
