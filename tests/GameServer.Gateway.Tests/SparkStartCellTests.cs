using GameServer.Core.World;

namespace GameServer.Gateway.Tests;

public sealed class SparkStartCellTests
{
    // A stop takes effect after the next tick's steps. At 37 cells/s the step into x=19 is at 19/37 s, inside the
    // 16 ms after t=0.5, so the client stands on 19 while the estimate is still on 18. Standing casts do not move.
    [Fact]
    public void A_moving_cast_starts_where_the_client_stops()
    {
        var walk = new PlayerWalk(0, 0, speed: 37);
        walk.Order(37, 0, now: 0);
        Assert.Equal((18, 0), walk.CellAt(0.5));
        Assert.Equal((19, 0), Game.ClientPredictedCastCell(walk, 0.5));
        Assert.Equal((4, 9), Game.ClientPredictedCastCell(new PlayerWalk(4, 9, speed: 37), 1));
    }

    // Lockstep already ends the stop at the frame's end (PlayerWalk.Order). The cast cell is that cell, read before
    // the order clears the path. 0.033 s at 37 cells/s is one step.
    [Fact]
    public void A_lockstep_cast_uses_the_frame_end()
    {
        var walk = new PlayerWalk(0, 0, speed: 37) { FrameOf = _ => (0, 0.033) };
        walk.Order(37, 0, now: 0);
        Assert.Equal((0, 0), walk.CellAt(0.01));
        Assert.Equal((1, 0), Game.ClientPredictedCastCell(walk, 0.01));
    }
}
