namespace Emqo.NoNameTag.Utilities
{
    /// <summary>
    /// 项目全局常量定义
    /// </summary>
    public static class Constants
    {
        // ==================== 颜色相关 ====================
        /// <summary>Unity 颜色标签格式</summary>
        public const string ColorTagFormat = "<color=#{0}>{1}</color>";

        /// <summary>默认颜色（白色）</summary>
        public const string DefaultColor = "FFFFFF";

        /// <summary>十六进制颜色长度</summary>
        public const int HexColorLength = 6;

        /// <summary>十六进制颜色正则表达式</summary>
        public const string HexColorPattern = @"^#?[0-9A-Fa-f]{6}$";

        // ==================== 优先级相关 ====================
        /// <summary>最小优先级</summary>
        public const int MinPriority = 0;

        /// <summary>最大优先级</summary>
        public const int MaxPriority = 1000;

        /// <summary>默认优先级</summary>
        public const int DefaultPriority = 0;

        // ==================== 权限相关 ====================
        /// <summary>权限字符串正则表达式</summary>
        public const string PermissionPattern = @"^[a-zA-Z0-9_.]+$";

        /// <summary>管理员权限</summary>
        public const string AdminPermission = "nonametag.admin";

        // ==================== 缓存相关 ====================
        /// <summary>权限缓存过期时间（秒）</summary>
        public const int PermissionCacheExpirationSeconds = 300;

        /// <summary>最大玩家缓存大小</summary>
        public const int MaxPlayerCacheSize = 1000;

        // ==================== 日志相关 ====================
        /// <summary>日志前缀</summary>
        public const string LogPrefix = "[NoNameTag] ";

        // ==================== 配置相关 ====================
        /// <summary>默认启用状态</summary>
        public const bool DefaultEnabled = true;

        /// <summary>默认调试模式</summary>
        public const bool DefaultDebugMode = false;

        /// <summary>默认应用到聊天消息</summary>
        public const bool DefaultApplyToChatMessages = true;

        /// <summary>默认应用到名称标签</summary>
        public const bool DefaultApplyToNameTags = true;
    }
}
