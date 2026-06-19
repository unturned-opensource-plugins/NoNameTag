using System;
using System.Text.RegularExpressions;
using Emqo.NoNameTag.Models;

namespace Emqo.NoNameTag.Utilities
{
    public static class ConfigValidator
    {
        private static readonly Regex HexColorRegex = new Regex(@"^#?[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
        private static readonly Regex UnityColorNameRegex = new Regex(@"^[a-zA-Z]+$", RegexOptions.Compiled);
        private static readonly Regex PermissionRegex = new Regex(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);

        public static bool ValidateHexColor(string color, out string error)
        {
            return ValidateColorValue(color, out error);
        }

        public static bool ValidateColorValue(string color, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(color))
            {
                error = "Color cannot be empty";
                return false;
            }

            if (HexColorRegex.IsMatch(color) || IsUnityColorName(color))
                return true;

            error = $"Invalid color value: {color}. Expected #RRGGBB, RRGGBB, or a Unity color name";
            return false;
        }

        private static bool IsUnityColorName(string color)
        {
            if (!UnityColorNameRegex.IsMatch(color))
                return false;

            switch (color.ToLowerInvariant())
            {
                case "black":
                case "blue":
                case "cyan":
                case "gray":
                case "grey":
                case "green":
                case "magenta":
                case "red":
                case "white":
                case "yellow":
                    return true;
                default:
                    return false;
            }
        }

        public static bool ValidatePermission(string permission, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(permission))
            {
                error = "Permission cannot be empty";
                return false;
            }

            if (!PermissionRegex.IsMatch(permission))
            {
                error = $"Invalid permission format: {permission}. Only alphanumeric characters, dots, and underscores are allowed";
                return false;
            }

            return true;
        }

        public static bool ValidatePriority(int priority, out string error, int minValue = 0, int maxValue = 1000)
        {
            error = null;

            if (priority < minValue || priority > maxValue)
            {
                error = $"Priority {priority} is out of range. Expected: {minValue}-{maxValue}";
                return false;
            }

            return true;
        }

        public static bool ValidateDisplayEffect(DisplayEffectConfig effect, out string error)
        {
            error = null;

            if (effect == null)
            {
                error = "DisplayEffect cannot be null";
                return false;
            }

            if (!string.IsNullOrEmpty(effect.PrefixColor) && !ValidateColorValue(effect.PrefixColor, out error))
            {
                error = $"PrefixColor: {error}";
                return false;
            }

            if (!string.IsNullOrEmpty(effect.NameColor) && !ValidateColorValue(effect.NameColor, out error))
            {
                error = $"NameColor: {error}";
                return false;
            }

            if (!string.IsNullOrEmpty(effect.SuffixColor) && !ValidateColorValue(effect.SuffixColor, out error))
            {
                error = $"SuffixColor: {error}";
                return false;
            }

            return true;
        }

        public static bool ValidatePermissionGroup(PermissionGroupConfig group, out string error)
        {
            error = null;

            if (group == null)
            {
                error = "PermissionGroup cannot be null";
                return false;
            }

            if (!ValidatePermission(group.Permission, out error))
            {
                return false;
            }

            if (!ValidatePriority(group.Priority, out error))
            {
                return false;
            }

            if (!ValidateDisplayEffect(group.DisplayEffect, out error))
            {
                return false;
            }

            return true;
        }

