using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emqo.NoNameTag.Commands
{
    public class WebLinkCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "web";
        public string Help => "Open website links";
        public string Syntax => "/web <command>";
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
            var webCmd = config.WebCommands?.FirstOrDefault(c =>
                string.Equals(c.Name, cmdName, StringComparison.OrdinalIgnoreCase));

            if (webCmd == null)
            {
                UnturnedChat.Say(player, $"未知命令: {cmdName}", Color.red);
                return;
            }

            player.Player.sendBrowserRequest(webCmd.Description, webCmd.Url);
        }

        private void ShowAvailableCommands(UnturnedPlayer player)
        {
            var config = NoNameTagPlugin.Instance.Configuration.Instance;
            if (config.WebCommands == null || config.WebCommands.Count == 0)
            {
                UnturnedChat.Say(player, "没有可用的网站命令", Color.yellow);
                return;
            }

            var names = string.Join(", ", config.WebCommands.Select(c => c.Name));
            UnturnedChat.Say(player, $"可用命令: {names}", Color.yellow);
        }
    }
}
