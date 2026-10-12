namespace SableKit.Desktop;

public partial class MainWindow : Window
{
    private readonly AppState state;
    private string page = "Actions";
    private string? selectedId;
    private bool dirty;
    internal Func<bool>? DiscardDecision { get; set; }
    internal bool HasDraft => dirty;
    internal string? EditingActionId => selectedId;
    public MainWindow(AppState state)
    {
        InitializeComponent(); this.state = state;
        SourceInitialized += (_, _) => NativeWindow.Apply(this, false);
        Closing += (_, e) => { if (dirty && !ConfirmDiscard()) e.Cancel = true; };
        state.ResultChanged += StateResultChanged;
        Closed += (_, _) => state.ResultChanged -= StateResultChanged;
        foreach (var (name, icon) in new[] { ("Actions", "spark"), ("Profiles", "profiles"), ("Tools", "grid"), ("Activity", "list"), ("Preferences", "settings") })
        {
            var b = Ui.Button(name, () => Navigate(name), "");
            b.Style = (Style)FindResource(typeof(Button)); b.HorizontalContentAlignment = HorizontalAlignment.Left; b.Padding = new Thickness(10, 11, 10, 11); b.Margin = new Thickness(0, 2, 0, 2); b.Tag = name;
            var row = new StackPanel { Orientation = Orientation.Horizontal }; row.Children.Add(new Icon { Kind = icon, Width = 17, Height = 17, Margin = new Thickness(0, 0, 12, 0) }); row.Children.Add(Ui.Text(name)); b.Content = row; Navigation.Children.Add(b);
        }
        Navigate("Actions");
    }
    private void StateResultChanged() { StatusText.Text = state.Status; if (page == "Activity") ShowActivity(); }
    private bool ConfirmDiscard() => !dirty || (DiscardDecision?.Invoke() ?? (MessageBox.Show(this, "Discard the changes in this editor?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes));
    public void Navigate(string destination)
    {
        if (!ConfirmDiscard()) return;
        dirty = false; page = destination; PageTitle.Text = page;
        AddButton.Visibility = page == "Actions" ? Visibility.Visible : Visibility.Collapsed;
        foreach (Button b in Navigation.Children) b.SetResourceReference(BackgroundProperty, b.Tag?.ToString() == page ? "Hover" : "Inset");
        PageSubtitle.Text = page switch { "Actions" => "Your capabilities, configured for the way you work.", "Profiles" => "One action library. A different focus for every workspace.", "Tools" => "Small tools, brought together.", "Activity" => "The latest result. Kept only for this session.", _ => "Make SableKit feel at home." };
        StatusText.Text = "";
        switch (page) { case "Actions": ShowActions(); break; case "Profiles": ShowProfiles(); break; case "Tools": ShowTools(); break; case "Activity": ShowActivity(); break; default: ShowPreferences(); break; }
    }
    private void ShowActions()
    {
        var layout = new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) }); layout.ColumnDefinitions.Add(new ColumnDefinition());
        var left = new DockPanel { Margin = new Thickness(0, 0, 22, 0) };
        var search = new TextBox { Tag = "Search actions", ToolTip = "Search action names and aliases", Margin = new Thickness(0, 0, 0, 12) }; System.Windows.Automation.AutomationProperties.SetName(search, "Search action library"); DockPanel.SetDock(search, Dock.Top); left.Children.Add(search);
        var synchronizingSelection = false;
        var list = new ListBox { Name = "ActionLibrary" }; left.Children.Add(list); layout.Children.Add(left);
        var editorHost = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch }; Grid.SetColumn(editorHost, 1); layout.Children.Add(editorHost);
        void Populate()
        {
            synchronizingSelection = true;
            list.Items.Clear();
            foreach (var item in state.Items.Where(i => ActionSettings.Matches(i.Definition, search.Text)))
            {
                var body = new StackPanel { Margin = new Thickness(12) };
                body.Children.Add(new TextBlock { Text = item.Name, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
                var sub = Ui.Text(item.ToolName + (item.Definition.Enabled ? "" : " · Disabled"), 11, true); sub.Margin = new Thickness(0, 5, 0, 0); body.Children.Add(sub);
                list.Items.Add(new ListBoxItem { Content = body, Tag = item.Definition });
            }
            list.SelectedItem = list.Items.Cast<ListBoxItem>().FirstOrDefault(i => ((ActionDefinition)i.Tag).Id == selectedId);
            synchronizingSelection = false;
            StatusText.Text = list.SelectedItem is null && selectedId is not null && !string.IsNullOrWhiteSpace(search.Text) ? "The current editor is outside these search results. Your draft is retained." : "";
        }
        Populate();
        list.SelectionChanged += (_, _) =>
        {
            if (synchronizingSelection || list.SelectedItem is not ListBoxItem { Tag: ActionDefinition definition }) return;
            if (!ConfirmDiscard())
            {
                synchronizingSelection = true;
                list.SelectedItem = list.Items.Cast<ListBoxItem>().FirstOrDefault(i => ((ActionDefinition)i.Tag).Id == selectedId);
                synchronizingSelection = false; return;
            }
            dirty = false; selectedId = definition.Id; editorHost.Content = BuildEditor(definition);
        };
        search.TextChanged += (_, _) => Populate();
        PageContent.Content = layout;
        var selected = list.Items.Cast<ListBoxItem>().FirstOrDefault(i => ((ActionDefinition)i.Tag).Id == selectedId) ?? list.Items.Cast<ListBoxItem>().FirstOrDefault();
        if (selected is not null)
        {
            synchronizingSelection = true; list.SelectedItem = selected; synchronizingSelection = false;
            editorHost.Content = BuildEditor((ActionDefinition)selected.Tag);
        }
        else editorHost.Content = Ui.Text("Create an action to start building your toolkit.", 15, true);
    }
    private UIElement BuildEditor(ActionDefinition source)
    {
        selectedId = source.Id;
        var outer = new DockPanel();
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) }; DockPanel.SetDock(bottom, Dock.Bottom); outer.Children.Add(bottom);
        var form = new StackPanel(); outer.Children.Add(Ui.Scroll(form));
        form.Children.Add(new TextBlock { Text = "Configure action", FontSize = 18, FontWeight = FontWeights.SemiBold });
        var name = Ui.Field(form, "Name", source.Name);
        var description = Ui.Field(form, "Description", source.Description);
        var aliases = Ui.Field(form, "Search aliases", source.Aliases, "Space-separated words that help you find this action.");
        var kind = Ui.Choice(form, "Tool operation", Enum.GetValues<ActionKind>().Cast<object>(), source.Kind);
        var options = new StackPanel(); form.Children.Add(options);
        TextBox? length = null, executable = null, arguments = null, target = null; CheckBox? inherit = null;
        void BuildOptions()
        {
            options.Children.Clear(); length = executable = arguments = target = null; inherit = null;
            switch ((ActionKind)kind.SelectedItem)
            {
                case ActionKind.Password:
                    options.Children.Add(Ui.Divider());
                    inherit = new CheckBox { Content = $"Use tool default ({state.Config.PasswordLength} characters)", IsChecked = source.PasswordLengthOverride is null };
                    options.Children.Add(inherit); length = Ui.Field(options, "Override for this action", (source.PasswordLengthOverride ?? state.Config.PasswordLength).ToString(), "8–128 random characters. Reset by choosing Use tool default.");
                    length.IsEnabled = inherit.IsChecked != true;
                    inherit.Checked += (_, _) => { length.IsEnabled = false; dirty = true; }; inherit.Unchecked += (_, _) => { length.IsEnabled = true; dirty = true; }; break;
                case ActionKind.Application:
                    executable = Ui.Field(options, "Application executable", source.Executable);
                    var browse = Ui.Button("Browse…", () => { var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Applications (*.exe)|*.exe" }; if (dialog.ShowDialog(this) == true) executable.Text = dialog.FileName; }); browse.HorizontalAlignment = HorizontalAlignment.Left; browse.Margin = new Thickness(0, 7, 0, 0); options.Children.Add(browse);
                    arguments = Ui.Field(options, "Arguments · one per line", string.Join(Environment.NewLine, source.Arguments), "Each line is passed as one literal argument. Clipboard input is never inserted into a command."); arguments.AcceptsReturn = true; arguments.Height = 78; arguments.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; break;
                case ActionKind.Network:
                    target = Ui.Field(options, "Target", source.Target, "Leave empty to read the clipboard when invoked. An explicit port selects a TCP check; otherwise, ping."); break;
            }
        }
        BuildOptions(); kind.SelectionChanged += (_, _) => { BuildOptions(); dirty = true; };
        var panel = Ui.Choice(form, "After invoking", Enum.GetValues<PanelBehavior>().Cast<object>(), source.PanelBehavior);
        var enabled = new CheckBox { Content = "Action enabled", IsChecked = source.Enabled }; form.Children.Add(enabled);
        var pinned = new CheckBox { Content = $"Pin in {state.Config.ActiveProfile.Name}", IsChecked = state.Config.ActiveProfile.ActionIds.Contains(source.Id) }; form.Children.Add(pinned);
        form.Children.Add(Ui.Divider());
        form.Children.Add(new TextBlock { Text = "Configuration preview", FontWeight = FontWeights.SemiBold, FontSize = 12 });
        var preview = Ui.Text("", 12, true); preview.Margin = new Thickness(0, 8, 0, 0); form.Children.Add(preview);
        var buttonPreview = new Border { Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(14, 12, 14, 12), CornerRadius = new CornerRadius(12) }; buttonPreview.SetResourceReference(Border.BackgroundProperty, "Inset");
        var previewName = new TextBlock { FontWeight = FontWeights.SemiBold }; buttonPreview.Child = previewName; form.Children.Add(buttonPreview);
        ActionDefinition Draft()
        {
            if (string.IsNullOrWhiteSpace(name.Text)) throw new InvalidOperationException("Give this action a name.");
            int? overrideLength = null;
            if ((ActionKind)kind.SelectedItem == ActionKind.Password && inherit?.IsChecked != true)
            { if (!int.TryParse(length?.Text, out var parsed) || parsed is < 8 or > 128) throw new InvalidOperationException("Choose a password length from 8 to 128."); overrideLength = parsed; }
            if ((ActionKind)kind.SelectedItem == ActionKind.Application && !File.Exists(executable?.Text.Trim())) throw new InvalidOperationException("Choose an existing application executable.");
            if ((ActionKind)kind.SelectedItem == ActionKind.Network && !string.IsNullOrWhiteSpace(target?.Text) && !NetworkTarget.TryParse(target.Text, out _, out var error)) throw new InvalidOperationException(error);
            return new ActionDefinition { Id = source.Id, Name = name.Text.Trim(), Description = description.Text.Trim(), Aliases = aliases.Text.Trim(), Kind = (ActionKind)kind.SelectedItem, Enabled = enabled.IsChecked == true, PasswordLengthOverride = overrideLength, Executable = executable?.Text.Trim() ?? "", Arguments = arguments?.Text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).ToList() ?? [], Target = target?.Text.Trim() ?? "", PanelBehavior = (PanelBehavior)panel.SelectedItem };
        }
        void Preview()
        {
            previewName.Text = name.Text;
            preview.Text = (ActionKind)kind.SelectedItem switch { ActionKind.Password => $"Generate {(inherit?.IsChecked == true ? state.Config.PasswordLength.ToString() : length?.Text)} characters and copy to clipboard.", ActionKind.Application => "Launch the chosen application with each argument passed separately.", _ => string.IsNullOrWhiteSpace(target?.Text) ? "Read clipboard on invocation; ping or test the explicit TCP port." : $"Check {target.Text}." };
        }
        Preview();
        form.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler((_, _) => { dirty = true; Preview(); }));
        form.AddHandler(System.Windows.Controls.Primitives.ToggleButton.CheckedEvent, new RoutedEventHandler((_, _) => { dirty = true; Preview(); }));
        form.AddHandler(System.Windows.Controls.Primitives.ToggleButton.UncheckedEvent, new RoutedEventHandler((_, _) => { dirty = true; Preview(); }));
        panel.SelectionChanged += (_, _) => dirty = true;
        var save = Ui.Button("Save action", () =>
        {
            try
            {
                if (state.IsRunning) throw new InvalidOperationException("Wait for running actions to finish before changing the library.");
                var draft = Draft(); var index = state.Config.Actions.FindIndex(a => a.Id == source.Id);
                if (index < 0) state.Config.Actions.Add(draft); else state.Config.Actions[index] = draft;
                var ids = state.Config.ActiveProfile.ActionIds; if (pinned.IsChecked == true && !ids.Contains(draft.Id)) ids.Add(draft.Id); if (pinned.IsChecked != true) ids.Remove(draft.Id);
                state.Save(); dirty = false; selectedId = draft.Id; ShowActions(); StatusText.Text = "Action saved.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException) { StatusText.Text = ex.Message; }
        }, "PrimaryButton"); bottom.Children.Add(save);
        var test = Ui.Button("Test run", () => { }); test.Click += async (_, _) => { try { var draft = Draft(); if (!draft.Enabled) throw new InvalidOperationException("Enable the action before testing it."); await state.ExecuteAsync(new ActionItem(draft)); } catch (InvalidOperationException ex) { StatusText.Text = ex.Message; } }; bottom.Children.Add(test);
        if (state.Config.Actions.Any(a => a.Id == source.Id))
        {
            var duplicate = Ui.Button("Duplicate", () =>
            {
                try { var draft = Draft(); draft.Id = Guid.NewGuid().ToString("N"); draft.Name += " copy"; PageContent.Content = BuildEditor(draft); dirty = true; StatusText.Text = "Unsaved copy of the current settings. Save action to add it to the library."; }
                catch (InvalidOperationException ex) { StatusText.Text = ex.Message; }
            });
            duplicate.ToolTip = "Create an unsaved copy of the current editor settings"; bottom.Children.Add(duplicate);
        }
        return outer;
    }
    private void AddAction(object sender, RoutedEventArgs e) { if (!ConfirmDiscard()) return; PageContent.Content = BuildEditor(new ActionDefinition { Name = "New action" }); dirty = true; }
    private void ShowProfiles()
    {
        var form = new StackPanel { Width = 590, HorizontalAlignment = HorizontalAlignment.Left };
        var current = Ui.Choice(form, "Profile", state.Config.Profiles.Cast<object>(), state.Config.ActiveProfile);
        current.SelectionChanged += (_, _) => { if (current.SelectedItem is Profile p) { state.Config.ActiveProfileId = p.Id; SaveSettings(); ShowProfiles(); } };
        var name = Ui.Field(form, "Profile name", state.Config.ActiveProfile.Name);
        form.Children.Add(Ui.Button("Rename profile", () => { if (string.IsNullOrWhiteSpace(name.Text)) { StatusText.Text = "Enter a profile name."; return; } state.Config.ActiveProfile.Name = name.Text.Trim(); SaveSettings(); ShowProfiles(); }));
        form.Children.Add(Ui.Divider()); form.Children.Add(Ui.Text("Pinned order", 18));
        foreach (var id in state.Config.ActiveProfile.ActionIds.ToArray())
        {
            var action = state.Config.Actions.First(a => a.Id == id); var row = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var commands = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(commands, Dock.Right);
            foreach (var (text, delta) in new[] { ("Up", -1), ("Down", 1) }) commands.Children.Add(Ui.Button(text, () => { var ids = state.Config.ActiveProfile.ActionIds; var index = ids.IndexOf(id); var next = index + delta; if (next >= 0 && next < ids.Count) { (ids[index], ids[next]) = (ids[next], ids[index]); SaveSettings(); ShowProfiles(); } }));
            commands.Children.Add(Ui.Button("Unpin", () => { state.Config.ActiveProfile.ActionIds.Remove(id); SaveSettings(); ShowProfiles(); })); row.Children.Add(commands);
            var label = Ui.Text(action.Name); label.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(label); form.Children.Add(row);
        }
        form.Children.Add(Ui.Divider());
        var newName = Ui.Field(form, "New profile", "", "Starts empty. Pin shared actions from the action editor.");
        form.Children.Add(Ui.Button("Create profile", () => { if (string.IsNullOrWhiteSpace(newName.Text)) { StatusText.Text = "Enter a profile name."; return; } var p = new Profile { Name = newName.Text.Trim() }; state.Config.Profiles.Add(p); state.Config.ActiveProfileId = p.Id; SaveSettings(); ShowProfiles(); }));
        PageContent.Content = Ui.Scroll(form);
    }
    private void ShowPreferences()
    {
        var form = new StackPanel { MaxWidth = 540, HorizontalAlignment = HorizontalAlignment.Left };
        form.Children.Add(Ui.Text("Appearance", 18));
        var theme = Ui.Choice(form, "Theme", new object[] { "Dark", "Light", "System" }, state.Config.Theme);
        var reduced = new CheckBox { Content = "Reduce motion", IsChecked = state.Config.ReducedMotion }; form.Children.Add(reduced);
        var note = Ui.Text("System motion and high-contrast preferences are respected. No continuous animations run in the background.", 12, true); note.Margin = new Thickness(0, 5, 0, 0); form.Children.Add(note);
        form.Children.Add(Ui.Divider()); form.Children.Add(Ui.Text("Password defaults", 18));
        var length = Ui.Field(form, "Default length", state.Config.PasswordLength.ToString(), "Inheriting actions update automatically. Action-specific overrides stay unchanged.");
        form.Children.Add(Ui.Divider()); form.Children.Add(Ui.Text("Quick access", 18));
        form.Children.Add(Ui.Text("Ctrl+Alt+K opens the quick panel. Alt+1–9 runs visible actions while the panel has focus. Escape hides the panel or cancels a running check.", 12, true));
        var save = Ui.Button("Save preferences", () =>
        {
            if (!int.TryParse(length.Text, out var value) || value is < 8 or > 128) { StatusText.Text = "Password length must be 8–128."; return; }
            state.Config.Theme = (string)theme.SelectedItem; state.Config.ReducedMotion = reduced.IsChecked == true; state.Config.PasswordLength = value; ThemeManager.Apply(state.Config.Theme); NativeWindow.Apply(this, false); if (SaveSettings()) dirty = false;
        }, "PrimaryButton"); save.Margin = new Thickness(0, 22, 0, 0); save.HorizontalAlignment = HorizontalAlignment.Left; form.Children.Add(save);
        theme.SelectionChanged += (_, _) => dirty = true;
        reduced.Checked += (_, _) => dirty = true; reduced.Unchecked += (_, _) => dirty = true;
        length.TextChanged += (_, _) => dirty = true;
        form.Children.Add(Ui.Divider()); form.Children.Add(Ui.Text("Settings stay in SableKit-data beside the app. The folder must be writable. No vault setup is needed for these tools.", 12, true));
        PageContent.Content = Ui.Scroll(form);
    }
    private void ShowTools()
    {
        var form = new StackPanel();
        foreach (var (title, detail, open) in new (string, string, Action?)[] {
            ("Passwords", "Available · random password generation and clipboard copying.", () => Navigate("Preferences")),
            ("SableNet", "Prototype · ping, TCP checks, DNS and reverse lookup.", () => ((App)Application.Current).OpenNetwork()),
            ("Applications", "Available · executable launch with literal arguments.", () => Navigate("Actions")),
            ("SableType", "Planned · clipboard typing and destination selection.", null),
            ("SableLatch", "Planned · integrated vault and protected entry actions.", null),
            ("Workflows & addons", "Planned · sequences, shared security and curated installation.", null) })
        {
            var row = new DockPanel { Margin = new Thickness(0, 0, 0, 22) };
            if (open is not null) { var b = Ui.Button("Open", open); DockPanel.SetDock(b, Dock.Right); row.Children.Add(b); }
            var body = new StackPanel(); body.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold }); var sub = Ui.Text(detail, 12, true); sub.Margin = new Thickness(0, 6, 16, 0); body.Children.Add(sub); row.Children.Add(body); form.Children.Add(row);
        }
        PageContent.Content = Ui.Scroll(form);
    }
    private void ShowActivity()
    {
        var form = new StackPanel(); form.Children.Add(Ui.Text(state.Status, 20));
        var result = new TextBox { Text = state.Result, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 150, Margin = new Thickness(0, 18, 0, 18) }; form.Children.Add(result);
        var copy = Ui.Button("Copy result", () => { }); copy.IsEnabled = !string.IsNullOrEmpty(state.Result); copy.Click += async (_, _) => { try { await AppState.CopyAsync(state.Result); StatusText.Text = "Result copied."; } catch (System.Runtime.InteropServices.COMException) { StatusText.Text = "Clipboard is busy. Try again."; } }; form.Children.Add(copy);
        PageContent.Content = Ui.Scroll(form);
    }
    private bool SaveSettings() { try { state.Save(); StatusText.Text = "Saved."; return true; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { StatusText.Text = ex.Message; return false; } }
    private void OpenPanel(object sender, RoutedEventArgs e) => ((App)Application.Current).OpenTray();
}
