using GameServer.Core.Protocol.Instance;

namespace GameServer.Gateway.Tests;

public sealed class MortarSequenceTests
{
    // Live Sand Spitter spits are 0x80bc, 0x80bd, …: a rising sequence with the top bit set, and the 0x1af names it.
    [Fact]
    public void A_monster_action_sequence_rises_with_the_top_bit_set()
    {
        var monster = new SpawnedMonster("Sand Spitter", new(0, 0, 0), 0, 0, 20);
        Assert.Equal((ushort)0x8001, monster.NextSequence());
        Assert.Equal((ushort)0x8002, monster.NextSequence());

        var hit = InstanceProtocol.ActionHit(0x20, 0x8002, Program.PlayerId, flags: 0x4201, kind: 0x03);
        Assert.Equal(0x80, hit[12]);
        Assert.Equal(0x02, hit[13]);
        Assert.Equal(0, hit[15]);
    }

    // The mortar clip is chosen from the swing's sequence. 0xffff is one fixed clip; a live-style sequence can pick the other.
    [Fact]
    public void A_rising_sequence_can_select_the_other_mortar_clip()
    {
        uint[] weights = [1, 1];
        int fixedClip = AnimationVariantSelector.Select(Game.MonsterActionSequence, weights);
        bool other = false;
        for (uint sequence = 0x8001; sequence <= 0x8020; sequence++)
            if (AnimationVariantSelector.Select(sequence, weights) != fixedClip) other = true;
        Assert.True(other);
    }
}
