using System;
using System.Text.RegularExpressions;
using Emqo.NoNameTag.Models;

namespace Emqo.NoNameTag.Utilities
{
    public static class ConfigValidator
    {
        private static readonly Regex HexColorRegex = new Regex(@"^#?[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
        private static readonly Regex PermissionRegex = new Regex(@"^[a-zA-Z0-9_.]+$", RegexOptions.Compiled);

        public static bool ValidateHexColor(string color, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(color))
            {
                error = "Color cannot be empty";
                return false;
            }

            if (!HexColorRegex.IsMatch(color))
            {
                error = $"Invalid hex color format: {color}. Expected format: #RRGGBB or RRGGBB";
                return false;
            }

            return true;
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

            if (!string.IsNullOrEmpty(effect.PrefixColor) && !ValidateHexColor(effect.PrefixColor, out error))
            {
                error = $"PrefixColor: {error}";
                return false;
            }

            if (!string.IsNullOrEmpty(effect.NameColor) && !ValidateHexColor(effect.NameColor, out error))
            {
                error = $"NameColor: {error}";
                return false;
            }

            if (!string.IsNullOrEmpty(effect.SuffixColor) && !ValidateHexColor(effect.SuffixColor, out error))
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

            if (!string.IsNullOrEmpty(config.DefaultNameColor) && !ValidateHexColor(config.DefaultNameColor, out error))
            {
                error = $"DefaultNameColor: {error}";
                return false;
            }

            // 验证头像配置
            if (config.AvatarSettings != null)
            {
                if (config.AvatarSettings.ChatAvatarSize <= 0 || config.AvatarSettings.ChatAvatarSize > 128)
                {
                    error = $"ChatAvatarSize must be between 1 and 128. Current: {config.AvatarSettings.ChatAvatarSize}";
                    return false;
                }
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

                if (message.DelaySeconds < 0)
                {
                    error = $"BroadcastGroup '{group.Name}' message[{i}] DelaySeconds cannot be negative";
                    return false;
                }
            }

            return true;
        }
    }
}
