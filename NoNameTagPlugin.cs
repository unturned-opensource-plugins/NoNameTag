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
            PlayerLife.onPlayerDied += OnPlayerDied;
        }

        private void UnregisterEventHandlers()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            UnturnedPlayerEvents.OnPlayerChatted -= OnPlayerChatted;
            PlayerLife.onPlayerDied -= OnPlayerDied;
        }

        private void RefreshAllDisplays()
        {
            NameTagManager.RefreshAllPlayers();
        }

        private void InitializeServices()
        {
            PermissionService = new PermissionService(Configuration.Instance);
            NameTagManager = new NameTagManager(Configuration.Instance, PermissionService);
            BroadcastService = new BroadcastService(Configuration.Instance, NameTagManager);

            Logger.Debug("All services initialized");
        }

        public void ReloadServices()
        {
            try
            {
                Logger.DebugEnabled = Configuration.Instance.DebugMode;
                Configuration.Instance.ClearCache();
                BroadcastService?.Dispose();
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
            if (group?.DisplayEffect == null) return (null, null);

            var formattedName = NameFormatter.FormatNameWithAvatar(
                player.DisplayName,
                group.DisplayEffect
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
                BroadcastService?.HandlePlayerDeath(sender, cause, limb, instigator);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Error handling player death");
            }
        }
    }
}
