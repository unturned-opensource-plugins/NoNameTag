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

        public bool ApplyToNameTags { get; set; } = true;

        public PriorityMode PriorityMode { get; set; } = PriorityMode.HighestPriority;

        [XmlArray("PermissionGroups")]
        [XmlArrayItem("PermissionGroup")]
        public List<PermissionGroupConfig> PermissionGroups { get; set; } = new List<PermissionGroupConfig>();

        [XmlElement("DeathMessage")]
        public DeathMessageConfig DeathMessage { get; set; } = new DeathMessageConfig();

        public void LoadDefaults()
        {
            Enabled = true;
            DebugMode = false;
            DefaultNameColor = "#FFFFFF";
            ApplyToChatMessages = true;
            ApplyToNameTags = true;
            PriorityMode = PriorityMode.HighestPriority;

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
        }
    }
}
