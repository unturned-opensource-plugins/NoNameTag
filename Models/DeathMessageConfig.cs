using System.Xml.Serialization;

namespace Emqo.NoNameTag.Models
{
    public class DeathMessageConfig
    {
        [XmlElement("Enabled")]
        public bool Enabled { get; set; } = true;

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
}
