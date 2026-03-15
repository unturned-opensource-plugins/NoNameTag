using System;
using Emqo.NoNameTag.Models;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Utilities
{
    /// <summary>
    /// 统一的名称格式化工具类
    /// </summary>
    public static class NameFormatter
    {
        /// <summary>
        /// 用颜色和大小包装文本（size=0 表示不设置大小）
        /// </summary>
        public static string WrapWithStyle(string text, string hexColor, int fontSize = 0)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var result = FormatColorTag(hexColor, text);

            if (fontSize > 0)
                result = $"<size={fontSize}>{result}</size>";

            return result;
        }

        /// <summary>
        /// 格式化带颜色和大小的名称标签
        /// </summary>
        public static string FormatColoredName(string playerName, DisplayEffectConfig effect)
        {
            if (effect == null)
                return playerName;

            var prefixPart = !string.IsNullOrEmpty(effect.Prefix)
                ? WrapWithStyle(effect.Prefix, effect.PrefixColor, effect.PrefixFontSize)
                : "";
            var namePart = WrapWithStyle(playerName, effect.NameColor, effect.NameFontSize);
            var suffixPart = !string.IsNullOrEmpty(effect.Suffix)
                ? WrapWithStyle(effect.Suffix, effect.SuffixColor, effect.SuffixFontSize)
                : "";

            return $"{prefixPart}{namePart}{suffixPart}";
        }

        public static string FormatPlayerName(string playerName, ulong steamId, DisplayEffectConfig effect = null)
        {
            var resolvedEffect = effect ?? CreateDefaultDisplayEffect();
            var baseName = resolvedEffect != null ? FormatColoredName(playerName, resolvedEffect) : playerName;
            var statsSuffix = BuildStatsSuffix(steamId);

            if (string.IsNullOrEmpty(statsSuffix))
                return baseName;

            return $"{baseName}{statsSuffix}";
        }

        private static DisplayEffectConfig CreateDefaultDisplayEffect()
        {
            var defaultNameColor = NoNameTagPlugin.Instance?.Configuration?.Instance?.DefaultNameColor;
            if (string.IsNullOrWhiteSpace(defaultNameColor))
                defaultNameColor = $"#{Constants.DefaultColor}";

            return new DisplayEffectConfig
            {
                NameColor = defaultNameColor,
                PrefixColor = defaultNameColor,
                SuffixColor = defaultNameColor
            };
        }

        /// <summary>
        /// 格式化带颜色的名称标签（用于聊天消息）
        /// </summary>
        public static string FormatNameWithAvatar(string playerName, DisplayEffectConfig effect)
        {
            return FormatColoredName(playerName, effect);
        }

        /// <summary>
        /// 格式化不带颜色的名称（仅前缀和后缀）
        /// </summary>
        public static string FormatPlainName(string playerName, DisplayEffectConfig effect)
        {
            if (effect == null)
                return playerName;

            var prefix = effect.Prefix ?? "";
            var suffix = effect.Suffix ?? "";

            return $"{prefix}{playerName}{suffix}";
        }

        private static string BuildStatsSuffix(ulong steamId)
        {
            var plugin = NoNameTagPlugin.Instance;
            var settings = plugin?.Configuration?.Instance?.StatsSettings;
            if (plugin?.PlayerStatsService == null
                || settings == null
                || !settings.Enabled
                || !settings.ShowInFormattedNames
                || string.IsNullOrWhiteSpace(settings.DisplayFormat))
            {
                return string.Empty;
            }

            var stats = plugin.PlayerStatsService.GetPlayerStats(steamId);
            if (stats == null)
                return string.Empty;

            var formatted = settings.DisplayFormat
                .Replace("{streak}", stats.CurrentKillstreak.ToString())
                .Replace("{kills}", stats.TotalKills.ToString())
                .Replace("{deaths}", stats.TotalDeaths.ToString());

            if (string.IsNullOrWhiteSpace(settings.DisplayColor) && settings.DisplayFontSize <= 0)
                return formatted;

            return WrapWithStyle(formatted, settings.DisplayColor, settings.DisplayFontSize);
        }

        /// <summary>
        /// 获取十六进制颜色值，移除 # 符号
        /// </summary>
        private static string GetHexColor(string colorValue)
        {
            if (string.IsNullOrEmpty(colorValue))
                return Constants.DefaultColor;

            return colorValue.TrimStart('#');
        }

        /// <summary>
        /// 判断是否为 hex 颜色（纯十六进制字符）
        /// </summary>
        private static bool IsHexColor(string color)
        {
            if (string.IsNullOrEmpty(color)) return false;
            var trimmed = color.TrimStart('#');
            if (trimmed.Length != 6) return false;
            foreach (var c in trimmed)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 生成 color 标签（兼容 hex 和 Unity 颜色名）
        /// </summary>
        private static string FormatColorTag(string colorValue, string text)
        {
            if (string.IsNullOrEmpty(colorValue))
                return string.Format(Constants.ColorTagFormat, Constants.DefaultColor, text);

            if (IsHexColor(colorValue))
                return string.Format(Constants.ColorTagFormat, GetHexColor(colorValue), text);

            // Unity 颜色名（white, red, yellow 等），不加 #
            return $"<color={colorValue}>{text}</color>";
        }

        /// <summary>
        /// 解析十六进制颜色为 Unity Color
        /// </summary>
        public static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Color.white;

            hex = hex.TrimStart('#');

            if (hex.Length != Constants.HexColorLength)
                return Color.white;

            try
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, 255);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to parse color: {hex} - {ex.Message}");
                return Color.white;
            }
        }

        /// <summary>
        /// 格式化带头像的广播消息
        /// </summary>
        public static (string message, string avatarUrl) FormatBroadcastMessageWithAvatar(string message, string avatar, AvatarPosition position)
        {
            if (string.IsNullOrEmpty(avatar))
                return (message, null);

            var isSpriteId = avatar.StartsWith("steam_") || avatar.StartsWith("icon_");

            if (isSpriteId)
            {
                return (message, avatar);
            }

            return position == AvatarPosition.Left
                ? ($"{avatar} {message}", null)
                : ($"{message} {avatar}", null);
        }
    }
}
