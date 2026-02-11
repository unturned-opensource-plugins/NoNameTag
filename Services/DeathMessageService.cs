using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 死亡消息服务
    /// </summary>
    internal class DeathMessageService
    {
        private readonly NoNameTagConfiguration _config;
        private readonly INameTagManager _nameTagManager;

        public DeathMessageService(NoNameTagConfiguration config, INameTagManager nameTagManager)
        {
            _config = config;
            _nameTagManager = nameTagManager;
        }

        public void HandlePlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!_config.Enabled || _config.Broadcast?.DeathMessage == null || !_config.Broadcast.DeathMessage.Enabled)
                return;

            if (_config.Broadcast.DeathMessage.DisplayMode == DisplayMode.None)
                return;

            try
            {
                if (sender == null || sender.player == null)
                    return;

                var victim = UnturnedPlayer.FromPlayer(sender.player);
                if (victim == null) return;

                UnturnedPlayer killer = null;

                if (instigator != CSteamID.Nil && instigator.m_SteamID != 0)
                {
                    try
                    {
                        killer = UnturnedPlayer.FromCSteamID(instigator);
                        if (killer != null && killer.CSteamID.m_SteamID == victim.CSteamID.m_SteamID)
                        {
                            killer = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Could not get killer: {ex.Message}", LogCategory.DeathMessage);
                        killer = null;
                    }
                }

                var message = FormatDeathMessage(victim, killer, cause);
                if (!string.IsNullOrEmpty(message))
                {
                    BroadcastDeathMessage(message, victim, killer);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error handling player death", LogCategory.DeathMessage);
            }
        }

        private string FormatDeathMessage(UnturnedPlayer victim, UnturnedPlayer killer, EDeathCause cause)
        {
            if (victim == null)
                return null;

            try
            {
                var deathConfig = _config.Broadcast.DeathMessage;
                if (deathConfig == null)
                    return null;

                bool isSelfKill = false;
                bool isPlayerKill = false;

                if (killer != null)
                {
                    isSelfKill = killer.CSteamID == victim.CSteamID;
                    isPlayerKill = !isSelfKill;
                }

                string format;
                if (isSelfKill)
                    format = deathConfig.SelfKillFormat;
                else if (isPlayerKill)
                    format = deathConfig.Format;
                else
                    format = GetFormatByCause(cause, deathConfig);

                var victimName = FormatPlayerName(victim);
                var killerName = killer != null ? FormatPlayerName(killer) : "";

                var result = format.Replace("{victim}", victimName).Replace("{killer}", killerName);

                // 应用死亡消息字体颜色和大小
                if (!string.IsNullOrEmpty(deathConfig.FontColor))
                    result = NameFormatter.WrapWithStyle(result, deathConfig.FontColor, deathConfig.FontSize);
                else if (deathConfig.FontSize > 0)
                    result = $"<size={deathConfig.FontSize}>{result}</size>";

                return result;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error in FormatDeathMessage", LogCategory.DeathMessage);
                return null;
            }
        }

        private string GetFormatByCause(EDeathCause cause, DeathMessageConfig config)
        {
            switch (cause)
            {
                case EDeathCause.BLEEDING: return config.BleedingFormat;
                case EDeathCause.ZOMBIE: return config.ZombieFormat;
                case EDeathCause.ANIMAL: return config.AnimalFormat;
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL: return config.VehicleFormat;
                case EDeathCause.BONES: return config.FallFormat;
                case EDeathCause.BREATH: return config.DrownFormat;
                case EDeathCause.FREEZING: return config.FreezeFormat;
                case EDeathCause.BURNING:
                case EDeathCause.BURNER: return config.BurnFormat;
                case EDeathCause.FOOD: return config.FoodFormat;
                case EDeathCause.WATER: return config.WaterFormat;
                case EDeathCause.INFECTION: return config.InfectionFormat;
                case EDeathCause.SUICIDE: return config.SelfKillFormat;
                case EDeathCause.GUN: return config.GunFormat;
                case EDeathCause.MELEE: return config.MeleeFormat;
                case EDeathCause.PUNCH: return config.PunchFormat;
                case EDeathCause.GRENADE: return config.GrenadeFormat;
                case EDeathCause.SHRED: return config.ShredFormat;
                case EDeathCause.LANDMINE: return config.LandmineFormat;
                case EDeathCause.ARENA: return config.ArenaFormat;
                case EDeathCause.MISSILE: return config.MissileFormat;
                case EDeathCause.CHARGE: return config.ChargeFormat;
                case EDeathCause.SPLASH: return config.SplashFormat;
                case EDeathCause.SENTRY: return config.SentryFormat;
                case EDeathCause.ACID: return config.AcidFormat;
                case EDeathCause.BOULDER: return config.BoulderFormat;
                case EDeathCause.SPARK: return config.SparkFormat;
                case EDeathCause.SPIT: return config.SpitFormat;
                case EDeathCause.KILL: return config.KillFormat;
                default: return config.DefaultFormat;
            }
        }

        private string FormatPlayerName(UnturnedPlayer player)
        {
            if (player == null)
                return "Unknown";

            string playerName;
            try
            {
                playerName = player.DisplayName ?? player.CharacterName ?? "Unknown";
            }
            catch
            {
                playerName = "Unknown";
            }

            var group = _nameTagManager?.GetPlayerEffect(player.CSteamID.m_SteamID);
            if (group?.DisplayEffect != null)
                return NameFormatter.FormatColoredName(playerName, group.DisplayEffect);

            return playerName;
        }

        private void BroadcastDeathMessage(string message, UnturnedPlayer victim, UnturnedPlayer killer)
        {
            var displayMode = _config.Broadcast.DeathMessage.DisplayMode;
            var visibility = _config.Broadcast.DeathMessage.Visibility;

            if (displayMode == DisplayMode.Console || displayMode == DisplayMode.Both)
                Logger.Info($"[死亡] {BroadcastHelper.StripRichText(message)}", LogCategory.DeathMessage);

            if (displayMode == DisplayMode.Chat || displayMode == DisplayMode.Both)
                SendDeathMessageToChat(message, visibility, victim, killer);

            Logger.Debug($"Death message broadcasted: {message} (DisplayMode: {displayMode}, Visibility: {visibility})", LogCategory.DeathMessage);
        }

        private void SendDeathMessageToChat(string message, DeathMessageVisibility visibility, UnturnedPlayer victim, UnturnedPlayer killer)
        {
            switch (visibility)
            {
                case DeathMessageVisibility.All:
                    ChatManager.serverSendMessage(message, Color.white, null, null, EChatMode.GLOBAL, null, true);
                    break;

                case DeathMessageVisibility.KillerOnly:
                    SendToPlayer(killer, message);
                    break;

                case DeathMessageVisibility.VictimOnly:
                    SendToPlayer(victim, message);
                    break;

                case DeathMessageVisibility.KillerAndVictimOnly:
                    SendToPlayer(killer, message);
                    SendToPlayer(victim, message);
                    break;
            }
        }

        private void SendToPlayer(UnturnedPlayer player, string message)
        {
            if (player == null) return;
            var steamPlayer = BroadcastHelper.GetSteamPlayer(player.CSteamID);
            if (steamPlayer != null)
                ChatManager.serverSendMessage(message, Color.white, steamPlayer, null, EChatMode.SAY, null, true);
        }
    }
}
