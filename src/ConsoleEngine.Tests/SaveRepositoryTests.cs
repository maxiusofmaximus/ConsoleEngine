using ConsoleEngine.Persistence;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class SaveRepositoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public SaveRepositoryTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private sealed class TestSave
    {
        public string SlotId    { get; set; } = "slot1";
        public string PlayerName { get; set; } = string.Empty;
        public int    Chapter    { get; set; }
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        var data = new TestSave { SlotId = "slot1", PlayerName = "Ari", Chapter = 3 };

        repo.Save(data);
        TestSave loaded = repo.Load("slot1");

        Assert.Equal("Ari", loaded.PlayerName);
        Assert.Equal(3,     loaded.Chapter);
    }

    [Fact]
    public void Load_NonExistentSlot_ThrowsFileNotFoundException()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        Assert.Throws<FileNotFoundException>(() => repo.Load("ghost"));
    }

    [Fact]
    public void HasAnySave_ReturnsFalseWhenEmpty()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        Assert.False(repo.HasAnySave());
    }

    [Fact]
    public void HasAnySave_ReturnsTrueAfterSave()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        repo.Save(new TestSave { SlotId = "s1" });
        Assert.True(repo.HasAnySave());
    }

    [Fact]
    public void ListSlotIds_ReturnsCorrectIds()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        repo.Save(new TestSave { SlotId = "alpha" });
        repo.Save(new TestSave { SlotId = "beta"  });

        var ids = repo.ListSlotIds().ToHashSet();
        Assert.Contains("alpha", ids);
        Assert.Contains("beta",  ids);
    }

    [Fact]
    public void LoadMostRecent_ReturnsNewestSave()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        repo.Save(new TestSave { SlotId = "old",  Chapter = 1 });
        System.Threading.Thread.Sleep(10);
        repo.Save(new TestSave { SlotId = "new",  Chapter = 5 });

        TestSave recent = repo.LoadMostRecent();
        Assert.Equal(5, recent.Chapter);
    }

    [Fact]
    public void LoadMostRecent_SingleSave_ReturnsThatSave()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        repo.Save(new TestSave { SlotId = "only", Chapter = 7 });

        TestSave recent = repo.LoadMostRecent();
        Assert.Equal(7, recent.Chapter);
    }

    [Fact]
    public void LoadMostRecent_NoSaves_ThrowsInvalidOperationException()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        Assert.Throws<InvalidOperationException>(() => repo.LoadMostRecent());
    }

    [Fact]
    public void Save_OverwritesExistingSlot()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        repo.Save(new TestSave { SlotId = "s1", Chapter = 1 });
        repo.Save(new TestSave { SlotId = "s1", Chapter = 99 });

        TestSave loaded = repo.Load("s1");
        Assert.Equal(99, loaded.Chapter);
    }

    [Fact]
    public void ListSlotIds_WhenEmpty_ReturnsNoIds()
    {
        var repo = new SaveRepository<TestSave>(_tempDir, s => s.SlotId);
        Assert.Empty(repo.ListSlotIds());
    }
}
