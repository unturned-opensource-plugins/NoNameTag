using Emqo.NoNameTag.Models;
using SDG.Unturned;

namespace Emqo.NoNameTag.Services
{
    public sealed class DeathAttributionResolver : IDeathAttributionResolver
    {
        private readonly IDamageAttributionService _damageAttributionService;

        public DeathAttributionResolver(IDamageAttributionService damageAttributionService)
        {
            _damageAttributionService = damageAttributionService;
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

            if (request.Cause == EDeathCause.BLEEDING
                && _damageAttributionService != null
                && _damageAttributionService.TryGetBleedAttribution(request.VictimSteamId, out var bleedAttribution)
                && IsValidKiller(bleedAttribution.AttackerSteamId, request.VictimSteamId))
            {
                return FromRecord(request.VictimSteamId, bleedAttribution, DeathAttributionSource.BleedAttribution);
            }

            if (SupportsRecentAttribution(request.Cause)
                && _damageAttributionService != null
                && _damageAttributionService.TryGetRecentAttribution(request.VictimSteamId, out var recentAttribution)
                && IsValidKiller(recentAttribution.AttackerSteamId, request.VictimSteamId))
            {
                return FromRecord(request.VictimSteamId, recentAttribution, DeathAttributionSource.RecentAttribution);
            }

            return CreateEmpty(request.VictimSteamId);
        }

        public static bool SupportsRecentAttribution(EDeathCause cause)
        {
            switch (cause)
            {
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.CHARGE:
                case EDeathCause.SPLASH:
                case EDeathCause.LANDMINE:
                case EDeathCause.VEHICLE:
                case EDeathCause.ROADKILL:
                case EDeathCause.BURNING:
                case EDeathCause.BURNER:
                    return true;
                default:
                    return false;
            }
        }

        private static DeathAttributionContext FromRecord(ulong victimSteamId, BleedAttributionRecord record, DeathAttributionSource source)
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
