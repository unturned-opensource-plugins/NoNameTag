using Emqo.NoNameTag.Models;
using UnityEngine;
using Logger = Emqo.NoNameTag.Utilities.PluginLogger;

namespace Emqo.NoNameTag.Utilities
{
    /// <summary>
    /// 统一的名称格式化工具类，消除代码重复
    /// </summary>
    public static class NameFormatter
    {
        /// <summary>
        /// 判断是否为 Sprite ID（以 steam_ 或 icon_ 开头）
        /// </summary>
        private static bool IsSpriteId(string avatar)
        {
            if (string.IsNullOrEmpty(avatar))
                return false;

            return avatar.StartsWith("steam_") || avatar.StartsWith("icon_");
        }

        /// <summary>
        /// 生成头像标签
        /// </summary>
        private static string BuildAvatarTag(string avatar, int size)
        {
            if (string.IsNullOrEmpty(avatar))
                return "";

            if (IsSpriteId(avatar))
            {
                return $"<sprite name=\"{avatar}\" size={size}>";
            }

            // Emoji 或其他文本
            return avatar;
        }
        /// <summary>
        /// 格式化带颜色的名称标签
        /// </summary>
        public static string FormatColoredName(string playerName, DisplayEffectConfig effect)
        {
            if (effect == null)
                return playerName;

            var prefixHex = GetHexColor(effect.PrefixColor);
            var nameHex = GetHexColor(effect.NameColor);
            var suffixHex = GetHexColor(effect.SuffixColor);

            var prefixPart = !string.IsNullOrEmpty(effect.Prefix)
                ? string.Format(Constants.ColorTagFormat, prefixHex, effect.Prefix)
                : "";
            var namePart = string.Format(Constants.ColorTagFormat, nameHex, playerName);
            var suffixPart = !string.IsNullOrEmpty(effect.Suffix)
                ? string.Format(Constants.ColorTagFormat, suffixHex, effect.Suffix)
                : "";

            return $"{prefixPart}{namePart}{suffixPart}";
        }

        /// <summary>
        /// 格式化不带颜色的名称（仅前缀和后缀）
        /// </summary>
        public static string FormatPlainName(string playerName, DisplayEffectConfig effect)
        {
            if (effect == null)
                return playerName;

            var prefix = !string.IsNullOrEmpty(effect.Prefix) ? effect.Prefix : "";
            var suffix = !string.IsNullOrEmpty(effect.Suffix) ? effect.Suffix : "";

            return $"{prefix}{playerName}{suffix}";
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
            catch
            {
                Logger.Warning($"Failed to parse color: {hex}");
                return Color.white;
            }
        }

        /// <summary>
        /// 格式化带头像的名称标签（用于聊天消息）
        /// </summary>
        public static string FormatNameWithAvatar(string playerName, DisplayEffectConfig effect, AvatarPosition position)
        {
            if (effect == null)
                return playerName;

            var prefixHex = GetHexColor(effect.PrefixColor);
            var nameHex = GetHexColor(effect.NameColor);
            var suffixHex = GetHexColor(effect.SuffixColor);

            var prefixPart = !string.IsNullOrEmpty(effect.Prefix)
                ? string.Format(Constants.ColorTagFormat, prefixHex, effect.Prefix)
                : "";
            var namePart = string.Format(Constants.ColorTagFormat, nameHex, playerName);
            var suffixPart = !string.IsNullOrEmpty(effect.Suffix)
                ? string.Format(Constants.ColorTagFormat, suffixHex, effect.Suffix)
                : "";

            return $"{prefixPart}{namePart}{suffixPart}";
        }

        /// <summary>
        /// 格式化带头像的广播消息
        /// </summary>
        public static (string message, string avatarUrl) FormatBroadcastMessageWithAvatar(string message, string avatar, AvatarPosition position)
        {
            if (string.IsNullOrEmpty(avatar))
                return (message, null);

            // 检查是否为 Sprite ID（以 steam_ 或 icon_ 开头）
            var isSpriteId = avatar.StartsWith("steam_") || avatar.StartsWith("icon_");

            if (isSpriteId)
            {
                // Sprite ID 作为头像 URL
                return (message, avatar);
            }

            // Emoji 或其他文本，直接嵌入消息
            return position == AvatarPosition.Left
                ? ($"{avatar} {message}", null)
                : ($"{message} {avatar}", null);
        }
    }
}
