using System.Windows.Automation;

namespace SableKit.Desktop;

internal static class Ui
{
    public static TextBlock Text(string text, double size = 13, bool muted = false)
    { var block = new TextBlock { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap }; if (muted) block.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); return block; }
    public static Button Button(string text, Action run, string style = "FilledButton")
    { var b = new Button { Content = text, Style = (Style)Application.Current.FindResource(string.IsNullOrEmpty(style) ? typeof(Button) : style), Margin = new Thickness(0, 0, 8, 0) }; b.Click += (_, _) => run(); return b; }
    public static TextBox Field(Panel parent, string label, string value = "", string? help = null)
    {
        parent.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 16, 0, 7), FontWeight = FontWeights.SemiBold, FontSize = 12 });
        var field = new TextBox { Text = value }; AutomationProperties.SetName(field, label); parent.Children.Add(field);
        if (help is not null) { var block = Text(help, 11, true); block.Margin = new Thickness(0, 6, 0, 0); parent.Children.Add(block); } return field;
    }
    public static ComboBox Choice(Panel parent, string label, IEnumerable<object> values, object? selected)
    {
        parent.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 16, 0, 7), FontWeight = FontWeights.SemiBold, FontSize = 12 });
        var combo = new ComboBox { ItemsSource = values, SelectedItem = selected }; AutomationProperties.SetName(combo, label); parent.Children.Add(combo); return combo;
    }
    public static Border Divider() { var b = new Border { Height = 1, Margin = new Thickness(0, 20, 0, 8) }; b.SetResourceReference(Border.BackgroundProperty, "Border"); return b; }
    public static ScrollViewer Scroll(UIElement content) => new() { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 10, 0) };
}
