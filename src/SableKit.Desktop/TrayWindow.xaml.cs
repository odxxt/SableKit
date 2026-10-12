using System.ComponentModel;

using System.Windows.Automation;



namespace SableKit.Desktop;



public partial class TrayWindow : Window

{

    private readonly AppState state;

    private bool refreshing;

    private NetworkTarget? suggestionTarget;

    public bool Pinned { get; private set; }

    public bool VerificationMode { get; set; }

    public TrayWindow(AppState state)

    {

        InitializeComponent(); this.state = state;

        SourceInitialized += (_, _) => NativeWindow.Apply(this, true);

        Deactivated += (_, _) => { if (!Pinned && !ProfilePicker.IsDropDownOpen && !VerificationMode) Hide(); };

        Closing += (_, e) => { if (!App.Exiting) { e.Cancel = true; Hide(); } };

        PreviewKeyDown += KeyDownPanel;

        state.Changed += Refresh; state.ResultChanged += UpdateResult; state.HideRequested += Hide;

        Refresh();

    }

    public void Open(bool position = true)

    {

        ThemeManager.Apply(state.Config.Theme);

        if (position)

        {

            var area = SystemParameters.WorkArea;

            Left = area.Right - Width - 14; Top = area.Bottom - Height - 14;

        }

        Refresh(); InspectClipboard(); Show(); Activate();

        NativeWindow.Apply(this, true);

        if (SearchBox.Visibility == Visibility.Visible) SearchBox.Focus(); else SearchButton.Focus();

    }

    private void InspectClipboard()

    {

        suggestionTarget = null; Suggestion.Visibility = Visibility.Collapsed;

        try

        {

            if (NetworkTarget.TryParse(AppState.ReadClipboard(), out var target, out _) && (System.Net.IPAddress.TryParse(target!.Host, out _) || target.Host.Contains('.')))

            {

                suggestionTarget = target; SuggestionTarget.Text = target.Display;

                SuggestionTitle.Text = target.Port.HasValue ? "Test connection" : "Ping address";

                AutomationProperties.SetName(Suggestion, $"{SuggestionTitle.Text} {target.Display}"); Suggestion.Visibility = Visibility.Visible;

            }

        }

        catch (System.Runtime.InteropServices.COMException) { state.SetResult("Clipboard is busy", "Try opening the panel again.", true); }

    }

    public void Refresh()

    {

        refreshing = true; ProfilePicker.ItemsSource = null; ProfilePicker.ItemsSource = state.Config.Profiles; ProfilePicker.SelectedItem = state.Config.ActiveProfile; refreshing = false;

        RenderActions();

    }

    private void RenderActions()

    {

        var focusedId = (Keyboard.FocusedElement as Button)?.Tag is ActionItem focused ? focused.Definition.Id : null;
        foreach (var item in state.Items) item.PropertyChanged -= ItemChanged;

        var profile = state.Config.ActiveProfile;

        var searching = !string.IsNullOrWhiteSpace(SearchBox.Text);

        var items = searching ? state.Items.Where(i => ActionSettings.Matches(i.Definition, SearchBox.Text)).ToList() : profile.ActionIds.Select(id => state.Items.First(i => i.Definition.Id == id)).ToList();

        ActionsHeading.Text = searching ? $"Actions · {items.Count}" : "Pinned actions";

        var grid = profile.GridView && !searching;

        var factory = new FrameworkElementFactory(grid ? typeof(System.Windows.Controls.Primitives.UniformGrid) : typeof(StackPanel));

        if (grid) factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, 2);

        ActionsList.ItemsPanel = new ItemsPanelTemplate(factory);

        ActionsList.Items.Clear();

        for (var index = 0; index < items.Count; index++)

        {

            var item = items[index]; item.PropertyChanged += ItemChanged;

            var button = new Button { Height = grid ? 72 : 46, Margin = grid ? new Thickness(2) : new Thickness(0, 1, 0, 1), Padding = new Thickness(9, 6, 9, 6), HorizontalContentAlignment = HorizontalAlignment.Stretch, Tag = item, IsEnabled = item.Definition.Enabled, ToolTip = item.Definition.Enabled ? item.Name + "\n" + item.Description : "Disabled. Enable this action in the main app." };

            AutomationProperties.SetName(button, item.Name);

            if (grid) button.SetResourceReference(BackgroundProperty, "Inset");

            var content = new Grid();

            if (item.Copied)

            {

                var copied = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                copied.Children.Add(new Icon { Kind = "check", Width = 17, Height = 17, Margin = new Thickness(0, 0, 7, 0) }); copied.Children.Add(new TextBlock { Text = "Copied", FontWeight = FontWeights.SemiBold }); content.Children.Add(copied);

            }

            else if (grid)

            {

                var column = new StackPanel(); column.Children.Add(new Icon { Kind = item.Icon, Width = 19, Height = 19, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 7) });

                column.Children.Add(new TextBlock { Text = item.Running ? "Running…" : item.Name, FontSize = 11, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxHeight = 29 });

                column.Children.Add(Muted(item.Running ? "Running…" : item.ToolName, 10)); content.Children.Add(column);

            }

            else

            {

                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); content.ColumnDefinitions.Add(new ColumnDefinition()); content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new Icon { Kind = item.Icon, Width = 18, Height = 18, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center }; content.Children.Add(icon);

                var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(text, 1);

