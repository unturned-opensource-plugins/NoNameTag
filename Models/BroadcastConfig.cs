using System.Collections.Generic;
using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    /// <summary>
    /// 消息显示模式
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// 聊天框显示
        /// </summary>
        Chat,

        /// <summary>
        /// 服务器控制台显示
        /// </summary>
        Console,

        /// <summary>
        /// 聊天框和控制台都显示
        /// </summary>
        Both,

        /// <summary>
        /// 禁用显示
        /// </summary>
        None
    }

    /// <summary>
    /// 死亡消息可见性控制枚举
    /// </summary>
    public enum DeathMessageVisibility
    {
        /// <summary>
        /// 全员可见（默认）
        /// </summary>
        All,

        /// <summary>
        /// 仅击杀者可见
        /// </summary>
        KillerOnly,

        /// <summary>
        /// 仅被杀者可见
        /// </summary>
        VictimOnly,

        /// <summary>
        /// 击杀者和被杀者可见
        /// </summary>
        KillerAndVictimOnly
    }

    /// <summary>
    /// 头像位置枚举
    /// </summary>
    public enum AvatarPosition
    {
        /// <summary>
        /// 头像在左侧
        /// </summary>
        Left,

        /// <summary>
        /// 头像在右侧
        /// </summary>
        Right
    }

    /// <summary>
    /// 广播消息配置
    /// </summary>
    public class BroadcastMessage
    {
        [XmlElement("Message")]
        public string Message { get; set; } = "";

        [XmlElement("DelaySeconds")]
        public int DelaySeconds { get; set; } = 0;

        [XmlElement("FontColor")]
        public string FontColor { get; set; } = "";

        [XmlElement("FontSize")]
        public int FontSize { get; set; } = 0;

        /// <summary>
        /// 头像：Sprite ID（如 "icon_vip"）或 Emoji（如 "🛡️"）
        /// </summary>
        [XmlElement("Avatar")]
        public string Avatar { get; set; } = "";

        /// <summary>
        /// 头像位置：Left 或 Right
        /// </summary>
        [XmlElement("AvatarPosition")]
        public AvatarPosition AvatarPosition { get; set; } = AvatarPosition.Left;

        public BroadcastMessage()
        {
        }

        public BroadcastMessage(string message, int delaySeconds = 0, string avatar = "", AvatarPosition position = AvatarPosition.Left)
        {
            Message = message;
            DelaySeconds = delaySeconds;
            Avatar = avatar;
            AvatarPosition = position;
        }
    }

    /// <summary>
    /// 广播组配置（用于轮播公告）
    /// 每个组独立计时和轮换，仅支持全员可见
    /// </summary>
    public class BroadcastGroupConfig
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 显示模式：Chat（聊天框）或 None（禁用）
        /// </summary>
        [XmlAttribute("displayMode")]
        public DisplayMode DisplayMode { get; set; } = DisplayMode.Chat;

        [XmlElement("RotationInterval")]
        public int RotationInterval { get; set; } = 60;

        [XmlArray("Messages")]
        [XmlArrayItem("Message")]
        public List<BroadcastMessage> Messages { get; set; } = new List<BroadcastMessage>();

        public BroadcastGroupConfig()
        {
        }

        public BroadcastGroupConfig(string name, int rotationInterval, List<BroadcastMessage> messages)
        {
            Name = name;
            RotationInterval = rotationInterval;
            Messages = messages;
        }
    }

    /// <summary>
    /// 增强的死亡消息配置（支持可见性控制）
    /// </summary>
    public class DeathMessageConfig
    {
        [XmlElement("Enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 显示模式：Chat（聊天框）或 None（禁用）
        /// </summary>
        [XmlElement("DisplayMode")]
        public DisplayMode DisplayMode { get; set; } = DisplayMode.Chat;

        /// <summary>
        /// 死亡消息可见性控制
        /// </summary>
        [XmlElement("Visibility")]
        public DeathMessageVisibility Visibility { get; set; } = DeathMessageVisibility.All;

        [XmlElement("FontColor")]
        public string FontColor { get; set; } = "";

        [XmlElement("FontSize")]
        public int FontSize { get; set; } = 0;

        [XmlElement("Format")]
        public string Format { get; set; } = "{killer} 击杀了 {victim}";

        [XmlElement("SelfKillFormat")]
        public string SelfKillFormat { get; set; } = "{victim} 自杀了";

        [XmlElement("BleedingFormat")]
        public string BleedingFormat { get; set; } = "{victim} 流血致死";

        [XmlElement("ZombieFormat")]
        public string ZombieFormat { get; set; } = "{victim} 被僵尸击杀";

        [XmlElement("AnimalFormat")]
        public string AnimalFormat { get; set; } = "{victim} 被动物击杀";

        [XmlElement("VehicleFormat")]
        public string VehicleFormat { get; set; } = "{victim} 死于车祸";

        [XmlElement("FallFormat")]
        public string FallFormat { get; set; } = "{victim} 摔死了";

        [XmlElement("DrownFormat")]
        public string DrownFormat { get; set; } = "{victim} 淹死了";

        [XmlElement("FreezeFormat")]
        public string FreezeFormat { get; set; } = "{victim} 冻死了";

        [XmlElement("BurnFormat")]
        public string BurnFormat { get; set; } = "{victim} 被烧死了";

        [XmlElement("FoodFormat")]
        public string FoodFormat { get; set; } = "{victim} 饿死了";

        [XmlElement("WaterFormat")]
        public string WaterFormat { get; set; } = "{victim} 渴死了";

        [XmlElement("InfectionFormat")]
        public string InfectionFormat { get; set; } = "{victim} 感染致死";

        [XmlElement("GunFormat")]
        public string GunFormat { get; set; } = "{victim} 被 {killer} 射杀";

        [XmlElement("MeleeFormat")]
        public string MeleeFormat { get; set; } = "{victim} 被 {killer} 砍死";

        [XmlElement("PunchFormat")]
        public string PunchFormat { get; set; } = "{victim} 被 {killer} 打死";

        [XmlElement("GrenadeFormat")]
        public string GrenadeFormat { get; set; } = "{victim} 被 {killer} 炸死";

        [XmlElement("ShredFormat")]
        public string ShredFormat { get; set; } = "{victim} 被陷阱撕碎";

        [XmlElement("LandmineFormat")]
        public string LandmineFormat { get; set; } = "{victim} 踩到地雷";

        [XmlElement("ArenaFormat")]
        public string ArenaFormat { get; set; } = "{victim} 在竞技场中死亡";

        [XmlElement("MissileFormat")]
        public string MissileFormat { get; set; } = "{victim} 被 {killer} 的导弹击中";

        [XmlElement("ChargeFormat")]
        public string ChargeFormat { get; set; } = "{victim} 被炸药炸死";

        [XmlElement("SplashFormat")]
        public string SplashFormat { get; set; } = "{victim} 被爆炸波及";

        [XmlElement("SentryFormat")]
        public string SentryFormat { get; set; } = "{victim} 被哨兵击杀";

        [XmlElement("AcidFormat")]
        public string AcidFormat { get; set; } = "{victim} 被酸液腐蚀";

        [XmlElement("BoulderFormat")]
        public string BoulderFormat { get; set; } = "{victim} 被巨石砸死";

        [XmlElement("SparkFormat")]
        public string SparkFormat { get; set; } = "{victim} 被电死";

        [XmlElement("SpitFormat")]
        public string SpitFormat { get; set; } = "{victim} 被僵尸吐痰击杀";

        [XmlElement("KillFormat")]
        public string KillFormat { get; set; } = "{victim} 被管理员处决";

        [XmlElement("HeadshotTag")]
        public string HeadshotTag { get; set; } = " [爆头]";

        [XmlElement("DefaultFormat")]
        public string DefaultFormat { get; set; } = "{victim} 死亡了";

        public DeathMessageConfig()
        {
        }
    }

    /// <summary>
    /// 欢迎消息配置
    /// </summary>
    public class WelcomeMessageConfig
    {
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; } = true;

        [XmlElement("Text")]
        public string Text { get; set; } = "欢迎 {player} 加入服务器！";

        [XmlElement("IconUrl")]
        public string IconUrl { get; set; } = "";

        [XmlElement("Color")]
        public string Color { get; set; } = "";

        [XmlElement("FontSize")]
        public int FontSize { get; set; } = 0;

        [XmlElement("LeaveColor")]
        public string LeaveColor { get; set; } = "";

        [XmlElement("LeaveFontSize")]
        public int LeaveFontSize { get; set; } = 0;

        /// <summary>
        /// 是否显示加入链接
        /// </summary>
        [XmlElement("EnableJoinLink")]
        public bool EnableJoinLink { get; set; } = false;

        [XmlElement("JoinLinkUrl")]
        public string JoinLinkUrl { get; set; } = "";

        [XmlElement("JoinLinkMessage")]
        public string JoinLinkMessage { get; set; } = "";

        /// <summary>
        /// 离开消息是否启用
        /// </summary>
        [XmlElement("EnableLeaveMessage")]
        public bool EnableLeaveMessage { get; set; } = true;

        [XmlElement("LeaveText")]
        public string LeaveText { get; set; } = "{player} 离开了服务器";

        [XmlElement("LeaveIconUrl")]
        public string LeaveIconUrl { get; set; } = "";
    }

    /// <summary>
    /// 文本命令配置
    /// </summary>
    public class TextCommandConfig
    {
        [XmlElement("Name")]
        public string Name { get; set; } = "";

        [XmlElement("Message")]
        public string Message { get; set; } = "";

        [XmlElement("IconUrl")]
        public string IconUrl { get; set; } = "";

        [XmlElement("Color")]
        public string Color { get; set; } = "white";
    }

    /// <summary>
    /// 网站命令配置
    /// </summary>
    public class WebCommandConfig
    {
        [XmlElement("Name")]
        public string Name { get; set; } = "";

        [XmlElement("Url")]
        public string Url { get; set; } = "";

        [XmlElement("Description")]
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 统一的广播配置（合并死亡消息和轮播公告）
    /// </summary>
    public class BroadcastConfig
    {
        [XmlElement("DeathMessage")]
        public DeathMessageConfig DeathMessage { get; set; } = new DeathMessageConfig();

        [XmlArray("BroadcastGroups")]
        [XmlArrayItem("BroadcastGroup")]
        public List<BroadcastGroupConfig> BroadcastGroups { get; set; } = new List<BroadcastGroupConfig>();

        public BroadcastConfig()
        {
        }
    }
}
