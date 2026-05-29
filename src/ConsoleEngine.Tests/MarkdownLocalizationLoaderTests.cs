using ConsoleEngine.Core;
using ConsoleEngine.Locale;
using Xunit;

namespace ConsoleEngine.Tests;

public sealed class MarkdownLocalizationLoaderTests
{
    private static LocalizationTable Parse(string md) =>
        MarkdownLocalizationLoader.Parse("en", md);

    private static string Get(LocalizationTable table, string key) =>
        table.TryGet(key, out string val) ? val : $"[{key}]";

    [Fact]
    public void Parse_ValidTable_ReturnsEntries()
    {
        var table = Parse("""
            # Locale: en

            | Key          | Value     |
            |--------------|-----------|
            | greeting     | Hello     |
            | farewell     | Goodbye   |
            """);

        Assert.Equal("Hello",   Get(table, "greeting"));
        Assert.Equal("Goodbye", Get(table, "farewell"));
    }

    [Fact]
    public void Parse_EmptyMarkdown_ReturnsEmptyTable()
    {
        var table = Parse(string.Empty);
        Assert.Equal(0, table.Count);
    }

    [Fact]
    public void Parse_CommentLines_AreIgnored()
    {
        var table = Parse("""
            | Key   | Value |
            |-------|-------|
            | hello | Hi    |
            > This is a comment and should be ignored
            """);

        Assert.Equal("Hi", Get(table, "hello"));
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void Parse_BlankLines_AreIgnored()
    {
        var table = Parse("""
            | Key   | Value |
            |-------|-------|

            | greet | Hey   |

            """);

        Assert.Equal("Hey", Get(table, "greet"));
    }

    [Fact]
    public void Parse_ExtraColumns_AreIgnored()
    {
        var table = Parse("""
            | Key   | Value | Extra  | Column |
            |-------|-------|--------|--------|
            | hello | Hi    | unused | data   |
            """);

        Assert.Equal("Hi", Get(table, "hello"));
    }

    [Fact]
    public void Parse_FormatStringValues_Preserved()
    {
        var table = Parse("""
            | Key       | Value        |
            |-----------|--------------|
            | greet_fmt | Hello, {0}!  |
            """);

        Assert.Equal("Hello, {0}!", Get(table, "greet_fmt"));
    }

    [Fact]
    public void Parse_MissingKey_TryGetReturnsFalse()
    {
        var table = Parse("""
            | Key   | Value |
            |-------|-------|
            | hello | Hi    |
            """);

        Assert.False(table.TryGet("unknown.key", out _));
    }

    [Fact]
    public void Parse_MultipleTablesInOneFile_AllEntriesLoaded()
    {
        var table = Parse("""
            ## Section A

            | Key | Value |
            |-----|-------|
            | a   | Alpha |

            ## Section B

            | Key | Value |
            |-----|-------|
            | b   | Beta  |
            """);

        Assert.Equal("Alpha", Get(table, "a"));
        Assert.Equal("Beta",  Get(table, "b"));
    }

    [Fact]
    public void Parse_EmptyLanguageCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MarkdownLocalizationLoader.Parse(string.Empty, "| k | v |\n|---|---|\n| x | y |"));
    }

    [Fact]
    public void Parse_WhitespaceLanguageCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MarkdownLocalizationLoader.Parse("   ", "| k | v |\n|---|---|\n| x | y |"));
    }

    [Fact]
    public void Parse_LanguageCode_IsPreserved()
    {
        var table = MarkdownLocalizationLoader.Parse("es", """
            | Key  | Value |
            |------|-------|
            | hola | Hola  |
            """);

        Assert.Equal("es", table.LanguageCode);
    }

    [Fact]
    public void Parse_RowWithEmptyKey_IsSkipped()
    {
        var table = Parse("""
            | Key   | Value |
            |-------|-------|
            |       | data  |
            | valid | yes   |
            """);

        Assert.Equal("yes", Get(table, "valid"));
        Assert.Equal(1, table.Count);
    }

    [Fact]
    public void Parse_EntryCount_MatchesRowCount()
    {
        var table = Parse("""
            | Key | Value |
            |-----|-------|
            | a   | one   |
            | b   | two   |
            | c   | three |
            """);

        Assert.Equal(3, table.Count);
    }

    [Fact]
    public void Parse_Entries_AreAccessibleViaReadOnlyDictionary()
    {
        var table = Parse("""
            | Key   | Value |
            |-------|-------|
            | color | blue  |
            """);

        Assert.True(table.Entries.ContainsKey("color"));
        Assert.Equal("blue", table.Entries["color"]);
    }
}
