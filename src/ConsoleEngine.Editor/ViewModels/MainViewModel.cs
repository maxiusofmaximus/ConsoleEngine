using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ConsoleEngine.Core;
using ConsoleEngine.Editor.Models;
using ConsoleEngine.Editor.Terminal;
using ConsoleEngine.Scenes;

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

    // ── Scene runner paths ────────────────────────────────────────────────
    /// <summary>Fast path: runner exe copied to Editor output dir by the MSBuild target.</summary>
    private static string SceneRunnerExe { get; } =
        Path.Combine(AppContext.BaseDirectory, "ConsoleEngine.SceneRunner.exe");

    /// <summary>Fallback: runner project, used when exe is not present (dev/source build).</summary>
    private static string SceneRunnerProject { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
                     "..", "..", "..", "..",      // net8.0 → Release → bin → ConsoleEngine.Editor → src
                     "ConsoleEngine.SceneRunner",
                     "ConsoleEngine.SceneRunner.csproj"));

    // ── Cached JSON options ───────────────────────────────────────────────
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    // ── Play process ──────────────────────────────────────────────────────
    private Process? _playProcess;

    // ── Preview throttle ──────────────────────────────────────────────────
    private bool _previewDirty;
    private readonly DispatcherTimer _previewTimer;

    // ── Terminal size preset ───────────────────────────────────────────────
    private TerminalPreset _selectedPreset = TerminalPresets[0];

    // ── State ─────────────────────────────────────────────────────────────
    private string _projectPath   = string.Empty;
    private SceneFileEntry? _selectedFile;
    private SceneDocument?  _doc;
    private bool  _isDirty;

    private string  _sceneTitle    = string.Empty;
    private string  _linesText     = string.Empty;
    private string  _asciiArtText  = string.Empty;
    private string  _textColor     = "Gray";
    private string  _artColor      = "DarkGreen";
    private bool    _promptContinue = true;
    private string  _previewText   = string.Empty;
    private string  _statusText    = "Open a project folder to begin.";

    // ── Sprite ────────────────────────────────────────────────────────────
    private string?  _spritePath;
    private int      _spriteWidth = 32;
    private int      _spriteRows  = 16;
    private Bitmap?  _spriteBitmap;

    // ── AI Terminal panel ─────────────────────────────────────────────────
    private bool              _isTerminalOpen;
    private string            _terminalOutput = string.Empty;
    private string            _terminalInput  = string.Empty;
    private string            _aiCommand      = "claude";
    private AiTerminalSession? _aiSession;

    // ── Constructor ───────────────────────────────────────────────────────
    public MainViewModel()
    {
        _previewTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(100),
            DispatcherPriority.Background,
            (_, _) => FlushPreviewIfDirty());
        _previewTimer.Start();
    }

    // ── Static metadata ───────────────────────────────────────────────────
    public static string WindowTitle { get; } = $"ConsoleEngine Editor — v{EngineVersion.Full}";

    // ── Terminal size presets (Width × Height in terminal columns/rows) ───
    public static IReadOnlyList<TerminalPreset> TerminalPresets { get; } =
    [
        new("80 × 24  (VT100)", 80, 24),
        new("120 × 30", 120, 30),
        new("160 × 40", 160, 40),
    ];

    // ── Observable collections ────────────────────────────────────────────
    public ObservableCollection<SceneFileEntry> SceneFiles { get; } = new();

    public static IReadOnlyList<string> AvailableColors { get; } =
        Enum.GetNames<ConsoleColor>().OrderBy(n => n).ToArray();

    // ── Properties ────────────────────────────────────────────────────────
    public string ProjectPath
    {
        get => _projectPath;
        set { Set(ref _projectPath, value); Notify(nameof(HasProject)); Notify(nameof(CanReload)); }
    }

    public bool HasProject  => !string.IsNullOrEmpty(_projectPath);
    public bool CanReload   => HasProject;

    public TerminalPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            Set(ref _selectedPreset, value);
            Notify(nameof(TerminalSizeLabel));
            RebuildPreview();
        }
    }

    public string TerminalSizeLabel => $"{_selectedPreset.Width} × {_selectedPreset.Height}";

    public SceneFileEntry? SelectedFile
    {
        get => _selectedFile;
        set
        {
            Set(ref _selectedFile, value);
            Notify(nameof(CanPlay));
            Notify(nameof(CanDelete));
        }
    }

    public bool HasDocument  => _doc is not null;
    public bool CanDelete    => _selectedFile is not null;
    public bool CanDuplicate => HasDocument;

    // ── Play / Stop ───────────────────────────────────────────────────────
    public bool IsPlaying => _playProcess is { HasExited: false };
    public bool CanPlay   => _selectedFile is not null && !IsPlaying;
    public bool CanStop   => IsPlaying;

    public void PlayScene()
    {
        if (_selectedFile is null) return;

        // Auto-save unsaved changes before playing
        if (_isDirty) TrySave(_selectedFile);

        string scene = _selectedFile.FilePath;

        // Build terminal command: prefer compiled exe, fall back to `dotnet run`
        string runnerCmd = File.Exists(SceneRunnerExe)
            ? $"\"{SceneRunnerExe}\" \"{scene}\""
            : $"dotnet run --project \"{SceneRunnerProject}\" -- \"{scene}\"";

        // Detect Windows Terminal (AppExecutionAlias stub — must NOT be launched directly
        // from inside a WT session, as that can close the host tab and kill the editor).
        // We route through "cmd.exe /c start" so the new process is fully detached.
        string wtExe = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");

        ProcessStartInfo psi;
        if (File.Exists(wtExe))
        {
            // cmd.exe /c start launches wt.exe detached — no parent-process link to the editor.
            psi = new ProcessStartInfo("cmd.exe",
                $"/c start \"\" \"{wtExe}\" new-tab --title \"Scene Preview\" -- cmd.exe /k {runnerCmd}")
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
            };
        }
        else
        {
            // /k keeps the window open after the runner exits so the user can read output.
            psi = new ProcessStartInfo("cmd.exe", $"/k {runnerCmd}")
            {
                UseShellExecute = true,
            };
        }

        try
        {
            Process? launched = Process.Start(psi);

            if (File.Exists(wtExe))
            {
                // WT stub exits in milliseconds — don't track it; Stop is not available.
                launched?.Dispose();
                _playProcess = null;
            }
            else
            {
                // Real cmd.exe process — track it so Stop works.
                _playProcess = launched;
                if (_playProcess != null)
                {
                    _playProcess.EnableRaisingEvents = true;
                    _playProcess.Exited += OnPlayProcessExited;
                }
            }

            NotifyPlayState();
            StatusText = $"▶ Playing: {Path.GetFileName(_selectedFile.FilePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"⚠ Could not launch terminal: {ex.Message}";
        }
    }

    public void StopScene()
    {
        if (_playProcess is null || _playProcess.HasExited) return;

        try
        {
            _playProcess.Kill(entireProcessTree: true);
        }
        catch { /* process may have already exited */ }

        _playProcess = null;
        NotifyPlayState();
        StatusText = "⏹ Playback stopped.";
    }

    private void OnPlayProcessExited(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _playProcess = null;
            NotifyPlayState();
            StatusText = "Scene playback finished.";
        });
    }

    private void NotifyPlayState()
    {
        Notify(nameof(IsPlaying));
        Notify(nameof(CanPlay));
        Notify(nameof(CanStop));
    }

    // ── Reload ────────────────────────────────────────────────────────────
    public void ReloadProject()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        string? current = _selectedFile?.FilePath;
        LoadProject(_projectPath);

        // Re-select the previously active scene if it still exists
        if (current is not null)
        {
            var reselect = SceneFiles.FirstOrDefault(f => f.FilePath == current);
            if (reselect is not null)
            {
                SelectedFile = reselect;
                LoadScene(reselect);
            }
        }

        StatusText = $"🔄 Reloaded: {SceneFiles.Count} scene(s).";
    }

    // ── Duplicate ─────────────────────────────────────────────────────────
    public SceneFileEntry? DuplicateScene()
    {
        if (_selectedFile is null || _doc is null) return null;

        string dir  = Path.GetDirectoryName(_selectedFile.FilePath) ?? _projectPath;
        string stem = Path.GetFileNameWithoutExtension(
                          Path.GetFileNameWithoutExtension(_selectedFile.FilePath)); // strip .scene.json

        // Find unique name: stem_copy.scene.json → stem_copy2.scene.json → …
        string newPath;
        string suffix = "_copy";
        int n = 0;
        do
        {
            string candidate = n == 0 ? $"{stem}{suffix}" : $"{stem}{suffix}{n}";
            newPath = Path.Combine(dir, $"{candidate}.scene.json");
            n++;
        }
        while (File.Exists(newPath));

        try
        {
            File.Copy(_selectedFile.FilePath, newPath);
            var entry = new SceneFileEntry(newPath, isNew: false);

            // Insert after the original
            int idx = SceneFiles.IndexOf(_selectedFile);
            if (idx >= 0 && idx < SceneFiles.Count - 1)
                SceneFiles.Insert(idx + 1, entry);
            else
                SceneFiles.Add(entry);

            StatusText = $"Duplicated → {Path.GetFileName(newPath)}";
            return entry;
        }
        catch (Exception ex)
        {
            StatusText = $"⚠ Duplicate failed: {ex.Message}";
            return null;
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────
    public bool DeleteScene(SceneFileEntry entry)
    {
        try
        {
            if (File.Exists(entry.FilePath))
                File.Delete(entry.FilePath);

            SceneFiles.Remove(entry);

            if (_selectedFile == entry)
            {
                _doc = null;
                _selectedFile = null;
                Notify(nameof(SelectedFile));
                Notify(nameof(HasDocument));
                Notify(nameof(CanSave));
                Notify(nameof(CanPlay));
                Notify(nameof(CanDelete));
                PreviewText = string.Empty;
            }

            StatusText = $"🗑 Deleted: {Path.GetFileName(entry.FilePath)}";
            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"⚠ Delete failed: {ex.Message}";
            return false;
        }
    }

    // ── Project ───────────────────────────────────────────────────────────
    public void LoadProject(string folderPath)
    {
        ProjectPath = folderPath;
        SceneFiles.Clear();
        _doc = null;
        Notify(nameof(HasDocument));

        // Exclude build artefact directories so bin/ copies don't appear as duplicates.
        static bool IsArtifactPath(string path) =>
            path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) ||
            path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar);

        foreach (string f in Directory.EnumerateFiles(folderPath, "*.scene.json", SearchOption.AllDirectories)
                                      .Where(f => !IsArtifactPath(f))
                                      .OrderBy(x => x))
        {
            SceneFiles.Add(new SceneFileEntry(f));
        }

        StatusText = SceneFiles.Count == 0
            ? $"Opened: {folderPath}  (no .scene.json files found — create one with ➕)"
            : $"Opened: {folderPath}  —  {SceneFiles.Count} scene(s) found.";

        WriteEditorState();
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
            WriteEditorState();
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading {Path.GetFileName(entry.FilePath)}: {ex.Message}";
        }
    }

    private void SetDocument(SceneDocument doc)
    {
        _doc = doc;
        _isDirty = false;

        _sceneTitle    = doc.Title;
        _linesText     = string.Join(Environment.NewLine, doc.Lines);
        _asciiArtText  = string.Join(Environment.NewLine, doc.AsciiArt);
        _textColor     = doc.TextColor;
        _artColor      = doc.ArtColor;
        _promptContinue = doc.PromptContinue;

        // Sprite — load bitmap first so Width/Rows auto-update from PNG dims
        _spritePath  = doc.SpritePath;
        LoadSpriteBitmap(doc.SpritePath);
        // If JSON had explicit dims, keep them (only auto-compute on first load when not set)
        if (doc.SpriteWidth > 0) { _spriteWidth = doc.SpriteWidth; Notify(nameof(SpriteWidth)); }
        if (doc.SpriteRows  > 0) { _spriteRows  = doc.SpriteRows;  Notify(nameof(SpriteRows));  }

        Notify(nameof(SceneTitle));
        Notify(nameof(LinesText));
        Notify(nameof(AsciiArtText));
        Notify(nameof(TextColor));
        Notify(nameof(ArtColor));
        Notify(nameof(PromptContinue));
        Notify(nameof(SpritePath));
        Notify(nameof(HasSprite));
        Notify(nameof(HasDocument));
        Notify(nameof(IsDirty));
        Notify(nameof(CanSave));
        Notify(nameof(CanDuplicate));
        Notify(nameof(CanPlay));

        RebuildPreview();
    }

    // ── New scene ─────────────────────────────────────────────────────────
    public SceneFileEntry CreateNewScene(string folder)
    {
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
            string json = JsonSerializer.Serialize(_doc, IndentedJson);
            File.WriteAllText(entry.FilePath, json);
            IsDirty = false;
            StatusText = $"Saved: {Path.GetFileName(entry.FilePath)}";
            WriteEditorState();

            int idx = SceneFiles.IndexOf(entry);
            if (idx >= 0)
                SceneFiles[idx] = new SceneFileEntry(entry.FilePath, isNew: false);

            return true;
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
            return false;
        }
    }

    // ── Scalar properties ─────────────────────────────────────────────────
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

    // ── Sprite properties ─────────────────────────────────────────────────

    /// <summary>Absolute path to the selected PNG sprite. <see langword="null"/> = no sprite.</summary>
    public string? SpritePath
    {
        get => _spritePath;
        set
        {
            Set(ref _spritePath, value);
            LoadSpriteBitmap(value);
            SyncAndPreview();
            MarkDirty();
        }
    }

    /// <summary>Sprite width in terminal columns (= pixel width of PNG).</summary>
    public int SpriteWidth
    {
        get => _spriteWidth;
        set { Set(ref _spriteWidth, value); SyncAndPreview(); MarkDirty(); }
    }

    /// <summary>Sprite height in terminal rows (= pixel height ÷ 2).</summary>
    public int SpriteRows
    {
        get => _spriteRows;
        set { Set(ref _spriteRows, value); SyncAndPreview(); MarkDirty(); }
    }

    /// <summary>Loaded bitmap for the preview thumbnail and centre-panel overlay. <see langword="null"/> when no sprite is selected.</summary>
    public Bitmap? SpriteBitmap
    {
        get => _spriteBitmap;
        private set { Set(ref _spriteBitmap, value); Notify(nameof(HasSprite)); }
    }

    public bool HasSprite => _spriteBitmap is not null;

    /// <summary>Clears the current sprite selection.</summary>
    public void ClearSprite()
    {
        SpritePath = null;
    }

    // ── Terminal panel properties ─────────────────────────────────────────

    public bool IsTerminalOpen
    {
        get => _isTerminalOpen;
        set => Set(ref _isTerminalOpen, value);
    }

    public string TerminalOutput
    {
        get => _terminalOutput;
        private set => Set(ref _terminalOutput, value);
    }

    public string TerminalInput
    {
        get => _terminalInput;
        set => Set(ref _terminalInput, value);
    }

    public string AiCommand
    {
        get => _aiCommand;
        set => Set(ref _aiCommand, value);
    }

    public bool IsAiRunning => _aiSession?.IsRunning ?? false;

    // ── Terminal panel commands ───────────────────────────────────────────

    public void ToggleTerminalPanel()
    {
        IsTerminalOpen = !_isTerminalOpen;
        if (_isTerminalOpen && HasProject) WriteEditorState();
    }

    public void LaunchAi()
    {
        if (!HasProject) return;
        WriteEditorState();

        // Dispose previous session without blocking the UI thread
        var old = _aiSession;
        _aiSession = null;
        old?.DisposeAsync().AsTask().ContinueWith(_ => { });

        TerminalOutput = string.Empty;

        string gameContext  = Path.Combine(_projectPath, "GAME_CONTEXT.md");
        string editorState  = Path.Combine(_projectPath, "EDITOR_STATE.json");

        string cmd = _aiCommand;
        if (File.Exists(gameContext))
            cmd += $" --context \"{gameContext}\"";
        cmd += $" --context \"{editorState}\"";

        try
        {
            _aiSession = AiTerminalSession.Spawn(cmd, _projectPath);
            _aiSession.OutputReceived += OnAiOutput;
            Notify(nameof(IsAiRunning));
            AppendTerminalLine($"▶ {cmd}");
        }
        catch (Exception ex)
        {
            AppendTerminalLine($"⚠ Could not start AI: {ex.Message}");
        }
    }

    public void SendAiInput()
    {
        if (_aiSession is null || !_aiSession.IsRunning) return;
        string line = _terminalInput;
        _aiSession.SendLine(line);
        AppendTerminalLine($"> {line}");
        TerminalInput = string.Empty;
    }

    public void KillAi()
    {
        var session = _aiSession;
        _aiSession  = null;
        Notify(nameof(IsAiRunning));
        session?.DisposeAsync().AsTask().ContinueWith(_ => { });
        AppendTerminalLine("[Session terminated]");
    }

    private void OnAiOutput(string raw)
    {
        string clean = AnsiStripper.Strip(raw);
        if (string.IsNullOrEmpty(clean)) return;
        Dispatcher.UIThread.Post(() => AppendTerminalOutput(clean));
    }

    private void AppendTerminalLine(string line)
    {
        Dispatcher.UIThread.Post(() => AppendTerminalOutput(line + Environment.NewLine));
    }

    private void AppendTerminalOutput(string text)
    {
        string combined = _terminalOutput + text;
        // Keep last 20 000 chars to avoid unbounded growth
        if (combined.Length > 20_000)
            combined = combined[^20_000..];
        TerminalOutput = combined;
    }

    private void WriteEditorState()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        try { EditorStateWriter.Write(_projectPath, _selectedFile?.FilePath); }
        catch { /* non-fatal */ }
    }

    private void LoadSpriteBitmap(string? path)
    {
        _spriteBitmap?.Dispose();
        _spriteBitmap = null;

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var bmp = new Bitmap(path);
                // Auto-compute terminal dimensions from PNG pixel size:
                //   SpriteWidth = pixel width  (1 terminal col = 1 pixel wide)
                //   SpriteRows  = pixel height / 2  (1 terminal row = 2 pixels tall)
                _spriteWidth = bmp.PixelSize.Width;
                _spriteRows  = Math.Max(1, bmp.PixelSize.Height / 2);
                SpriteBitmap = bmp;
                Notify(nameof(SpriteWidth));
                Notify(nameof(SpriteRows));
            }
            catch
            {
                SpriteBitmap = null;
            }
        }
        else
        {
            SpriteBitmap = null;
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
        _doc.Title          = _sceneTitle;
        _doc.Lines          = SplitLines(_linesText);
        _doc.AsciiArt       = SplitLines(_asciiArtText);
        _doc.TextColor      = _textColor;
        _doc.ArtColor       = _artColor;
        _doc.PromptContinue = _promptContinue;
        _doc.SpritePath     = _spritePath;
        _doc.SpriteWidth    = _spriteWidth;
        _doc.SpriteRows     = _spriteRows;
        _previewDirty = true;
    }

    private void FlushPreviewIfDirty()
    {
        if (!_previewDirty) return;
        _previewDirty = false;
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        if (_doc is null) { PreviewText = string.Empty; return; }
        PreviewText = ScenePlayer.RenderToString(
            _doc.ToDefinition(),
            previewWidth:  _selectedPreset.Width,
            previewHeight: _selectedPreset.Height);
    }

    private static string[] SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.None)
            .Select(l => l.TrimEnd('\r'))
            .ToArray();
}

/// <summary>A named terminal dimension preset shown in the size picker.</summary>
public sealed record TerminalPreset(string Label, int Width, int Height)
{
    public override string ToString() => Label;
}
