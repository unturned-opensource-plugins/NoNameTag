namespace Emqo.NoNameTag.Models
{
    public sealed class StatsSettingsConfig
    {
        public bool Enabled { get; set; } = true;

        public string DatabaseRelativePath { get; set; } = "Data/nonametag.litedb";

        public int CommandTimeoutSeconds { get; set; } = 5;

        public bool ShowInFormattedNames { get; set; } = true;

        public string DisplayFormat { get; set; } = " [KS:{streak} K:{kills} D:{deaths}]";

        public string DisplayColor { get; set; } = "#C0C0C0";

        public int DisplayFontSize { get; set; } = 0;

        public int BleedHitRetentionSeconds { get; set; } = 15;

        public int BleedSourceRetentionSeconds { get; set; } = 180;
    }
}
