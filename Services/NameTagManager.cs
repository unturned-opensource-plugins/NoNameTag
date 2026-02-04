using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    public class NameTagManager : INameTagManager
    {
        private readonly NoNameTagConfiguration _config;
        private readonly PermissionService _permissionService;
        private readonly Dictionary<ulong, PermissionGroupConfig> _playerEffects;
        private const int MaxCacheSize = 1000; // 防止无限增长

        public NameTagManager(NoNameTagConfiguration config, PermissionService permissionService)
        {
            _config = config;
            _permissionService = permissionService;
            _playerEffects = new Dictionary<ulong, PermissionGroupConfig>();
        }

        public void ApplyDisplayEffect(UnturnedPlayer player)
        {
            if (player == null) return;

            try
            {
                var group = _permissionService.GetPlayerPermissionGroup(player);
                if (group != null)
                {
                    _playerEffects[player.CSteamID.m_SteamID] = group;
                    Logger.Debug($"Applied display effect to {player.DisplayName}: {group.Permission}");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, $"Error applying display effect to {player?.DisplayName}");
            }
        }

        public void RemoveDisplayEffect(UnturnedPlayer player)
        {
            if (player == null) return;

            var steamId = player.CSteamID.m_SteamID;
            _playerEffects.Remove(steamId);

            // 定期清理过期条目
            if (_playerEffects.Count > MaxCacheSize)
            {
                CleanupCache();
            }
        }

        public void RefreshAllPlayers()
        {
            _playerEffects.Clear();
            foreach (var client in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(client);
                ApplyDisplayEffect(player);
            }
            Logger.Debug($"Refreshed {_playerEffects.Count} player effects");
        }

        public void RefreshPlayer(UnturnedPlayer player)
        {
            if (player == null) return;
            RemoveDisplayEffect(player);
            ApplyDisplayEffect(player);
        }

        public PermissionGroupConfig GetPlayerEffect(ulong steamId)
        {
            _playerEffects.TryGetValue(steamId, out var group);
            return group;
        }

        private void CleanupCache()
        {
            // 移除不在线的玩家
            var onlineSteamIds = new HashSet<ulong>();
            foreach (var client in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(client);
                if (player != null)
                    onlineSteamIds.Add(player.CSteamID.m_SteamID);
            }

            var keysToRemove = _playerEffects.Keys
                .Where(k => !onlineSteamIds.Contains(k))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _playerEffects.Remove(key);
            }

            Logger.Debug($"Cleaned up {keysToRemove.Count} offline player entries. Cache size: {_playerEffects.Count}");
        }
    }
}
