using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    public class DisplayEffectConfig
    {
        [XmlElement("Prefix")]
        public string Prefix { get; set; } = "";

        [XmlElement("PrefixColor")]
        public string PrefixColor { get; set; } = "#FFFFFF";

        [XmlElement("PrefixFontSize")]
        public int PrefixFontSize { get; set; } = 0;

        [XmlElement("NameColor")]
        public string NameColor { get; set; } = "#FFFFFF";

        [XmlElement("NameFontSize")]
        public int NameFontSize { get; set; } = 0;

        [XmlElement("Suffix")]
        public string Suffix { get; set; } = "";

        [XmlElement("SuffixColor")]
        public string SuffixColor { get; set; } = "#FFFFFF";

        [XmlElement("SuffixFontSize")]
        public int SuffixFontSize { get; set; } = 0;

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
