using ConsoleEngine.Core;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class FlagStoreTests
{
    [Fact]
    public void Set_And_Get_String()
    {
        var store = new FlagStore();
        store.Set("player.name", "Ari");
        Assert.Equal("Ari", store.Get<string>("player.name"));
    }

    [Fact]
    public void Set_And_Get_Int()
    {
        var store = new FlagStore();
        store.Set("chapter", 3);
        Assert.Equal(3, store.Get<int>("chapter"));
    }

    [Fact]
    public void Set_And_Get_Bool()
    {
        var store = new FlagStore();
        store.Set("visited.forest", true);
        Assert.True(store.Get<bool>("visited.forest"));
    }

    [Fact]
    public void Get_MissingKey_ReturnsDefault()
    {
        var store = new FlagStore();
        Assert.Equal(0,    store.Get<int>("missing"));
        Assert.Null(store.Get<string>("also.missing"));
    }

    [Fact]
    public void Has_ReturnsTrueAfterSet()
    {
        var store = new FlagStore();
        Assert.False(store.Has("x"));
        store.Set("x", 1);
        Assert.True(store.Has("x"));
    }

    [Fact]
    public void Remove_DeletesEntry()
    {
        var store = new FlagStore();
        store.Set("key", 42);
        store.Remove("key");
        Assert.False(store.Has("key"));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var store = new FlagStore();
        store.Set("a", 1);
        store.Set("b", 2);
        store.Clear();
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Keys_AreCaseInsensitive()
    {
        var store = new FlagStore();
        store.Set("MyKey", 99);
        Assert.True(store.Has("mykey"));
        Assert.Equal(99, store.Get<int>("MYKEY"));
    }

    [Fact]
    public void RoundTrip_ToJson_FromJson_PreservesData()
    {
        var original = new FlagStore();
        original.Set("name", "Ari");
        original.Set("chapter", 2);
        original.Set("done", true);

        string json = original.ToJson();
        FlagStore loaded = FlagStore.FromJson(json);

        Assert.Equal("Ari", loaded.Get<string>("name"));
        Assert.Equal(2,     loaded.Get<int>("chapter"));
        Assert.True(loaded.Get<bool>("done"));
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        var store = new FlagStore();
        store.Set("score", 100);

        bool found = store.TryGet("score", out int val);
        Assert.True(found);
        Assert.Equal(100, val);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var store = new FlagStore();
        bool found = store.TryGet("missing", out int val);
        Assert.False(found);
        Assert.Equal(0, val);
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        var store = new FlagStore();
        store.Set("score", 10);
        store.Set("score", 99);
        Assert.Equal(99, store.Get<int>("score"));
    }

    [Fact]
    public void Keys_ContainsAllSetKeys()
    {
        var store = new FlagStore();
        store.Set("a", 1);
        store.Set("b", 2);
        store.Set("c", 3);
        Assert.Contains("a", store.Keys);
        Assert.Contains("b", store.Keys);
        Assert.Contains("c", store.Keys);
        Assert.Equal(3, store.Keys.Count);
    }

    [Fact]
    public void Remove_MissingKey_ReturnsFalse()
    {
        var store = new FlagStore();
        Assert.False(store.Remove("nonexistent"));
    }

    [Fact]
    public void Get_WrongType_ReturnsDefault()
    {
        var store = new FlagStore();
        store.Set("name", "Ari");
        int result = store.Get<int>("name");
        Assert.Equal(0, result);
    }

    [Fact]
    public void Count_TracksEntries()
    {
        var store = new FlagStore();
        Assert.Equal(0, store.Count);
        store.Set("x", 1);
        Assert.Equal(1, store.Count);
        store.Set("y", 2);
        Assert.Equal(2, store.Count);
        store.Remove("x");
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void FromJson_EmptyObject_ReturnsEmptyStore()
    {
        FlagStore store = FlagStore.FromJson("{}");
        Assert.Equal(0, store.Count);
    }
}
