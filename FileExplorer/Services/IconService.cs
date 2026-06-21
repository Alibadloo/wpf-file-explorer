using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileExplorer.Services;

public static class IconService
{
    private static readonly Dictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource GetIcon(string path, bool isDirectory)
    {
        var key = isDirectory ? "__folder__" : Path.GetExtension(path).ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var icon = ExtractShellIcon(path);
        var result = icon ?? MakeGeometryIcon(isDirectory);
        _cache[key] = result;
        return result;
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x010;

    private static ImageSource? ExtractShellIcon(string path)
    {
        try
        {
            var info = new SHFILEINFO();
            var result = SHGetFileInfo(path, 0, ref info, (uint)System.Runtime.InteropServices.Marshal.SizeOf(info),
                SHGFI_ICON | SHGFI_SMALLICON);
            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            DestroyIcon(info.hIcon);
            return source;
        }
        catch { return null; }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static ImageSource MakeGeometryIcon(bool isDirectory)
    {
        var brush = isDirectory ? new SolidColorBrush(Color.FromRgb(255, 214, 0))
                                : new SolidColorBrush(Color.FromRgb(100, 149, 237));
        var geometry = isDirectory
            ? Geometry.Parse("M0,3 L5,3 L7,1 L14,1 L14,11 L0,11 Z")
            : Geometry.Parse("M1,0 L9,0 L13,4 L13,15 L1,15 Z M9,0 L9,4 L13,4");

        var drawing = new GeometryDrawing(brush, null, geometry);
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
