using Emqo.NoNameTag.Utilities;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Services
{
    /// <summary>
    /// 欢迎/离开消息服务
    /// </summary>
    internal class WelcomeMessageService
    {
        private readonly NoNameTagConfiguration _config;

        public WelcomeMessageService(NoNameTagConfiguration config)
        {
            _config = config;
        }

        public void SendWelcomeMessage(UnturnedPlayer player)
        {
            if (player == null || _config.WelcomeMessage == null || !_config.WelcomeMessage.Enabled)
                return;

            try
            {
                var welcomeConfig = _config.WelcomeMessage;
                var messageText = BroadcastHelper.ReplaceVariables(welcomeConfig.Text);
                messageText = messageText.Replace("{player}", player.DisplayName);
                messageText = messageText.Replace("{br}", "\n");
                messageText = messageText.Replace("{", "<").Replace("}", ">");

                // 应用欢迎消息字体颜色和大小
                if (!string.IsNullOrEmpty(welcomeConfig.Color))
                    messageText = NameFormatter.WrapWithStyle(messageText, welcomeConfig.Color, welcomeConfig.FontSize);
                else if (welcomeConfig.FontSize > 0)
                    messageText = $"<size={welcomeConfig.FontSize}>{messageText}</size>";

                var iconUrl = string.IsNullOrEmpty(welcomeConfig.IconUrl) ? null : BroadcastHelper.ReplaceVariables(welcomeConfig.IconUrl);

                ChatManager.serverSendMessage(messageText, Color.white, null, null, EChatMode.GLOBAL, iconUrl, true);

                if (welcomeConfig.EnableJoinLink && !string.IsNullOrEmpty(welcomeConfig.JoinLinkUrl))
                {
                    var linkMessage = $"<link=\"{welcomeConfig.JoinLinkUrl}\">{welcomeConfig.JoinLinkMessage}</link>";
                    ChatManager.serverSendMessage(linkMessage, Color.white, null, player.SteamPlayer(), EChatMode.SAY, iconUrl, true);
                }

                Logger.Debug($"Welcome message sent for player: {player.DisplayName}", LogCategory.Plugin);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error sending welcome message for {player?.DisplayName}", LogCategory.Plugin);
            }
        }

        public void SendLeaveMessage(UnturnedPlayer player)
        {
            if (player == null || _config.WelcomeMessage == null || !_config.WelcomeMessage.EnableLeaveMessage)
                return;

            try
            {
                var welcomeConfig = _config.WelcomeMessage;
                var messageText = BroadcastHelper.ReplaceVariables(welcomeConfig.LeaveText);
                messageText = messageText.Replace("{player}", player.DisplayName);
                messageText = messageText.Replace("{br}", "\n");
                messageText = messageText.Replace("{", "<").Replace("}", ">");

                // 应用离开消息字体颜色和大小
                if (!string.IsNullOrEmpty(welcomeConfig.LeaveColor))
                    messageText = NameFormatter.WrapWithStyle(messageText, welcomeConfig.LeaveColor, welcomeConfig.LeaveFontSize);
                else if (welcomeConfig.LeaveFontSize > 0)
                    messageText = $"<size={welcomeConfig.LeaveFontSize}>{messageText}</size>";

                var iconUrl = string.IsNullOrEmpty(welcomeConfig.LeaveIconUrl) ? null : BroadcastHelper.ReplaceVariables(welcomeConfig.LeaveIconUrl);

                ChatManager.serverSendMessage(messageText, Color.white, null, null, EChatMode.GLOBAL, iconUrl, true);

                Logger.Debug($"Leave message sent for player: {player.DisplayName}", LogCategory.Plugin);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"Error sending leave message for {player?.DisplayName}", LogCategory.Plugin);
            }
        }
    }
}
