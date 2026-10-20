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

        public static void FillClippedRectangle(byte[] pixels, int stride, int width, int height,
            double x, double y, double size, byte r, byte g, byte b)
        {
            if (size < 1.2)
            {
                int px = (int)Math.Round(x);
                int py = (int)Math.Round(y);
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    int idx = py * stride + (px << 2);
                    if (idx >= 0 && idx < pixels.Length - 3)
                    {
                        pixels[idx] = b;
                        pixels[idx + 1] = g;
                        pixels[idx + 2] = r;
                        pixels[idx + 3] = 255;
                    }
                }
                return;
            }

            int x0 = Math.Max(0, (int)Math.Round(x));
            int y0 = Math.Max(0, (int)Math.Round(y));
            int x1 = Math.Min(width, (int)Math.Round(x + size));
            int y1 = Math.Min(height, (int)Math.Round(y + size));

            if (x1 <= x0) x1 = Math.Min(width, x0 + 1);
            if (y1 <= y0) y1 = Math.Min(height, y0 + 1);

            for (int cy = y0; cy < y1; cy++)
            {
                int row = cy * stride;
                for (int cx = x0; cx < x1; cx++)
                {
                    int idx = row + (cx << 2);
                    if (idx >= 0 && idx < pixels.Length - 3)
                    {
                        pixels[idx] = b;
                        pixels[idx + 1] = g;
                        pixels[idx + 2] = r;
                        pixels[idx + 3] = 255;
                    }
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

    // 1. Треугольник Серпинского (с поддержкой LOD и отсечения невидимых областей)
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

            // Bounding box culling (отсечение ветвей за экраном)
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

            // LOD: если треугольник на экране меньше 1.2 пикселя, дальнейшее деление не имеет смысла
            // При приближении (зуме) widthSpan вырастает, и рекурсия идет глубже!
            if (depth == 0 || (widthSpan < 1.2 && heightSpan < 1.2))
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

    // 2. Кривая Гильберта (с поддержкой LOD и отсечения невидимых областей)
    public class HilbertCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 1) depth = 1;
            if (depth > 13) depth = 13;

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
            // Вычисляем экранные координаты углов квадранта
            double sx0 = (x - panX) * zoom + width / 2.0;
            double sy0 = (y - panY) * zoom + height / 2.0;
            double sxi = xi * zoom;
            double sxj = xj * zoom;
            double syi = yi * zoom;
            double syj = yj * zoom;

            double sx1 = sx0 + sxi;
            double sy1 = sy0 + sxj;
            double sx2 = sx0 + syi;
            double sy2 = sy0 + syj;
            double sx3 = sx0 + sxi + syi;
            double sy3 = sy0 + sxj + syj;

            double minX = Math.Min(Math.Min(sx0, sx1), Math.Min(sx2, sx3));
            double maxX = Math.Max(Math.Max(sx0, sx1), Math.Max(sx2, sx3));
            double minY = Math.Min(Math.Min(sy0, sy1), Math.Min(sy2, sy3));
            double maxY = Math.Max(Math.Max(sy0, sy1), Math.Max(sy2, sy3));

            // Viewport Culling: если квадрант целиком вне видимого экрана
            if (maxX < 0 || minX >= width || maxY < 0 || minY >= height)
            {
                Point exitPoint = GetHilbertExitPoint(x, y, xi, xj, yi, yj, depth);
                if (lastPoint.HasValue)
                {
                    double lsx = (lastPoint.Value.X - panX) * zoom + width / 2.0;
                    double lsy = (lastPoint.Value.Y - panY) * zoom + height / 2.0;
                    double esx = (exitPoint.X - panX) * zoom + width / 2.0;
                    double esy = (exitPoint.Y - panY) * zoom + height / 2.0;

                    FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, lsx, lsy, esx, esy, 170, 0, 220);
                }
                lastPoint = exitPoint;
                return;
            }

            double screenSide = Math.Sqrt(sxi * sxi + sxj * sxj);

            // LOD: если ячейка на экране меньше 1.2 пикселей, дальше не делим
            if (depth <= 0 || screenSide < 1.2)
            {
                double cx = x + (xi + yi) / 2.0;
                double cy = y + (xj + yj) / 2.0;
                Point currentPoint = new Point(cx, cy);

                if (lastPoint.HasValue)
                {
                    double lsx = (lastPoint.Value.X - panX) * zoom + width / 2.0;
                    double lsy = (lastPoint.Value.Y - panY) * zoom + height / 2.0;
                    double csx = (currentPoint.X - panX) * zoom + width / 2.0;
                    double csy = (currentPoint.Y - panY) * zoom + height / 2.0;

                    FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, lsx, lsy, csx, csy, 170, 0, 220); // Аметистовый/пурпурный
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

        private static Point GetHilbertExitPoint(double x, double y, double xi, double xj, double yi, double yj, int depth)
        {
            while (depth > 0)
            {
                double hxi = xi / 2.0;
                double hxj = xj / 2.0;
                double hyi = yi / 2.0;
                double hyj = yj / 2.0;

                x = x + hxi + yi;
                y = y + hxj + yj;
                double nxi = -hyi;
                double nxj = -hyj;
                double nyi = -hxi;
                double nyj = -hxj;

                xi = nxi; xj = nxj;
                yi = nyi; yj = nyj;
                depth--;
            }
            return new Point(x + (xi + yi) / 2.0, y + (xj + yj) / 2.0);
        }
    }

    // 3. Кривая Леви (с поддержкой LOD и Viewport Culling)
    public class LevyCurveFractal : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int canvasWidth, int canvasHeight, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 0) depth = 0;
            if (depth > 27) depth = 27;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(canvasWidth, canvasHeight, 96, 96,
                    PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * canvasHeight];
                Array.Fill<byte>(pixels, 255); // Белый фон

                double startLength = Math.Min(canvasWidth, canvasHeight) * 0.4;
                double x1 = -startLength / 2.0;
                double y1 = 0.0;
                double x2 = startLength / 2.0;
                double y2 = 0.0;

                DrawLevyRecursive(pixels, stride, canvasWidth, canvasHeight, x1, y1, x2, y2, depth, zoom, panX, panY);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawLevyRecursive(byte[] pixels, int stride, int width, int height, double x1, double y1, double x2, double y2, int depth, double zoom, double panX, double panY)
        {
            double sx1 = (x1 - panX) * zoom + width / 2.0;
            double sy1 = (y1 - panY) * zoom + height / 2.0;
            double sx2 = (x2 - panX) * zoom + width / 2.0;
            double sy2 = (y2 - panY) * zoom + height / 2.0;

            // Viewport culling: радиус описывающей окружности ветви
            double dx = sx2 - sx1;
            double dy = sy2 - sy1;
            double screenDistSq = dx * dx + dy * dy;
            double screenR = Math.Sqrt(screenDistSq) * 1.5;
            double cx = (sx1 + sx2) / 2.0;
            double cy = (sy1 + sy2) / 2.0;

            // Если ветвь целиком за экраном — мгновенный выход
            if (cx + screenR < 0 || cx - screenR >= width || cy + screenR < 0 || cy - screenR >= height)
            {
                return;
            }

            // LOD: если отрезок на экране меньше 1 пикселя (screenDistSq < 1.0) или depth == 0
            // При полном виде отрезки не дробятся понапрасну в доли пикселя, и отрисовка мгновенна!
            // А при приближении отрезок вырастает, и рекурсия идет глубже!
            if (depth == 0 || screenDistSq < 1.0)
            {
                FractalGraphicsHelper.DrawClippedLine(pixels, stride, width, height, sx1, sy1, sx2, sy2, 255, 191, 0);
                return;
            }

            double mx = (x1 + x2 - (y2 - y1)) / 2.0;
            double my = (y1 + y2 + (x2 - x1)) / 2.0;

            DrawLevyRecursive(pixels, stride, width, height, x1, y1, mx, my, depth - 1, zoom, panX, panY);
            DrawLevyRecursive(pixels, stride, width, height, mx, my, x2, y2, depth - 1, zoom, panX, panY);
        }
    }

    // 5. Фрактал Вичека (крестообразная форма с поддержкой LOD и отсечения невидимых областей)
    public class VicsekFractalAlgorithm : IFractalAlgorithm
    {
        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth, double zoom = 1.0, double panX = 0, double panY = 0)
        {
            if (depth < 1) depth = 1;
            if (depth > 11) depth = 11;

            return await Task.Run(() =>
            {
                var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                int stride = bitmap.BackBufferStride;
                byte[] pixels = new byte[stride * height];
                Array.Fill<byte>(pixels, 255); // Чистый белый фон

                double size = Math.Min(width, height) * 0.75;
                double startX = -size / 2.0;
                double startY = -size / 2.0;

                DrawVicsekRecursive(pixels, stride, width, height, startX, startY, size, depth, zoom, panX, panY);

                bitmap.Lock();
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Unlock();
                bitmap.Freeze();

                return bitmap;
            });
        }

        private void DrawVicsekRecursive(byte[] pixels, int stride, int width, int height,
            double x, double y, double size, int depth, double zoom, double panX, double panY)
        {
            double sx = (x - panX) * zoom + width / 2.0;
            double sy = (y - panY) * zoom + height / 2.0;
            double sSize = size * zoom;

            // Viewport Culling: если весь квадрат находится за пределами экрана — мгновенный выход
            if (sx + sSize < 0 || sx >= width || sy + sSize < 0 || sy >= height)
            {
                return;
            }

            // LOD (уровень детализации): если квадрат на экране меньше 1.2 пикселя или достигнута глубина depth == 0
            // При полном виде микроквадраты не дробятся до бесконечности (нет лагов),
            // а при приближении (зуме) квадрат увеличивается и рекурсия уходит дальше вглубь фрактала!
            if (depth == 0 || sSize < 1.2)
            {
                FractalGraphicsHelper.FillClippedRectangle(pixels, stride, width, height, sx, sy, sSize, 215, 45, 45);
                return;
            }

            double s = size / 3.0;

            // Классическая крестообразная декомпозиция Вичека: 5 квадратов из 9
            // 1. Центр
            DrawVicsekRecursive(pixels, stride, width, height, x + s, y + s, s, depth - 1, zoom, panX, panY);
            // 2. Верх
            DrawVicsekRecursive(pixels, stride, width, height, x + s, y, s, depth - 1, zoom, panX, panY);
            // 3. Низ
            DrawVicsekRecursive(pixels, stride, width, height, x + s, y + 2.0 * s, s, depth - 1, zoom, panX, panY);
            // 4. Лево
            DrawVicsekRecursive(pixels, stride, width, height, x, y + s, s, depth - 1, zoom, panX, panY);
            // 5. Право
            DrawVicsekRecursive(pixels, stride, width, height, x + 2.0 * s, y + s, s, depth - 1, zoom, panX, panY);
        }
    }
}
