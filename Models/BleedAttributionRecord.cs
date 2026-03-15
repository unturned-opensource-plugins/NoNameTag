using System;
using SDG.Unturned;

namespace Emqo.NoNameTag.Models
{
    public sealed class BleedAttributionRecord
    {
        public ulong VictimSteamId { get; set; }

        public ulong AttackerSteamId { get; set; }

        public DateTimeOffset LastBleedCapableHitAtUtc { get; set; }

        public DateTimeOffset? BleedBoundAtUtc { get; set; }

        public EDeathCause Cause { get; set; }

        public string WeaponName { get; set; }

        public int? DistanceMeters { get; set; }

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public bool IsExpired(DateTimeOffset nowUtc)
        {
            return ExpiresAtUtc <= nowUtc;
        }
    }
}
