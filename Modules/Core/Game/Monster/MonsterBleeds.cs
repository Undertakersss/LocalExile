using System.Net.Sockets;
using GameServer.Core.Protocol.Instance;
using GameServer.Core.Protocol.Login;
using GameServer.Core.World;

static partial class Game
{
    static readonly string[] BleedIncreases = ["damage_+%", "damage_over_time_+%", "physical_damage_+%",
        "physical_damage_over_time_+%", "bleeding_damage_+%", "base_ailment_damage_+%", "attack_damage_+%"];

    /// <summary>
    /// An attack hit that dealt physical damage tries to bleed (20-damage.md §3). Spells do not. The chance is the
    /// panel's attack bleed chance (<c>bleed_on_hit_with_attacks_%</c> and <c>local_chance_to_bleed_on_hit_%</c>).
    /// The base is 70% of that physical per second for 5 s, times the bleed increases and the damage-over-time
    /// multipliers (a critical strike adds the built-in +50, as ignite and poison do). A critical strike does not
    /// bleed by itself. The moving bonus is not applied. Only the strongest bleed deals damage
    /// (<see cref="MonsterBleeds"/>). Under the Monsters lock.
    /// </summary>
    internal static void BleedOnHitLocked(SpawnedMonster target, DamagePacket mitigated, bool spell, bool critical,
        Func<string, int> get, double now, Random random)
    {
        if (spell || target.Dead || target.Life == 0 || target.Invulnerable) return;
        double physical = mitigated[DamageType.Physical].Min;
        if (physical <= 0 || BleedProhibited(get)) return;
        int chance = get("bleed_on_hit_with_attacks_%") + get("local_chance_to_bleed_on_hit_%");
        if (!RandomRolls.Chance(chance, 100, random)) return;
        var mods = target.Mods.Effects;
        if (mods["avoid_bleed_%"] > 0 && RandomRolls.Chance(mods["avoid_bleed_%"], 100, random)) return;
        if (mods["immune_to_bleeding"] != 0 || mods["cannot_be_bleeding"] != 0) return;

        double faster = Math.Max(0, 1 + get("damaging_ailments_deal_damage_+%_faster") / 100.0);
        if (faster <= 0) return;
        double dot = 1 + ((double)get("dot_multiplier_+") + get("physical_dot_multiplier_+")
            + (critical ? CriticalAilmentDotMultiplier + get("critical_ailment_dot_multiplier_+") : 0)) / 100.0;
        var bleed = Ailments.Bleed(DamagePacket.Of(DamageType.Physical, physical, physical), BleedIncreases.Sum(get), dot);
        double seconds = Ailments.BleedDurationSeconds
            * Math.Max(0, 1 + ((double)get("bleed_duration_+%") + get("base_bleed_duration_+%")
                + get("base_all_ailment_duration_+%")) / 100.0) / faster;
        if (bleed.DamagePerSecond <= 0 || seconds <= 0) return;
        target.Bleeds.Add(now, bleed.DamagePerSecond * faster, seconds);
    }

    static bool BleedProhibited(Func<string, int> get) =>
        get("never_damaging_ailments") != 0 || DamageStats.Prohibited(DamageType.Physical, get, attack: true);

    internal static async Task TickBleeds(string remote, NetworkStream stream, ConnectionCiphers ciphers, ClientState conn)
    {
        var hits = new List<(uint Id, SpawnedMonster M, uint Life, int Damage)>();
        var ended = new List<(uint Id, ushort Buff)>();
        var shown = new List<(uint Id, ushort Buff, double Remaining)>();
        ushort? row = LabyrinthTrials.BleedingBuffRow;
        using (ProjectileMutation(conn))
        {
            if (conn.Closed || conn.LeavingArea) return;
            double now = ProjectileWorldTime(conn);
            foreach (var (id, m) in conn.Monsters)
            {
                if (m.Bleeds.Idle) continue;
                int damage = 0;
                if (m.Dead || m.Life == 0) m.Bleeds.Clear();
                else damage = m.Bleeds.Advance(now, MonsterDotTaken(m, DamageType.Physical, now));
                if (damage > 0)
                {
                    (m.Life, bool stageStarts) = TakeOwnerDamageLocked(conn, m, damage);
                    m.Aggro = m.Life > 0;
                    if (m.Life == 0)
                    {
                        m.Dead = true;
                        CollectVaalSoulAtDeathLocked(conn, m);
                        m.Bleeds.Clear();
                    }
                    hits.Add((id, m, m.Life, damage));
                    if (stageStarts) CommitBossStage(remote, stream, ciphers, conn, id, m, now);
                }
                if (m.Bleeds.Buff is ushort old && (m.Dead || m.Life == 0 || !m.Bleeds.Bleeding))
                {
                    ended.Add((id, old));
                    m.Bleeds.Buff = null;
                }
                else if (row is not null && m.Bleeds.Bleeding && m.Bleeds.Buff is null)
                {
                    ushort buff = conn.NextBuff++;
                    m.Bleeds.Buff = buff;
                    shown.Add((id, buff, m.Bleeds.Until - now));
                }
            }
        }
        foreach (var (id, buff) in ended)
            await Send(remote, stream, ciphers, InstanceEntities.RemoveBuff(id, buff), null);
        if (row is ushort bleeding)
            foreach (var (id, buff, remaining) in shown)
                await Send(remote, stream, ciphers, InstanceEntities.AddBuff(id, buff, bleeding, PlayerId,
                    u68: BitConverter.SingleToUInt32Bits((float)Math.Max(0, remaining))), null);
        foreach (var (id, m, life, damage) in hits)
        {
            if (life == 0)
                await Send(remote, stream, ciphers, InstanceEntities.StatsUpdate(id, [(StatKeys.HideMiniLifeBar, 1)]), null);
            await Send(remote, stream, ciphers, InstanceProtocol.LifeUpdate(id, life),
                life == 0 ? $"0x01b1 monster 0x{id:x} ({m.Label}) bled to death" : null);
            if (life == 0) await MonsterKilled(remote, stream, ciphers, conn, id, m);
        }
    }
}
