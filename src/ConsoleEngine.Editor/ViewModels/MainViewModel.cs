using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ConsoleEngine.Core;
using ConsoleEngine.Editor.Models;

namespace ConsoleEngine.Editor.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    // ── INotifyPropertyChanged ────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // ── State ─────────────────────────────────────────────────────────────
    private string _projectPath = string.Empty;
    private SceneFileEntry? _selectedFile;
    private SceneDocument? _doc;
    private bool _isDirty;

    private string _sceneTitle  = string.Empty;
    private string _linesText   = string.Empty;
    private string _asciiArtText = string.Empty;
    private string _textColor   = "Gray";
    private string _artColor    = "DarkGreen";
    private bool   _promptContinue = true;
    private string _previewText = string.Empty;
    private string _statusText  = "Open a project folder to begin.";

    // ── Static metadata ───────────────────────────────────────────────────
    public static string WindowTitle { get; } = $"ConsoleEngine Editor — v{EngineVersion.Full}";

    // ── Observable collections ────────────────────────────────────────────
    public ObservableCollection<SceneFileEntry> SceneFiles { get; } = new();

    public static IReadOnlyList<string> AvailableColors { get; } =
        Enum.GetNames<ConsoleColor>().OrderBy(n => n).ToArray();

    // ── Properties ────────────────────────────────────────────────────────
    public string ProjectPath
    {
        get => _projectPath;
        set { Set(ref _projectPath, value); Notify(nameof(HasProject)); }
    }

    public bool HasProject => !string.IsNullOrEmpty(_projectPath);

    public SceneFileEntry? SelectedFile
    {
        get => _selectedFile;
        set { Set(ref _selectedFile, value); }
    }

    public bool HasDocument => _doc is not null;

    public string SceneTitle
    {
        get => _sceneTitle;
        set { Set(ref _sceneTitle, value); SyncAndPreview(); MarkDirty(); }
    }

    public string LinesText
    {
        get => _linesText;
        set { Set(ref _linesText, value); SyncAndPreview(); MarkDirty(); }
    }

    public string AsciiArtText
    {
        get => _asciiArtText;
        set { Set(ref _asciiArtText, value); SyncAndPreview(); MarkDirty(); }
    }

    public string TextColor
    {
        get => _textColor;
        set { Set(ref _textColor, value); SyncAndPreview(); MarkDirty(); }
    }

    public string ArtColor
    {
        get => _artColor;
        set { Set(ref _artColor, value); SyncAndPreview(); MarkDirty(); }
    }

    public bool PromptContinue
    {
        get => _promptContinue;
        set { Set(ref _promptContinue, value); SyncAndPreview(); MarkDirty(); }
    }

    public string PreviewText
    {
        get => _previewText;
        private set => Set(ref _previewText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set { Set(ref _isDirty, value); Notify(nameof(CanSave)); }
    }

    public bool CanSave => _doc is not null && _isDirty;

    // ── Project ───────────────────────────────────────────────────────────
    public void LoadProject(string folderPath)
    {
        ProjectPath = folderPath;
        SceneFiles.Clear();
        _doc = null;
        Notify(nameof(HasDocument));

        // Exclude build artefact directories so bin/ copies don't appear as duplicates.
        static bool IsArtifactPath(string path) =>
            path.Contains(Path.DirectorySeparatorChar + "bin"  + Path.DirectorySeparatorChar) ||
            path.Contains(Path.DirectorySeparatorChar + "obj"  + Path.DirectorySeparatorChar);

        foreach (string f in Directory.EnumerateFiles(folderPath, "*.scene.json", SearchOption.AllDirectories)
                                      .Where(f => !IsArtifactPath(f))
                                      .OrderBy(x => x))
        {
            SceneFiles.Add(new SceneFileEntry(f));
        }

        StatusText = SceneFiles.Count == 0
            ? $"Opened: {folderPath}  (no .scene.json files found — create one with ➕)"
            : $"Opened: {folderPath}  —  {SceneFiles.Count} scene(s) found.";
    }

    // ── Document load ─────────────────────────────────────────────────────
    public void LoadScene(SceneFileEntry entry)
    {
        try
        {
            string json = File.ReadAllText(entry.FilePath);
            var doc = JsonSerializer.Deserialize<SceneDocument>(json) ?? SceneDocument.Empty();
            SetDocument(doc);
            StatusText = $"Loaded: {Path.GetFileName(entry.FilePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading {Path.GetFileName(entry.FilePath)}: {ex.Message}";
        }
    }

    private void SetDocument(SceneDocument doc)
    {
        _doc = doc;

        // Suppress dirty marking during initial load
        _isDirty = false;

        _sceneTitle   = doc.Title;
        _linesText    = string.Join(Environment.NewLine, doc.Lines);
        _asciiArtText = string.Join(Environment.NewLine, doc.AsciiArt);
        _textColor    = doc.TextColor;
        _artColor     = doc.ArtColor;
        _promptContinue = doc.PromptContinue;

        Notify(nameof(SceneTitle));
        Notify(nameof(LinesText));
        Notify(nameof(AsciiArtText));
        Notify(nameof(TextColor));
        Notify(nameof(ArtColor));
        Notify(nameof(PromptContinue));
        Notify(nameof(HasDocument));
        Notify(nameof(IsDirty));
        Notify(nameof(CanSave));

        RebuildPreview();
    }

    // ── New scene ─────────────────────────────────────────────────────────
    public SceneFileEntry CreateNewScene(string folder)
    {
        // Find a unique name
        int n = 1;
        string path;
        do { path = Path.Combine(folder, $"scene{n:D3}.scene.json"); n++; }
        while (File.Exists(path));

        var entry = new SceneFileEntry(path, isNew: true);
        SceneFiles.Add(entry);
        SetDocument(SceneDocument.Empty());
        _isDirty = true;
        Notify(nameof(IsDirty));
        Notify(nameof(CanSave));

        StatusText = $"New scene: {Path.GetFileName(path)}  (unsaved)";
        return entry;
    }

    // ── Save ──────────────────────────────────────────────────────────────
    public bool TrySave(SceneFileEntry entry)
    {
        if (_doc is null) return false;

        try
        {
            string json = JsonSerializer.Serialize(_doc, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(entry.FilePath, json);
            IsDirty = false;
            StatusText = $"Saved: {Path.GetFileName(entry.FilePath)}";

            // Refresh the entry's display name (remove '*' prefix)
            int idx = SceneFiles.IndexOf(entry);
            if (idx >= 0)
            {
                SceneFiles[idx] = new SceneFileEntry(entry.FilePath, isNew: false);
            }
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
            return false;
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────
    private void MarkDirty()
    {
        if (_doc is null) return;
        IsDirty = true;
    }

    private void SyncAndPreview()
    {
        if (_doc is null) return;
        _doc.Title  = _sceneTitle;
        _doc.Lines  = SplitLines(_linesText);
        _doc.AsciiArt = SplitLines(_asciiArtText);
        _doc.TextColor = _textColor;
        _doc.ArtColor  = _artColor;
        _doc.PromptContinue = _promptContinue;
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        if (_doc is null) { PreviewText = string.Empty; return; }

        const int W = 54;
        var sb = new StringBuilder();

        // Top bar
        sb.AppendLine(new string('═', W));

        // Title
        if (!string.IsNullOrWhiteSpace(_doc.Title))
        {
            sb.AppendLine($"  {_doc.Title}");
            sb.AppendLine(new string('─', W));
        }

        sb.AppendLine();

        // Narration lines
        foreach (string line in _doc.Lines)
            sb.AppendLine("  " + line);

        sb.AppendLine();
        sb.AppendLine(new string('─', W));

        // ASCII art block
        if (_doc.AsciiArt.Length > 0)
        {
            sb.AppendLine($"  [Art · color: {_doc.ArtColor}]");
            foreach (string row in _doc.AsciiArt)
                sb.AppendLine("  " + row);
        }

        sb.AppendLine(new string('═', W));

        // Continue prompt
        if (_doc.PromptContinue)
            sb.AppendLine($"  [ PRESS ENTER TO CONTINUE ]");

        PreviewText = sb.ToString();
    }

    private static string[] SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.None)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();
}
