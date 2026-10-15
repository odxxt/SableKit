using System.Windows.Controls.Primitives;

namespace SableKit.Desktop;

internal static class WindowResize
{
    internal static void Attach(Window window)
    {
        var content=(UIElement)window.Content;window.Content=null;var root=new Grid();root.Children.Add(content);window.Content=root;
        foreach(var (x,y) in new[]{(-1,-1),(0,-1),(1,-1),(-1,0),(1,0),(-1,1),(0,1),(1,1)})
        {
            var handle=new Thumb{Width=x==0?double.NaN:8,Height=y==0?double.NaN:8,HorizontalAlignment=x<0?HorizontalAlignment.Left:x>0?HorizontalAlignment.Right:HorizontalAlignment.Stretch,VerticalAlignment=y<0?VerticalAlignment.Top:y>0?VerticalAlignment.Bottom:VerticalAlignment.Stretch,Cursor=x==0?Cursors.SizeNS:y==0?Cursors.SizeWE:x==y?Cursors.SizeNWSE:Cursors.SizeNESW,Focusable=false};
            var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.BackgroundProperty,Brushes.Transparent);handle.Template=new ControlTemplate(typeof(Thumb)){VisualTree=border};
            if(x==0)handle.Margin=new Thickness(8,0,8,0);if(y==0)handle.Margin=new Thickness(0,8,0,8);
            handle.DragDelta+=(_,e)=>Resize(window,x,y,e.HorizontalChange,e.VerticalChange);root.Children.Add(handle);
        }
    }
    internal static void Resize(Window window,int x,int y,double dx,double dy)
    {
        if(window.WindowState!=WindowState.Normal||!window.IsEnabled)return;
        if(x!=0){var old=window.Width;var width=Math.Clamp(old+x*dx,window.MinWidth,window.MaxWidth);if(x<0)window.Left+=old-width;window.Width=width;}
        if(y!=0){var old=window.Height;var height=Math.Clamp(old+y*dy,window.MinHeight,window.MaxHeight);if(y<0)window.Top+=old-height;window.Height=height;}
    }
}
