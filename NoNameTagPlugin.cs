using Emqo.NoNameTag.Models;
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

        public PermissionService PermissionService { get; private set; }
        public NameTagManager NameTagManager { get; private set; }
        public BroadcastService BroadcastService { get; private set; }

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
                var (formattedMessage, avatarUrl) = BuildFormattedChatMessage(player, message);
                if (string.IsNullOrEmpty(formattedMessage)) return;

                cancel = true;
                Logger.Debug($"Chat message - Player: {player.DisplayName}, SteamID: {player.CSteamID.m_SteamID}, AvatarUrl: {avatarUrl ?? "null"}, Message: {formattedMessage}", LogCategory.Plugin);
                // 参数顺序：message, color, fromPlayer, toPlayer, chatMode, iconUrl, useRichText
                ChatManager.serverSendMessage(formattedMessage, Color.white, player.SteamPlayer(), null, chatMode, avatarUrl, true);
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

        private (string message, string avatarUrl) BuildFormattedChatMessage(UnturnedPlayer player, string message)
        {
            var group = NameTagManager.GetPlayerEffect(player.CSteamID.m_SteamID);
            if (group?.DisplayEffect == null) return (null, null);

            var formattedName = NameFormatter.FormatNameWithAvatar(
                player.DisplayName,
                group.DisplayEffect,
                AvatarPosition.Left
            );

            // 不设置 iconUrl，让 Unturned 自动显示玩家的 Steam 头像
            string avatarUrl = null;

            // 构建最终消息
            string finalMessage = $"{formattedName}: {message}";

            // 转换富文本格式：{ 和 } 转换为 < 和 >
            finalMessage = finalMessage.Replace("{", "<").Replace("}", ">");

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
