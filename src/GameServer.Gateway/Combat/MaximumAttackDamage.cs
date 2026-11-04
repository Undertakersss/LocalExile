using GameServer.Core.World;

/// <summary>"More Maximum Attack Damage" (Devout of Ryslatha's <c>maximum_attack_damage_+%_final_from_ascendancy</c>):
/// the maximum of the attack's base and added damage, which for a hit is the maximum of every final component after
/// conversion. PoB CalcOffence applies MaxDamage MORE to each type's own maximum and carries it through conversion,
/// which is the same. It scales the damaging ailments an attack inflicts too (the wiki). ContactIgnite runs Apply on
/// the ignite row's maxima, which is that flat maximum scaled before the row's increases. The client folds its sources into
/// <c>combined_maximum_attack_damage_+%_final</c> as a product (FUN_1425d25b0); this is the only one the server has.</summary>
static class MaximumAttackDamage
{
    public const string FromAscendancy = "maximum_attack_damage_+%_final_from_ascendancy";

    public static DamagePacket Apply(DamagePacket hit, Func<string, int> get)
    {
        int percent = get(FromAscendancy);
        if (percent == 0) return hit;
        double factor = Math.Max(0, 1 + percent / 100.0);
        return hit.Select((_, range) => new(range.Min, range.Max * factor));
    }
}
