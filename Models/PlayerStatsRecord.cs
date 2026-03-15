namespace Emqo.NoNameTag.Models
{
    public sealed class PlayerStatsRecord
    {
        public ulong SteamId { get; set; }

        public int TotalKills { get; set; }

        public int TotalDeaths { get; set; }

        public int CurrentKillstreak { get; set; }
    }
}
