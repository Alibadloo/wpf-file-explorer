using System.IO;
using FileExplorer.Models;

namespace FileExplorer.Services;

public class FileSystemService
{
    public IEnumerable<FileSystemItem> GetItems(string path, bool showHidden = false)
    {
        DirectoryInfo dir;
        try { dir = new DirectoryInfo(path); } catch { yield break; }
        if (!dir.Exists) yield break;

        DirectoryInfo[] dirs;
        FileInfo[] files;
        try { dirs = dir.GetDirectories(); } catch { dirs = []; }
        try { files = dir.GetFiles(); } catch { files = []; }

        foreach (var subDir in dirs)
        {
            FileAttributes attrs;
            try { attrs = subDir.Attributes; } catch { continue; }
            if (!showHidden && attrs.HasFlag(FileAttributes.Hidden)) continue;
            if (attrs.HasFlag(FileAttributes.System)) continue;
            FileSystemItem item;
            try { item = FileSystemItem.FromDirectoryInfo(subDir); } catch { continue; }
            yield return item;
        }

        foreach (var file in files)
        {
            FileAttributes attrs;
            try { attrs = file.Attributes; } catch { continue; }
            if (!showHidden && attrs.HasFlag(FileAttributes.Hidden)) continue;
            FileSystemItem item;
            try { item = FileSystemItem.FromFileInfo(file); } catch { continue; }
            yield return item;
        }
    }

    public IEnumerable<DriveItem> GetDrives()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            yield return new DriveItem
            {
                Name = drive.Name,
                RootPath = drive.RootDirectory.FullName,
                Label = TryGetLabel(drive),
                DriveType = drive.DriveType.ToString(),
                TotalSize = drive.TotalSize,
                FreeSpace = drive.TotalFreeSpace,
            };
        }
    }

    public void CreateFolder(string parentPath, string folderName)
    {
        var fullPath = Path.Combine(parentPath, folderName);
        if (Directory.Exists(fullPath))
            throw new InvalidOperationException($"Folder '{folderName}' already exists.");
        Directory.CreateDirectory(fullPath);
    }

    public void CreateFile(string parentPath, string fileName)
    {
        var fullPath = Path.Combine(parentPath, fileName);
        if (File.Exists(fullPath))
            throw new InvalidOperationException($"File '{fileName}' already exists.");
        File.Create(fullPath).Dispose();
    }

    public void Delete(string path, bool isDirectory)
    {
        if (isDirectory)
            Directory.Delete(path, recursive: true);
        else
            File.Delete(path);
    }

    public void Rename(string path, string newName, bool isDirectory)
    {
        var parent = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(parent, newName);
        if (isDirectory)
            Directory.Move(path, newPath);
        else
            File.Move(path, newPath);
    }

    public void Copy(string sourcePath, string destinationDir, bool isDirectory)
    {
        var name = Path.GetFileName(sourcePath);
        var destPath = GetUniqueDestination(Path.Combine(destinationDir, name), isDirectory);

        if (isDirectory)
            CopyDirectory(sourcePath, destPath);
        else
            File.Copy(sourcePath, destPath);
    }

    public void Move(string sourcePath, string destinationDir, bool isDirectory)
    {
        var name = Path.GetFileName(sourcePath);
        var destPath = GetUniqueDestination(Path.Combine(destinationDir, name), isDirectory);

        if (isDirectory)
            Directory.Move(sourcePath, destPath);
        else
            File.Move(sourcePath, destPath);
    }

    public IEnumerable<FileSystemItem> Search(string rootPath, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        return SearchInternal(rootPath, query.ToLowerInvariant());
    }

    private static IEnumerable<FileSystemItem> SearchInternal(string rootPath, string query)
    {
        var results = new List<FileSystemItem>();
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            DirectoryInfo dir;
            try { dir = new DirectoryInfo(current); } catch { continue; }

            DirectoryInfo[] subDirs;
            FileInfo[] files;
            try { subDirs = dir.GetDirectories(); } catch { subDirs = []; }
            try { files = dir.GetFiles(); } catch { files = []; }

            foreach (var subDir in subDirs)
            {
                if (subDir.Attributes.HasFlag(FileAttributes.System)) continue;
                if (subDir.Name.ToLowerInvariant().Contains(query))
                    results.Add(FileSystemItem.FromDirectoryInfo(subDir));
                stack.Push(subDir.FullName);
            }

            foreach (var file in files)
            {
                if (file.Name.ToLowerInvariant().Contains(query))
                    results.Add(FileSystemItem.FromFileInfo(file));
            }
        }

        return results;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static string GetUniqueDestination(string path, bool isDirectory)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = isDirectory ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
        var ext = isDirectory ? "" : Path.GetExtension(path);
        int i = 2;
        string newPath;
        do { newPath = Path.Combine(dir, $"{name} ({i++}){ext}"); }
        while (File.Exists(newPath) || Directory.Exists(newPath));
        return newPath;
    }

    private static string TryGetLabel(DriveInfo drive)
    {
        try { return drive.VolumeLabel; } catch { return string.Empty; }
    }
}
