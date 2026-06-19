namespace Emqo.NoNameTag.Services
{
    public sealed class DeathAttributionResolver : IDeathAttributionResolver
    {
        private readonly IDeathAttributionSource _attributionSource;

        public DeathAttributionResolver(IDeathAttributionSource attributionSource)
        {
            _attributionSource = attributionSource;
        }

        public DeathAttributionContext Resolve(DeathAttributionRequest request)
        {
            if (request == null || request.VictimSteamId == 0)
                return CreateEmpty(request?.VictimSteamId ?? 0);

            if (IsValidKiller(request.InstigatorSteamId, request.VictimSteamId))
            {
                return new DeathAttributionContext
                {
                    VictimSteamId = request.VictimSteamId,
                    KillerSteamId = request.InstigatorSteamId,
                    Source = DeathAttributionSource.DirectInstigator
                };
            }

            if (request.Cause == DeathAttributionCause.Bleeding
                && _attributionSource != null
                && _attributionSource.TryGetBleedDeathAttribution(request.VictimSteamId, out var bleedAttribution)
                && IsValidKiller(bleedAttribution.AttackerSteamId, request.VictimSteamId))
            {
                return FromRecord(request.VictimSteamId, bleedAttribution, DeathAttributionSource.BleedAttribution);
            }

            if (SupportsRecentAttribution(request.Cause)
                && _attributionSource != null
                && _attributionSource.TryGetRecentDeathAttribution(request.VictimSteamId, out var recentAttribution)
                && IsValidKiller(recentAttribution.AttackerSteamId, request.VictimSteamId))
            {
                return FromRecord(request.VictimSteamId, recentAttribution, DeathAttributionSource.RecentAttribution);
            }

            return CreateEmpty(request.VictimSteamId);
        }

        public static bool SupportsRecentAttribution(DeathAttributionCause cause)
        {
            switch (cause)
            {
                case DeathAttributionCause.Grenade:
                case DeathAttributionCause.Missile:
                case DeathAttributionCause.Charge:
                case DeathAttributionCause.Splash:
                case DeathAttributionCause.Landmine:
                case DeathAttributionCause.Vehicle:
                case DeathAttributionCause.Roadkill:
                case DeathAttributionCause.Burning:
                case DeathAttributionCause.Burner:
                    return true;
                default:
                    return false;
            }
        }

        private static DeathAttributionContext FromRecord(ulong victimSteamId, DeathAttributionRecord record, DeathAttributionSource source)
        {
            return new DeathAttributionContext
            {
                VictimSteamId = victimSteamId,
                KillerSteamId = record.AttackerSteamId,
                WeaponName = record.WeaponName,
                DistanceMeters = record.DistanceMeters,
                Source = source
            };
        }

        private static DeathAttributionContext CreateEmpty(ulong victimSteamId)
        {
            return new DeathAttributionContext
            {
                VictimSteamId = victimSteamId,
                Source = DeathAttributionSource.None
            };
        }

        private static bool IsValidKiller(ulong killerSteamId, ulong victimSteamId)
        {
            return killerSteamId != 0 && killerSteamId != victimSteamId;
        }
    }
}
