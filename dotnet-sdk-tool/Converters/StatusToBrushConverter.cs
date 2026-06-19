using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace dotnet_sdk_tool_template.Converters;

/// <summary>
/// Maps a channel <c>StatusKey</c> ("active", "maintenance", "preview", "eol") to a badge brush.
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public static readonly StatusToBrushConverter Instance = new();

    private static readonly SolidColorBrush Active = new(Color.Parse("#9ECE6A"));       // green
    private static readonly SolidColorBrush Maintenance = new(Color.Parse("#E0AF68"));  // amber
    private static readonly SolidColorBrush Preview = new(Color.Parse("#7AA2F7"));      // blue
    private static readonly SolidColorBrush Eol = new(Color.Parse("#565F89"));          // comment
    private static readonly SolidColorBrush Unknown = new(Color.Parse("#565F89"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            "active" => Active,
            "maintenance" => Maintenance,
            "preview" => Preview,
            "eol" => Eol,
            _ => Unknown,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}