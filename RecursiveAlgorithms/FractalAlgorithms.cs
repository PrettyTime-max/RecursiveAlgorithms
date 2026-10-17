using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RecursiveAlgorithms
{
    public interface IFractalAlgorithm
    {
        Task<WriteableBitmap> CalculateBitmapAsync(int canvasWidth, int canvasHeight, int depth);
    }

    // 1. Кривая Дракона
    public class DragonCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth)
        {
            if (depth < 0) depth = 0;
            if (depth > 25) depth = 25;

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

    // 2. Треугольник Серпинского (чисто зеленый)
    public class SierpinskiTriangleAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth)
        {
            if (depth < 0) depth = 0;
            if (depth > 12) depth = 12;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];

                double side = 400;
                double h = side * Math.Sqrt(3) / 2;

                Point p1 = new Point(width / 2.0, height / 2.0 - h / 2);
                Point p2 = new Point(width / 2.0 - side / 2.0, height / 2.0 + h / 2);
                Point p3 = new Point(width / 2.0 + side / 2.0, height / 2.0 + h / 2);

                DrawSierpinski(pixels, stride, width, height, p1, p2, p3, depth);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawSierpinski(byte[] pixels, int stride, int width, int height, Point p1, Point p2, Point p3, int depth)
        {
            if (depth == 0)
            {
                DrawLine(pixels, stride, width, height, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
                DrawLine(pixels, stride, width, height, (int)p2.X, (int)p2.Y, (int)p3.X, (int)p3.Y);
                DrawLine(pixels, stride, width, height, (int)p3.X, (int)p3.Y, (int)p1.X, (int)p1.Y);
            }
            else
            {
                Point mid12 = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
                Point mid23 = new Point((p2.X + p3.X) / 2.0, (p2.Y + p3.Y) / 2.0);
                Point mid31 = new Point((p3.X + p1.X) / 2.0, (p3.Y + p1.Y) / 2.0);

                DrawSierpinski(pixels, stride, width, height, p1, mid12, mid31, depth - 1);
                DrawSierpinski(pixels, stride, width, height, mid12, p2, mid23, depth - 1);
                DrawSierpinski(pixels, stride, width, height, mid31, mid23, p3, depth - 1);
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
                        pixels[index] = 0;       // Blue
                        pixels[index + 1] = 255; // Green
                        pixels[index + 2] = 0;   // Red
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