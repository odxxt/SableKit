namespace SableKit.Desktop;

internal static class ActionVisual
{
    internal static Grid Create(ActionItem item, Configuration config, bool grid, int index = -1)
    {
            var content = new Grid();

            if (item.Copied)

            {

                var copied = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                copied.Children.Add(new Icon { Kind = "check", Width = 17, Height = 17, Margin = new Thickness(0, 0, 7, 0) }); copied.Children.Add(new TextBlock { Text = "Copied", FontWeight = FontWeights.SemiBold }); content.Children.Add(copied);

            }

            else if (grid)

            {

                var column = new StackPanel(); column.Children.Add(new Icon { Kind = item.Icon, Width = 19, Height = 19, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 7) });

                column.Children.Add(new TextBlock { Text = item.Running ? "Running…" : item.Name, FontSize = 11, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });

                column.Children.Add(Muted(item.Running ? "Running…" : item.ToolName, 10)); content.Children.Add(column);

            }

            else

            {

                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); content.ColumnDefinitions.Add(new ColumnDefinition()); content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new Icon { Kind = item.Icon, Width = 18, Height = 18, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center }; content.Children.Add(icon);

                var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(text, 1);

                text.Children.Add(new TextBlock { Text = item.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });

                var subtitle = item.State is ExecutionState.Running or ExecutionState.Failed or ExecutionState.Cancelled ? item.Feedback : item.Definition.Kind == ActionKind.Password ? $"{ActionSettings.PasswordLength(config, item.Definition)} characters · copy" : item.Description;

                text.Children.Add(Muted(subtitle, 10)); content.Children.Add(text);

                if (index >= 0 && index < 9) { var hint = Muted($"Alt {index + 1}", 10); hint.VerticalAlignment = VerticalAlignment.Center; hint.Margin = new Thickness(6, 0, 0, 0); Grid.SetColumn(hint, 2); content.Children.Add(hint); }

            }


        return content;
    }
    private static TextBlock Muted(string text, double size)
    {
        var block=new TextBlock { Text=text, FontSize=size, Margin=new Thickness(0,3,0,0), TextTrimming=TextTrimming.CharacterEllipsis };
        block.SetResourceReference(TextBlock.ForegroundProperty,"Muted"); return block;
    }
}
