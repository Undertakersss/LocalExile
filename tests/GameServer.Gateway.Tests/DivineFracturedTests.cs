using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

public class DivineFracturedTests
{
    [Fact]
    public void Divine_rerolls_a_fractured_mod_and_leaves_it_fractured()
    {
        var rolls = Rolls();
        var item = new Item(1, ["Mods"], 0)
        {
            Rarity = ItemRarity.Rare, FracturedCount = 1, NameWords = [9],
            Mods = [new ItemMod(ModSection.Explicit, 20, [1])],
        };

        var divined = rolls.Reforge(item, new Random(1));

        Assert.NotNull(divined);
        Assert.Equal(5, Assert.Single(divined.Mods).Values[0]);
        Assert.Equal((byte)1, divined.FracturedCount);
        Assert.Equal(ItemRarity.Rare, divined.Rarity);
        Assert.Equal(item.NameWords, divined.NameWords);
    }

    [Fact]
    public void Divine_keeps_a_locked_side_and_rerolls_the_other()
    {
        var rolls = Rolls();
        var item = new Item(1, ["Mods"], 0)
        {
            Rarity = ItemRarity.Rare,
            Mods =
            [
                new ItemMod(ModSection.Explicit, 20, [1]),
                new ItemMod(ModSection.Explicit, 21, [1]),
                new ItemMod(ModSection.Explicit, 22, [1]),
            ],
        };

        var divined = rolls.Reforge(item, new Random(1));

        Assert.NotNull(divined);
        Assert.Equal(1, divined.Mods[0].Values[0]);
        Assert.Equal(5, divined.Mods[1].Values[0]);
        Assert.Equal(1, divined.Mods[2].Values[0]);
    }

    static ItemRolls Rolls()
    {
        string root = Directory.CreateTempSubdirectory("localexile-divine-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "tables"));
        File.WriteAllText(Path.Combine(root, "tables", "Stats.json"),
            """
            [{"Id":"added_physical_damage","IsLocal":false},
            {"Id":"item_generation_cannot_change_prefixes","IsLocal":false}]
            """);
        string Mod(string id, int hash, int generation, int stat, int min, int max) =>
            "{\"Id\":\"" + id + "\",\"HASH16\":" + hash + ",\"Name\":\"" + id + "\",\"Domain\":1,\"GenerationType\":" + generation
            + ",\"Level\":1,\"StatsKey1\":" + stat + ",\"Stat1Min\":" + min + ",\"Stat1Max\":" + max
            + ",\"StatsKey2\":null,\"StatsKey3\":null,\"StatsKey4\":null,\"StatsKey5\":null,\"StatsKey6\":null,\"StatsKey7\":null,\"StatsKey8\":null"
            + ",\"Families\":[],\"SpawnWeight_TagsKeys\":[],\"SpawnWeight_Values\":[],\"GenerationWeight_TagsKeys\":[],\"GenerationWeight_Values\":[],\"IsEssenceOnlyModifier\":false}";
        File.WriteAllText(Path.Combine(root, "tables", "Mods.json"),
            "[" + string.Join(",", Mod("Prefix", 20, 1, 0, 5, 5), Mod("Suffix", 21, 2, 0, 5, 5), Mod("Lock", 22, 2, 1, 1, 1)) + "]");
        return new ItemRolls(new ItemCatalog(root));
    }
}
