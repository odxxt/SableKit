namespace SableKit.Desktop;

public partial class MainWindow : Window
{
    private readonly AppState state;
    private string page = "Actions";
    private bool dirty;
    internal Func<bool>? DiscardDecision { get; set; }
    internal bool HasDraft => dirty;
    public MainWindow(AppState state)
    {
        InitializeComponent(); this.state = state;
        SourceInitialized += (_, _) => { NativeWindow.Apply(this, false); FitToWorkArea(NativeWindow.WorkArea(this)); };
        StateChanged += (_, _) =>
        {
            var maximized = WindowState == WindowState.Maximized;
            MaximizeButton.Content = maximized ? "\uE923" : "\uE922";
            MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
            System.Windows.Automation.AutomationProperties.SetName(MaximizeButton, maximized ? "Restore window" : "Maximize window");
        };
        Closing += (_, e) =>
        {
            if (!ConfirmDiscard() || editors.Any(editor=>!editor.CanClose())) { e.Cancel=true;return; }
            foreach(var editor in editors.ToArray()) editor.CloseApproved();
        };
        state.ResultChanged += StateResultChanged;
        state.Changed += LibraryChanged;
        Closed += (_, _) => { state.ResultChanged -= StateResultChanged; state.Changed -= LibraryChanged; };
        foreach (var name in new[] { "Actions", "Profiles", "Tools", "Activity", "Preferences" })
        {
            var b=Ui.Button(name,()=>Navigate(name),"TabButton"); b.Tag=name; b.Margin=new Thickness(0,0,6,0);
            System.Windows.Automation.AutomationProperties.SetName(b,name+" tab"); Navigation.Children.Add(b);
        }
        Navigate("Actions");
    }
    internal void FitToWorkArea(Rect area)
    {
        var width = Math.Min(920, area.Width * 0.90);
        var height = Math.Min(640, area.Height * 0.88);
        MinWidth = Math.Min(720, width); MinHeight = Math.Min(440, height);
        Width = width; Height = height;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top + (area.Height - Height) / 2;
    }
    private void DragHeader(object sender,MouseButtonEventArgs e)
    {
        if(TrayWindow.FindParent<Button>(e.OriginalSource as DependencyObject) is not null) return;
        if(e.ClickCount==2) MaximizeWindow(sender,e);
        else if(e.LeftButton==MouseButtonState.Pressed && WindowState==WindowState.Normal) DragMove();
    }
    private void MinimizeWindow(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
    private void MaximizeWindow(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
    private void StateResultChanged() { StatusText.Text = state.Status; if (page == "Activity") ShowActivity(); }
    private bool ConfirmDiscard() => !dirty || (DiscardDecision?.Invoke() ?? (MessageBox.Show(this, "Discard the changes in this editor?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes));
    public void Navigate(string destination)
    {
        if (!ConfirmDiscard()) return;
        dirty = false; page = destination; PageTitle.Text = page;
        AddButton.Visibility = page == "Actions" ? Visibility.Visible : Visibility.Collapsed;
        foreach(Button b in Navigation.Children)
        {
            var selected=b.Tag?.ToString()==page;
            b.SetResourceReference(BackgroundProperty,selected?"Hover":"PanelSurface");
            b.SetResourceReference(ForegroundProperty,selected?"Text":"Muted"); b.FontWeight=selected?FontWeights.SemiBold:FontWeights.Normal;
        }
        PageSubtitle.Text = page switch { "Actions" => "Select an action to configure it.", "Profiles" => "One action library. A different focus for every workspace.", "Tools" => "Small tools, brought together.", "Activity" => "The latest result. Kept only for this session.", _ => "Make SableKit feel at home." };
        StatusText.Text = "";
        switch (page) { case "Actions": ShowActions(); break; case "Profiles": ShowProfiles(); break; case "Tools": ShowTools(); break; case "Activity": ShowActivity(); break; default: ShowPreferences(); break; }
    }
    internal IReadOnlyCollection<ActionEditorWindow> Editors => editors;
    private readonly HashSet<ActionEditorWindow> editors = [];
    private string libraryQuery = "";
    internal void OpenEditor(ActionDefinition definition, bool isNew=false)
    {
        var existing=editors.FirstOrDefault(e=>e.EditingActionId==definition.Id);
        if(existing is not null){existing.Activate();return;}
        var editor=new ActionEditorWindow(state,definition,isNew){Owner=this};
        editors.Add(editor); editor.Closed+=(_,_)=>editors.Remove(editor); editor.Show(); editor.Activate();
    }
    private void ShowActions()
    {
        var layout=new DockPanel();
        var search=new TextBox { Tag="Find an action", Text=libraryQuery, Width=280, MaxWidth=320, HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,0,0,16) };
        System.Windows.Automation.AutomationProperties.SetName(search,"Search action library");
        DockPanel.SetDock(search,Dock.Top); layout.Children.Add(search);
        var grid=new WrapPanel { Name="ActionLibrary", Orientation=Orientation.Horizontal };
        var empty=Ui.Text("No actions match. Try another name or alias.",13,true);
        var content=new StackPanel(); content.Children.Add(grid); content.Children.Add(empty);
        var scroll=Ui.Scroll(content); scroll.Padding=new Thickness(0); layout.Children.Add(scroll);
        void SizeCards()
        {
            var available=Math.Max(180,Math.Min(scroll.ViewportWidth,grid.ActualWidth>0?grid.ActualWidth:scroll.ViewportWidth)-2);
            var columns=Math.Max(1,(int)(available/228));
            foreach(Button tile in grid.Children) tile.Width=Math.Max(180,Math.Floor(available/columns)-12);
        }
        void Populate()
        {
            grid.Children.Clear();
            foreach(var item in state.Items.Where(i=>ActionSettings.Matches(i.Definition,search.Text)))
            {
                var tile=new Button { Tag=item.Definition, Height=118, Margin=new Thickness(0,0,12,12), Padding=new Thickness(16), HorizontalContentAlignment=HorizontalAlignment.Stretch, ToolTip="Configure "+item.Name };
                tile.SetResourceReference(BackgroundProperty,"Inset");
                System.Windows.Automation.AutomationProperties.SetName(tile,"Configure "+item.Name);
                var body=new Grid(); body.RowDefinitions.Add(new RowDefinition { Height=new GridLength(30) }); body.RowDefinitions.Add(new RowDefinition()); body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
                body.Children.Add(new Icon { Kind=item.Icon,Width=20,Height=20,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top });
                var title=new TextBlock { Text=item.Name,FontSize=14,FontWeight=FontWeights.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center }; Grid.SetRow(title,1); body.Children.Add(title);
                var detail=Ui.Text(item.ToolName+(item.Definition.Enabled?"":" - Disabled"),11,true); Grid.SetRow(detail,2); body.Children.Add(detail);
                tile.Content=body; tile.Click+=(_,_)=>OpenEditor(item.Definition); grid.Children.Add(tile);
            }
            empty.Visibility=grid.Children.Count==0?Visibility.Visible:Visibility.Collapsed;
            SizeCards();
        }
        search.TextChanged+=(_,_)=>{libraryQuery=search.Text;Populate();};
        grid.SizeChanged+=(_,_)=>SizeCards(); scroll.SizeChanged+=(_,_)=>SizeCards(); scroll.ScrollChanged+=(_,e)=>{if(e.ViewportWidthChange!=0)SizeCards();};
        Populate(); PageContent.Content=layout;
    }
    private void AddAction(object sender,RoutedEventArgs e)=>OpenEditor(new ActionDefinition { Name="New action" },true);
    private void LibraryChanged() { if(page=="Actions") ShowActions(); }
    private void ShowProfiles()
    {
        var form = new StackPanel { MaxWidth = 590, HorizontalAlignment = HorizontalAlignment.Stretch };
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
            ("SableNet", "Prototype · ping, TCP checks, DNS and reverse lookup.", () => ((App)Application.Current).OpenNetwork()),
            ("SableType", "Planned · clipboard typing and destination selection.", null),
            ("SableLatch", "Planned · integrated vault and protected entry actions.", null),
            ("Addon catalog", "Planned - curated and manual tool installation.", null) })
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
}
