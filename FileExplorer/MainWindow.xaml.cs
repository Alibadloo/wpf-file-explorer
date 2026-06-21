using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileExplorer.ViewModels;

namespace FileExplorer;

public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            DataContext = new MainViewModel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading: {ex.Message}\n\n{ex.StackTrace}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FileList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: not null })
            ViewModel?.OpenItemCommand.Execute(null);
    }

    private void FileList_KeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel is null) return;
        switch (e.Key)
        {
            case Key.Enter:
                ViewModel.OpenItemCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete:
                ViewModel.DeleteCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.F2:
                ViewModel.RenameCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Back:
                ViewModel.GoBackCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void PathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel is not null)
        {
            var box = (TextBox)sender;
            ViewModel.NavigateCommand.Execute(box.Text);
            FileListView.Focus();
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            ViewModel?.SearchCommand.Execute(null);
        else if (e.Key == Key.Escape)
            ViewModel?.ClearSearchCommand.Execute(null);
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeNodeViewModel node && node.IsDirectory && !string.IsNullOrEmpty(node.FullPath))
            ViewModel?.NavigateTo(node.FullPath);
    }

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is GridViewColumnHeader { Tag: string column })
            ViewModel?.SortCommand.Execute(column);
    }

    private void BreadcrumbPart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Content: string part } || ViewModel is null) return;
        var parts = ViewModel.CurrentPath.Split(Path.DirectorySeparatorChar);
        var idx = Array.IndexOf(parts, part);
        if (idx >= 0)
        {
            var path = string.Join(Path.DirectorySeparatorChar.ToString(), parts[..(idx + 1)]);
            ViewModel.NavigateCommand.Execute(path);
        }
    }

    private void NavigateHome_Click(object sender, RoutedEventArgs e)
        => ViewModel?.NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private void NavigateDesktop_Click(object sender, RoutedEventArgs e)
        => ViewModel?.NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

    private void NavigateDocuments_Click(object sender, RoutedEventArgs e)
        => ViewModel?.NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    private void NavigateDownloads_Click(object sender, RoutedEventArgs e)
        => ViewModel?.NavigateTo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
}
