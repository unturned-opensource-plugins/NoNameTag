using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    public class DisplayEffectConfig
    {
        [XmlElement("Prefix")]
        public string Prefix { get; set; } = "";

        [XmlElement("PrefixColor")]
        public string PrefixColor { get; set; } = "#FFFFFF";

        [XmlElement("NameColor")]
        public string NameColor { get; set; } = "#FFFFFF";

        [XmlElement("Suffix")]
        public string Suffix { get; set; } = "";

        [XmlElement("SuffixColor")]
        public string SuffixColor { get; set; } = "#FFFFFF";

        public DisplayEffectConfig()
        {
        }

        public DisplayEffectConfig(string prefix, string prefixColor, string nameColor, string suffix = "", string suffixColor = "#FFFFFF")
        {
            Prefix = prefix;
            PrefixColor = prefixColor;
            NameColor = nameColor;
            Suffix = suffix;
            SuffixColor = suffixColor;
        }
    }
}
