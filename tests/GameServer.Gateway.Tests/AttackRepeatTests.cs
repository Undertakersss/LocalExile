namespace GameServer.Gateway.Tests;

/// <summary>Multistrike (attack_repeat_count): extra swings of one use, one attack time apart.</summary>
public sealed class AttackRepeatTests
{
    [Fact]
    public void Attack_repeat_count_is_the_extra_swings_unless_repeats_are_disabled()
    {
        Assert.Equal(2, Game.AttackRepeats(name => name == "attack_repeat_count" ? 2 : 0));
        Assert.Equal(0, Game.AttackRepeats(name => name == "attack_repeat_count" ? 2 : name == "disable_skill_repeats" ? 1 : 0));
        Assert.Equal(0, Game.AttackRepeats(_ => -3));
    }

    [Fact]
    public void Each_repeat_contacts_one_attack_time_after_the_previous()
    {
        Assert.Equal(1.4, Game.RepeatContactAt(1.0, 0, 0.5, 0.4));
        Assert.Equal(2.4, Game.RepeatContactAt(1.0, 2, 0.5, 0.4));
    }
}