                text.Children.Add(new TextBlock { Text = item.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });

                var subtitle = item.State is ExecutionState.Running or ExecutionState.Failed or ExecutionState.Cancelled ? item.Feedback : item.Definition.Kind == ActionKind.Password ? $"{ActionSettings.PasswordLength(state.Config, item.Definition)} characters · copy" : item.Description;

                text.Children.Add(Muted(subtitle, 10)); content.Children.Add(text);

                if (index < 9) { var hint = Muted($"Alt {index + 1}", 10); hint.VerticalAlignment = VerticalAlignment.Center; hint.Margin = new Thickness(6, 0, 0, 0); Grid.SetColumn(hint, 2); content.Children.Add(hint); }

            }

            if (item.Copied && IsVisible && !state.Config.ReducedMotion && SystemParameters.ClientAreaAnimation) content.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0.65, 1, TimeSpan.FromMilliseconds(140)) { FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop });

            button.Content = content; button.Click += async (_, _) => await state.ExecuteAsync(item); ActionsList.Items.Add(button);

        }

        if (focusedId is not null && IsActive)
        {
            var focusedButton = ActionsList.Items.Cast<Button>().FirstOrDefault(b => b.Tag is ActionItem i && i.Definition.Id == focusedId);
            if (focusedButton is not null) focusedButton.Focus(); else SearchButton.Focus();
        }
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        EmptyText.Text = searching ? "No actions match. Try another name or alias." : "Pin actions from the main app to make them available here.";

        ViewIcon.Kind = profile.GridView ? "list" : "grid"; ViewButton.ToolTip = profile.GridView ? "Switch pinned actions to list" : "Switch pinned actions to grid";

        AutomationProperties.SetName(ViewButton, ViewButton.ToolTip.ToString());

    }

    private TextBlock Muted(string text, double size) { var block = new TextBlock { Text = text, FontSize = size, Margin = new Thickness(0, 3, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis }; block.SetResourceReference(TextBlock.ForegroundProperty, "Muted"); return block; }

    private void ItemChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(ActionItem.State)) RenderActions(); }

    private void UpdateResult() { ActionsHeading.Visibility = Visibility.Hidden; ResultText.Text = state.Status; ResultText.SetResourceReference(TextBlock.ForegroundProperty, state.LastFailed ? "Bad" : "Muted"); ResultButton.ToolTip = state.Result; ResultButton.Visibility = Visibility.Visible; }

    public void TogglePin(object? sender, RoutedEventArgs? e) { Pinned = !Pinned; Topmost = Pinned; PinButton.SetResourceReference(BackgroundProperty, Pinned ? "Hover" : "Surface"); PinButton.ToolTip = Pinned ? "Unpin · hide when focus moves away" : "Pin · keep panel open"; AutomationProperties.SetName(PinButton, Pinned ? "Unpin panel" : "Pin panel"); }

    private void ToggleSearch(object sender, RoutedEventArgs e) { SearchBox.Visibility = SearchBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; SearchBox.Text = ""; if (SearchBox.IsVisible) SearchBox.Focus(); }

    private void SearchChanged(object sender, TextChangedEventArgs e) { if (state is not null) RenderActions(); }

    public void ToggleView(object? sender, RoutedEventArgs? e) { state.Config.ActiveProfile.GridView = !state.Config.ActiveProfile.GridView; Save(); }

    private void ProfileChanged(object sender, SelectionChangedEventArgs e) { if (!refreshing && ProfilePicker.SelectedItem is Profile p) { state.Config.ActiveProfileId = p.Id; Save(); } }

    private void Save() { try { state.Save(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { state.SetResult("Settings could not be saved", ex.Message, true); } }

    private async void RunSuggestion(object sender, RoutedEventArgs e) { if (suggestionTarget is not null) await state.ExecuteAsync(new ActionItem(new ActionDefinition { Name = SuggestionTitle.Text, Kind = ActionKind.Network }), suggestionTarget.Display); }

    private void DragHeader(object sender, MouseButtonEventArgs e) { if (e.OriginalSource is not Button && e.LeftButton == MouseButtonState.Pressed) try { DragMove(); } catch (InvalidOperationException) { } }

    private void HideClick(object sender, RoutedEventArgs e) => Hide();

    private void OpenMain(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).OpenMain(); }

    private void OpenPasswords(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).OpenMain("Preferences"); }

    private void OpenTools(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).OpenMain("Tools"); }

    private void OpenResult(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).OpenMain("Activity"); }

    private void OpenNetwork(object sender, RoutedEventArgs e) { Hide(); ((App)Application.Current).OpenNetwork(); }

    private async void KeyDownPanel(object sender, KeyEventArgs e)

    {

        if (e.Key == Key.Escape) { if (state.IsRunning) state.CancelAll(); else if (SearchBox.IsVisible) { SearchBox.Text = ""; SearchBox.Visibility = Visibility.Collapsed; SearchButton.Focus(); } else Hide(); e.Handled = true; }

        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { ToggleSearch(this, e); e.Handled = true; }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (Keyboard.Modifiers == ModifierKeys.Alt && key >= Key.D1 && key <= Key.D9)

        {

            var index = key - Key.D1; if (index < ActionsList.Items.Count && ActionsList.Items[index] is Button b && b.Tag is ActionItem item) await state.ExecuteAsync(item); e.Handled = true;

        }

    }

}

