namespace ConsoleEngine.Editor.Models;

/// <summary>
/// Represents a single <c>.scene.json</c> file shown in the left-panel file list.
/// </summary>
public sealed class SceneFileEntry
{
    public string FilePath { get; }
    public string DisplayName { get; }
    public bool IsNew { get; }

    public SceneFileEntry(string filePath, bool isNew = false)
    {
        FilePath    = filePath;
        DisplayName = isNew ? "* " + Path.GetFileNameWithoutExtension(filePath)
                            : Path.GetFileNameWithoutExtension(filePath);
        IsNew       = isNew;
    }

    public override string ToString() => DisplayName;
}
