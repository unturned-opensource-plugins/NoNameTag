using System;
using Emqo.NoNameTag.Models;
using Emqo.NoNameTag.Services;
using SDG.Unturned;
using Xunit;

namespace Emqo.NoNameTag.Tests
{
    public sealed class DeathAttributionResolverTests
    {
        [Fact]
        public void DirectInstigator_TakesPrecedenceOverBleedAttribution()
        {
            var damage = new DamageAttributionService(new StatsSettingsConfig { Enabled = true });
            damage.RecordAttributedHit(2, 1, EDeathCause.GUN, "Rifle", 15, DateTimeOffset.UtcNow);
            damage.HandleBleedingStateChanged(1, true, DateTimeOffset.UtcNow);
            var resolver = new DeathAttributionResolver(damage);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 3,
                Cause = EDeathCause.BLEEDING
            });

            Assert.Equal(3UL, attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.DirectInstigator, attribution.Source);
            Assert.Null(attribution.WeaponName);
        }

        [Fact]
        public void BleedingDeath_UsesActiveBleedAttributionWhenNoDirectInstigatorExists()
        {
            var now = DateTimeOffset.UtcNow;
            var damage = new DamageAttributionService(new StatsSettingsConfig { Enabled = true });
            damage.RecordAttributedHit(2, 1, EDeathCause.GUN, "Rifle", 15, now);
            damage.HandleBleedingStateChanged(1, true, now);
            var resolver = new DeathAttributionResolver(damage);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = EDeathCause.BLEEDING
            });

            Assert.Equal(2UL, attribution.KillerSteamId);
            Assert.Equal("Rifle", attribution.WeaponName);
            Assert.Equal(15, attribution.DistanceMeters);
            Assert.Equal(DeathAttributionSource.BleedAttribution, attribution.Source);
        }

        [Fact]
        public void SupportedDelayedCause_UsesRecentAttribution()
        {
            var damage = new DamageAttributionService(new StatsSettingsConfig { Enabled = true });
            damage.RecordAttributedHit(2, 1, EDeathCause.GRENADE, "Grenade", 9, DateTimeOffset.UtcNow);
            var resolver = new DeathAttributionResolver(damage);

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = EDeathCause.GRENADE
            });

            Assert.Equal(2UL, attribution.KillerSteamId);
            Assert.Equal("Grenade", attribution.WeaponName);
            Assert.Equal(9, attribution.DistanceMeters);
            Assert.Equal(DeathAttributionSource.RecentAttribution, attribution.Source);
        }

        [Fact]
        public void SelfKill_DoesNotProduceKillerCredit()
        {
            var resolver = new DeathAttributionResolver(new DamageAttributionService(new StatsSettingsConfig { Enabled = true }));

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 1,
                Cause = EDeathCause.GUN
            });

            Assert.Null(attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.None, attribution.Source);
        }

        [Fact]
        public void MissingKiller_ReturnsEmptyAttributionForUnsupportedCause()
        {
            var resolver = new DeathAttributionResolver(new DamageAttributionService(new StatsSettingsConfig { Enabled = true }));

            var attribution = resolver.Resolve(new DeathAttributionRequest
            {
                VictimSteamId = 1,
                InstigatorSteamId = 0,
                Cause = EDeathCause.ZOMBIE
            });

            Assert.Null(attribution.KillerSteamId);
            Assert.Equal(DeathAttributionSource.None, attribution.Source);
        }
    }
}
