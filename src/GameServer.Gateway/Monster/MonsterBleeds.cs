using GameServer.Core.World;

/// <summary>
/// The player's bleeds on one monster. Several can be on it at once; only the strongest deals damage, and each keeps
/// its own expiry, so a weaker, longer one resumes when the strongest ends (the same rule as ignite; the moving
/// bonus is not modelled). Guarded by the connection's Monsters lock.
/// </summary>
sealed class MonsterBleeds
{
    readonly record struct Bleed(double From, double Until, double DamagePerSecond);
    readonly List<Bleed> active = [];
    double last, carry;
    double expiryRate = 1;

    public void SetExpiryRate(double now, double rate)
    {
        rate = Math.Max(.25, rate);
        for (int i = 0; i < active.Count; i++)
            active[i] = active[i] with { Until = RecipientEffectTime.RetimedDeadline(active[i].Until, now, expiryRate, rate) };
        expiryRate = rate;
    }

    /// <summary>The native <c>bleeding</c> buff instance, when the buff table has that row.</summary>
    public ushort? Buff;

    public bool Bleeding => active.Count > 0;

    public bool Idle => active.Count == 0 && Buff is null;

    public double Until => active.Count == 0 ? 0 : active.Max(b => b.Until);

    /// <summary>The strongest bleed's damage per second at <paramref name="now"/> (0 when none is running).</summary>
    public double PerSecond(double now) =>
        active.Where(b => b.From <= now && now < b.Until).Select(b => b.DamagePerSecond).DefaultIfEmpty().Max();

    public bool Add(double now, double perSecond, double seconds)
    {
        if (!(perSecond > 0) || !(seconds > 0)) return false;
        if (active.Count == 0) { last = now; carry = 0; }
        active.Add(new(now, now + seconds / expiryRate, perSecond));
        return true;
    }

    /// <summary>The whole physical damage the strongest bleed deals from the last advance to <paramref name="until"/>,
    /// times <paramref name="factor"/>; the fraction carries. Ended bleeds are dropped.</summary>
    public int Advance(double until, double factor)
    {
        if (active.Count == 0 || !(until > last)) return 0;
        var cuts = active.SelectMany(b => new[] { b.From, b.Until }).Where(t => last < t && t < until)
            .Append(last).Append(until).Distinct().Order().ToArray();
        double damage = carry;
        for (int k = 1; k < cuts.Length; k++)
        {
            double mid = (cuts[k - 1] + cuts[k]) / 2;
            damage += active.Where(b => b.From <= mid && mid < b.Until).Select(b => b.DamagePerSecond).DefaultIfEmpty().Max()
                * Math.Max(0, factor) * (cuts[k] - cuts[k - 1]);
        }
        last = until;
        active.RemoveAll(b => b.Until <= until);
        int whole = (int)Math.Min(int.MaxValue, Math.Floor(Math.Max(0, damage)));
        carry = active.Count == 0 ? 0 : damage - whole;
        return whole;
    }

    public void Clear() { active.Clear(); carry = 0; }
}
