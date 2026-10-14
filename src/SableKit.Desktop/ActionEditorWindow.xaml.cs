namespace SableKit.Desktop;

public partial class ActionEditorWindow : Window
{
    private readonly AppState state;
    private readonly Profile profile;
    private bool dirty;
    internal Func<bool>? DiscardDecision { get; set; }
    internal bool HasDraft => dirty;
    internal string? EditingActionId { get; private set; }
    internal Button PreviewButton { get; private set; } = null!;
    internal ScrollViewer FormScroll { get; private set; } = null!;
    public ActionEditorWindow(AppState state, ActionDefinition source, bool isNew=false)
    {
        InitializeComponent(); this.state=state; profile=state.Config.ActiveProfile;
        SourceInitialized+=(_,_)=>{NativeWindow.Apply(this,false); FitToWorkArea(NativeWindow.WorkArea(this));};
        EditorContent.Content=BuildEditor(source); dirty=isNew;
        state.ResultChanged+=ResultChanged;
        Closing+=(_,e)=>{ if(!CanClose()) e.Cancel=true; };
        Closed+=(_,_)=>state.ResultChanged-=ResultChanged;
    }
    internal bool CanClose()=>!dirty || (DiscardDecision?.Invoke() ?? MessageBox.Show(this,"Discard the changes in this action?","Unsaved action",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes);
    internal void CloseApproved(){dirty=false;Close();}
    internal void FitToWorkArea(Rect area)
    {
        MinWidth=Math.Min(560,area.Width*.9); MinHeight=Math.Min(400,area.Height*.88);
        Width=Math.Min(660,area.Width*.9); Height=Math.Min(650,area.Height*.88);
        Left=area.Left+(area.Width-Width)/2; Top=area.Top+(area.Height-Height)/2;
    }
    private void DragHeader(object sender,MouseButtonEventArgs e) { if(TrayWindow.FindParent<Button>(e.OriginalSource as DependencyObject) is null && e.LeftButton==MouseButtonState.Pressed) DragMove(); }
    private void CloseEditor(object sender,RoutedEventArgs e)=>Close();
    private void ResultChanged()=>StatusText.Text=state.Status;
    private UIElement BuildEditor(ActionDefinition source)
    {
        EditingActionId = source.Id;
        var outer = new DockPanel();
        var previewArea = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        previewArea.ColumnDefinitions.Add(new ColumnDefinition());
        previewArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(308) });
        var previewControls = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,0) };
        previewControls.Children.Add(Ui.Text("Button preview",12,true));
        var modes = new StackPanel { Orientation=Orientation.Horizontal, Margin=new Thickness(0,8,0,0) };
        var listMode=Ui.Button("List",()=>{},""); var gridMode=Ui.Button("Grid",()=>{},"");
        listMode.FontSize=gridMode.FontSize=11; listMode.Padding=gridMode.Padding=new Thickness(8,4,8,4);
        modes.Children.Add(listMode); modes.Children.Add(gridMode); previewControls.Children.Add(modes); previewArea.Children.Add(previewControls);
        PreviewButton = new Button { HorizontalAlignment=HorizontalAlignment.Right, HorizontalContentAlignment=HorizontalAlignment.Stretch, Padding=new Thickness(9,6,9,6), IsHitTestVisible=false, Focusable=false };
        System.Windows.Automation.AutomationProperties.SetName(PreviewButton,"Action button preview, does not execute");
        Grid.SetColumn(PreviewButton,1); previewArea.Children.Add(PreviewButton);
        previewArea.Height=82; DockPanel.SetDock(previewArea,Dock.Top); outer.Children.Add(previewArea);
        var previewGrid = state.Config.ActiveProfile.GridView;
        var bottom = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) }; DockPanel.SetDock(bottom, Dock.Bottom); outer.Children.Add(bottom);
        var form = new StackPanel(); FormScroll=Ui.Scroll(form); outer.Children.Add(FormScroll);
        var name = Ui.Field(form, "Name", source.Name);
        var description = Ui.Field(form, "Description", source.Description);
        var aliases = Ui.Field(form, "Search aliases", source.Aliases, "Space-separated words that help you find this action.");
        var kind = Ui.Choice(form, "Action type", Enum.GetValues<ActionKind>().Cast<object>(), source.Kind);
        var options = new StackPanel(); form.Children.Add(options);
        TextBox? length = null, executable = null, arguments = null, target = null; CheckBox? inherit = null;
        void BuildOptions()
        {
            options.Children.Clear(); length = executable = arguments = target = null; inherit = null;
            switch ((ActionKind)kind.SelectedItem)
            {
                case ActionKind.Password:
                    options.Children.Add(Ui.Divider());
                    inherit = new CheckBox { Content = $"Use SableKit default ({state.Config.PasswordLength} characters)", IsChecked = source.PasswordLengthOverride is null };
                    options.Children.Add(inherit); length = Ui.Field(options, "Override for this action", (source.PasswordLengthOverride ?? state.Config.PasswordLength).ToString(), "8–128 random characters. Reset by choosing Use SableKit default.");
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
        var pinned = new CheckBox { Content = $"Pin in {profile.Name}", IsChecked = profile.ActionIds.Contains(source.Id) }; form.Children.Add(pinned);
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
            PreviewButton.Width=previewGrid?148:308; PreviewButton.Height=previewGrid?72:46;
            PreviewButton.SetResourceReference(BackgroundProperty,previewGrid?"Inset":"PanelSurface");
            PreviewButton.Content=ActionVisual.Create(new ActionItem(previewDefinition),state.Config,previewGrid);
            listMode.SetResourceReference(BackgroundProperty,previewGrid?"PanelSurface":"Hover");
            gridMode.SetResourceReference(BackgroundProperty,previewGrid?"Hover":"PanelSurface");
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
                state.Save(); dirty = false; EditorContent.Content = BuildEditor(draft); StatusText.Text = "Action saved.";
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
