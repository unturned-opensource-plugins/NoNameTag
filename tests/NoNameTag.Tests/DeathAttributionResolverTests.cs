using Emqo.NoNameTag.Services;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class DeathAttributionResolverTests
    {
        [Fact]
        public void DirectInstigator_TakesPrecedenceOverBleedAttribution()
        {
            var source = new StubDeathAttributionSource
            {
                BleedRecord = Record(2, "Rifle", 15)
            };
            var resolver = new DeathAttributionResolver(source);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 3,
                Cause = DeathAttributionCause.Bleeding
            });

            Assert.Equal(3UL, attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.DirectInstigator, attribution.Source);
            Assert.Null(attribution.WeaponName);
        }

        [Fact]
        public void BleedingDeath_UsesActiveBleedAttributionWhenNoDirectInstigatorExists()
        {
            var source = new StubDeathAttributionSource
            {
                BleedRecord = Record(2, "Rifle", 15)
            };
            var resolver = new DeathAttributionResolver(source);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = DeathAttributionCause.Bleeding
            });

            Assert.Equal(2UL, attribution.KillerSteamId);
            Assert.Equal("Rifle", attribution.WeaponName);
            Assert.Equal(15, attribution.DistanceMeters);
            Assert.Equal(DeathAttributionSource.BleedAttribution, attribution.Source);
        }

        [Fact]
        public void SupportedDelayedCause_UsesRecentAttribution()
        {
            var source = new StubDeathAttributionSource
            {
                RecentRecord = Record(2, "Grenade", 9)
            };
            var resolver = new DeathAttributionResolver(source);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = DeathAttributionCause.Grenade
            });

            Assert.Equal(2UL, attribution.KillerSteamId);
            Assert.Equal("Grenade", attribution.WeaponName);
            Assert.Equal(9, attribution.DistanceMeters);
            Assert.Equal(DeathAttributionSource.RecentAttribution, attribution.Source);
        }

        [Fact]
        public void SelfKill_DoesNotProduceKillerCredit()
        {
            var resolver = new DeathAttributionResolver(new StubDeathAttributionSource());

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 1,
                Cause = DeathAttributionCause.Gun
            });

            Assert.Null(attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.None, attribution.Source);
        }

        [Fact]
        public void MissingKiller_ReturnsEmptyAttributionForUnsupportedCause()
        {
            var resolver = new DeathAttributionResolver(new StubDeathAttributionSource());

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = DeathAttributionCause.Zombie
            });

            Assert.Null(attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.None, attribution.Source);
        }

        private static DeathAttributionRecord Record(ulong attackerSteamId, string weaponName, int distanceMeters)
        {
            return new DeathAttributionRecord
            {
                AttackerSteamId = attackerSteamId,
                WeaponName = weaponName,
                DistanceMeters = distanceMeters
            };
        }

        private sealed class StubDeathAttributionSource : IDeathAttributionSource
        {
            public DeathAttributionRecord BleedRecord { get; set; }
            public DeathAttributionRecord RecentRecord { get; set; }

            public bool TryGetBleedDeathAttribution(ulong victimSteamId, out DeathAttributionRecord record)
            {
                record = BleedRecord;
                return record != null;
            }

            public bool TryGetRecentDeathAttribution(ulong victimSteamId, out DeathAttributionRecord record)
            {
                record = RecentRecord;
                return record != null;
            }
        }
    }
}
