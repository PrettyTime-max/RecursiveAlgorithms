using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FractalDrawer
{
    public interface IFractalAlgorithm
    {
        Task<WriteableBitmap> CalculateBitmapAsync(int canvasWidth, int canvasHeight, int depth);
    }

    public class DragonCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth)
        {
            if (depth > 25) depth = 25; // Ограничение глубины

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];

                double x1 = width / 2.0 - 80;
                double y1 = height / 2.0;
                double x2 = width / 2.0 + 80;
                double y2 = height / 2.0;

                DrawDragonToBuffer(pixels, stride, width, height, x1, y1, x2, y2, depth, true);

                // Копируем готовый массив байтов в видеопамять Bitmap за один проход
                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawDragonToBuffer(byte[] pixels, int stride, int width, int height, double x1, double y1, double x2, double y2, int depth, bool left)
        {
            if (depth == 0)
            {
                DrawLine(pixels, stride, width, height, (int)x1, (int)y1, (int)x2, (int)y2);
            }
            else
            {
                double x3 = (x1 + x2) / 2.0 + (y2 - y1) / (left ? 2.0 : -2.0);
                double y3 = (y1 + y2) / 2.0 - (x2 - x1) / (left ? 2.0 : -2.0);

                DrawDragonToBuffer(pixels, stride, width, height, x1, y1, x3, y3, depth - 1, true);
                DrawDragonToBuffer(pixels, stride, width, height, x3, y3, x2, y2, depth - 1, false);
            }
        }

        private void DrawLine(byte[] pixels, int stride, int maxWidth, int maxHeight, int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < maxWidth && y0 >= 0 && y0 < maxHeight)
                {
                    int index = y0 * stride + x0 * 4;
                    if (index >= 0 && index < pixels.Length - 3)
                    {
                        // Цвет: DodgerBlue (B=255, G=144, R=30, A=255)
                        pixels[index] = 255;     // Blue
                        pixels[index + 1] = 144; // Green
                        pixels[index + 2] = 30;  // Red
                        pixels[index + 3] = 255; // Alpha
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}