using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    public class NameTagManager : INameTagManager
    {
        private readonly NoNameTagConfiguration _config;
        private readonly IPermissionService _permissionService;
        private readonly ConcurrentDictionary<ulong, PermissionGroupConfig> _playerEffects;
        private const int MaxCacheSize = Constants.MaxPlayerCacheSize;

        public NameTagManager(NoNameTagConfiguration config, IPermissionService permissionService)
        {
            _config = config;
            _permissionService = permissionService;
            _playerEffects = new ConcurrentDictionary<ulong, PermissionGroupConfig>();
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
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error applying display effect to {player?.DisplayName}");
            }
        }

        public void RemoveDisplayEffect(UnturnedPlayer player)
        {
            if (player == null) return;

            var steamId = player.CSteamID.m_SteamID;
            _playerEffects.TryRemove(steamId, out _);

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

        public void ClearAll()
        {
            _playerEffects.Clear();
        }

        private void CleanupCache()
        {
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
                _playerEffects.TryRemove(key, out _);
            }

            Logger.Debug($"Cleaned up {keysToRemove.Count} offline player entries. Cache size: {_playerEffects.Count}");
        }
    }
}
