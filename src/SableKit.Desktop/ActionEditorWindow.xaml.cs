namespace SableKit.Desktop;

public partial class ActionEditorWindow : Window
{
    private readonly AppState state;
    private Profile profile;
    private bool dirty;
    internal Func<bool>? DiscardDecision { get; set; }
    internal bool HasDraft => dirty;
    internal event Action<string>? Saved;
    internal string? EditingActionId { get; private set; }
    internal Button PreviewButton { get; private set; } = null!;
    internal ScrollViewer FormScroll { get; private set; } = null!;
    public ActionEditorWindow(AppState state, ActionDefinition source, bool isNew=false)
    {
        InitializeComponent(); this.state=state; profile=state.Config.ActiveProfile;
        SourceInitialized+=(_,_)=>{NativeWindow.Apply(this,false); FitToWorkArea(NativeWindow.WorkArea(this));};
        LoadAction(source,isNew);
        state.ResultChanged+=ResultChanged;
        Closing+=(_,e)=>{ if(!CanClose()) e.Cancel=true; };
        Closed+=(_,_)=>state.ResultChanged-=ResultChanged;
    }
    internal bool TryEdit(ActionDefinition source,bool isNew=false)
    {
        if(!CanClose()) return false;
        profile=state.Config.ActiveProfile; LoadAction(source,isNew); return true;
    }
    private void LoadAction(ActionDefinition source,bool isNew)
    {
        StatusText.Text=""; dirty=false;
        if(isNew) ShowTemplates(); else EditorContent.Content=BuildEditor(source);
    }
    private void ShowTemplates()
    {
        EditingActionId=null;EditorTitle.Text="New action";
        var body=new StackPanel { Margin=new Thickness(0,22,0,0) };
        body.Children.Add(Ui.Text("What would you like to do?",24));
        var intro=Ui.Text("Pick a starting point. Make it yours in the next step.",13,true);intro.Margin=new Thickness(0,8,0,24);body.Children.Add(intro);
        var choices=new System.Windows.Controls.Primitives.UniformGrid { Columns=3 };
        foreach(var (kind,title,detail,icon) in new[]{(ActionKind.Password,"Generate password","Create and copy a fresh password.","key"),(ActionKind.Application,"Open application","Launch an app with your own arguments.","terminal"),(ActionKind.Network,"Check connection","Ping an address or test a TCP port.","network")})
        {
            var content=new StackPanel();content.Children.Add(new Icon { Kind=icon,Width=26,Height=26,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,18) });
            content.Children.Add(Ui.Text(title,14));var note=Ui.Text(detail,12,true);note.Margin=new Thickness(0,8,0,0);content.Children.Add(note);
            var button=new Button { Content=content,Height=172,Padding=new Thickness(16),Margin=new Thickness(0,0,12,0),HorizontalContentAlignment=HorizontalAlignment.Stretch,Tag=kind };
            button.SetResourceReference(BackgroundProperty,"Inset");System.Windows.Automation.AutomationProperties.SetName(button,title);
            button.Click+=(_,_)=>{EditorContent.Content=BuildEditor(new ActionDefinition { Name=title,Kind=kind });dirty=true;};choices.Children.Add(button);
        }
        body.Children.Add(choices);EditorContent.Content=Ui.Scroll(body);
    }
    internal bool CanClose()=>!dirty || (DiscardDecision?.Invoke() ?? MessageBox.Show(this,"Discard the changes in this action?","Unsaved action",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes);
    internal void CloseApproved(){dirty=false;Close();}
    internal void FitToWorkArea(Rect area)
    {
        MinWidth=Math.Min(700,area.Width*.9); MinHeight=Math.Min(420,area.Height*.88);
        Width=Math.Min(800,area.Width*.9); Height=Math.Min(540,area.Height*.88);
        Left=area.Left+(area.Width-Width)/2; Top=area.Top+(area.Height-Height)/2;
    }
    private void DragHeader(object sender,MouseButtonEventArgs e) { if(TrayWindow.FindParent<Button>(e.OriginalSource as DependencyObject) is null && e.LeftButton==MouseButtonState.Pressed) DragMove(); }
    private void CloseEditor(object sender,RoutedEventArgs e)=>Close();
    private void ResultChanged()=>StatusText.Text=state.Status;
    private UIElement BuildEditor(ActionDefinition source)
    {
        EditingActionId = source.Id;
        EditorTitle.Text=state.Config.Actions.Any(a=>a.Id==source.Id)?"Edit action":"Create action";
        var outer=new DockPanel();
        var bottom=new WrapPanel { Margin=new Thickness(0,16,0,0) }; DockPanel.SetDock(bottom,Dock.Bottom); outer.Children.Add(bottom);
        var layout=new Grid(); layout.ColumnDefinitions.Add(new ColumnDefinition()); layout.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(242) }); outer.Children.Add(layout);
        var side=new StackPanel { Margin=new Thickness(22,10,0,0) }; Grid.SetColumn(side,1); layout.Children.Add(side);
        side.Children.Add(Ui.Text("Live preview",12,true));
        PreviewButton=new Button { HorizontalAlignment=HorizontalAlignment.Left, HorizontalContentAlignment=HorizontalAlignment.Stretch, Padding=new Thickness(9,6,9,6), Margin=new Thickness(0,12,0,14), IsHitTestVisible=false, Focusable=false };
        System.Windows.Automation.AutomationProperties.SetName(PreviewButton,"Action button preview, does not execute"); side.Children.Add(PreviewButton);
        var modes=new StackPanel { Orientation=Orientation.Horizontal };
        var listMode=Ui.Button("List",()=>{},""); var gridMode=Ui.Button("Grid",()=>{},"");
        listMode.Width=gridMode.Width=70; modes.Children.Add(listMode); modes.Children.Add(gridMode); side.Children.Add(modes);
        var previewNote=Ui.Text("This is how your action will appear in the quick panel.",12,true);previewNote.Margin=new Thickness(0,18,0,0);side.Children.Add(previewNote);
        var previewGrid=state.Config.ActiveProfile.GridView;
        var formHost=new DockPanel(); layout.Children.Add(formHost);
        var tabs=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,6) }; DockPanel.SetDock(tabs,Dock.Top);formHost.Children.Add(tabs);
        var form=new Grid(); var setup=new StackPanel();var behavior=new StackPanel { Visibility=Visibility.Collapsed };form.Children.Add(setup);form.Children.Add(behavior);
        FormScroll=Ui.Scroll(form);formHost.Children.Add(FormScroll);
        var setupTab=Ui.Button("Setup",()=>{},"TabButton");var behaviorTab=Ui.Button("Behavior",()=>{},"TabButton");
        setupTab.Width=behaviorTab.Width=105;tabs.Children.Add(setupTab);tabs.Children.Add(behaviorTab);
        void SelectSection(bool advanced){setup.Visibility=advanced?Visibility.Collapsed:Visibility.Visible;behavior.Visibility=advanced?Visibility.Visible:Visibility.Collapsed;setupTab.SetResourceReference(BackgroundProperty,advanced?"Surface":"Inset");behaviorTab.SetResourceReference(BackgroundProperty,advanced?"Inset":"Surface");FormScroll.ScrollToTop();}
        setupTab.Click+=(_,_)=>SelectSection(false);behaviorTab.Click+=(_,_)=>SelectSection(true);SelectSection(false);
        var name=Ui.Field(setup,"Name",source.Name);
        var description=Ui.Field(setup,"Description",source.Description);
        var kind=Ui.Choice(behavior,"Action type",Enum.GetValues<ActionKind>().Cast<object>(),source.Kind);
        var aliases=Ui.Field(behavior,"Search aliases",source.Aliases,"Words that help you find this action.");
        var options=new StackPanel();setup.Children.Add(options);
        TextBox? length = null, executable = null, arguments = null, target = null; CheckBox? inherit = null;
        void BuildOptions()
        {
            options.Children.Clear(); length = executable = arguments = target = null; inherit = null;
            switch ((ActionKind)kind.SelectedItem)
            {
                case ActionKind.Password:
                    options.Children.Add(new Border { Height=12 });
                    inherit = new CheckBox { Content = $"Use SableKit default ({state.Config.PasswordLength} characters)", IsChecked = source.PasswordLengthOverride is null };
                    options.Children.Add(inherit); length = Ui.Field(options, "Override for this action", (source.PasswordLengthOverride ?? state.Config.PasswordLength).ToString(), "8–128 characters. Uncheck the default to customize.");
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
        var panel = Ui.Choice(behavior, "After invoking", Enum.GetValues<PanelBehavior>().Cast<object>(), source.PanelBehavior);
        var enabled = new CheckBox { Content = "Action enabled", IsChecked = source.Enabled }; behavior.Children.Add(enabled);
        var pinned = new CheckBox { Content = $"Pin in {profile.Name}", IsChecked = profile.ActionIds.Contains(source.Id) || !state.Config.Actions.Any(a=>a.Id==source.Id) }; behavior.Children.Add(pinned);
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
            var previewDefinition = new ActionDefinition { Name=string.IsNullOrWhiteSpace(name.Text)?"New action":name.Text,
                Description=description.Text, Kind=(ActionKind)kind.SelectedItem,
                PasswordLengthOverride=inherit?.IsChecked==false && int.TryParse(length?.Text,out var count) && count is >=8 and <=128 ? count : null };
            PreviewButton.Width=previewGrid?148:220; PreviewButton.Height=previewGrid?72:46;
            PreviewButton.SetResourceReference(BackgroundProperty,previewGrid?"Inset":"Surface");
            PreviewButton.Content=ActionVisual.Create(new ActionItem(previewDefinition),state.Config,previewGrid);
            listMode.SetResourceReference(BackgroundProperty,previewGrid?"Surface":"Hover");
            gridMode.SetResourceReference(BackgroundProperty,previewGrid?"Hover":"Surface");
        }
        listMode.Click+=(_,_)=>{previewGrid=false;Preview();};
        gridMode.Click+=(_,_)=>{previewGrid=true;Preview();};
        kind.SelectionChanged+=(_,_)=>Preview();
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
                var ids = profile.ActionIds; if (pinned.IsChecked == true && !ids.Contains(draft.Id)) ids.Add(draft.Id); if (pinned.IsChecked != true) ids.Remove(draft.Id);
                state.Save(); dirty = false; Saved?.Invoke(draft.Id); Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException) { StatusText.Text = ex.Message; }
        }, "PrimaryButton"); bottom.Children.Add(save);
        var test = Ui.Button("Test run", () => { }); test.Click += async (_, _) => { try { var draft = Draft(); if (!draft.Enabled) throw new InvalidOperationException("Enable the action before testing it."); await state.ExecuteAsync(new ActionItem(draft)); } catch (InvalidOperationException ex) { StatusText.Text = ex.Message; } }; bottom.Children.Add(test);
        if (state.Config.Actions.Any(a => a.Id == source.Id))
        {
            var duplicate = Ui.Button("Duplicate", () =>
            {
                try { var draft = Draft(); draft.Id = Guid.NewGuid().ToString("N"); draft.Name += " copy"; EditorContent.Content = BuildEditor(draft); dirty = true; StatusText.Text = "Unsaved copy of the current settings. Save action to add it to the library."; }
                catch (InvalidOperationException ex) { StatusText.Text = ex.Message; }
            });
            duplicate.ToolTip = "Create an unsaved copy of the current editor settings"; bottom.Children.Add(duplicate);
        }
        return outer;
    }
}
