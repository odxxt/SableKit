using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace SableKit.Desktop;

internal static class UiMotion
{
    internal static bool Enabled=>SystemParameters.ClientAreaAnimation && !((App)Application.Current).State.Config.ReducedMotion;
    internal static void Saved(FrameworkElement target)
    {
        if(!Enabled || !target.IsVisible)return;
        var layer=AdornerLayer.GetAdornerLayer(target);if(layer is null)return;
        var gleam=new SaveGleam(target){IsHitTestVisible=false};layer.Add(gleam);
        var animation=new DoubleAnimation(-1,1,TimeSpan.FromMilliseconds(650)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};
        var removed=false;
        void Remove(){if(removed)return;removed=true;gleam.BeginAnimation(SaveGleam.ProgressProperty,null);layer.Remove(gleam);target.IsVisibleChanged-=VisibilityChanged;}
        void VisibilityChanged(object sender,DependencyPropertyChangedEventArgs e){if(!target.IsVisible)Remove();}
        target.IsVisibleChanged+=VisibilityChanged;animation.Completed+=(_,_)=>Remove();gleam.BeginAnimation(SaveGleam.ProgressProperty,animation);
    }
    private sealed class SaveGleam(UIElement host):Adorner(host)
    {
        internal static readonly DependencyProperty ProgressProperty=DependencyProperty.Register("Progress",typeof(double),typeof(SaveGleam),new FrameworkPropertyMetadata(-1d,FrameworkPropertyMetadataOptions.AffectsRender));
        protected override void OnRender(DrawingContext dc)
        {
            var width=ActualWidth;var progress=(double)GetValue(ProgressProperty);var color=((SolidColorBrush)Application.Current.FindResource("Text")).Color;
            var brush=new LinearGradientBrush(new GradientStopCollection{new(Color.FromArgb(0,color.R,color.G,color.B),0),new(Color.FromArgb(40,color.R,color.G,color.B),.5),new(Color.FromArgb(0,color.R,color.G,color.B),1)},new Point(0,0),new Point(1,0));
            dc.PushClip(new RectangleGeometry(new Rect(0,0,width,ActualHeight),12,12));dc.DrawRectangle(brush,null,new Rect(progress*width,0,width,ActualHeight));dc.Pop();
        }
    }
}
