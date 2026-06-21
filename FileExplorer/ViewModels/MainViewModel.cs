using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using FileExplorer.Commands;
using FileExplorer.Models;
using FileExplorer.Services;

namespace FileExplorer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly FileSystemService _fsService = new();
    private readonly ClipboardService _clipboard = new();
    private readonly NavigationHistory _history = new();

    private string _currentPath = string.Empty;
    private string _searchQuery = string.Empty;
    private string _statusText = "Ready";
    private FileSystemItem? _selectedItem;
    private bool _isDarkMode = true;
    private bool _showHiddenFiles;
    private bool _isSearching;
    private string _sortColumn = "Name";
    private bool _sortAscending = true;

    public ObservableCollection<TreeNodeViewModel> TreeNodes { get; } = new();
    public ObservableCollection<FileSystemItem> FileItems { get; } = new();
    public ObservableCollection<DriveItem> Drives { get; } = new();

    public string CurrentPath
    {
        get => _currentPath;
        set { _currentPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathParts)); }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set { _searchQuery = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public FileSystemItem? SelectedItem
    {
        get => _selectedItem;
        set { _selectedItem = value; OnPropertyChanged(); }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set { _isDarkMode = value; OnPropertyChanged(); ApplyTheme(); }
    }

    public bool ShowHiddenFiles
    {
        get => _showHiddenFiles;
        set { _showHiddenFiles = value; OnPropertyChanged(); RefreshCurrentDirectory(); }
    }

    public bool IsSearching
    {
        get => _isSearching;
        set { _isSearching = value; OnPropertyChanged(); }
    }

    public bool CanGoBack => _history.CanGoBack;
    public bool CanGoForward => _history.CanGoForward;

    public IEnumerable<string> PathParts => CurrentPath
        .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

    // Commands
    public RelayCommand NavigateCommand { get; }
    public RelayCommand GoBackCommand { get; }
    public RelayCommand GoForwardCommand { get; }
    public RelayCommand GoUpCommand { get; }
    public RelayCommand OpenItemCommand { get; }
    public RelayCommand CreateFolderCommand { get; }
    public RelayCommand CreateFileCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand CutCommand { get; }
    public RelayCommand PasteCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand ClearSearchCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand ToggleHiddenCommand { get; }
    public RelayCommand OpenPropertiesCommand { get; }
    public RelayCommand SortCommand { get; }

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand(p => NavigateTo(p as string ?? string.Empty));
        GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack);
        GoForwardCommand = new RelayCommand(_ => GoForward(), _ => CanGoForward);
        GoUpCommand = new RelayCommand(_ => GoUp(), _ => !string.IsNullOrEmpty(Path.GetDirectoryName(CurrentPath)));
        OpenItemCommand = new RelayCommand(_ => OpenSelected());
        CreateFolderCommand = new RelayCommand(_ => CreateFolder());
        CreateFileCommand = new RelayCommand(_ => CreateFile());
        DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedItem is not null);
        RenameCommand = new RelayCommand(_ => RenameSelected(), _ => SelectedItem is not null);
        CopyCommand = new RelayCommand(_ => CopySelected(), _ => SelectedItem is not null);
        CutCommand = new RelayCommand(_ => CutSelected(), _ => SelectedItem is not null);
        PasteCommand = new RelayCommand(_ => Paste(), _ => _clipboard.HasItem);
        SearchCommand = new RelayCommand(_ => ExecuteSearch());
        ClearSearchCommand = new RelayCommand(_ => ClearSearch());
        RefreshCommand = new RelayCommand(_ => RefreshCurrentDirectory());
        ToggleThemeCommand = new RelayCommand(_ => IsDarkMode = !IsDarkMode);
        ToggleHiddenCommand = new RelayCommand(_ => ShowHiddenFiles = !ShowHiddenFiles);
        OpenPropertiesCommand = new RelayCommand(_ => ShowProperties(), _ => SelectedItem is not null);
        SortCommand = new RelayCommand(p => SetSort(p as string ?? "Name"));

        LoadDrives();
        LoadTreeRoots();
        NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private void LoadDrives()
    {
        Drives.Clear();
        foreach (var drive in _fsService.GetDrives())
            Drives.Add(drive);
    }

    private void LoadTreeRoots()
    {
        TreeNodes.Clear();
        foreach (var drive in Drives)
            TreeNodes.Add(new TreeNodeViewModel(drive.DisplayName, drive.RootPath, true));
    }

    public void NavigateTo(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
        _history.Navigate(path);
        CurrentPath = path;
        LoadDirectory(path);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private void LoadDirectory(string path)
    {
        FileItems.Clear();
        try
        {
            var items = _fsService.GetItems(path, ShowHiddenFiles).ToList();
            ApplySort(items);
            foreach (var item in items)
                FileItems.Add(item);

            var dirCount = items.Count(i => i.IsDirectory);
            var fileCount = items.Count(i => !i.IsDirectory);
            StatusText = $"{dirCount} folder(s), {fileCount} file(s)";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Access denied.";
            MessageBox.Show("You don't have permission to access this folder.", "Access Denied",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void GoBack()
    {
        var path = _history.GoBack();
        if (path is null) return;
        CurrentPath = path;
        LoadDirectory(path);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private void GoForward()
    {
        var path = _history.GoForward();
        if (path is null) return;
        CurrentPath = path;
        LoadDirectory(path);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private void GoUp()
    {
        var parent = Path.GetDirectoryName(CurrentPath);
        if (parent is not null) NavigateTo(parent);
    }

    private void OpenSelected()
    {
        if (SelectedItem is null) return;
        if (SelectedItem.IsDirectory)
            NavigateTo(SelectedItem.FullPath);
        else
            Process.Start(new ProcessStartInfo(SelectedItem.FullPath) { UseShellExecute = true });
    }

    private void CreateFolder()
    {
        var name = PromptInput("New Folder", "Enter folder name:", "New Folder");
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            _fsService.CreateFolder(CurrentPath, name);
            RefreshCurrentDirectory();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CreateFile()
    {
        var name = PromptInput("New File", "Enter file name:", "New File.txt");
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            _fsService.CreateFile(CurrentPath, name);
            RefreshCurrentDirectory();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void DeleteSelected()
    {
        if (SelectedItem is null) return;
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedItem.Name}'?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        try
        {
            _fsService.Delete(SelectedItem.FullPath, SelectedItem.IsDirectory);
            RefreshCurrentDirectory();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void RenameSelected()
    {
        if (SelectedItem is null) return;
        var name = PromptInput("Rename", "Enter new name:", SelectedItem.Name);
        if (string.IsNullOrWhiteSpace(name) || name == SelectedItem.Name) return;
        try
        {
            _fsService.Rename(SelectedItem.FullPath, name, SelectedItem.IsDirectory);
            RefreshCurrentDirectory();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void CopySelected()
    {
        if (SelectedItem is null) return;
        _clipboard.Copy(SelectedItem.FullPath, SelectedItem.IsDirectory);
        StatusText = $"Copied: {SelectedItem.Name}";
    }

    private void CutSelected()
    {
        if (SelectedItem is null) return;
        _clipboard.Cut(SelectedItem.FullPath, SelectedItem.IsDirectory);
        StatusText = $"Cut: {SelectedItem.Name}";
    }

    private void Paste()
    {
        if (!_clipboard.HasItem || _clipboard.SourcePath is null) return;
        try
        {
            if (_clipboard.Operation == ClipboardOperation.Copy)
                _fsService.Copy(_clipboard.SourcePath, CurrentPath, _clipboard.IsDirectory);
            else
            {
                _fsService.Move(_clipboard.SourcePath, CurrentPath, _clipboard.IsDirectory);
                _clipboard.Clear();
            }
            RefreshCurrentDirectory();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ExecuteSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        IsSearching = true;
        FileItems.Clear();
        StatusText = "Searching...";

        Task.Run(() =>
        {
            var results = _fsService.Search(CurrentPath, SearchQuery).ToList();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in results) FileItems.Add(item);
                StatusText = $"Found {results.Count} result(s) for '{SearchQuery}'";
                IsSearching = false;
            });
        });
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        IsSearching = false;
        RefreshCurrentDirectory();
    }

    public void RefreshCurrentDirectory()
    {
        if (!string.IsNullOrEmpty(CurrentPath))
            LoadDirectory(CurrentPath);
    }

    private void SetSort(string column)
    {
        if (_sortColumn == column)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = column;
            _sortAscending = true;
        }
        RefreshCurrentDirectory();
    }

    private void ApplySort(List<FileSystemItem> items)
    {
        IEnumerable<FileSystemItem> sorted = _sortColumn switch
        {
            "Name" => _sortAscending
                ? items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Name)
                : items.OrderBy(i => !i.IsDirectory).ThenByDescending(i => i.Name),
            "Size" => _sortAscending
                ? items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Size)
                : items.OrderBy(i => !i.IsDirectory).ThenByDescending(i => i.Size),
            "DateModified" => _sortAscending
                ? items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.DateModified)
                : items.OrderBy(i => !i.IsDirectory).ThenByDescending(i => i.DateModified),
            "Type" => _sortAscending
                ? items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Type)
                : items.OrderBy(i => !i.IsDirectory).ThenByDescending(i => i.Type),
            _ => items
        };

        var list = sorted.ToList();
        items.Clear();
        items.AddRange(list);
    }

    private void ShowProperties()
    {
        if (SelectedItem is null) return;
        var info = SelectedItem.IsDirectory
            ? $"Name: {SelectedItem.Name}\nPath: {SelectedItem.FullPath}\nCreated: {SelectedItem.DateCreated}\nModified: {SelectedItem.DateModified}"
            : $"Name: {SelectedItem.Name}\nPath: {SelectedItem.FullPath}\nSize: {SelectedItem.SizeFormatted}\nType: {SelectedItem.Type}\nCreated: {SelectedItem.DateCreated}\nModified: {SelectedItem.DateModified}";
        MessageBox.Show(info, "Properties", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyTheme()
    {
        var file = IsDarkMode ? "DarkTheme.xaml" : "LightTheme.xaml";
        var uri = new Uri($"/FileExplorer;component/Themes/{file}", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
    }

    private static string? PromptInput(string title, string prompt, string defaultValue)
    {
        var dialog = new Views.InputDialog(title, prompt, defaultValue);
        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }

    private static void ShowError(string message)
        => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
