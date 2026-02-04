using System.Collections.Generic;
using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
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
        /// 死亡消息可见性控制
        /// </summary>
        [XmlElement("Visibility")]
        public DeathMessageVisibility Visibility { get; set; } = DeathMessageVisibility.All;

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

        [XmlElement("DefaultFormat")]
        public string DefaultFormat { get; set; } = "{victim} 死亡了";

        public DeathMessageConfig()
        {
        }
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
