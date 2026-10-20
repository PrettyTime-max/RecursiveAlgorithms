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
        Task<WriteableBitmap> CalculateBitmapAsync(int canvasWidth, int canvasHeight, int depth, double zoom = 1.0, double panX = 0, double panY = 0);
    }

    // Вспомогательный класс для быстрой графики, клиппинга и рисования линий
    internal static class FractalGraphicsHelper
    {
        private const int INSIDE = 0;
        private const int LEFT = 1;
        private const int RIGHT = 2;
        private const int BOTTOM = 4;
        private const int TOP = 8;

        public static void DrawClippedLine(byte[] pixels, int stride, int width, int height,
            double x0, double y0, double x1, double y1, byte r, byte g, byte b)
        {
            if (ClipLine(ref x0, ref y0, ref x1, ref y1, 0, 0, width - 1, height - 1))
            {
                DrawLine(pixels, stride, width, height, (int)Math.Round(x0), (int)Math.Round(y0), (int)Math.Round(x1), (int)Math.Round(y1), r, g, b);
            }
        }

        public static void DrawLine(byte[] pixels, int stride, int maxWidth, int maxHeight, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
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
                        pixels[index] = b;     // Blue
                        pixels[index + 1] = g; // Green
                        pixels[index + 2] = r; // Red
                        pixels[index + 3] = 255;
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

        private static int ComputeOutCode(double x, double y, int xmin, int ymin, int xmax, int ymax)
        {
            int code = INSIDE;
            if (x < xmin) code |= LEFT;
            else if (x > xmax) code |= RIGHT;
            if (y < ymin) code |= TOP;
            else if (y > ymax) code |= BOTTOM;
            return code;
        }

        private static bool ClipLine(ref double x0, ref double y0, ref double x1, ref double y1, int xmin, int ymin, int xmax, int ymax)
        {
            int outcode0 = ComputeOutCode(x0, y0, xmin, ymin, xmax, ymax);
            int outcode1 = ComputeOutCode(x1, y1, xmin, ymin, xmax, ymax);

            while (true)
            {
                if ((outcode0 | outcode1) == 0) return true;
                if ((outcode0 & outcode1) != 0) return false;

                int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;
                double x = 0, y = 0;

                if ((outcodeOut & TOP) != 0)
                {
                    x = x0 + (x1 - x0) * (ymin - y0) / (y1 - y0);
                    y = ymin;
                }
                else if ((outcodeOut & BOTTOM) != 0)
                {
                    x = x0 + (x1 - x0) * (ymax - y0) / (y1 - y0);
                    y = ymax;
                }
                else if ((outcodeOut & RIGHT) != 0)
                {
                    y = y0 + (y1 - y0) * (xmax - x0) / (x1 - x0);
                    x = xmax;
                }
                else if ((outcodeOut & LEFT) != 0)
                {
                    y = y0 + (y1 - y0) * (xmin - x0) / (x1 - x0);
                    x = xmin;
                }

                if (outcodeOut == outcode0)
                {
                    x0 = x;
                    y0 = y;
                    outcode0 = ComputeOutCode(x0, y0, xmin, ymin, xmax, ymax);
                }
                else
                {
                    x1 = x;
                    y1 = y;
                    outcode1 = ComputeOutCode(x1, y1, xmin, ymin, xmax, ymax);
                }
            }
        }
    }

    // 1. Кривая Дракона (Хартера-Хейтуэя) с поддержкой глубокого динамического зума
    public class DragonCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 0) depth = 0;
            if (depth > 25) depth = 25;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];
                Array.Fill<byte>(pixels, 255); // Чистый белый фон

                // Начальный отрезок дракона в мировых координатах (длина 320 единиц)
                double x1 = -160.0;
                double y1 = 0.0;
                double x2 = 160.0;
                double y2 = 0.0;

                DrawDragonRecursive(pixels, stride, width, height, x1, y1, x2, y2, depth, true, zoom, panX, panY);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawDragonRecursive(byte[] pixels, int stride, int width, int height,
            double x1, double y1, double x2, double y2, int depth, bool left,
            double zoom, double panX, double panY)
        {
            double sx1 = (x1 - panX) * zoom + width / 2.0;
            double sy1 = (y1 - panY) * zoom + height / 2.0;
            double sx2 = (x2 - panX) * zoom + width / 2.0;
            double sy2 = (y2 - panY) * zoom + height / 2.0;

            // Bounding box culling
            double dx = sx2 - sx1;
            double dy = sy2 - sy1;
            double screenDistSq = dx * dx + dy * dy;
            double screenR = Math.Sqrt(screenDistSq) * 1.3;
            double cx = (sx1 + sx2) / 2.0;
            double cy = (sy1 + sy2) / 2.0;

            if (cx + screenR < 0 || cx - screenR >= width || cy + screenR < 0 || cy - screenR >= height)
            {
                return;
            }

            // Базовый случай: глубина 0 или размер на экране меньше 0.8 пикселя (при зуме раскрывается дальше)
            if (depth == 0 || screenDistSq < 0.64)
            {
                FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx1, sy1, sx2, sy2, 30, 144, 255); // Сине-голубой
                return;
            }

            double x3 = (x1 + x2) / 2.0 + (y2 - y1) / (left ? 2.0 : -2.0);
            double y3 = (y1 + y2) / 2.0 - (x2 - x1) / (left ? 2.0 : -2.0);

            DrawDragonRecursive(pixels, stride, width, height, x1, y1, x3, y3, depth - 1, true, zoom, panX, panY);
            DrawDragonRecursive(pixels, stride, width, height, x3, y3, x2, y2, depth - 1, false, zoom, panX, panY);
        }
    }

    // 2. Треугольник Серпинского с поддержкой глубокого динамического зума
    public class SierpinskiTriangleAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 0) depth = 0;
            if (depth > 16) depth = 16;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];
                Array.Fill<byte>(pixels, 255); // Чистый белый фон

                double side = 460.0;
                double h = side * Math.Sqrt(3) / 2.0;

                Point p1 = new Point(0, -h / 2.0);
                Point p2 = new Point(-side / 2.0, h / 2.0);
                Point p3 = new Point(side / 2.0, h / 2.0);

                DrawSierpinskiRecursive(pixels, stride, width, height, p1, p2, p3, depth, zoom, panX, panY);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawSierpinskiRecursive(byte[] pixels, int stride, int width, int height,
            Point p1, Point p2, Point p3, int depth, double zoom, double panX, double panY)
        {
            double sx1 = (p1.X - panX) * zoom + width / 2.0;
            double sy1 = (p1.Y - panY) * zoom + height / 2.0;
            double sx2 = (p2.X - panX) * zoom + width / 2.0;
            double sy2 = (p2.Y - panY) * zoom + height / 2.0;
            double sx3 = (p3.X - panX) * zoom + width / 2.0;
            double sy3 = (p3.Y - panY) * zoom + height / 2.0;

            // Bounding box culling
            double minX = Math.Min(sx1, Math.Min(sx2, sx3));
            double maxX = Math.Max(sx1, Math.Max(sx2, sx3));
            double minY = Math.Min(sy1, Math.Min(sy2, sy3));
            double maxY = Math.Max(sy1, Math.Max(sy2, sy3));

            if (maxX < 0 || minX >= width || maxY < 0 || minY >= height)
            {
                return;
            }

            double widthSpan = maxX - minX;
            double heightSpan = maxY - minY;

            if (depth == 0 || (widthSpan < 1.5 && heightSpan < 1.5))
            {
                FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx1, sy1, sx2, sy2, 0, 180, 0);
                FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx2, sy2, sx3, sy3, 0, 180, 0);
                FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx3, sy3, sx1, sy1, 0, 180, 0);
                return;
            }

            Point mid12 = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
            Point mid23 = new Point((p2.X + p3.X) / 2.0, (p2.Y + p3.Y) / 2.0);
            Point mid31 = new Point((p3.X + p1.X) / 2.0, (p3.Y + p1.Y) / 2.0);

            DrawSierpinskiRecursive(pixels, stride, width, height, p1, mid12, mid31, depth - 1, zoom, panX, panY);
            DrawSierpinskiRecursive(pixels, stride, width, height, mid12, p2, mid23, depth - 1, zoom, panX, panY);
            DrawSierpinskiRecursive(pixels, stride, width, height, mid31, mid23, p3, depth - 1, zoom, panX, panY);
        }
    }

    // 3. Кривая Гильберта (алгоритм заполнения пространства) с поддержкой глубокого динамического зума
    public class HilbertCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 1) depth = 1;
            if (depth > 10) depth = 10;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];
                Array.Fill<byte>(pixels, 255); // Чистый белый фон

                double size = 420.0;
                double startX = -size / 2.0;
                double startY = -size / 2.0;

                Point? lastPoint = null;
                GenerateHilbert(startX, startY, size, 0, 0, size, depth, ref lastPoint, pixels, stride, width, height, zoom, panX, panY);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void GenerateHilbert(
            double x, double y,
            double xi, double xj,
            double yi, double yj,
            int depth,
            ref Point? lastPoint,
            byte[] pixels, int stride, int width, int height,
            double zoom, double panX, double panY)
        {
            if (depth <= 0)
            {
                double cx = x + (xi + yi) / 2.0;
                double cy = y + (xj + yj) / 2.0;
                Point currentPoint = new Point(cx, cy);

                if (lastPoint.HasValue)
                {
                    double sx1 = (lastPoint.Value.X - panX) * zoom + width / 2.0;
                    double sy1 = (lastPoint.Value.Y - panY) * zoom + height / 2.0;
                    double sx2 = (currentPoint.X - panX) * zoom + width / 2.0;
                    double sy2 = (currentPoint.Y - panY) * zoom + height / 2.0;

                    FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx1, sy1, sx2, sy2, 170, 0, 220); // Аметистовый/пурпурный
                }

                lastPoint = currentPoint;
            }
            else
            {
                double screenSide = Math.Sqrt(xi * xi + xj * xj) * zoom;
                if (screenSide < 0.8)
                {
                    double cx = x + (xi + yi) / 2.0;
                    double cy = y + (xj + yj) / 2.0;
                    Point currentPoint = new Point(cx, cy);

                    if (lastPoint.HasValue)
                    {
                        double sx1 = (lastPoint.Value.X - panX) * zoom + width / 2.0;
                        double sy1 = (lastPoint.Value.Y - panY) * zoom + height / 2.0;
                        double sx2 = (currentPoint.X - panX) * zoom + width / 2.0;
                        double sy2 = (currentPoint.Y - panY) * zoom + height / 2.0;

                        FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx1, sy1, sx2, sy2, 170, 0, 220);
                    }

                    lastPoint = currentPoint;
                    return;
                }

                double hxi = xi / 2.0;
                double hxj = xj / 2.0;
                double hyi = yi / 2.0;
                double hyj = yj / 2.0;

                GenerateHilbert(x, y, hyi, hyj, hxi, hxj, depth - 1, ref lastPoint, pixels, stride, width, height, zoom, panX, panY);
                GenerateHilbert(x + hxi, y + hxj, hxi, hxj, hyi, hyj, depth - 1, ref lastPoint, pixels, stride, width, height, zoom, panX, panY);
                GenerateHilbert(x + hxi + hyi, y + hxj + hyj, hxi, hxj, hyi, hyj, depth - 1, ref lastPoint, pixels, stride, width, height, zoom, panX, panY);
                GenerateHilbert(x + hxi + yi, y + hxj + yj, -hyi, -hyj, -hxi, -hxj, depth - 1, ref lastPoint, pixels, stride, width, height, zoom, panX, panY);
            }
        }
    }

    // Кривая Леви
    public class LevyCurveFractal : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int canvasWidth, int canvasHeight, int depth)
        {
            if (depth < 0) depth = 0;
            if (depth > 20) depth = 20;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(canvasWidth, canvasHeight, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * canvasHeight];

                double startLength = Math.Min(canvasWidth, canvasHeight) * 0.4;
                double centerX = canvasWidth / 2.0;

                double centerY = (canvasHeight / 2.0) - (startLength / 2.0);

                double x1 = centerX - startLength / 2;
                double y1 = centerY;
                double x2 = centerX + startLength / 2;
                double y2 = centerY;

                DrawLevy(pixels, stride, canvasWidth, canvasHeight, x1, y1, x2, y2, depth);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawLevy(byte[] pixels, int stride, int width, int height, double x1, double y1, double x2, double y2, int depth)
        {
            if (depth == 0)
            {
                DrawLine(pixels, stride, width, height, (int)x1, (int)y1, (int)x2, (int)y2);
                return;
            }

            double dx = x2 - x1;
            double dy = y2 - y1;

            double x3 = (x1 + x2 - dy) / 2.0;
            double y3 = (y1 + y2 + dx) / 2.0;

            DrawLevy(pixels, stride, width, height, x1, y1, x3, y3, depth - 1);
            DrawLevy(pixels, stride, width, height, x3, y3, x2, y2, depth - 1);
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
                        pixels[index + 1] = 191; // Green
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