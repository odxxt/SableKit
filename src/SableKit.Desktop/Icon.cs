using System.Globalization;
namespace SableKit.Desktop;

public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Icon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Icon() { Width = 18; Height = 18; SetResourceReference(StrokeProperty, "Text"); }
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(string), typeof(Icon), new FrameworkPropertyMetadata("spark", FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    private static readonly Dictionary<string, string> Glyphs = new()
    {
        ["search"]="\uE721", ["pin"]="\uE718", ["close"]="\uE8BB", ["key"]="\uE8D7",
        ["terminal"]="\uE756", ["network"]="\uE774", ["check"]="\uE73E", ["grid"]="\uE80A",
        ["list"]="\uE8FD", ["settings"]="\uE713", ["arrow"]="\uE72A", ["expand"]="\uE8A7",
        ["plus"]="\uE710", ["profiles"]="\uE77B", ["lock"]="\uE72E", ["info"]="\uE946",
        ["spark"]="\uE80F", ["stop"]="\uE71A", ["copy"]="\uE8C8"
    };
    protected override Size MeasureOverride(Size availableSize) => new(double.IsNaN(Width) ? 18 : Width, double.IsNaN(Height) ? 18 : Height);
    protected override void OnRender(DrawingContext dc)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if(Kind is "grid" or "list")
        {
            var pen=new Pen(Stroke,1.25);var scale=Math.Min(ActualWidth,ActualHeight)/18;
            dc.PushTransform(new ScaleTransform(scale,scale));
            if(Kind=="grid") foreach(var x in new[]{2.0,10.0}) foreach(var y in new[]{2.0,10.0}) dc.DrawRoundedRectangle(null,pen,new Rect(x,y,6,6),1,1);
            else foreach(var y in new[]{3.0,9.0,15.0}) {dc.DrawRoundedRectangle(Stroke,null,new Rect(2,y-1,2,2),.5,.5);dc.DrawLine(pen,new Point(7,y),new Point(16,y));}
            dc.Pop();return;
        }
        var text = new FormattedText(Glyphs.GetValueOrDefault(Kind, "\uE80F"), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe MDL2 Assets"), Math.Min(ActualWidth, ActualHeight), Stroke, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point((ActualWidth-text.Width)/2, (ActualHeight-text.Height)/2));
    }
}
