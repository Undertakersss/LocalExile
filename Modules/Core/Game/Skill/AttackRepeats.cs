static partial class Game
{
    // Multistrike (11-skills.md, melee ctor 0x141b6eb70): the skill's attack_repeat_count (stat 0x4eb) is how many
    // extra times the one use plays. The client's animation end (0x1419e3960) takes one off and plays the swing again,
    // so each contact is one attack time after the last. One cost and one critical roll, as Spell Echo. A channel
    // (Cyclone) and disable_skill_repeats do not repeat. Network stats run no calculators, so the count is the
    // attack_repeat_count already on the supported skill (Multistrike's granted stat when the tables are extracted).
    internal static int AttackRepeats(Func<string, int> get) =>
        get("disable_skill_repeats") != 0 ? 0 : Math.Max(0, get("attack_repeat_count"));

    internal static int AttackRepeats(ClientState conn, int effect, Func<string, int> get) =>
        !SkillsFor(conn).IsAttack(effect) || Supports.Of(effect)?.Types.Contains(47) == true ? 0 : AttackRepeats(get);

    internal static int AttackRepeats(ClientState conn, ushort skillId, int effect) =>
        AttackRepeats(conn, effect, name => Stats.Key(name) is uint key ? SupportedStats(conn, skillId).GetValueOrDefault(key) : 0);

    /// <summary>When repeat <paramref name="repeat"/> of a use contacts: the first contact at
    /// <paramref name="contact"/>, each later one a full <paramref name="cycle"/> later.</summary>
    internal static double RepeatContactAt(double acceptedAt, int repeat, double cycle, double contact) =>
        acceptedAt + Math.Max(0, repeat) * Math.Max(0, cycle) + contact;
}
