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
        InitializeComponent(); this.state = state; WindowResize.Attach(this);
        PreviewMouseDown+=(_,e)=>
        {
            if(e.ChangedButton==MouseButton.Left && ProfileNameInput is { } field && e.OriginalSource is DependencyObject source && !ReferenceEquals(field,source) && !field.IsAncestorOf(source))
                FinishProfileName(true);
        };
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
        foreach (var name in new[] { "Actions", "Profiles", "Tools", "Preferences" })
        {
            var b=Ui.Button(name,()=>Navigate(name),"TabButton"); b.Tag=name; b.Width=100; b.FontWeight=FontWeights.SemiBold; b.Margin=new Thickness(0,0,4,0);
            System.Windows.Automation.AutomationProperties.SetName(b,name+" tab"); Navigation.Children.Add(b);
        }
        Navigate("Actions");
    }
    internal void FitToWorkArea(Rect area)
    {
        var width = Math.Min(920, area.Width * 0.90);
        var height = Math.Min(640, area.Height * 0.88);
        MinWidth = Math.Min(760, area.Width*.85); MinHeight = Math.Min(500, area.Height*.85);
        MaxWidth=area.Width; MaxHeight=area.Height;
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
    private void StateResultChanged() { StatusText.Text = state.Status; }
    private bool ConfirmDiscard() => !dirty || (DiscardDecision?.Invoke() ?? (MessageBox.Show(this, "Discard the changes in this editor?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes));
    public void Navigate(string destination)
    {
        if(editors.Count>0){editors.First().Activate();return;}
        if (!ConfirmDiscard()) return;
        dirty = false; page = destination=="Activity"?"Actions":destination; PageTitle.Text = page;
        ActionCommands.Visibility = page == "Actions" ? Visibility.Visible : Visibility.Collapsed;
        foreach(Button b in Navigation.Children)
        {
            var selected=b.Tag?.ToString()==page;
            b.SetResourceReference(BackgroundProperty,selected?"Inset":"Surface");
            b.SetResourceReference(ForegroundProperty,selected?"Text":"Muted"); b.FontWeight=FontWeights.SemiBold;
        }
        PageSubtitle.Text = page switch { "Actions" => "Select an action to configure it.", "Profiles" => "Choose what appears in your quick panel.", "Tools" => "Your installed capabilities, ready to use.", _ => "Appearance, defaults and quick access." };
        StatusText.Text = "";
        switch (page) { case "Actions": ShowActions(); break; case "Profiles": ShowProfiles(); break; case "Tools": ShowTools(); break; default: ShowPreferences(); break; }
    }
    internal IReadOnlyCollection<ActionEditorWindow> Editors => editors;
    private readonly HashSet<ActionEditorWindow> editors = [];
    private string libraryQuery = "";
    internal void OpenEditor(ActionDefinition definition, bool isNew=false)
    {
        var existing=editors.FirstOrDefault();
        if(existing is not null){existing.Activate();return;}
        var editor=new ActionEditorWindow(state,definition,isNew){Owner=this};
        editors.Add(editor);ModalShade.Visibility=Visibility.Visible;IsEnabled=false;
        string? saved=null;editor.Saved+=id=>saved=id;
        editor.Closed+=(_,_)=>
        {
            editors.Remove(editor);ModalShade.Visibility=Visibility.Collapsed;IsEnabled=true;Activate();
            if(saved is not null){LastSavedActionId=saved;LibrarySearch.Clear();libraryQuery="";Navigate("Actions");StatusText.Text="Action saved.";Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>HighlightSaved(saved)));}
        };
        editor.Show();editor.Activate();
    }
    internal string? LastSavedActionId { get; private set; }
    private void HighlightSaved(string id)
    {
        if(!IsVisible)return;
        var tile=libraryTiles?.Children.OfType<Button>().FirstOrDefault(b=>b.Tag is ActionDefinition a && a.Id==id);
        tile?.BringIntoView();if(tile is not null)UiMotion.Saved(tile);
    }
    private WrapPanel? libraryTiles;
    private bool libraryGrid=true;
    private bool librarySearchOpen;
    private void ToggleLibrarySearch(object sender,RoutedEventArgs e)
    {
        librarySearchOpen=!librarySearchOpen;LibrarySearch.Visibility=librarySearchOpen?Visibility.Visible:Visibility.Collapsed;
        if(!librarySearchOpen)LibrarySearch.Clear();
        LibrarySearchButton.SetResourceReference(BackgroundProperty,librarySearchOpen?"Hover":"Surface");
        if(librarySearchOpen)LibrarySearch.Focus();
    }
    private void LibrarySearchChanged(object sender,TextChangedEventArgs e){if(state is not null && page=="Actions"){libraryQuery=LibrarySearch.Text;ShowActions();}}
    private void LibrarySearchKeys(object sender,KeyEventArgs e){if(e.Key==Key.Escape){ToggleLibrarySearch(sender,e);LibrarySearchButton.Focus();e.Handled=true;}}
    private void ShowLibraryList(object sender,RoutedEventArgs e){libraryGrid=false;ShowActions();}
    private void ShowLibraryGrid(object sender,RoutedEventArgs e){libraryGrid=true;ShowActions();}
    private void ShowActions()
    {
        var layout=new DockPanel();
        LibraryListButton.SetResourceReference(BackgroundProperty,libraryGrid?"Surface":"Hover");LibraryGridButton.SetResourceReference(BackgroundProperty,libraryGrid?"Hover":"Surface");
        var grid=new WrapPanel { Name="ActionLibrary", Orientation=Orientation.Horizontal };libraryTiles=grid;
        var empty=Ui.Text("No actions match. Try another name or alias.",13,true);
        var content=new StackPanel(); content.Children.Add(grid); content.Children.Add(empty);
        var scroll=Ui.Scroll(content); scroll.Padding=new Thickness(0); layout.Children.Add(scroll);
        void SizeCards()
        {
            var available=Math.Max(180,Math.Min(scroll.ViewportWidth,grid.ActualWidth>0?grid.ActualWidth:scroll.ViewportWidth)-2);
            var columns=libraryGrid?Math.Max(1,(int)(available/240)):1;
            foreach(Button tile in grid.Children) tile.Width=Math.Max(180,Math.Floor(available/columns)-12);
        }
        void Populate()
        {
            grid.Children.Clear();
            foreach(var item in state.Items.Where(i=>ActionSettings.Matches(i.Definition,libraryQuery)))
            {
                var tile=new Button { Tag=item.Definition, Height=libraryGrid?118:58, Margin=new Thickness(0,0,12,12), Padding=libraryGrid?new Thickness(16):new Thickness(16,7,16,7), HorizontalContentAlignment=HorizontalAlignment.Stretch, ToolTip="Configure "+item.Name };
                tile.SetResourceReference(BackgroundProperty,"Inset");
                System.Windows.Automation.AutomationProperties.SetName(tile,"Configure "+item.Name);
                var body=new Grid(); body.RowDefinitions.Add(new RowDefinition { Height=new GridLength(30) }); body.RowDefinitions.Add(new RowDefinition()); body.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
                body.Children.Add(new Icon { Kind=item.Icon,Width=20,Height=20,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top });
                var title=new TextBlock { Text=item.Name,FontSize=14,FontWeight=FontWeights.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center }; Grid.SetRow(title,1); body.Children.Add(title);
                var detail=Ui.Text(item.ToolName+(item.Definition.Enabled?"":" - Disabled"),11,true); Grid.SetRow(detail,2); body.Children.Add(detail);
                tile.Content=libraryGrid?body:ActionVisual.Create(item,state.Config,false); tile.Click+=(_,_)=>OpenEditor(item.Definition); grid.Children.Add(tile);
            }
            empty.Visibility=grid.Children.Count==0?Visibility.Visible:Visibility.Collapsed;
            SizeCards();
        }
        grid.SizeChanged+=(_,_)=>SizeCards(); scroll.SizeChanged+=(_,_)=>SizeCards(); scroll.ScrollChanged+=(_,e)=>{if(e.ViewportWidthChange!=0)SizeCards();};
        Populate(); PageContent.Content=layout;
    }
    private void AddAction(object sender,RoutedEventArgs e)=>OpenEditor(new ActionDefinition { Name="New action" },true);
    private void LibraryChanged() { if(page=="Actions") ShowActions(); }
    private static Border Section(string title,string detail,UIElement controls)
    {
        var row=new Grid { Margin=new Thickness(0,18,0,18) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(190) });row.ColumnDefinitions.Add(new ColumnDefinition());
        var labels=new StackPanel { Margin=new Thickness(0,0,28,0) }; labels.Children.Add(Ui.Text(title,15));
        var note=Ui.Text(detail,12,true);note.Margin=new Thickness(0,7,0,0);labels.Children.Add(note);row.Children.Add(labels);Grid.SetColumn(controls,1);row.Children.Add(controls);
        var border=new Border { Child=row,BorderThickness=new Thickness(0,0,0,1) };border.SetResourceReference(Border.BorderBrushProperty,"Border");return border;
    }
    private Profile? renamingProfile;
    private bool creatingProfile, finishingName, showAvailable;
    internal TextBox? ProfileNameInput { get; private set; }
    internal void BeginProfileName(Profile? profile)
    {
        if(ProfileNameInput is not null)FinishProfileName(true);
        renamingProfile=profile;creatingProfile=profile is null;ShowProfiles();
    }
    internal void FinishProfileName(bool commit)
    {
        if(finishingName || ProfileNameInput is null)return;finishingName=true;
        var name=ProfileNameInput.Text.Trim();var source=renamingProfile;var wasNew=creatingProfile;
        ProfileNameInput=null;creatingProfile=false;renamingProfile=null;
        if(commit && name.Length>0)
        {
            var oldActive=state.Config.ActiveProfileId;var oldName=source?.Name;
            var profile=source ?? new Profile { Name=name };
            if(wasNew){state.Config.Profiles.Add(profile);state.Config.ActiveProfileId=profile.Id;}else profile.Name=name;
            try{state.Save();StatusText.Text=wasNew?"Profile created.":"Profile renamed.";}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {if(wasNew){state.Config.Profiles.Remove(profile);state.Config.ActiveProfileId=oldActive;}else profile.Name=oldName!;StatusText.Text=ex.Message;}
        }
        finishingName=false;if(page=="Profiles")ShowProfiles();
    }
    internal void AddProfileAction(string id)
    {
        var profile=state.Config.ActiveProfile;
        if(!state.Config.Actions.Any(a=>a.Id==id)||profile.ActionIds.Contains(id))return;
        profile.ActionIds.Add(id);
        try{state.Save();ShowProfiles();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){profile.ActionIds.Remove(id);StatusText.Text=ex.Message;}
    }
    private ScrollViewer? profileChooser;
    private void ShowProfiles(bool preserveNames=false)
    {
        var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(210) });layout.ColumnDefinitions.Add(new ColumnDefinition());
        var chooser=new StackPanel { Margin=new Thickness(0,0,24,0) };chooser.Children.Add(Ui.Text("Your profiles",12,true));
        void NameEditor(Profile? profile)
        {
            var field=new TextBox { Text=profile?.Name??"",Tag="Profile name",Margin=new Thickness(0,8,0,0),Opacity=profile is null?.55:1 };
            ProfileNameInput=field;System.Windows.Automation.AutomationProperties.SetName(field,profile is null?"New profile name":"Rename profile");
            field.TextChanged+=(_,_)=>field.Opacity=profile is null && string.IsNullOrWhiteSpace(field.Text)?.55:1;
            field.PreviewKeyDown+=(_,e)=>{if(e.Key is Key.Enter or Key.Escape){FinishProfileName(e.Key==Key.Enter);e.Handled=true;}};
            field.LostKeyboardFocus+=(_,_)=>{if(ReferenceEquals(ProfileNameInput,field))FinishProfileName(true);};chooser.Children.Add(field);
            Dispatcher.BeginInvoke(()=>{if(ReferenceEquals(ProfileNameInput,field)){field.Focus();field.SelectAll();}});
        }
        foreach(var profile in state.Config.Profiles)
        {
            if(ReferenceEquals(profile,renamingProfile)){NameEditor(profile);continue;}
            var button=Ui.Button(profile.Name,()=>{if(ActionReorder.SuppressClick || state.Config.ActiveProfileId==profile.Id)return;state.Config.ActiveProfileId=profile.Id;SaveSettings();ShowProfiles(true);},"");
            button.Tag=profile;button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,8,0,0);button.Padding=new Thickness(12,10,12,10);button.ToolTip="Double-click to rename";
            button.MouseDoubleClick+=(_,e)=>{BeginProfileName(profile);e.Handled=true;};button.SetResourceReference(BackgroundProperty,profile.Id==state.Config.ActiveProfileId?"Hover":"Surface");chooser.Children.Add(button);
        }
        if(creatingProfile)NameEditor(null);
        var add=Ui.Button("+  New profile",()=>BeginProfileName(null),"");add.Margin=new Thickness(0,12,0,0);add.HorizontalContentAlignment=HorizontalAlignment.Left;chooser.Children.Add(add);
        if(preserveNames && profileChooser is not null && profileChooser.Parent is Grid previous)
        {
            previous.Children.Remove(profileChooser);
            if(profileChooser.Content is StackPanel existing)foreach(var button in existing.Children.OfType<Button>())
                if(button.Tag is Profile profile)button.SetResourceReference(BackgroundProperty,profile.Id==state.Config.ActiveProfileId?"Hover":"Surface");
        }
        else profileChooser=Ui.Scroll(chooser);
        layout.Children.Add(profileChooser);
        var detail=new DockPanel { Margin=new Thickness(10,0,0,0) };
        var top=new DockPanel { Margin=new Thickness(0,0,0,16) };var addActions=Ui.Button("Add actions",()=>{showAvailable=!showAvailable;ShowProfiles();});DockPanel.SetDock(addActions,Dock.Right);top.Children.Add(addActions);
        var labels=new StackPanel();var title=Ui.Text(state.Config.ActiveProfile.Name,20);title.ToolTip="Double-click to rename";title.MouseLeftButtonDown+=(_,e)=>{if(e.ClickCount==2)BeginProfileName(state.Config.ActiveProfile);};labels.Children.Add(title);var hint=Ui.Text("Drag to reorder. Double-click a profile name to rename it.",12,true);hint.Margin=new Thickness(0,6,12,0);labels.Children.Add(hint);top.Children.Add(labels);DockPanel.SetDock(top,Dock.Top);detail.Children.Add(top);
        var content=new StackPanel();var pins=new StackPanel { Name="ProfileActionRows" };content.Children.Add(pins);
        void RenderPins()
        {
            pins.Children.Clear();
            foreach(var id in state.Config.ActiveProfile.ActionIds)
            {
                var action=state.Config.Actions.First(a=>a.Id==id);var row=new Grid { Tag=id,Height=50,Margin=new Thickness(0,0,0,5),Background=Brushes.Transparent };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(26) });row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(30) });
                var grip=new TextBlock { Text="\uE784",FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=13,VerticalAlignment=VerticalAlignment.Center,Foreground=(Brush)FindResource("Muted") };row.Children.Add(grip);
                var label=Ui.Text(action.Name);label.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(label,1);row.Children.Add(label);
                var remove=Ui.Button("\uE8BB",()=>{state.Config.ActiveProfile.ActionIds.Remove(id);SaveSettings();ShowProfiles();},"HeaderButton");remove.ToolTip="Remove from profile";System.Windows.Automation.AutomationProperties.SetName(remove,"Unpin "+action.Name);Grid.SetColumn(remove,2);row.Children.Add(remove);
                row.Focusable=true;System.Windows.Automation.AutomationProperties.SetName(row,action.Name+", Alt+Up or Alt+Down to reorder");row.PreviewKeyDown+=(_,e)=>{if(Keyboard.Modifiers==ModifierKeys.Alt && e.Key is Key.Up or Key.Down){var at=state.Config.ActiveProfile.ActionIds.IndexOf(id);var insertion=e.Key==Key.Up?at-1:at+2;ActionReorder.Move(state,state.Config.ActiveProfile,id,insertion);RenderPins();e.Handled=true;}};pins.Children.Add(row);
            }
        }
        RenderPins();
        if(pins.Children.Count==0){var empty=Ui.Text("No actions in this profile yet. Choose Add actions to start.",13,true);empty.Margin=new Thickness(0,12,0,20);content.Children.Add(empty);}
        if(showAvailable)
        {
            var available=state.Config.Actions.Where(a=>!state.Config.ActiveProfile.ActionIds.Contains(a.Id)).ToArray();
            var availableTitle=Ui.Text("Add from your action library",14);availableTitle.Margin=new Thickness(0,22,0,12);content.Children.Add(availableTitle);
            foreach(var action in available){var button=Ui.Button("+  "+action.Name,()=>AddProfileAction(action.Id),"");button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,4,0,4);content.Children.Add(button);}
            if(available.Length==0)content.Children.Add(Ui.Text("All your actions are already in this profile.",12,true));
        }
        var scroll=Ui.Scroll(content);detail.Children.Add(scroll);Grid.SetColumn(detail,1);layout.Children.Add(detail);
        _=new ActionReorder(scroll,()=>pins.Children.Cast<FrameworkElement>(),()=>!state.IsRunning,idMove);
        void idMove(string id,int at){try{ActionReorder.Move(state,state.Config.ActiveProfile,id,at);RenderPins();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){StatusText.Text=ex.Message;}}
        PageContent.Content=layout;
    }
    private void ShowPreferences()
    {
        var layout=new DockPanel();var commands=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,16,0,0) };DockPanel.SetDock(commands,Dock.Bottom);layout.Children.Add(commands);
        var form=new StackPanel();layout.Children.Add(Ui.Scroll(form));
        var appearance=new StackPanel();var theme=Ui.Choice(appearance,"Theme",new object[]{"Dark","Light","System"},state.Config.Theme);
        var reduced=new CheckBox { Content="Reduce motion",IsChecked=state.Config.ReducedMotion,Margin=new Thickness(0,14,0,0) };appearance.Children.Add(reduced);
        form.Children.Add(Section("Appearance","Choose your theme and how much the interface moves.",appearance));
        var passwords=new StackPanel();var length=Ui.Field(passwords,"Default length",state.Config.PasswordLength.ToString(),"8–128 characters. Actions with their own override stay unchanged.");
        form.Children.Add(Section("Password generation","Defaults for the actions that inherit them.",passwords));
        var access=new StackPanel();access.Children.Add(Ui.Text("Ctrl + Alt + K    Open quick panel",13));var shortcuts=Ui.Text("Alt + 1–9    Run a visible action\nEscape    Hide the panel or stop a check",12,true);shortcuts.Margin=new Thickness(0,10,0,0);access.Children.Add(shortcuts);
        form.Children.Add(Section("Quick access","Keyboard shortcuts available in this build.",access));
        var storage=Ui.Text("Settings are saved in SableKit-data beside the app. No vault setup is needed for ordinary actions.",12,true);form.Children.Add(Section("Portable settings","Your configuration stays with SableKit.",storage));
        var save=Ui.Button("Save preferences",()=>{
            if(!int.TryParse(length.Text,out var value) || value is <8 or >128){StatusText.Text="Password length must be 8–128.";return;}
            state.Config.Theme=(string)theme.SelectedItem;state.Config.ReducedMotion=reduced.IsChecked==true;state.Config.PasswordLength=value;ThemeManager.Apply(state.Config.Theme);if(SaveSettings())dirty=false;
        },"PrimaryButton");commands.Children.Add(save);
        theme.SelectionChanged+=(_,_)=>dirty=true;reduced.Checked+=(_,_)=>dirty=true;reduced.Unchecked+=(_,_)=>dirty=true;length.TextChanged+=(_,_)=>dirty=true;
        PageContent.Content=layout;
    }
    private void ShowTools()
    {
        var form=new StackPanel();
        var header=Ui.Text("Available",12,true);header.Margin=new Thickness(0,0,0,14);form.Children.Add(header);
        var network=new Grid();network.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(54) });network.ColumnDefinitions.Add(new ColumnDefinition());
        network.Children.Add(new Icon { Kind="network",Width=30,Height=30,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,4,18,0) });
        var body=new StackPanel();Grid.SetColumn(body,1);network.Children.Add(body);body.Children.Add(Ui.Text("SableNet",22));
        var description=Ui.Text("Check an address, inspect DNS, or test a TCP port.",13,true);description.Margin=new Thickness(0,8,0,18);body.Children.Add(description);
        var commands=new WrapPanel();commands.Children.Add(Ui.Button("Open SableNet",OpenNetworkTool,"PrimaryButton"));
        commands.Children.Add(Ui.Button("Create network action",()=>OpenEditor(new ActionDefinition { Name="Check connection",Kind=ActionKind.Network })));body.Children.Add(commands);
        var installed=new Border { Child=network,Padding=new Thickness(22),CornerRadius=new CornerRadius(12) };installed.SetResourceReference(BackgroundProperty,"Inset");form.Children.Add(installed);
        var builtin=Ui.Text("Included in SableKit",16);builtin.Margin=new Thickness(0,26,0,8);form.Children.Add(builtin);
        form.Children.Add(Ui.Text("Password generation and application launching are available from New action. They do not need an addon.",13,true));
        var planned=Ui.Button("Show planned tools",()=>{},"");planned.HorizontalAlignment=HorizontalAlignment.Left;planned.Margin=new Thickness(0,24,0,0);planned.SetResourceReference(ForegroundProperty,"Muted");
        var coming=new StackPanel { Margin=new Thickness(12,12,0,0),Visibility=Visibility.Collapsed };coming.Children.Add(Ui.Text("SableType — clipboard typing\nSableLatch — protected vault actions\nAddon catalog — discover and install tools",12,true));
        planned.Click+=(_,_)=>{var show=coming.Visibility!=Visibility.Visible;coming.Visibility=show?Visibility.Visible:Visibility.Collapsed;planned.Content=show?"Hide planned tools":"Show planned tools";};form.Children.Add(planned);form.Children.Add(coming);
        PageContent.Content=Ui.Scroll(form);
    }
    internal void OpenNetworkTool()
    {
        if(editors.Count>0 || !ConfirmDiscard())return;
        Hide();((App)Application.Current).OpenNetwork();
    }
    private bool SaveSettings() { try { state.Save(); StatusText.Text = "Saved."; return true; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { StatusText.Text = ex.Message; return false; } }
}
