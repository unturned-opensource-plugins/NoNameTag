using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emqo.NoNameTag.Commands
{
    public class CustomTextCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "text";
        public string Help => "Execute custom text commands";
        public string Syntax => "/text <command>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (!(caller is UnturnedPlayer player)) return;

            if (command.Length == 0)
            {
                ShowAvailableCommands(player);
                return;
            }

            var cmdName = command[0];
            var config = NoNameTagPlugin.Instance.Configuration.Instance;
            var textCmd = config.TextCommands?.FirstOrDefault(c =>
                string.Equals(c.Name, cmdName, StringComparison.OrdinalIgnoreCase));

            if (textCmd == null)
            {
                UnturnedChat.Say(player, $"未知命令: {cmdName}", Color.red);
                return;
            }

            var message = textCmd.Message.Replace("{", "<").Replace("}", ">");
            var iconUrl = string.IsNullOrEmpty(textCmd.IconUrl) ? null : textCmd.IconUrl;

            ChatManager.serverSendMessage(message, Color.white, null, player.SteamPlayer(), EChatMode.SAY, iconUrl, true);
        }

        private void ShowAvailableCommands(UnturnedPlayer player)
        {
            var config = NoNameTagPlugin.Instance.Configuration.Instance;
            if (config.TextCommands == null || config.TextCommands.Count == 0)
            {
                UnturnedChat.Say(player, "没有可用的文本命令", Color.yellow);
                return;
            }

            var names = string.Join(", ", config.TextCommands.Select(c => c.Name));
            UnturnedChat.Say(player, $"可用命令: {names}", Color.yellow);
        }
    }
}
