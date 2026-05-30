using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ConsoleEngine.Editor.Models;
using ConsoleEngine.Editor.ViewModels;

namespace ConsoleEngine.Editor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
    }

    // ── Group 1: Project ──────────────────────────────────────────────────

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        var results = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title         = "Open Project Folder",
            AllowMultiple = false,
        });

        if (results.Count > 0)
            _vm.LoadProject(results[0].Path.LocalPath);
    }

    private void OnNewScene(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.ProjectPath)) return;
        var entry = _vm.CreateNewScene(_vm.ProjectPath);
        SceneList.SelectedItem = entry;
        SceneList.ScrollIntoView(entry);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedFile is not SceneFileEntry entry) return;
        _vm.TrySave(entry);
        RefreshSelection();
    }

    private void OnReload(object? sender, RoutedEventArgs e) => _vm.ReloadProject();

    // ── Group 2: Playback ─────────────────────────────────────────────────

    private void OnPlay(object? sender, RoutedEventArgs e) => _vm.PlayScene();

    private void OnStop(object? sender, RoutedEventArgs e) => _vm.StopScene();

    // ── Group 3: Scene operations ─────────────────────────────────────────

    private void OnDuplicate(object? sender, RoutedEventArgs e)
    {
        var entry = _vm.DuplicateScene();
        if (entry is null) return;
        SceneList.SelectedItem = entry;
        SceneList.ScrollIntoView(entry);
    }

    private async void OnDelete(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedFile is not SceneFileEntry entry) return;

        bool confirmed = await ShowConfirmDialog(
            $"Delete \"{Path.GetFileName(entry.FilePath)}\"?",
            "This action cannot be undone.");

        if (confirmed)
            _vm.DeleteScene(entry);
    }

    // ── Sprite ────────────────────────────────────────────────────────────

    private async void OnBrowseSprite(object? sender, RoutedEventArgs e)
    {
        var results = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Select Sprite PNG",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] },
                new FilePickerFileType("All Files") { Patterns = ["*"]     },
            ],
        });

        if (results.Count > 0)
            _vm.SpritePath = results[0].Path.LocalPath;
    }

    private void OnClearSprite(object? sender, RoutedEventArgs e) => _vm.ClearSprite();

    // ── Left panel: scene selected ────────────────────────────────────────

    private void OnSceneSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SceneFileEntry entry)
            _vm.LoadScene(entry);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void RefreshSelection()
    {
        int idx = SceneList.SelectedIndex;
        SceneList.SelectedIndex = -1;
        SceneList.SelectedIndex = idx;
    }

    /// <summary>
    /// Shows a lightweight inline confirmation dialog.
    /// Returns <see langword="true"/> if the user confirmed.
    /// </summary>
    private async Task<bool> ShowConfirmDialog(string message, string detail = "")
    {
        bool result = false;

        var dialog = new Window
        {
            Title                   = "Confirm",
            Width                   = 380,
            Height                  = 140,
            WindowStartupLocation   = WindowStartupLocation.CenterOwner,
            CanResize               = false,
            ShowInTaskbar           = false,
            Background              = new SolidColorBrush(Color.Parse("#252526")),
        };

        var root = new StackPanel { Margin = new Thickness(20, 16), Spacing = 12 };

        root.Children.Add(new TextBlock
        {
            Text         = string.IsNullOrEmpty(detail) ? message : $"{message}\n{detail}",
            TextWrapping = TextWrapping.Wrap,
            FontSize     = 13,
            Foreground   = new SolidColorBrush(Colors.White),
        });

        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing             = 8,
        };

        var btnDelete = new Button { Content = "Delete", Padding = new Thickness(14, 5) };
        var btnCancel = new Button { Content = "Cancel", Padding = new Thickness(14, 5) };

        btnDelete.Click += (_, _) => { result = true;  dialog.Close(); };
        btnCancel.Click += (_, _) => { result = false; dialog.Close(); };

        btnRow.Children.Add(btnDelete);
        btnRow.Children.Add(btnCancel);
        root.Children.Add(btnRow);
        dialog.Content = root;

        await dialog.ShowDialog(this);
        return result;
    }
}
