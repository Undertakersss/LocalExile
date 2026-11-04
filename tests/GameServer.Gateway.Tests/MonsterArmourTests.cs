using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

/// <summary>Player physical hits take a monster's level armour (DefaultMonsterStats.Armour and the type's increased
/// percent) through the same formula as the player's armour.</summary>
public class MonsterArmourTests
{
    [Fact]
    public void Level_armour_and_the_type_bonus_are_one_rating()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"localexile-armour-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(dir, "tables"));
        try
        {
            const string stats = """
                [{"MonsterLife":1,"MinionLife":1,"AltLife1":1,"AltLife2":1,"Damage":1,
                "Experience":1,"Accuracy":1,"Evasion":1,"EvasiveEvasion":1,"MinionDamage":1,"Armour":16161}]
                """;
            const string types = """
                [{"IsPlayerMinion":false,"AltLife1":false,"AltLife2":false,"Armour":0},
                {"IsPlayerMinion":false,"AltLife1":false,"AltLife2":false,"Armour":33},
                {"IsPlayerMinion":false,"AltLife1":false,"AltLife2":false,"Armour":100}]
                """;
            File.WriteAllText(Path.Combine(dir, "tables", "DefaultMonsterStats.json"), stats);
            File.WriteAllText(Path.Combine(dir, "tables", "MonsterTypes.json"), types);
            var catalog = new MonsterCatalog(dir);
            var plain = new MonsterCatalog.Variety(0, "plain", "Plain", "Metadata/Monsters/Plain", null, null,
                MonsterType: 0);
            Assert.Equal(16161, catalog.BaseArmour(plain, 1));
            Assert.Equal(21494, catalog.BaseArmour(plain with { MonsterType = 1 }, 1));
            Assert.Equal(32322, catalog.BaseArmour(plain with { MonsterType = 2 }, 1));
            Assert.Null(catalog.BaseArmour(plain, 2));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void A_physical_hit_is_reduced_by_monster_armour_and_other_types_are_not()
    {
        var monster = new SpawnedMonster("armour", new(0, 0, 0), 0, 0, 1000);
        var physical = DamagePacket.Of(DamageType.Physical, 100, 100);
        var fire = DamagePacket.Of(DamageType.Fire, 100, 100);
        Assert.Equal(100, Game.MonsterTakesMitigated(monster, physical, spell: false));
        // 1000 / (1000 + 100 × 5) = 67%, so 33 remains. A huge rating stops at the 90% cap.
        Assert.Equal(33, Game.MonsterTakesMitigated(monster, physical, spell: false, armourRating: 1000));
        Assert.Equal(10, Game.MonsterTakesMitigated(monster, physical, spell: false, armourRating: 1_000_000));
        Assert.Equal(100, Game.MonsterTakesMitigated(monster, fire, spell: false, armourRating: 1000));
    }
}
