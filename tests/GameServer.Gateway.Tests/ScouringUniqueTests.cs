using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

public class ScouringUniqueTests
{
    [Fact]
    public void Scouring_a_unique_returns_the_normal_base()
    {
        var rolls = new ItemRolls(Catalog());
        var unique = new Item(0x11111111, ["Base", "Mods", "Quality"], 99)
        {
            Rarity = ItemRarity.Unique, Quality = 12, Identified = true, NameWords = [7],
            Sockets = [new ItemSocket(SocketColour.Red)],
            Mods =
            [
                new ItemMod(ModSection.Implicit, 1, [4]),
                new ItemMod(ModSection.Explicit, 2, [10]),
                new ItemMod(ModSection.Explicit, 3, [20]),
            ],
        };

        var scoured = rolls.Scour(unique);

        Assert.NotNull(scoured);
        Assert.Equal(ItemRarity.Normal, scoured.Rarity);
        Assert.Equal([(ModSection.Implicit, (ushort)1)], scoured.Mods.Select(m => (m.Section, m.Hash)));
        Assert.Equal(12, scoured.Quality);
        Assert.Empty(scoured.NameWords);
        Assert.Equal(11, scoured.VisualIdentity);
        Assert.Equal(SocketColour.Red, Assert.Single(scoured.Sockets).Colour);
    }

    [Fact]
    public void Scouring_still_clears_a_magic_item_and_refuses_a_normal_one()
    {
        var rolls = new ItemRolls(Catalog());
        var magic = new Item(0x11111111, ["Base", "Mods"], 11)
        {
            Rarity = ItemRarity.Magic, NameWords = [3],
            Mods = [new ItemMod(ModSection.Explicit, 2, [8]), new ItemMod(ModSection.Implicit, 1, [1])],
        };
        var normal = magic with { Rarity = ItemRarity.Normal, Mods = [new ItemMod(ModSection.Implicit, 1, [1])] };

        var scoured = rolls.Scour(magic);

        Assert.NotNull(scoured);
        Assert.Equal(ItemRarity.Normal, scoured.Rarity);
        Assert.Equal(ModSection.Implicit, Assert.Single(scoured.Mods).Section);
        Assert.Null(rolls.Scour(normal));
    }

    static ItemCatalog Catalog()
    {
        string root = Directory.CreateTempSubdirectory("localexile-scour-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "tables"));
        File.WriteAllText(Path.Combine(root, "item-types.json"),
            """{"types":[{"type":"Metadata/Items/Weapons/Wands/Wand1","components":["Base","Mods","Quality"],"read":3}]}""");
        File.WriteAllText(Path.Combine(root, "tables", "ItemVisualIdentity.json"), """[{"HASH16":11}]""");
        File.WriteAllText(Path.Combine(root, "tables", "BaseItemTypes.json"),
            """
            [{"Id":"Metadata/Items/Weapons/Wands/Wand1","Name":"Driftwood Wand","HASH32":286331153,"Width":1,"Height":3,
            "ItemVisualIdentity":0,"ItemClassesKey":null,"ItemStance":null,"DropLevel":1,"Implicit_ModsKeys":[],"TagsKeys":[],
            "ModDomain":1}]
            """);
        return new ItemCatalog(root);
    }
}
