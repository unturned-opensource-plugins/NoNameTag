namespace Emqo.NoNameTag.Services
{
    public sealed class DeathAttributionRequest
    {
        public ulong VictimSteamId { get; set; }
        public ulong InstigatorSteamId { get; set; }
        public DeathAttributionCause Cause { get; set; }
    }

    public sealed class DeathAttributionContext
    {
        public static readonly DeathAttributionContext Empty = new DeathAttributionContext();

        public ulong VictimSteamId { get; set; }
        public ulong? KillerSteamId { get; set; }
        public string WeaponName { get; set; }
        public int? DistanceMeters { get; set; }
        public DeathAttributionSource Source { get; set; }
    }

    public sealed class DeathAttributionRecord
    {
        public ulong AttackerSteamId { get; set; }
        public string WeaponName { get; set; }
        public int? DistanceMeters { get; set; }
    }

    public enum DeathAttributionCause
    {
        Unknown,
        Bleeding,
        Burning,
        Burner,
        Charge,
        Grenade,
        Landmine,
        Missile,
        Roadkill,
        Splash,
        Vehicle,
        Gun,
        Melee,
        Punch,
        Zombie
    }

    public enum DeathAttributionSource
    {
        None,
        DirectInstigator,
        BleedAttribution,
        RecentAttribution
    }
}
