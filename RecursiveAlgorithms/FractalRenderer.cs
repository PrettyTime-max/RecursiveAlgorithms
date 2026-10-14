using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FractalDrawer
{
    public static class FractalRenderer
    {
        public static UIElement CreatePath(StreamGeometry geometry)
        {
            if (geometry == null) return null;

            return new Path
            {
                Data = geometry,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1.0
            };
        }
    }
}