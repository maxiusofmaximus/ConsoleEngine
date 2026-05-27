using Avalonia.Controls;
using Avalonia.Interactivity;
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

    // ── Toolbar: Open Project ─────────────────────────────────────────────
    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        var results = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title            = "Open Project Folder",
            AllowMultiple    = false,
        });

        if (results.Count > 0)
        {
            string path = results[0].Path.LocalPath;
            _vm.LoadProject(path);
        }
    }

    // ── Toolbar: New Scene ────────────────────────────────────────────────
    private void OnNewScene(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.ProjectPath)) return;

        var entry = _vm.CreateNewScene(_vm.ProjectPath);

        // Select the new entry in the list
        SceneList.SelectedItem = entry;
        // Scroll to show it
        SceneList.ScrollIntoView(entry);
    }

    // ── Toolbar: Save ─────────────────────────────────────────────────────
    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_vm.SelectedFile is not SceneFileEntry entry) return;
        _vm.TrySave(entry);

        // Refresh the selected item binding after DisplayName update
        int idx = SceneList.SelectedIndex;
        SceneList.SelectedIndex = -1;
        SceneList.SelectedIndex = idx;
    }

    // ── Left panel: scene selected ────────────────────────────────────────
    private void OnSceneSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is SceneFileEntry entry)
        {
            _vm.LoadScene(entry);
        }
    }
}
