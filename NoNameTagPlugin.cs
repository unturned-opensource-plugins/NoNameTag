using Emqo.NoNameTag.Services;
using Emqo.NoNameTag.Utilities;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag
{
    public class NoNameTagPlugin : RocketPlugin<NoNameTagConfiguration>
    {
        public static NoNameTagPlugin Instance { get; private set; }

        public IPermissionService PermissionService { get; private set; }
        public INameTagManager NameTagManager { get; private set; }
        public IBroadcastService BroadcastService { get; private set; }
        public IPlayerStatsService PlayerStatsService { get; private set; }
        public IDamageAttributionService DamageAttributionService { get; private set; }

        protected override void Load()
        {
            Instance = this;
            Logger.DebugEnabled = Configuration.Instance.DebugMode;

            try
            {
                if (!ConfigValidator.ValidateConfiguration(Configuration.Instance, out var configError))
                {
                    Logger.Warning($"Configuration validation failed: {configError}");
                }

                InitializeServices();
                RegisterEventHandlers();
                RefreshAllDisplays();
                BroadcastService?.StartAllBroadcasts();

                Logger.Info($"{Name} {Assembly.GetName().Version.ToString(3)} has been loaded!");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to load plugin");
            }
        }

        protected override void Unload()
        {
            try
            {
                UnregisterEventHandlers();
                BroadcastService?.Dispose();
                DamageAttributionService?.ClearAll();
                PlayerStatsService?.Dispose();
                NameTagManager?.ClearAll();
                PermissionService?.ClearAllCache();
                UnityMainThreadDispatcher.DestroyInstance();
                Instance = null;
                Logger.Info($"{Name} has been unloaded!");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error during plugin unload");
            }
        }

        private void RegisterEventHandlers()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerChatted += OnPlayerChatted;
            DamageTool.damagePlayerRequested += OnDamagePlayerRequested;
            PlayerLife.OnTellBleeding_Global += OnPlayerBleedingUpdated;
            PlayerLife.OnRevived_Global += OnPlayerRevived;
            PlayerLife.onPlayerDied += OnPlayerDied;
        }

        private void UnregisterEventHandlers()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerChatted -= OnPlayerChatted;
            DamageTool.damagePlayerRequested -= OnDamagePlayerRequested;
            PlayerLife.OnTellBleeding_Global -= OnPlayerBleedingUpdated;
            PlayerLife.OnRevived_Global -= OnPlayerRevived;
            PlayerLife.onPlayerDied -= OnPlayerDied;
        }

        private void RefreshAllDisplays()
        {
            NameTagManager.RefreshAllPlayers();
        }

        private void InitializeServices()
        {
            PermissionService = new PermissionService(Configuration.Instance);
            PlayerStatsService = new PlayerStatsService(Configuration.Instance.StatsSettings);
            DamageAttributionService = new DamageAttributionService(Configuration.Instance.StatsSettings);
            NameTagManager = new NameTagManager(Configuration.Instance, PermissionService);
            BroadcastService = new BroadcastService(Configuration.Instance, NameTagManager, DamageAttributionService);

            Logger.Debug("All services initialized");
        }

        public void ReloadServices()
        {
            try
            {
                Logger.DebugEnabled = Configuration.Instance.DebugMode;
                Configuration.Instance.ClearCache();
                BroadcastService?.Dispose();
                DamageAttributionService?.ClearAll();
                PlayerStatsService?.Dispose();
                NameTagManager?.ClearAll();
                PermissionService?.ClearAllCache();
                InitializeServices();
                RefreshAllDisplays();
                BroadcastService?.StartAllBroadcasts();
                Logger.Info("Services reloaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Failed to reload services");
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (!IsValidPlayerConnection(player)) return;

            try
            {
                ApplyPlayerEffects(player);
                LogPlayerConnection(player);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling player connect: {player?.DisplayName}");
            }
        }

        private bool IsValidPlayerConnection(UnturnedPlayer player)
        {
            return Configuration.Instance.Enabled
                && player != null
                && player.Player != null
                && player.CSteamID != CSteamID.Nil;
        }

        private void ApplyPlayerEffects(UnturnedPlayer player)
        {
            PlayerStatsService?.EnsurePlayer(player.CSteamID.m_SteamID);
            NameTagManager.ApplyDisplayEffect(player);
            BroadcastService?.SendWelcomeMessage(player);
        }

        private void LogPlayerConnection(UnturnedPlayer player)
        {
            Logger.Debug($"Player connected: {player.DisplayName}");
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;

            try
            {
                CleanupPlayerData(player);
                LogPlayerDisconnection(player);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling player disconnect: {player?.DisplayName}");
            }
        }

        private void CleanupPlayerData(UnturnedPlayer player)
        {
            BroadcastService?.SendLeaveMessage(player);
            DamageAttributionService?.ClearVictim(player.CSteamID.m_SteamID);
            NameTagManager.RemoveDisplayEffect(player);
            PermissionService?.ClearPlayerCache(player.CSteamID.m_SteamID);
        }

        private void LogPlayerDisconnection(UnturnedPlayer player)
        {
            Logger.Debug($"Player disconnected: {player.DisplayName}");
        }

        private void OnPlayerChatted(UnturnedPlayer player, ref Color color, string message, EChatMode chatMode, ref bool cancel)
        {
            // 跳过命令消息，让 Rocket 处理
            if (message.StartsWith("/") || cancel)
                return;

            if (!IsValidChatMessage(player)) return;

            try
            {
                var (formattedMessage, avatarUrl) = BuildFormattedChatMessage(player, message, chatMode);
                if (string.IsNullOrEmpty(formattedMessage)) return;

                cancel = true;
                Logger.Debug($"Chat message - Player: {player.DisplayName}, SteamID: {player.CSteamID.m_SteamID}, ChatMode: {chatMode}, Message: {formattedMessage}", LogCategory.Plugin);

                // 根据聊天模式决定发送范围
                switch (chatMode)
                {
                    case EChatMode.LOCAL:
                        // 区域聊天：只发给附近玩家
                        foreach (var client in Provider.clients)
                        {
                            if (client?.player == null) continue;
                            var distance = (client.player.transform.position - player.Player.transform.position).sqrMagnitude;
                            // Unturned 区域聊天默认范围约 64m（sqrMagnitude = 4096）
                            if (distance <= 4096f)
                            {
                                ChatManager.serverSendMessage(formattedMessage, Color.white, player.SteamPlayer(), client, EChatMode.LOCAL, avatarUrl, true);
                            }
                        }
                        break;

                    case EChatMode.GROUP:
                        // 组聊天：只发给同组玩家
                        var senderGroupId = player.Player.quests.groupID;
                        if (senderGroupId == CSteamID.Nil)
                        {
                            // 不在组里，只发给自己
                            ChatManager.serverSendMessage(formattedMessage, Color.white, player.SteamPlayer(), player.SteamPlayer(), EChatMode.GROUP, avatarUrl, true);
                        }
                        else
                        {
                            foreach (var client in Provider.clients)
                            {
                                if (client?.player == null) continue;
                                var clientPlayer = UnturnedPlayer.FromSteamPlayer(client);
                                if (clientPlayer?.Player?.quests?.groupID == senderGroupId)
                                {
                                    ChatManager.serverSendMessage(formattedMessage, Color.white, player.SteamPlayer(), client, EChatMode.GROUP, avatarUrl, true);
                                }
                            }
                        }
                        break;

                    default:
                        // GLOBAL / SAY：发给所有人
                        ChatManager.serverSendMessage(formattedMessage, Color.white, player.SteamPlayer(), null, chatMode, avatarUrl, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error handling chat message from {player?.DisplayName}");
            }
        }

        private bool IsValidChatMessage(UnturnedPlayer player)
        {
            return Configuration.Instance.Enabled
                && Configuration.Instance.ApplyToChatMessages
                && player != null;
        }

        private (string message, string avatarUrl) BuildFormattedChatMessage(UnturnedPlayer player, string message, EChatMode chatMode)
        {
            var group = NameTagManager.GetPlayerEffect(player.CSteamID.m_SteamID);
            var formattedName = NameFormatter.FormatPlayerName(
                player.DisplayName,
                player.CSteamID.m_SteamID,
                group?.DisplayEffect
            );

            // 不设置 iconUrl，让 Unturned 自动显示玩家的 Steam 头像
            string avatarUrl = null;

            // 过滤玩家输入中的富文本标签，防止 Rich Text 注入
            var safeMessage = message.Replace("<", "").Replace(">", "").Replace("{", "").Replace("}", "");

            // 根据聊天模式添加 [A]/[G] 前缀
            string modePrefix = "";
            if (chatMode == EChatMode.LOCAL)
                modePrefix = "[A] ";
            else if (chatMode == EChatMode.GROUP)
                modePrefix = "[G] ";

            // 构建最终消息（仅对格式化名称部分转换富文本）
            string finalMessage = $"{modePrefix}{formattedName}: {safeMessage}";

            return (finalMessage, avatarUrl);
        }

        private void OnPlayerDied(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            if (!Configuration.Instance.Enabled) return;

            try
            {
                var victimSteamId = sender?.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                var resolvedKillerSteamId = ResolveKillerSteamId(victimSteamId, cause, instigator);

                if (victimSteamId != 0)
                {
                    PlayerStatsService?.RecordPlayerDeath(victimSteamId, resolvedKillerSteamId ?? 0);
                }

                BroadcastService?.HandlePlayerDeath(sender, cause, limb, instigator);

                if (victimSteamId != 0)
                {
                    DamageAttributionService?.ClearVictim(victimSteamId);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error handling player death");
            }
        }

        private void OnDamagePlayerRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (!Configuration.Instance.Enabled)
                return;

            if (!shouldAllow)
                return;

            try
            {
                var victim = parameters.player;
                if (victim == null)
                    return;

                var attackerSteamId = parameters.killer.m_SteamID;
                var victimSteamId = victim.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                var bleedingModifier = parameters.bleedingModifier;
                if (attackerSteamId == 0 || victimSteamId == 0 || attackerSteamId == victimSteamId)
                {
                    return;
                }

                if (IsAttributionTrackableCause(parameters.cause))
                {
                    var weaponName = ResolveWeaponName(parameters, attackerSteamId);
                    var distanceMeters = ResolveHitDistanceMeters(parameters, attackerSteamId);
                    DamageAttributionService?.RecordAttributedHit(attackerSteamId, victimSteamId, parameters.cause, weaponName, distanceMeters);

                    var isAlreadyBleeding = victim.life?.isBleeding == true;
                    if (bleedingModifier != DamagePlayerParameters.Bleeding.Never
                        && bleedingModifier != DamagePlayerParameters.Bleeding.Heal
                        && IsBleedTrackableCause(parameters.cause)
                        && (bleedingModifier == DamagePlayerParameters.Bleeding.Always || isAlreadyBleeding))
                    {
                        DamageAttributionService?.HandleBleedingStateChanged(victimSteamId, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Skipped damage attribution update: {ex.Message}", LogCategory.Plugin);
            }
        }

        private void OnPlayerBleedingUpdated(PlayerLife playerLife)
        {
            if (!Configuration.Instance.Enabled || playerLife == null)
                return;

            try
            {
                var steamId = playerLife.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
                if (steamId == 0)
                    return;

                DamageAttributionService?.HandleBleedingStateChanged(steamId, playerLife.isBleeding);
                DamageAttributionService?.ClearExpired();
            }
            catch (Exception ex)
            {
                Logger.Debug($"Skipped bleeding attribution update: {ex.Message}", LogCategory.Plugin);
            }
        }

        private void OnPlayerRevived(PlayerLife playerLife)
        {
            var steamId = playerLife?.channel?.owner?.playerID.steamID.m_SteamID ?? 0UL;
            if (steamId == 0)
                return;

            DamageAttributionService?.ClearVictim(steamId);
        }

        private ulong? ResolveKillerSteamId(ulong victimSteamId, EDeathCause cause, CSteamID instigator)
        {
            if (instigator != CSteamID.Nil && instigator.m_SteamID != 0 && instigator.m_SteamID != victimSteamId)
            {
                return instigator.m_SteamID;
            }

            if (cause == EDeathCause.BLEEDING
                && DamageAttributionService != null
                && DamageAttributionService.TryGetBleedKillerSteamId(victimSteamId, out var bleedKillerSteamId)
                && bleedKillerSteamId != 0
                && bleedKillerSteamId != victimSteamId)
            {
                return bleedKillerSteamId;
            }

            if (SupportsRecentAttribution(cause)
                && DamageAttributionService != null
                && DamageAttributionService.TryGetRecentKillerSteamId(victimSteamId, out var recentKillerSteamId)
                && recentKillerSteamId != 0
                && recentKillerSteamId != victimSteamId)
            {
                return recentKillerSteamId;
            }

            return null;
        }

        private static bool IsAttributionTrackableCause(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GUN:
                case EDeathCause.MELEE:
                case EDeathCause.PUNCH:
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.CHARGE:
                case EDeathCause.SPLASH:
                case EDeathCause.SHRED:
                case EDeathCause.LANDMINE:
                case EDeathCause.SENTRY:
                case EDeathCause.KILL:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBleedTrackableCause(EDeathCause cause)
        {
            return IsAttributionTrackableCause(cause);
        }

        private static bool SupportsRecentAttribution(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.CHARGE:
                case EDeathCause.SPLASH:
                case EDeathCause.LANDMINE:
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                case EDeathCause.BURNING:
                case EDeathCause.BURNER:
                    return true;
                default:
                    return false;
            }
        }

        private static string ResolveWeaponName(DamagePlayerParameters parameters, ulong attackerSteamId)
        {
            var attacker = TryResolvePlayer(attackerSteamId);
            if (attacker?.Player?.equipment?.asset is ItemAsset itemAsset && !string.IsNullOrWhiteSpace(itemAsset.itemName))
            {
                return itemAsset.itemName;
            }

            return GetFallbackWeaponName(parameters.cause);
        }

        private static int? ResolveHitDistanceMeters(DamagePlayerParameters parameters, ulong attackerSteamId)
        {
            var attacker = TryResolvePlayer(attackerSteamId);
            var victim = parameters.player;
            if (attacker?.Player?.transform == null || victim?.transform == null)
            {
                return null;
            }

            try
            {
                var distance = Vector3.Distance(attacker.Player.transform.position, victim.transform.position);
                if (float.IsNaN(distance) || float.IsInfinity(distance))
                {
                    return null;
                }

                return Mathf.RoundToInt(distance);
            }
            catch
            {
                return null;
            }
        }

        private static UnturnedPlayer TryResolvePlayer(ulong steamId)
        {
            if (steamId == 0)
            {
                return null;
            }

            try
            {
                return UnturnedPlayer.FromCSteamID(new CSteamID(steamId));
            }
            catch
            {
                return null;
            }
        }

        private static string GetFallbackWeaponName(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GUN: return "枪械";
                case EDeathCause.MELEE: return "近战";
                case EDeathCause.PUNCH: return "拳击";
                case EDeathCause.GRENADE: return "手雷";
                case EDeathCause.MISSILE: return "导弹";
                case EDeathCause.CHARGE: return "炸药";
                case EDeathCause.LANDMINE: return "地雷";
                case EDeathCause.SPLASH: return "爆炸";
                case EDeathCause.SHRED: return "陷阱";
                case EDeathCause.SENTRY: return "哨戒炮";
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL: return "载具";
                case EDeathCause.KILL: return "管理员处决";
                default: return null;
            }
        }
    }
}
