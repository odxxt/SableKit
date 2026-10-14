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
            var b=Ui.Button(name,()=>Navigate(name),"TabButton"); b.Tag=name; b.Width=100; b.FontWeight=FontWeights.SemiBold; b.Margin=new Thickness(0,0,4,0);
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
        AddButton.Visibility = LibrarySearchButton.Visibility = page == "Actions" ? Visibility.Visible : Visibility.Collapsed;
        foreach(Button b in Navigation.Children)
        {
            var selected=b.Tag?.ToString()==page;
            b.SetResourceReference(BackgroundProperty,selected?"Inset":"Surface");
            b.SetResourceReference(ForegroundProperty,selected?"Text":"Muted"); b.FontWeight=FontWeights.SemiBold;
        }
        PageSubtitle.Text = page switch { "Actions" => "Select an action to configure it.", "Profiles" => "Choose what appears in your quick panel.", "Tools" => "Your installed capabilities, ready to use.", "Activity" => "Output from your latest action.", _ => "Appearance, defaults and quick access." };
        StatusText.Text = "";
        switch (page) { case "Actions": ShowActions(); break; case "Profiles": ShowProfiles(); break; case "Tools": ShowTools(); break; case "Activity": ShowActivity(); break; default: ShowPreferences(); break; }
    }
    internal IReadOnlyCollection<ActionEditorWindow> Editors => editors;
    private readonly HashSet<ActionEditorWindow> editors = [];
    private string libraryQuery = "";
    internal void OpenEditor(ActionDefinition definition, bool isNew=false)
    {
        var existing=editors.FirstOrDefault();
        if(existing is not null)
        {
            if(existing.EditingActionId!=definition.Id || isNew) existing.TryEdit(definition,isNew);
            existing.Activate(); return;
        }
        var editor=new ActionEditorWindow(state,definition,isNew){Owner=this};
        editors.Add(editor); editor.Closed+=(_,_)=>editors.Remove(editor); editor.Show(); editor.Activate();
    }
    private bool librarySearchOpen;
    private TextBox? librarySearch;
    private void ToggleLibrarySearch(object sender,RoutedEventArgs e)
    {
        librarySearchOpen=!librarySearchOpen;
        if(!librarySearchOpen) libraryQuery="";
        ShowActions();
        if(librarySearchOpen) librarySearch?.Focus();
    }
    private void ShowActions()
    {
        var layout=new DockPanel();
        var search=new TextBox { Visibility=librarySearchOpen?Visibility.Visible:Visibility.Collapsed, Tag="Find an action", Text=libraryQuery, Width=280, MaxWidth=320, HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,0,0,16) };
        librarySearch=search; LibrarySearchButton.SetResourceReference(BackgroundProperty,librarySearchOpen?"Hover":"Surface");
        search.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){ToggleLibrarySearch(search,e);LibrarySearchButton.Focus();e.Handled=true;}};
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
    private static Border Section(string title,string detail,UIElement controls)
    {
        var row=new Grid { Margin=new Thickness(0,18,0,18) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(190) });row.ColumnDefinitions.Add(new ColumnDefinition());
        var labels=new StackPanel { Margin=new Thickness(0,0,28,0) }; labels.Children.Add(Ui.Text(title,15));
        var note=Ui.Text(detail,12,true);note.Margin=new Thickness(0,7,0,0);labels.Children.Add(note);row.Children.Add(labels);Grid.SetColumn(controls,1);row.Children.Add(controls);
        var border=new Border { Child=row,BorderThickness=new Thickness(0,0,0,1) };border.SetResourceReference(Border.BorderBrushProperty,"Border");return border;
    }
    private void ShowProfiles()
    {
        var layout=new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(210) });layout.ColumnDefinitions.Add(new ColumnDefinition());
        var chooser=new StackPanel { Margin=new Thickness(0,0,24,0) };
        chooser.Children.Add(Ui.Text("Your profiles",12,true));
        foreach(var profile in state.Config.Profiles)
        {
            var button=Ui.Button(profile.Name,()=>{state.Config.ActiveProfileId=profile.Id;SaveSettings();ShowProfiles();},"");
            button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,8,0,0);button.Padding=new Thickness(12,10,12,10);
            button.SetResourceReference(BackgroundProperty,profile.Id==state.Config.ActiveProfileId?"Hover":"Surface");chooser.Children.Add(button);
        }
        var newName=Ui.Field(chooser,"New profile","",null);
        var add=Ui.Button("Create profile",()=>{if(string.IsNullOrWhiteSpace(newName.Text)){StatusText.Text="Enter a profile name.";return;}var profile=new Profile { Name=newName.Text.Trim() };state.Config.Profiles.Add(profile);state.Config.ActiveProfileId=profile.Id;SaveSettings();ShowProfiles();});add.Margin=new Thickness(0,10,0,0);chooser.Children.Add(add);
        layout.Children.Add(Ui.Scroll(chooser));
        var detail=new StackPanel { Margin=new Thickness(10,0,0,0) };
        var rename=new DockPanel();var renameButton=Ui.Button("Rename",()=>{});DockPanel.SetDock(renameButton,Dock.Right);renameButton.Margin=new Thickness(10,0,0,0);rename.Children.Add(renameButton);
        var name=new TextBox { Text=state.Config.ActiveProfile.Name };System.Windows.Automation.AutomationProperties.SetName(name,"Profile name");rename.Children.Add(name);detail.Children.Add(rename);
        renameButton.Click+=(_,_)=>{if(string.IsNullOrWhiteSpace(name.Text)){StatusText.Text="Enter a profile name.";return;}state.Config.ActiveProfile.Name=name.Text.Trim();SaveSettings();ShowProfiles();};
        var heading=Ui.Text("Pinned actions",16);heading.Margin=new Thickness(0,28,0,8);detail.Children.Add(heading);
        detail.Children.Add(Ui.Text("Order them as you want them to appear in the quick panel.",12,true));
        foreach(var id in state.Config.ActiveProfile.ActionIds.ToArray())
        {
            var action=state.Config.Actions.First(a=>a.Id==id);var row=new DockPanel { Margin=new Thickness(0,12,0,0),LastChildFill=true };
            var commands=new StackPanel { Orientation=Orientation.Horizontal };DockPanel.SetDock(commands,Dock.Right);
            foreach(var (label,delta) in new[]{("Move up",-1),("Move down",1)})
            {
                var button=Ui.Button(delta<0?"\uE70E":"\uE70D",()=>{var ids=state.Config.ActiveProfile.ActionIds;var index=ids.IndexOf(id);var next=index+delta;if(next>=0 && next<ids.Count){(ids[index],ids[next])=(ids[next],ids[index]);SaveSettings();ShowProfiles();}},"HeaderButton");
                button.ToolTip=label;System.Windows.Automation.AutomationProperties.SetName(button,label+" "+action.Name);
                var current=state.Config.ActiveProfile.ActionIds.IndexOf(id);button.IsEnabled=current+delta>=0 && current+delta<state.Config.ActiveProfile.ActionIds.Count;commands.Children.Add(button);
            }
            var remove=Ui.Button("\uE8BB",()=>{state.Config.ActiveProfile.ActionIds.Remove(id);SaveSettings();ShowProfiles();},"HeaderButton");remove.ToolTip="Unpin "+action.Name;System.Windows.Automation.AutomationProperties.SetName(remove,"Unpin "+action.Name);commands.Children.Add(remove);row.Children.Add(commands);
            var labelText=Ui.Text(action.Name);labelText.VerticalAlignment=VerticalAlignment.Center;row.Children.Add(labelText);detail.Children.Add(row);
        }
        if(state.Config.ActiveProfile.ActionIds.Count==0){var note=Ui.Text("No pinned actions yet. Open an action and choose Pin in this profile under Behavior.",13,true);note.Margin=new Thickness(0,20,0,0);detail.Children.Add(note);}
        var right=Ui.Scroll(detail);Grid.SetColumn(right,1);layout.Children.Add(right);PageContent.Content=layout;
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
        var commands=new WrapPanel();commands.Children.Add(Ui.Button("Open SableNet",()=>((App)Application.Current).OpenNetwork(),"PrimaryButton"));
        commands.Children.Add(Ui.Button("Create network action",()=>OpenEditor(new ActionDefinition { Name="Check connection",Kind=ActionKind.Network })));body.Children.Add(commands);
        var installed=new Border { Child=network,Padding=new Thickness(22),CornerRadius=new CornerRadius(12) };installed.SetResourceReference(BackgroundProperty,"Inset");form.Children.Add(installed);
        var builtin=Ui.Text("Included in SableKit",16);builtin.Margin=new Thickness(0,26,0,8);form.Children.Add(builtin);
        form.Children.Add(Ui.Text("Password generation and application launching are available from New action. They do not need an addon.",13,true));
        var planned=Ui.Button("Show planned tools",()=>{},"");planned.HorizontalAlignment=HorizontalAlignment.Left;planned.Margin=new Thickness(0,24,0,0);planned.SetResourceReference(ForegroundProperty,"Muted");
        var coming=new StackPanel { Margin=new Thickness(12,12,0,0),Visibility=Visibility.Collapsed };coming.Children.Add(Ui.Text("SableType — clipboard typing\nSableLatch — protected vault actions\nAddon catalog — discover and install tools",12,true));
        planned.Click+=(_,_)=>{var show=coming.Visibility!=Visibility.Visible;coming.Visibility=show?Visibility.Visible:Visibility.Collapsed;planned.Content=show?"Hide planned tools":"Show planned tools";};form.Children.Add(planned);form.Children.Add(coming);
        PageContent.Content=Ui.Scroll(form);
    }
    private void ShowActivity()
    {
        var layout=new DockPanel();
        var controls=new DockPanel { Margin=new Thickness(0,0,0,16) };
        var copy=Ui.Button("Copy output",()=>{});copy.IsEnabled=!string.IsNullOrEmpty(state.Result);DockPanel.SetDock(copy,Dock.Right);controls.Children.Add(copy);
        var status=Ui.Text(string.IsNullOrWhiteSpace(state.Result)?"No output yet":state.Status,15);status.VerticalAlignment=VerticalAlignment.Center;controls.Children.Add(status);DockPanel.SetDock(controls,Dock.Top);layout.Children.Add(controls);
        if(string.IsNullOrWhiteSpace(state.Result)){layout.Children.Add(Ui.Text("Run an action from the quick panel. Its latest output will appear here for this session.",13,true));}
        else {var result=new TextBox { Text=state.Result,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.NoWrap,FontFamily=new FontFamily("Consolas"),FontSize=12,VerticalContentAlignment=VerticalAlignment.Top,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,BorderThickness=new Thickness(0),Padding=new Thickness(16) };System.Windows.Automation.AutomationProperties.SetName(result,"Latest action output");layout.Children.Add(result);}
        copy.Click+=async (_,_)=>{try{await AppState.CopyAsync(state.Result);StatusText.Text="Output copied.";}catch(System.Runtime.InteropServices.COMException){StatusText.Text="Clipboard is busy. Try again.";}};
        PageContent.Content=layout;
    }
    private bool SaveSettings() { try { state.Save(); StatusText.Text = "Saved."; return true; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { StatusText.Text = ex.Message; return false; } }
}
