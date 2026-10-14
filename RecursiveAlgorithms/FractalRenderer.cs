using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FractalDrawer
{
    public static class FractalRenderer
    {
        public static UIElement CreateImage(WriteableBitmap bitmap)
        {
            if (bitmap == null) return null;

            return new Image
            {
                Source = bitmap,
                Width = bitmap.Width,
                Height = bitmap.Height,
                Stretch = Stretch.None
            };
        }
    }
}