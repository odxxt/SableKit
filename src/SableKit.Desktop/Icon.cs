namespace SableKit.Desktop;

public sealed class Icon : FrameworkElement
{
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Icon), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Icon() { Width = 18; Height = 18; SetResourceReference(StrokeProperty, "Text"); }
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(string), typeof(Icon), new FrameworkPropertyMetadata("spark", FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    private static readonly Dictionary<string, Geometry> Shapes = new()
    {
        ["search"] = Geometry.Parse("M10,3 A7,7 0 1 1 9.99,3 M15,15 L21,21"),
        ["pin"] = Geometry.Parse("M8,3 L16,3 M9,3 L9,9 L6,13 L18,13 L15,9 L15,3 M12,13 L12,21"),
        ["close"] = Geometry.Parse("M6,6 L18,18 M18,6 L6,18"),
        ["key"] = Geometry.Parse("M9,4 A5,5 0 1 1 8.99,4 M12.5,12.5 L21,21 M17,17 L20,14 M19,19 L22,16"),
        ["terminal"] = Geometry.Parse("M4,5 L20,5 L20,19 L4,19 Z M7,9 L10,12 L7,15 M13,15 L17,15"),
        ["network"] = Geometry.Parse("M12,2 A10,10 0 1 1 11.99,2 M2,12 L22,12 M12,2 C5,7 5,17 12,22 M12,2 C19,7 19,17 12,22"),
        ["check"] = Geometry.Parse("M5,12 L10,17 L20,6"),
        ["grid"] = Geometry.Parse("M4,4 L10,4 L10,10 L4,10 Z M14,4 L20,4 L20,10 L14,10 Z M4,14 L10,14 L10,20 L4,20 Z M14,14 L20,14 L20,20 L14,20 Z"),
        ["list"] = Geometry.Parse("M4,5 L6,5 M10,5 L20,5 M4,12 L6,12 M10,12 L20,12 M4,19 L6,19 M10,19 L20,19"),
        ["settings"] = Geometry.Parse("M4,6 L20,6 M4,12 L20,12 M4,18 L20,18 M8,3 L8,9 M16,9 L16,15 M10,15 L10,21"),
        ["arrow"] = Geometry.Parse("M5,12 L19,12 M13,6 L19,12 L13,18"),
        ["expand"] = Geometry.Parse("M13,4 L20,4 L20,11 M20,4 L10,14 M9,4 L4,4 L4,20 L20,20 L20,15"),
        ["plus"] = Geometry.Parse("M12,4 L12,20 M4,12 L20,12"),
        ["profiles"] = Geometry.Parse("M8,3 A4,4 0 1 1 7.99,3 M2,21 L2,18 C2,13 14,13 14,18 L14,21 M16,5 C22,5 22,12 16,12 M18,15 C22,15 22,18 22,21"),
        ["lock"] = Geometry.Parse("M7,10 L7,7 C7,0 17,0 17,7 L17,10 M5,10 L19,10 L19,21 L5,21 Z M12,14 L12,17"),
        ["info"] = Geometry.Parse("M12,2 A10,10 0 1 1 11.99,2 M12,10 L12,17 M12,6 L12,7"),
        ["spark"] = Geometry.Parse("M14,2 L5,14 L11,14 L10,22 L20,9 L13,9 Z"),
        ["stop"] = Geometry.Parse("M6,6 L18,6 L18,18 L6,18 Z"),
        ["copy"] = Geometry.Parse("M8,8 L20,8 L20,21 L8,21 Z M16,8 L16,3 L3,3 L3,16 L8,16")
    };
    static Icon() { foreach (var geometry in Shapes.Values) geometry.Freeze(); }
    protected override Size MeasureOverride(Size availableSize) => new(double.IsNaN(Width) ? 18 : Width, double.IsNaN(Height) ? 18 : Height);
    protected override void OnRender(DrawingContext dc)
    {
        var pen = new Pen(Stroke, 1.45) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        dc.PushTransform(new ScaleTransform(ActualWidth / 24, ActualHeight / 24));
        dc.DrawGeometry(null, pen, Shapes.GetValueOrDefault(Kind, Shapes["spark"])); dc.Pop();
    }
}
