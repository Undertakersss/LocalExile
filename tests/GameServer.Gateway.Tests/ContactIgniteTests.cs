using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

public sealed class ContactIgniteTests
{
    static int Stat(string name, params (string Name, int Value)[] values) =>
        values.FirstOrDefault(v => v.Name == name).Value;

    static List<(string Stat, int Value)> FireRows(Func<string, int> get) =>
        AilmentPanelStats.Compute(new("main_hand", Spell: false, Melee: true),
            DamagePacket.Of(DamageType.Fire, 100, 200), new DamageConversion(), get);

    // 90% of 100–200 fire is 90–180 ignite damage per second. More maximum attack damage scales only an attack's maxima.
    [Fact]
    public void More_maximum_attack_damage_scales_attack_ignite_maxima()
    {
        int Get(string name) => Stat(name, ("base_chance_to_ignite_%", 100),
            (MaximumAttackDamage.FromAscendancy, 30));
        var rows = FireRows(Get);
        var attack = Game.ContactIgnite(rows, "main_hand", Get, attack: true);
        var spell = Game.ContactIgnite(rows, "main_hand", Get, attack: false);

        Assert.Equal(100, attack.Chance);
        Assert.Equal(4, attack.Seconds);
        Assert.Equal(90, attack.Min);
        Assert.Equal(234, attack.Max, 9);
        Assert.Equal(90, attack.CriticalMin);
        Assert.Equal(234, attack.CriticalMax, 9);
        Assert.Equal(180, spell.Max);
        Assert.Equal(180, spell.CriticalMax);
        Assert.Equal(MonsterIgnites.Settings.None,
            Game.ContactIgnite(rows, "main_hand", name => name == "cannot_inflict_status_ailments" ? 1 : Get(name), true));
    }

    // A landed fire contact ignites a living monster. A critical strike needs no chance. Avoidance, immunity, a corpse
    // and a zero ignite range add nothing, and the monster's own ignite-duration reduction shortens the burn.
    [Fact]
    public void A_fire_contact_ignites_a_living_monster()
    {
        var conn = new ClientState(new(1, "ignite", 300, 300, 100, 100, null, []), 1, 1);
        var settings = new MonsterIgnites.Settings(100, 90, 90, 120, 120, 4);
        SpawnedMonster Monster(params (string Name, int Value)[] mods) => new("burning", new(105, 100, 0), 0, 0, 1000)
        {
            Life = 1000,
            Mods = new MonsterModSet([], [], new MonsterModEffects(mods.ToDictionary(m => m.Name, m => m.Value))),
        };

        var hit = Monster();
        Assert.NotNull(Game.ApplyContactIgniteLocked(conn, 1, hit, settings, critical: false, now: 10));
        Assert.Equal(90, hit.Ignites.PerSecond(10));
        Assert.Equal(14, hit.Ignites.Until);

        var critical = Monster();
        Assert.NotNull(Game.ApplyContactIgniteLocked(conn, 2, critical, settings with { Chance = 0 }, critical: true, now: 0));
        Assert.Equal(120, critical.Ignites.PerSecond(0));
        Assert.Null(Game.ApplyContactIgniteLocked(conn, 3, Monster(), settings with { Chance = 0 }, critical: false, now: 0));
        Assert.Null(Game.ApplyContactIgniteLocked(conn, 4, Monster(("avoid_ignite_%", 100)), settings, false, 0));
        Assert.Null(Game.ApplyContactIgniteLocked(conn, 5, Monster(("cannot_be_ignited", 1)), settings, false, 0));
        var dead = Monster();
        dead.Life = 0;
        Assert.Null(Game.ApplyContactIgniteLocked(conn, 6, dead, settings, false, 0));
        Assert.Null(Game.ApplyContactIgniteLocked(conn, 7, Monster(), settings with { Min = 0, Max = 0 }, false, 0));

        var resisted = Monster(("base_self_ignite_duration_-%", 50));
        Assert.NotNull(Game.ApplyContactIgniteLocked(conn, 8, resisted, settings, false, now: 3));
        Assert.Equal(5, resisted.Ignites.Until);

        // The default melee helper reads the weapon's flat fire the same way, before any named stat.
        var weapon = new SkillCatalog.Unarmed(1000, 0, 0, 8) { LocalDamage = DamagePacket.Of(DamageType.Fire, 100, 200) };
        var melee = Game.MeleeAttackIgnite(weapon, new Dictionary<uint, int>());
        Assert.Equal(90, melee.Min);
        Assert.Equal(180, melee.Max);
        Assert.Equal(4, melee.Seconds);
        Assert.Equal(0, melee.Chance);
    }
}
