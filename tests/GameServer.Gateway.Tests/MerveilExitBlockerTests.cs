namespace GameServer.Gateway.Tests;

/// <summary>Act 1's exit into Act 2 (a1q3, 12-quests.md §9): the saved kill lifts the server's MerveilExitBlockers,
/// including when the client already had the row through a waypoint access flag.</summary>
public class MerveilExitBlockerTests
{
    const string Flag = "a1q3-KilledSeaWitch";
    static readonly Quests.BlockerRule Exit = new("1_1_11_2", "Metadata/QuestObjects/MerveilExitBlocker", 4, Flag);

    [Fact]
    public void Killing_the_twisted_saves_the_flag_and_clears_the_exit_blocker()
    {
        string path = Path.Combine(Path.GetTempPath(), $"localexile-merveil-{Guid.NewGuid():N}.db");
        Saves.Open(path);
        try
        {
            var conn = Connection();
            conn.QuestBlockers.Add((Exit, 30, 40));
            Game.UpdateQuestBlockers(conn);
            Assert.Contains(conn.StationaryBlockers, b => b.X == 30 && b.Y == 40 && b.Size == 4);

            var committed = Game.CommitQuestFlags(conn, [Flag]);
            Assert.NotNull(committed);
            Assert.Contains(Flag, committed.Changed);
            Assert.Contains(Flag, CharacterSaves.Load("SeaWitch")!.QuestFlags!);
            Assert.DoesNotContain(conn.StationaryBlockers, b => b.X == 30 && b.Y == 40);
        }
        finally { Saves.Db.Dispose(); try { File.Delete(path); } catch (IOException) { } }
    }

    // The client already counted the row, so EffectiveChanges is empty, but the walk map still has the blocker.
    [Fact]
    public void A_sea_witch_row_the_client_already_had_still_lifts_the_server_blocker()
    {
        var effective = Quests.EffectiveChanges([Flag], _ => true, _ => true);
        Assert.Empty(effective);
        Assert.True(Game.QuestBlockersFollow([Flag], [(Exit, 12, 18)]));
        Assert.False(Game.QuestBlockersFollow(effective, [(Exit, 12, 18)]));

        var conn = Connection();
        conn.QuestBlockers.Add((Exit, 12, 18));
        conn.SetBlocker(Game.QuestBlockerFirstId, 12, 18, Exit.Size);
        conn.QuestFlags.Add(Flag);
        Game.UpdateQuestBlockers(conn);
        Assert.DoesNotContain(conn.StationaryBlockers, b => b.X == 12 && b.Y == 18);
    }

    static ClientState Connection() =>
        new(new AreaLayout(0, "1_1_11_2", 64, 64, 10, 10, null, []), 1, 1) { Character = "SeaWitch" };
}
