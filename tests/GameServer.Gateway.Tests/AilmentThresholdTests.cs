namespace GameServer.Gateway.Tests;

public class AilmentThresholdTests
{
    [Fact]
    public void Scaled_threshold_stays_at_life_when_the_columns_match()
    {
        Assert.Equal(4000, MonsterCatalog.ScaledAilmentThreshold(4000, 100, 100));
        Assert.Equal(10000, MonsterCatalog.ScaledAilmentThreshold(4000, 100, 250));
        Assert.Equal(66, MonsterCatalog.ScaledAilmentThreshold(100, 3, 2));
        Assert.Equal(4000, MonsterCatalog.ScaledAilmentThreshold(4000, 0, 250));
        Assert.Equal(4000, MonsterCatalog.ScaledAilmentThreshold(4000, 100, 0));
        Assert.Equal(0, MonsterCatalog.ScaledAilmentThreshold(0, 100, 250));
    }

    [Fact]
    public void Catalog_scales_from_the_level_row_and_ignores_a_missing_column()
    {
        string raised = Write(Row(100, 250), Row(100, 100));
        string missing = Write(Row(100, null));
        try
        {
            var catalog = new MonsterCatalog(raised);
            Assert.Equal(10000, catalog.AilmentThreshold(1, 4000));
            Assert.Equal(4000, catalog.AilmentThreshold(2, 4000));
            Assert.Equal(4000, catalog.AilmentThreshold(3, 4000));
            Assert.Equal(0, catalog.AilmentThreshold(1, 0));
            Assert.Equal(4000, new MonsterCatalog(missing).AilmentThreshold(1, 4000));
        }
        finally
        {
            Directory.Delete(raised, true);
            Directory.Delete(missing, true);
        }
    }

    static string Row(int life, int? threshold)
    {
        string extra = threshold is int value ? $",\"AilmentThreshold\":{value}" : "";
        return "{\"MonsterLife\":" + life + ",\"MinionLife\":1,\"AltLife1\":9,\"AltLife2\":9,"
            + "\"Damage\":1,\"Experience\":1,\"Accuracy\":1,\"Evasion\":1,\"EvasiveEvasion\":1,\"MinionDamage\":1"
            + extra + "}";
    }

    static string Write(params string[] rows)
    {
        string root = Directory.CreateTempSubdirectory("localexile-ailment-").FullName;
        Directory.CreateDirectory(Path.Combine(root, "tables"));
        File.WriteAllText(Path.Combine(root, "tables", "DefaultMonsterStats.json"), "[" + string.Join(",", rows) + "]");
        File.WriteAllText(Path.Combine(root, "tables", "MonsterTypes.json"),
            """[{"IsPlayerMinion":false,"AltLife1":false,"AltLife2":false}]""");
        return root;
    }
}
