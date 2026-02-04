using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    public class DeathMessageService : IDeathMessageService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly NameTagManager _nameTagManager;

        public DeathMessageService(NoNameTagConfiguration config, NameTagManager nameTagManager)
        {
            _config = config;
            _nameTagManager = nameTagManager;
        }

        public void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!_config.Enabled || _config.DeathMessage == null || !_config.DeathMessage.Enabled)
                return;

            try
            {
                var victim = UnturnedPlayer.FromPlayer(sender.player);
                if (victim == null) return;

                UnturnedPlayer killer = null;
                if (instigator != CSteamID.Nil && instigator.m_SteamID != 0)
                {
                    killer = UnturnedPlayer.FromCSteamID(instigator);
                }

                var message = FormatDeathMessage(victim, killer, cause);
                if (!string.IsNullOrEmpty(message))
                {
                    BroadcastDeathMessage(message);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, "Error handling player death");
            }
        }

        private string FormatDeathMessage(UnturnedPlayer victim, UnturnedPlayer killer, EDeathCause cause)
        {
            var deathConfig = _config.DeathMessage;
            string format;

            bool isSelfKill = killer != null && killer.CSteamID == victim.CSteamID;
            bool isPlayerKill = killer != null && !isSelfKill;

            if (isSelfKill)
            {
                format = deathConfig.SelfKillFormat;
            }
            else if (isPlayerKill)
            {
                format = deathConfig.Format;
            }
            else
            {
                format = GetFormatByCause(cause, deathConfig);
            }

            var victimName = FormatPlayerName(victim);
            var killerName = killer != null ? FormatPlayerName(killer) : "";

            // 使用 StringBuilder 优化字符串替换
            var replacements = new Dictionary<string, string>
            {
                { "{victim}", victimName },
                { "{killer}", killerName }
            };

            return ReplaceMultiple(format, replacements);
        }

        /// <summary>
        /// 使用 StringBuilder 进行多个字符串替换，性能更优
        /// </summary>
        private string ReplaceMultiple(string text, Dictionary<string, string> replacements)
        {
            var sb = new StringBuilder(text);
            foreach (var kvp in replacements)
            {
                sb.Replace(kvp.Key, kvp.Value);
            }
            return sb.ToString();
        }

        private string GetFormatByCause(EDeathCause cause, DeathMessageConfig config)
        {
            switch (cause)
            {
                case EDeathCause.BLEEDING:
                    return config.BleedingFormat;
                case EDeathCause.ZOMBIE:
                    return config.ZombieFormat;
                case EDeathCause.ANIMAL:
                    return config.AnimalFormat;
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                    return config.VehicleFormat;
                case EDeathCause.BONES:
                    return config.FallFormat;
                case EDeathCause.BREATH:
                    return config.DrownFormat;
                case EDeathCause.FREEZING:
                    return config.FreezeFormat;
                case EDeathCause.BURNING:
                case EDeathCause.BURNER:
                    return config.BurnFormat;
                case EDeathCause.FOOD:
                    return config.FoodFormat;
                case EDeathCause.WATER:
                    return config.WaterFormat;
                case EDeathCause.INFECTION:
                    return config.InfectionFormat;
                case EDeathCause.SUICIDE:
                    return config.SelfKillFormat;
                default:
                    return config.DefaultFormat;
            }
        }

        private string FormatPlayerName(UnturnedPlayer player)
        {
            var group = _nameTagManager.GetPlayerEffect(player.CSteamID.m_SteamID);
            return group?.DisplayEffect != null
                ? NameFormatter.FormatColoredName(player.DisplayName, group.DisplayEffect)
                : player.DisplayName;
        }

        private void BroadcastDeathMessage(string message)
        {
            ChatManager.serverSendMessage(message, Color.white, null, null, EChatMode.GLOBAL, null, true);
        }
    }
}
