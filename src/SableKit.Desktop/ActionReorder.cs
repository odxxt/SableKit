using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace SableKit.Desktop;

// Threshold, insertion marker and edge scrolling adapted from SableLatch RowInteractions.cs.
// See SABLELATCH-NOTICE.txt. Only the active local gesture is accepted.
internal sealed class ActionReorder
{
    private readonly FrameworkElement host;
    private readonly Func<IEnumerable<FrameworkElement>> rows;
    private readonly Func<bool> allowed;
    private readonly Action<string,int> move;
    private FrameworkElement? pending;
    private Point start;
    private string? active;
    private readonly string format="SableKit/reorder/"+Guid.NewGuid().ToString("N");
    private InsertionLine? marker;
    private AdornerLayer? layer;
    private int insertion=-1;
    private long lastScroll;
    private static long suppressUntil;
    internal static bool IsDragging { get; private set; }
    internal static bool SuppressClick => IsDragging || Environment.TickCount64<suppressUntil;
    internal static string? Id(FrameworkElement row) => row.Tag switch { string s=>s, ActionItem a=>a.Definition.Id, ActionDefinition a=>a.Id, _=>null };
    internal ActionReorder(FrameworkElement host,Func<IEnumerable<FrameworkElement>> rows,Func<bool> allowed,Action<string,int> move)
    {
        this.host=host;this.rows=rows;this.allowed=allowed;this.move=move;host.AllowDrop=true;
        host.PreviewMouseLeftButtonDown+=(_,e)=>
        {
            pending=null;if(!allowed())return;
            var source=e.OriginalSource as DependencyObject;
            if(source is null || TrayWindow.FindParent<TextBox>(source) is not null)return;
            pending=rows().FirstOrDefault(r=>ReferenceEquals(r,source)||r.IsAncestorOf(source));
            var button=TrayWindow.FindParent<Button>(source);
            if(button is not null && !ReferenceEquals(button,pending)) pending=null;
            if(pending is not null)start=e.GetPosition(host);
        };
        host.PreviewMouseMove+=(_,e)=>
        {
            if(e.LeftButton!=MouseButtonState.Pressed){pending=null;return;}
            if(pending is null || !allowed())return;
            var point=e.GetPosition(host);
            if(Math.Abs(point.X-start.X)<SystemParameters.MinimumHorizontalDragDistance && Math.Abs(point.Y-start.Y)<SystemParameters.MinimumVerticalDragDistance)return;
            var source=pending;pending=null;active=Id(source);if(active is null)return;
            IsDragging=true;Mouse.Capture(null);
            try{DragDrop.DoDragDrop(source,new DataObject(format,active),DragDropEffects.Move);}
            finally{active=null;IsDragging=false;suppressUntil=Environment.TickCount64+200;Clear();}
            e.Handled=true;
        };
        host.PreviewDragOver+=Over;
        host.QueryContinueDrag+=(_,e)=>{if(!allowed()||!host.IsVisible)e.Action=DragAction.Cancel;};
        host.PreviewDragLeave+=(_,e)=>{if(!host.IsMouseOver)Clear();};
        host.PreviewDrop+=(_,e)=>
        {
            Over(host,e);var at=insertion;Clear();
            if(at<0 || !Accept(e.Data,out var id))return;
            var before=rows().Where(r=>Id(r)is not null).ToDictionary(r=>Id(r)!,r=>r.TranslatePoint(new Point(),host));
            move(id,at);host.UpdateLayout();
            if(!UiMotion.Enabled)return;
            foreach(var row in rows())if(Id(row)is string key && before.TryGetValue(key,out var old))
            {
                var current=row.TranslatePoint(new Point(),host);var transform=new TranslateTransform();row.RenderTransform=transform;
                transform.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(old.X-current.X,0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut }});
                transform.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(old.Y-current.Y,0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut }});
            }
        };
        host.Unloaded+=(_,_)=>{pending=null;Clear();};
    }
    private bool Accept(IDataObject data,out string id)
    { id="";if(!allowed()||active is null||!data.GetDataPresent(format)||data.GetData(format)is not string value||value!=active)return false;id=value;return true; }
    private void Over(object sender,DragEventArgs e)
    {
        e.Handled=true;e.Effects=DragDropEffects.None;
        if(!Accept(e.Data,out _)){Clear();return;}
        var point=e.GetPosition(host);var items=rows().ToArray();if(items.Length==0)return;
        FrameworkElement target=items[^1];var after=true;insertion=items.Length;
        for(var i=0;i<items.Length;i++)
        {
            var row=items[i];var p=row.TranslatePoint(new Point(),host);
            var sameLine=point.Y>=p.Y && point.Y<=p.Y+row.ActualHeight;
            if(point.Y<p.Y || (sameLine && point.X<p.X+row.ActualWidth))
            {target=row;var multiple=items.Any(other=>!ReferenceEquals(row,other)&&Math.Abs(other.TranslatePoint(new Point(),host).Y-p.Y)<2);after=multiple?point.X>p.X+row.ActualWidth/2:point.Y>p.Y+row.ActualHeight/2;insertion=i+(after?1:0);break;}
        }
        var top=target.TranslatePoint(new Point(),host);
        layer??=AdornerLayer.GetAdornerLayer(host);
        if(marker is null && layer is not null){marker=new InsertionLine(host){IsHitTestVisible=false};layer.Add(marker);}
        if(marker is not null){marker.Start=new Point(top.X+3,top.Y+(after?target.ActualHeight:0));marker.End=new Point(top.X+target.ActualWidth-3,marker.Start.Y);marker.InvalidateVisual();}
        var scroll=host as ScrollViewer ?? TrayWindow.FindParent<ScrollViewer>(host);
        if(scroll is not null && Environment.TickCount64-lastScroll>=70)
        {
            var y=e.GetPosition(scroll).Y;if(y<18 || y>scroll.ActualHeight-18){lastScroll=Environment.TickCount64;SmoothScrolling.Wheel(scroll,y<18?40:-40);}
        }
        e.Effects=DragDropEffects.Move;
    }
    private void Clear(){if(marker is not null)layer?.Remove(marker);marker=null;layer=null;insertion=-1;}
    internal static bool Move(AppState state,Profile profile,string id,int insertion)
    {
        var from=profile.ActionIds.IndexOf(id);if(from<0||insertion<0||insertion>profile.ActionIds.Count)return false;
        var to=insertion>from?insertion-1:insertion;if(to==from)return false;
        var old=profile.ActionIds.ToArray();profile.ActionIds.RemoveAt(from);profile.ActionIds.Insert(to,id);
        try{state.Save();return true;}catch{profile.ActionIds.Clear();profile.ActionIds.AddRange(old);throw;}
    }
    private sealed class InsertionLine(UIElement host):Adorner(host)
    {
        internal Point Start,End;
        protected override void OnRender(DrawingContext dc){var brush=(Brush)Application.Current.FindResource("Text");dc.DrawLine(new Pen(brush,2),Start,End);dc.DrawEllipse(brush,null,Start,2,2);}
    }
}
