using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

public class OrbOfBindingTests
{
    [Fact]
    public void Binding_makes_a_normal_item_rare_with_at_most_four_white_links()
    {
        var (rolls, body) = Catalog("Body Armour");
        var six = Item(body.Hash, level: 50, sockets: 6);
        var bound = CurrencyEffects.Binding(six, body, rolls, new Random(1));

        Assert.NotNull(bound);
        Assert.Equal(ItemRarity.Rare, bound.Rarity);
        Assert.True(bound.Identified);
        Assert.Equal(4, bound.Sockets.Count);
        Assert.All(bound.Sockets, s => Assert.Equal(SocketColour.White, s.Colour));
        Assert.Equal((byte)4, Assert.Single(bound.Links!));

        var low = CurrencyEffects.Binding(Item(body.Hash, level: 1, sockets: 0), body, rolls, new Random(1));
        Assert.NotNull(low);
        Assert.Equal(2, low.Sockets.Count);
        Assert.Equal((byte)2, Assert.Single(low.Links!));
    }

    [Fact]
    public void Binding_respects_the_class_cap_and_refuses_gems_and_unsocketed_bases()
    {
        var (rolls, shield) = Catalog("Shield");
        var bound = CurrencyEffects.Binding(Item(shield.Hash, level: 50, sockets: 1), shield, rolls, new Random(1));
        Assert.NotNull(bound);
        Assert.Equal(3, bound.Sockets.Count);
        Assert.All(bound.Sockets, s => Assert.Equal(SocketColour.White, s.Colour));

        var gemmed = Item(shield.Hash, level: 50, sockets: 1) with
        {
            Sockets = [new ItemSocket(SocketColour.Red, new Item(2, ["SkillGem"], 0))],
        };
        Assert.Null(CurrencyEffects.Binding(gemmed, shield, rolls, new Random(1)));
        Assert.Null(CurrencyEffects.Binding(Item(shield.Hash, level: 50, sockets: 0) with { Rarity = ItemRarity.Magic }, shield, rolls, new Random(1)));

        var (ringRolls, ring) = Catalog("Ring", sockets: false);
        var upgraded = CurrencyEffects.Binding(Item(ring.Hash, level: 50, sockets: 0, components: ["Base", "Mods"]), ring, ringRolls, new Random(1));
        Assert.NotNull(upgraded);
        Assert.Equal(ItemRarity.Rare, upgraded.Rarity);
        Assert.Empty(upgraded.Sockets);
    }

    static Item Item(uint hash, byte level, int sockets, IReadOnlyList<string>? components = null) =>
        new(hash, components ?? ["Base", "Mods", "Sockets"], 1)
        {
            ItemLevel = level, Rarity = ItemRarity.Normal,
            Sockets = Enumerable.Range(0, sockets).Select(_ => new ItemSocket(SocketColour.Red)).ToArray(),
        };

    static (ItemRolls Rolls, ItemCatalog.BaseType Type) Catalog(string itemClass, bool sockets = true)
    {
        string root = Directory.CreateTempSubdirectory("localexile-binding-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "tables"));
        string comps = sockets ? "\"Base\",\"Mods\",\"Sockets\"" : "\"Base\",\"Mods\"";
        int read = sockets ? 3 : 2;
        File.WriteAllText(Path.Combine(root, "item-types.json"),
            "{\"types\":[{\"type\":\"Metadata/Items/Test\",\"components\":[" + comps + "],\"read\":" + read + ",\"tags\":[\"default\"]}]}");
        File.WriteAllText(Path.Combine(root, "tables", "ItemVisualIdentity.json"), """[{"HASH16":1}]""");
        File.WriteAllText(Path.Combine(root, "tables", "Tags.json"), """[{"Id":"default"}]""");
        File.WriteAllText(Path.Combine(root, "tables", "ItemClasses.json"),
            "[{" + "\"Id\":\"" + itemClass + "\",\"ItemStance\":null}]");
        File.WriteAllText(Path.Combine(root, "tables", "BaseItemTypes.json"),
            """
            [{"Id":"Metadata/Items/Test","Name":"Test","HASH32":1,"Width":2,"Height":2,"ItemVisualIdentity":0,
            "ItemClassesKey":0,"ItemStance":null,"DropLevel":1,"Implicit_ModsKeys":[],"TagsKeys":[0],"ModDomain":1}]
            """);
        File.WriteAllText(Path.Combine(root, "tables", "Stats.json"),
            """[{"Id":"added_physical_damage","IsLocal":false}]""");
        string keys = string.Join(",", Enumerable.Range(2, 7).Select(i => "\"StatsKey" + i + "\":null"));
        File.WriteAllText(Path.Combine(root, "tables", "Mods.json"),
            "[{" + "\"Id\":\"Prefix\",\"HASH16\":20,\"Name\":\"Prefix\",\"Domain\":1,\"GenerationType\":1,\"Level\":1,"
            + "\"StatsKey1\":0,\"Stat1Min\":1,\"Stat1Max\":1," + keys
            + ",\"Families\":[],\"SpawnWeight_TagsKeys\":[0],\"SpawnWeight_Values\":[100],"
            + "\"GenerationWeight_TagsKeys\":[],\"GenerationWeight_Values\":[],\"IsEssenceOnlyModifier\":false}]");
        var catalog = new ItemCatalog(root);
        return (new ItemRolls(catalog, root), catalog.ByHash(1)!);
    }
}
