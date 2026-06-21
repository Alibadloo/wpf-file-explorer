using System.Globalization;
using System.Windows.Data;
using FileExplorer.Models;

namespace FileExplorer.Converters;

[ValueConversion(typeof(long), typeof(string))]
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long size)
            return FileSystemItem.FormatSize(size);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
