using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace SpotWhy.Converters;

public static class HighlightBehavior
{
    public static readonly DependencyProperty EnableHighlightProperty =
        DependencyProperty.RegisterAttached("EnableHighlight", typeof(bool), typeof(HighlightBehavior),
            new PropertyMetadata(false, OnEnableHighlightChanged));

    public static readonly DependencyProperty HighlightQueryProperty =
        DependencyProperty.RegisterAttached("HighlightQuery", typeof(string), typeof(HighlightBehavior),
            new PropertyMetadata("", OnHighlightQueryChanged));

    public static void SetEnableHighlight(TextBlock element, bool value) =>
        element.SetValue(EnableHighlightProperty, value);

    public static bool GetEnableHighlight(TextBlock element) =>
        (bool)element.GetValue(EnableHighlightProperty);

    public static void SetHighlightQuery(TextBlock element, string value) =>
        element.SetValue(HighlightQueryProperty, value);

    public static string GetHighlightQuery(TextBlock element) =>
        (string)element.GetValue(HighlightQueryProperty);

    private static void OnEnableHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock && e.NewValue is bool enabled && enabled)
            UpdateHighlight(textBlock, GetHighlightQuery(textBlock));
    }

    private static void OnHighlightQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock && GetEnableHighlight(textBlock))
            UpdateHighlight(textBlock, e.NewValue as string);
    }

    private static void UpdateHighlight(TextBlock textBlock, string? query)
    {
        var text = textBlock.Text;
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(text))
            return;

        var lowerText = text.ToLowerInvariant();
        var lowerQuery = query!.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(lowerQuery))
            return;

        var startIndex = lowerText.IndexOf(lowerQuery, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
            return;

        textBlock.Inlines.Clear();

        if (startIndex > 0)
            textBlock.Inlines.Add(new Run(text.Substring(0, startIndex)));

        var highlightedRun = new Run(text.Substring(startIndex, lowerQuery.Length))
        {
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(78, 160, 110))
        };
        textBlock.Inlines.Add(highlightedRun);

        if (startIndex + lowerQuery.Length < text.Length)
            textBlock.Inlines.Add(new Run(text.Substring(startIndex + lowerQuery.Length)));
    }
}
