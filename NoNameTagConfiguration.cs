using System.Collections.Generic;
using System.Xml.Serialization;
using Emqo.NoNameTag.Models;
using Rocket.API;

namespace Emqo.NoNameTag
{
    public enum PriorityMode
    {
        FirstMatch,
        HighestPriority
    }

    public class NoNameTagConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled { get; set; } = true;

        public bool DebugMode { get; set; } = false;

        public string DefaultNameColor { get; set; } = "#FFFFFF";

        public bool ApplyToChatMessages { get; set; } = true;

        public PriorityMode PriorityMode { get; set; } = PriorityMode.HighestPriority;

        [XmlElement("AvatarSettings")]
        public AvatarConfig AvatarSettings { get; set; } = new AvatarConfig();

        [XmlArray("PermissionGroups")]
        [XmlArrayItem("PermissionGroup")]
        public List<PermissionGroupConfig> PermissionGroups { get; set; } = new List<PermissionGroupConfig>();

        [XmlElement("DeathMessage")]
        public DeathMessageConfig DeathMessage { get; set; } = new DeathMessageConfig();

        [XmlArray("BroadcastGroups")]
        [XmlArrayItem("BroadcastGroup")]
        public List<BroadcastGroupConfig> BroadcastGroups { get; set; } = new List<BroadcastGroupConfig>();

        /// <summary>
        /// 统一的广播配置（用于 BroadcastService）
        /// </summary>
        [XmlIgnore]
        public BroadcastConfig Broadcast
        {
            get
            {
                return new BroadcastConfig
                {
                    DeathMessage = DeathMessage,
                    BroadcastGroups = BroadcastGroups
                };
            }
        }

        public void LoadDefaults()
        {
            Enabled = true;
            DebugMode = false;
            DefaultNameColor = "#FFFFFF";
            ApplyToChatMessages = true;
            PriorityMode = PriorityMode.HighestPriority;

            AvatarSettings = new AvatarConfig();

            PermissionGroups = new List<PermissionGroupConfig>
            {
                new PermissionGroupConfig(
                    "nonametag.vip",
                    10,
                    new DisplayEffectConfig("[VIP] ", "#FFD700", "#00FF00")
                ),
                new PermissionGroupConfig(
                    "nonametag.mvp",
                    20,
                    new DisplayEffectConfig("[MVP] ", "#FF4500", "#00BFFF", " *", "#FF4500")
                )
            };

            DeathMessage = new DeathMessageConfig();

            BroadcastGroups = new List<BroadcastGroupConfig>
            {
                new BroadcastGroupConfig
                {
                    Name = "server_rules",
                    Enabled = true,
                    RotationInterval = 120,
                    Messages = new List<BroadcastMessage>
                    {
                        new BroadcastMessage("欢迎来到服务器！请遵守规则。", 0),
                        new BroadcastMessage("使用 /help 查看可用命令。", 0),
                        new BroadcastMessage("禁止恶意破坏和骚扰其他玩家。", 0)
                    }
                }
            };
        }
    }
}
