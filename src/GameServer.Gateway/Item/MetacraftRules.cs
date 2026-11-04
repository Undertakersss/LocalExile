using GameServer.Core.Protocol.Instance;

/// <summary>Snapshot rules before mutation: removing the protecting craft must not unlock its affixes mid-operation.</summary>
sealed class MetacraftRules(Item item, ModCatalog catalog)
{
    bool Has(string stat) => item.Mods.Where(m => m.Section == ModSection.Explicit).Any(m =>
        catalog.ByHash(m.Hash)?.Stats.Select((s, i) => s.Id == stat && i < m.Values.Count && m.Values[i] > 0).Any(v => v) == true);

    /// <summary>A prefix or suffix lock. A Divine Orb still rerolls a fractured mod, so this does not include
    /// fractures.</summary>
    public bool Locked(int index) => catalog.ByHash(item.Mods[index].Hash) is { } mod &&
        (mod.GenerationType == ItemRolls.Prefix && Has("item_generation_cannot_change_prefixes") ||
         mod.GenerationType == ItemRolls.Suffix && Has("item_generation_cannot_change_suffixes"));

    public bool Protected(int index) => item.FracturedIndexes().Contains(index) || Locked(index);

    public bool Allows(ModCatalog.Mod mod) =>
        !(Has("item_generation_cannot_roll_attack_affixes") && mod.Tags.Contains("attack")) &&
        !(Has("item_generation_cannot_roll_caster_affixes") && mod.Tags.Contains("caster"));
}