        public static bool ValidateConfiguration(NoNameTagConfiguration config, out string error)
        {
            error = null;

            if (config == null)
            {
                error = "Configuration cannot be null";
                return false;
            }

            if (!string.IsNullOrEmpty(config.DefaultNameColor) && !ValidateColorValue(config.DefaultNameColor, out error))
            {
                error = $"DefaultNameColor: {error}";
                return false;
            }

            if (config.PermissionGroups != null)
            {
                for (int i = 0; i < config.PermissionGroups.Count; i++)
                {
                    if (!ValidatePermissionGroup(config.PermissionGroups[i], out error))
                    {
                        error = $"PermissionGroup[{i}]: {error}";
                        return false;
                    }
                }
            }

            if (config.BroadcastGroups != null)
            {
                for (int i = 0; i < config.BroadcastGroups.Count; i++)
                {
                    if (!ValidateBroadcastGroup(config.BroadcastGroups[i], out error))
                    {
                        error = $"BroadcastGroup[{i}]: {error}";
                        return false;
                    }
                }
            }

            if (config.StatsSettings != null && !ValidateStatsSettings(config.StatsSettings, out error))
            {
                error = $"StatsSettings: {error}";
                return false;
            }

            if (config.DeathMessage != null && !ValidateDeathMessage(config.DeathMessage, out error))
            {
                error = $"DeathMessage: {error}";
                return false;
            }

            if (config.WelcomeMessage != null && !ValidateWelcomeMessage(config.WelcomeMessage, out error))
            {
                error = $"WelcomeMessage: {error}";
                return false;
            }

            if (config.TextCommands != null)
            {
                for (int i = 0; i < config.TextCommands.Count; i++)
                {
                    if (!ValidateTextCommand(config.TextCommands[i], out error))
                    {
                        error = $"TextCommand[{i}]: {error}";
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool ValidateStatsSettings(StatsSettingsConfig settings, out string error)
        {
            error = null;

            if (settings == null)
            {
                error = "StatsSettings cannot be null";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(settings.DisplayColor) && !ValidateColorValue(settings.DisplayColor, out error))
            {
                error = $"DisplayColor: {error}";
                return false;
            }

            if (settings.CommandTimeoutSeconds <= 0)
            {
                error = "CommandTimeoutSeconds must be greater than 0";
                return false;
            }

            if (settings.BleedHitRetentionSeconds <= 0)
            {
                error = "BleedHitRetentionSeconds must be greater than 0";
                return false;
            }

            if (settings.BleedSourceRetentionSeconds <= 0)
            {
                error = "BleedSourceRetentionSeconds must be greater than 0";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证广播组配置
        /// </summary>
        public static bool ValidateBroadcastGroup(BroadcastGroupConfig group, out string error)
        {
            error = null;

            if (group == null)
            {
                error = "BroadcastGroup cannot be null";
                return false;
            }

            if (string.IsNullOrWhiteSpace(group.Name))
            {
                error = "BroadcastGroup name cannot be empty";
                return false;
            }

            if (group.RotationInterval <= 0)
            {
                error = $"RotationInterval must be greater than 0. Current: {group.RotationInterval}";
                return false;
            }

            if (group.Messages == null || group.Messages.Count == 0)
            {
                error = $"BroadcastGroup '{group.Name}' must have at least one message";
                return false;
            }

            // 验证每条消息
            for (int i = 0; i < group.Messages.Count; i++)
            {
                var message = group.Messages[i];
                if (message == null)
                {
                    error = $"BroadcastGroup '{group.Name}' message[{i}] cannot be null";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(message.Message))
                {
                    error = $"BroadcastGroup '{group.Name}' message[{i}] cannot be empty";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(message.FontColor) && !ValidateColorValue(message.FontColor, out error))
                {
                    error = $"BroadcastGroup '{group.Name}' message[{i}] FontColor: {error}";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateDeathMessage(DeathMessageConfig config, out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(config.FontColor) && !ValidateColorValue(config.FontColor, out error))
            {
                error = $"FontColor: {error}";
                return false;
            }

            return true;
        }

        private static bool ValidateWelcomeMessage(WelcomeMessageConfig config, out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(config.Color) && !ValidateColorValue(config.Color, out error))
            {
                error = $"Color: {error}";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(config.LeaveColor) && !ValidateColorValue(config.LeaveColor, out error))
            {
                error = $"LeaveColor: {error}";
                return false;
            }

            return true;
        }

        private static bool ValidateTextCommand(TextCommandConfig config, out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(config.Color) && !ValidateColorValue(config.Color, out error))
            {
                error = $"Color: {error}";
                return false;
            }

            return true;
        }
    }
}
