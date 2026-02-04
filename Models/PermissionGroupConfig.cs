using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    public class PermissionGroupConfig
    {
        [XmlAttribute("permission")]
        public string Permission { get; set; } = "";

        [XmlAttribute("priority")]
        public int Priority { get; set; } = 0;

        [XmlElement("DisplayEffect")]
        public DisplayEffectConfig DisplayEffect { get; set; } = new DisplayEffectConfig();

        public PermissionGroupConfig()
        {
        }

        public PermissionGroupConfig(string permission, int priority, DisplayEffectConfig displayEffect)
        {
            Permission = permission;
            Priority = priority;
            DisplayEffect = displayEffect;
        }
    }
}
