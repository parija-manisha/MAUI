using System.Globalization;

namespace NotesApp.Converters;

public class BoolToStrikeThroughConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextDecorations.Strikethrough : TextDecorations.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class CheckedTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return Colors.Gray;

        return Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecordButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Stop" : "Record";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecordButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#E53935") : Color.FromArgb("#512BD4");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
