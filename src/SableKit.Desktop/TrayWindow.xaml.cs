using System.ComponentModel;
using System.Windows.Automation;
namespace SableKit.Desktop;

public partial class TrayWindow : Window
{
    private readonly AppState state;
    private readonly PanelPlacement placement;
    private IReadOnlyList<DetectedTarget> suggestions=[];
    private bool positioned;
    private readonly ProfileWheelGate profileWheel = new();
    private int clipboardRequest;
    private long trayDeactivatedAt=long.MinValue/2;
    public bool Pinned { get; private set; }
    public bool VerificationMode { get; set; }
    public TrayWindow(AppState state)
    {
        InitializeComponent(); this.state=state;
        placement=new PanelPlacement(this,Path.Combine(state.DataDirectory,"window.json"));
        SourceInitialized+=(_,_)=>NativeWindow.Apply(this,true);
        Deactivated+=(_,_)=>HandleDeactivated(NativeWindow.PointerOverTray());
        Closing+=(_,e)=>{ if(!App.Exiting){ e.Cancel=true;Hide(); } };
        PreviewKeyDown+=KeyDownPanel;
        state.Changed+=Refresh; state.ResultChanged+=UpdateResult; state.HideRequested+=Hide;
        Refresh();
    }
    internal void HandleDeactivated(bool trayInteraction)
    {
        if(Pinned || VerificationMode) return;
        if(trayInteraction) trayDeactivatedAt=Environment.TickCount64;
        Hide();
    }
    internal void ToggleFromTray()
    {
        if(IsVisible || Environment.TickCount64-trayDeactivatedAt<500) { Hide(); trayDeactivatedAt=long.MinValue/2; }
        else Open();
    }
    internal Dictionary<string,double>? OpenTimings { get; set; }
    public void Open(bool position=true)
    {
        var timer = OpenTimings is null ? null : System.Diagnostics.Stopwatch.StartNew();
        void Mark(string stage) { if(timer is null) return; OpenTimings![stage]=timer.Elapsed.TotalMilliseconds; timer.Restart(); }
        ThemeManager.Apply(state.Config.Theme);
        if(position && !positioned) { placement.Restore(); positioned=true; }
        Mark("ThemeAndPlacement"); Show(); Mark("Show"); Activate(); Mark("Activate"); InspectClipboard(); Mark("ClipboardRequest");
        if(SearchBox.Visibility==Visibility.Visible) SearchBox.Focus();
    }
    internal void SavePosition() { placement.Save(); positioned=true; }
    internal void ResetPosition() { placement.Reset(); positioned=true; }
    private async void InspectClipboard()
    {
        var request=++clipboardRequest;
        suggestions=[]; Suggestion.Visibility=Visibility.Collapsed;
        try
        {
            var text=await AppState.ReadClipboardAsync();
            if(request!=clipboardRequest || !IsVisible) return;
            suggestions=ClipboardTargets.Extract(text);
            if(suggestions.Count==0) return;
            var detected=suggestions[0];
            SuggestionTitle.Text=suggestions.Count>1 ? "Choose a target" : detected.Error is not null ? "Review address" : detected.Target.Port is null ? "Ping address" : "Test TCP connection";
            SuggestionTarget.Text=suggestions.Count>1 ? $"{suggestions.Count} addresses in clipboard" : detected.Display;
            AutomationProperties.SetName(Suggestion,$"{SuggestionTitle.Text} {SuggestionTarget.Text}");
            Suggestion.Visibility=Visibility.Visible;
        }
        catch(System.Runtime.InteropServices.COMException) { }
    }
    public void Refresh()
    {
        ProfileName.Text=state.Config.ActiveProfile.Name;
        ProfileButton.ToolTip=$"{state.Config.ActiveProfile.Name} · {state.Config.Profiles.FindIndex(p=>p.Id==state.Config.ActiveProfileId)+1} of {state.Config.Profiles.Count}";
        PreviousProfile.IsEnabled=NextProfile.IsEnabled=state.Config.Profiles.Count>1;
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

            var content = ActionVisual.Create(item, state.Config, grid, index);

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

    private void ItemChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(ActionItem.State)) RenderActions(); }

    private void UpdateResult()
    {
        if(state.LastActionKind==ActionKind.Password && !state.LastFailed) return;
        ActionsRegion.Visibility=Visibility.Collapsed; ResultRegion.Visibility=Visibility.Visible;
        ResultTitle.Text=state.LastActionName; ResultOutput.Text=state.Result; ResultOutput.ScrollToEnd(); ResultStatus.Text=state.Status;
        ResultButton.Content=state.LastActionKind==ActionKind.Network?"Open SableNet":"Details";
    }
    private void DismissResult(object sender,RoutedEventArgs e) { ResultRegion.Visibility=Visibility.Collapsed; ActionsRegion.Visibility=Visibility.Visible; }
    public void TogglePin(object? sender,RoutedEventArgs? e)
    {
        Pinned=!Pinned; Topmost=Pinned;
        if(Pinned) PinButton.SetResourceReference(BackgroundProperty,"Hover"); else PinButton.Background=Brushes.Transparent;
        PinButton.ToolTip=Pinned?"Unpin · hide when focus moves away":"Pin · keep open"; AutomationProperties.SetName(PinButton,Pinned?"Unpin panel":"Pin panel");
    }
    private void ToggleSearch(object sender,RoutedEventArgs e)=>SetSearchVisible(SearchBox.Visibility!=Visibility.Visible);
    private void SetSearchVisible(bool visible)
    {
        DismissResult(this,new RoutedEventArgs()); SearchBox.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        if(visible) SearchButton.SetResourceReference(BackgroundProperty,"Hover"); else { SearchButton.Background=Brushes.Transparent; SearchBox.Text=""; }
        SearchButton.ToolTip=visible?"Close search":"Search"; if(visible) SearchBox.Focus();
    }
    private void SearchChanged(object sender,TextChangedEventArgs e) { if(state is not null) RenderActions(); }
    public void ToggleView(object? sender,RoutedEventArgs? e) { state.Config.ActiveProfile.GridView=!state.Config.ActiveProfile.GridView; Save(); }
    internal void CycleProfile(int direction)
    {
        var profiles=state.Config.Profiles; var index=profiles.FindIndex(p=>p.Id==state.Config.ActiveProfileId);
        state.Config.ActiveProfileId=profiles[(index+direction+profiles.Count)%profiles.Count].Id; Save();
    }
    private void Next(object sender,RoutedEventArgs e)=>CycleProfile(1);
    private void Previous(object sender,RoutedEventArgs e)=>CycleProfile(-1);
    private void ProfileWheel(object sender,MouseWheelEventArgs e) { var direction=profileWheel.Step(e.Delta,Environment.TickCount64); if(direction!=0) CycleProfile(direction); e.Handled=true; }
    private void ProfileKeys(object sender,KeyEventArgs e) { if(e.Key is Key.Left or Key.Right) { if(!e.IsRepeat) CycleProfile(e.Key==Key.Right?1:-1);e.Handled=true; } }
    private void Save() { try{state.Save();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){state.SetResult("Settings could not be saved",ex.Message,true);} }
    private async void RunSuggestion(object sender,RoutedEventArgs e)
    {
        if(suggestions.Count!=1 || suggestions[0].Error is not null) { Hide(); ((App)Application.Current).OpenNetwork(candidates:suggestions); return; }
        await state.ExecuteAsync(new ActionItem(new ActionDefinition{Name=SuggestionTitle.Text,Kind=ActionKind.Network}),suggestions[0].Target.Display);
    }
    private void DragHeader(object sender,MouseButtonEventArgs e)
    {
        if(FindParent<Button>(e.OriginalSource as DependencyObject) is not null) return;
        try { if(e.ClickCount==2 && FindParent<TextBlock>(e.OriginalSource as DependencyObject)==PanelTitle) { ResetPosition();e.Handled=true; } else { DragMove();SavePosition(); } }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException) { state.SetResult("Position could not be saved",ex.Message,true); }
    }
    internal static T? FindParent<T>(DependencyObject? source) where T:DependencyObject
    {
        while(source is not null) { if(source is T match)return match; source=source is Visual?VisualTreeHelper.GetParent(source):LogicalTreeHelper.GetParent(source); } return null;
    }
    private void HideClick(object sender,RoutedEventArgs e)=>Hide();
    private void OpenMain(object sender,RoutedEventArgs e) { Hide();((App)Application.Current).OpenMain(); }
    private void OpenResult(object sender,RoutedEventArgs e)
    {
        Hide();
        if(state.LastActionKind==ActionKind.Network) ((App)Application.Current).OpenNetwork(state.LastNetworkTarget,state.Result,state.LastCandidates);
        else ((App)Application.Current).OpenMain("Activity");
    }
    private void OpenNetwork(object sender,RoutedEventArgs e) { Hide();((App)Application.Current).OpenNetwork(); }
    private async void KeyDownPanel(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape){if(state.IsRunning)state.CancelAll();else if(ResultRegion.IsVisible)DismissResult(this,e);else if(SearchBox.IsVisible)SetSearchVisible(false);else Hide();e.Handled=true;}
        if(e.Key==Key.F && Keyboard.Modifiers==ModifierKeys.Control){SetSearchVisible(true);e.Handled=true;}
        var key=e.Key==Key.System?e.SystemKey:e.Key;
        if(Keyboard.Modifiers==ModifierKeys.Alt && key>=Key.D1 && key<=Key.D9 && ActionsRegion.IsVisible)
        { var index=key-Key.D1; if(index<ActionsList.Items.Count && ActionsList.Items[index] is Button b && b.Tag is ActionItem item) await state.ExecuteAsync(item);e.Handled=true; }
    }
}
