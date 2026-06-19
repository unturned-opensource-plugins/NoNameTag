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
        private readonly ConcurrentDictionary<ulong, string> _formattedPlayerNames;
        private const int MaxCacheSize = Constants.MaxPlayerCacheSize;

        public NameTagManager(NoNameTagConfiguration config, IPermissionService permissionService)
        {
            _config = config;
            _permissionService = permissionService;
            _playerEffects = new ConcurrentDictionary<ulong, PermissionGroupConfig>();
            _formattedPlayerNames = new ConcurrentDictionary<ulong, string>();
        }

        public void ApplyDisplayEffect(UnturnedPlayer player)
        {
            if (player == null) return;

            try
            {
                var group = _permissionService.GetPlayerPermissionGroup(player);
                var steamId = player.CSteamID.m_SteamID;
                if (group != null)
                {
                    _playerEffects[steamId] = group;
                    Logger.Debug($"Applied display effect to {player.DisplayName}: {group.Permission}");
                }
                else
                {
                    _playerEffects.TryRemove(steamId, out _);
                }

                var safePlayerName = RichTextSanitizer.SanitizeUntrustedPlayerText(player.DisplayName);
                _formattedPlayerNames[steamId] = NameFormatter.FormatPlayerName(
                    safePlayerName,
                    steamId,
                    group?.DisplayEffect
                );
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
            _formattedPlayerNames.TryRemove(steamId, out _);

            if (_playerEffects.Count > MaxCacheSize || _formattedPlayerNames.Count > MaxCacheSize)
            {
                CleanupCache();
            }
        }

        public void RefreshAllPlayers()
        {
            _playerEffects.Clear();
            _formattedPlayerNames.Clear();
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

        public string GetFormattedPlayerName(ulong steamId, string fallbackPlayerName)
        {
            if (steamId != 0 && _formattedPlayerNames.TryGetValue(steamId, out var formattedName))
                return formattedName;

            // 聊天热路径的降级分支：不要调用 FormatPlayerName / GetPlayerStats，
            // 避免缓存未命中时把聊天消息变成数据库读取路径。
            var group = GetPlayerEffect(steamId);
            var safePlayerName = RichTextSanitizer.SanitizeUntrustedPlayerText(fallbackPlayerName);
            return NameFormatter.FormatPlayerNameWithoutStats(safePlayerName, group?.DisplayEffect);
        }

        public void ClearAll()
        {
            _playerEffects.Clear();
            _formattedPlayerNames.Clear();
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
                .Concat(_formattedPlayerNames.Keys)
                .Distinct()
                .Where(k => !onlineSteamIds.Contains(k))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _playerEffects.TryRemove(key, out _);
                _formattedPlayerNames.TryRemove(key, out _);
            }

            Logger.Debug($"Cleaned up {keysToRemove.Count} offline player entries. Cache size: {_playerEffects.Count}, formatted names: {_formattedPlayerNames.Count}");
        }
    }
}
