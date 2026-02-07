using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    public class NameTagDisplayService : INameTagDisplayService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly NameTagManager _nameTagManager;
        private readonly Dictionary<ulong, string> _originalNicknames;

        public NameTagDisplayService(NoNameTagConfiguration config, NameTagManager nameTagManager)
        {
            _config = config;
            _nameTagManager = nameTagManager;
            _originalNicknames = new Dictionary<ulong, string>();
        }

        public void ApplyNameTag(UnturnedPlayer player)
        {
            if (player == null || !_config.Enabled) return;

            try
            {
                var internalPlayer = player.Player;
                if (internalPlayer?.channel?.owner == null) return;

                // playerID 是 struct，不能为 null，但需要检查 steamID 是否有效
                var steamId = internalPlayer.channel.owner.playerID.steamID.m_SteamID;
                if (steamId == 0) return;

                var displayName = internalPlayer.channel.owner.playerID.characterName;
                if (string.IsNullOrEmpty(displayName)) return;

                if (!_originalNicknames.ContainsKey(steamId))
                {
                    _originalNicknames[steamId] = internalPlayer.channel.owner.playerID.nickName ?? displayName;
                }

                var group = _nameTagManager.GetPlayerEffect(steamId);
                if (group == null || group.DisplayEffect == null) return;

                var formattedName = NameFormatter.FormatColoredName(displayName, group.DisplayEffect);
                internalPlayer.channel.owner.playerID.nickName = formattedName;

                Logger.Debug($"Applied name tag to {displayName}");
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, "Error applying name tag");
            }
        }

        public void RemoveNameTag(UnturnedPlayer player)
        {
            if (player == null) return;

            ulong steamId = 0;
            try
            {
                steamId = player.CSteamID.m_SteamID;
            }
            catch
            {
                return;
            }

            if (steamId == 0) return;

            try
            {
                if (_originalNicknames.TryGetValue(steamId, out var originalName))
                {
                    if (player.Player?.channel?.owner != null)
                    {
                        player.Player.channel.owner.playerID.nickName = originalName;
                    }
                    _originalNicknames.Remove(steamId);
                    Logger.Debug($"Removed name tag cache for {steamId}");
                }
            }
            catch
            {
                _originalNicknames.Remove(steamId);
            }
        }

        public void RefreshNameTag(UnturnedPlayer player)
        {
            if (player == null) return;

            RemoveNameTag(player);
            ApplyNameTag(player);
        }

        public void RefreshAllNameTags()
        {
            foreach (var client in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(client);
                if (player != null)
                    RefreshNameTag(player);
            }
        }

        public void ClearAllNameTags()
        {
            foreach (var client in Provider.clients)
            {
                var player = UnturnedPlayer.FromSteamPlayer(client);
                if (player != null)
                    RemoveNameTag(player);
            }
            _originalNicknames.Clear();
        }
    }
}
