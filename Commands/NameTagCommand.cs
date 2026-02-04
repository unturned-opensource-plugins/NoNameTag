using Emqo.NoNameTag.Services;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Commands
{
    public class NameTagCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public string Name => "nametag";

        public string Help => "Manage NoNameTag plugin settings";

        public string Syntax => "/nametag <reload|refresh|check> [player]";

        public List<string> Aliases => new List<string> { "nt" };

        public List<string> Permissions => new List<string> { "nonametag.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 0)
            {
                SendMessage(caller, $"Usage: {Syntax}", Color.yellow);
                SendMessage(caller, "Subcommands: reload, refresh, check", Color.yellow);
                SendMessage(caller, "Edit permission groups in the configuration file and use /nametag reload", Color.yellow);
                return;
            }

            var subCommand = command[0].ToLower();

            switch (subCommand)
            {
                case "reload":
                    ExecuteReload(caller);
                    break;
                case "refresh":
                    ExecuteRefresh(caller, command);
                    break;
                case "check":
                    ExecuteCheck(caller, command);
                    break;
                default:
                    SendMessage(caller, $"Unknown subcommand: {subCommand}", Color.red);
                    SendMessage(caller, $"Usage: {Syntax}", Color.yellow);
                    break;
            }
        }

        private void ExecuteReload(IRocketPlayer caller)
        {
            try
            {
                NoNameTagPlugin.Instance.Configuration.Load();
                NoNameTagPlugin.Instance.ReloadServices();
                SendMessage(caller, "Configuration reloaded and all players refreshed.", Color.green);
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex, "Error reloading configuration");
                SendMessage(caller, "Error reloading configuration. Check console for details.", Color.red);
            }
        }

        private void ExecuteRefresh(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 2)
            {
                NoNameTagPlugin.Instance.NameTagManager?.RefreshAllPlayers();
                SendMessage(caller, "All players refreshed.", Color.green);
                return;
            }

            var targetPlayer = UnturnedPlayer.FromName(command[1]);
            if (targetPlayer == null)
            {
                SendMessage(caller, $"Player '{command[1]}' not found.", Color.red);
                return;
            }

            NoNameTagPlugin.Instance.NameTagManager?.RefreshPlayer(targetPlayer);
            SendMessage(caller, $"Refreshed display for {targetPlayer.DisplayName}.", Color.green);
        }

        private void ExecuteCheck(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer targetPlayer;

            if (command.Length < 2)
            {
                if (caller is UnturnedPlayer unturnedCaller)
                {
                    targetPlayer = unturnedCaller;
                }
                else
                {
                    SendMessage(caller, "Please specify a player name.", Color.red);
                    return;
                }
            }
            else
            {
                targetPlayer = UnturnedPlayer.FromName(command[1]);
                if (targetPlayer == null)
                {
                    SendMessage(caller, $"Player '{command[1]}' not found.", Color.red);
                    return;
                }
            }

            var group = NoNameTagPlugin.Instance.NameTagManager?.GetPlayerEffect(targetPlayer.CSteamID.m_SteamID);

            if (group == null)
            {
                SendMessage(caller, $"{targetPlayer.DisplayName} has no special display effect.", Color.white);
            }
            else
            {
                var effect = group.DisplayEffect;
                SendMessage(caller, $"Player: {targetPlayer.DisplayName}", Color.white);
                SendMessage(caller, $"Permission: {group.Permission} (Priority: {group.Priority})", Color.white);
                SendMessage(caller, $"Prefix: {effect.Prefix} | Name Color: {effect.NameColor} | Suffix: {effect.Suffix}", Color.white);
            }
        }

        private void SendMessage(IRocketPlayer caller, string message, Color color)
        {
            if (caller is UnturnedPlayer player)
            {
                UnturnedChat.Say(player, message, color);
            }
            else
            {
                Rocket.Core.Logging.Logger.Log(message);
            }
        }
    }
}
