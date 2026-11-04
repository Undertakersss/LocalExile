using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

/// <summary>Attack hits bleed for 70% of their physical damage per second. Only the strongest bleed deals damage.</summary>
public sealed class MonsterBleedTests
{
    [Fact]
    public void Only_the_strongest_bleed_deals_damage()
    {
        var bleeds = new MonsterBleeds();
        Assert.True(bleeds.Add(0, 100, 1));
        Assert.True(bleeds.Add(0, 40, 3));
        Assert.Equal(100, bleeds.PerSecond(0.5));
        Assert.Equal(100, bleeds.Advance(1, 1));
        Assert.Equal(40, bleeds.PerSecond(1.5));
        Assert.Equal(80, bleeds.Advance(3, 1));
        Assert.False(bleeds.Bleeding);
    }

    [Fact]
    public void An_attack_with_bleed_chance_bleeds_for_seventy_percent_of_its_physical()
    {
        var hit = DamagePacket.Of(DamageType.Physical, 100, 100);
        var fire = DamagePacket.Of(DamageType.Fire, 100, 100);
        int Get(string name) => name == "bleed_on_hit_with_attacks_%" ? 100 : 0;

        var bled = Monster();
        Game.BleedOnHitLocked(bled, hit, spell: false, critical: false, Get, 0, new Random(1));
        Assert.Equal(70, bled.Bleeds.PerSecond(0.1));
        Assert.Equal(70, bled.Bleeds.Advance(1, 1));

        var spell = Monster();
        Game.BleedOnHitLocked(spell, hit, spell: true, critical: false, Get, 0, new Random(1));
        Assert.False(spell.Bleeds.Bleeding);

        var noChance = Monster();
        Game.BleedOnHitLocked(noChance, hit, spell: false, critical: false, _ => 0, 0, new Random(1));
        Assert.False(noChance.Bleeds.Bleeding);

        var banned = Monster();
        Game.BleedOnHitLocked(banned, hit, spell: false, critical: false,
            name => name is "bleed_on_hit_with_attacks_%" or "base_deal_no_physical_damage" ? 1 : 0, 0, new Random(1));
        Assert.False(banned.Bleeds.Bleeding);

        var elemental = Monster();
        Game.BleedOnHitLocked(elemental, fire, spell: false, critical: false, Get, 0, new Random(1));
        Assert.False(elemental.Bleeds.Bleeding);
    }

    static SpawnedMonster Monster() => new("bleed", new(0, 0, 0), 0, 0, 1000);
}
