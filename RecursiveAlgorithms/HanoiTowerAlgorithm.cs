using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RecursiveAlgorithms
{
    public class HanoiTowerAlgorithm : IFractalAlgorithm
    {
        private List<(int from, int to)> _moves = new List<(int from, int to)>();
        private List<int>[] _initialPegs;
        private int _totalDisks = 0;

        public async Task<WriteableBitmap> CalculateBitmapAsync(int width, int height, int depth)
        {
            if (depth < 1) depth = 1;
            if (depth > 6) depth = 6;

            if (_totalDisks != depth)
            {
                _totalDisks = depth;
                _moves.Clear();
                GetHanoiMoves(depth, 0, 2, 1, _moves);

                _initialPegs = new List<int>[3];
                for (int i = 0; i < 3; i++) _initialPegs[i] = new List<int>();
                for (int i = depth; i >= 1; i--)
                {
                    _initialPegs[0].Add(i);
                }
            }

            return await Task.Run(() => CalculateStepBitmap(width, height, depth, depth));
        }

        public WriteableBitmap CalculateStepBitmap(int width, int height, int disksCount, int stepIndex)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = bitmap.BackBufferStride;
            byte[] pixels = new byte[stride * height];

            // Белый чистый фон (под стиль приложения)
            ClearBackground(pixels, width, height, stride, 255, 255, 255);

            // Основание и стержни в светло-серых тонах
            DrawBaseAndPegs(pixels, width, height, stride);

            var pegs = new List<int>[3];
            for (int i = 0; i < 3; i++) pegs[i] = new List<int>();
            for (int i = disksCount; i >= 1; i--) pegs[0].Add(i);

            var moves = new List<(int from, int to)>();
            GetHanoiMoves(disksCount, 0, 2, 1, moves);

            int limit = Math.Min(stepIndex, moves.Count);
            for (int s = 0; s < limit; s++)
            {
                var move = moves[s];
                if (pegs[move.from].Count > 0)
                {
                    int disk = pegs[move.from][pegs[move.from].Count - 1];
                    pegs[move.from].RemoveAt(pegs[move.from].Count - 1);
                    pegs[move.to].Add(disk);
                }
            }

            DrawPegsContent(pixels, width, height, stride, pegs);

            bitmap.Lock();
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            bitmap.Unlock();
            bitmap.Freeze();

            return bitmap;
        }

        public int GetTotalMoves(int disksCount)
        {
            var moves = new List<(int from, int to)>();
            GetHanoiMoves(disksCount, 0, 2, 1, moves);
            return moves.Count;
        }

        private void GetHanoiMoves(int n, int from, int to, int aux, List<(int, int)> moves)
        {
            if (n == 1)
            {
                moves.Add((from, to));
                return;
            }
            GetHanoiMoves(n - 1, from, aux, to, moves);
            moves.Add((from, to));
            GetHanoiMoves(n - 1, aux, to, from, moves);
        }

        private void ClearBackground(byte[] pixels, int width, int height, int stride, byte r, byte g, byte b)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * 4;
                    pixels[index] = b;
                    pixels[index + 1] = g;
                    pixels[index + 2] = r;
                    pixels[index + 3] = 255;
                }
            }
        }

        private void DrawBaseAndPegs(byte[] pixels, int width, int height, int stride)
        {
            int baseY = height - 120;
            int baseWidth = width - 100;
            int baseHeight = 14;
            int baseX = 50;

            // Серый цвет подставки (#D8D8D8)
            FillRect(pixels, width, height, stride, baseX, baseY, baseWidth, baseHeight, 216, 216, 216);
            DrawRectBorder(pixels, width, height, stride, baseX, baseY, baseWidth, baseHeight, 180, 180, 180);

            // Стержни (#A0A0A0)
            int[] pegX = { width / 4, width / 2, (width * 3) / 4 };
            int pegWidth = 10;
            int pegHeight = 220;
            int pegY = baseY - pegHeight;

            foreach (var px in pegX)
            {
                FillRect(pixels, width, height, stride, px - pegWidth / 2, pegY, pegWidth, pegHeight, 160, 160, 160);
                DrawRectBorder(pixels, width, height, stride, px - pegWidth / 2, pegY, pegWidth, pegHeight, 130, 130, 130);
            }
        }

        private void DrawPegsContent(byte[] pixels, int width, int height, int stride, List<int>[] pegs)
        {
            int baseY = height - 140;
            int[] pegX = { width / 4, width / 2, (width * 3) / 4 };
            int diskHeight = 22;

            // Бледные/пастельные гармоничные оттенки (RGB)
            byte[,] pastelColors = {
                { 220, 150, 150 }, // Мягкий бледный красный / розоватый
                { 225, 180, 140 }, // Бледный оранжевый / персиковый
                { 225, 215, 140 }, // Пастельный желтый
                { 150, 200, 160 }, // Мягкий бледный зеленый
                { 140, 180, 215 }, // Приглушенный голубой
                { 180, 160, 210 }  // Мягкий лавандовый
            };

            for (int p = 0; p < 3; p++)
            {
                int currentY = baseY;
                for (int i = 0; i < pegs[p].Count; i++)
                {
                    int diskSize = pegs[p][i];
                    int dWidth = 50 + diskSize * 28;
                    int dX = pegX[p] - dWidth / 2;
                    int dY = currentY;

                    int colorIdx = (diskSize - 1) % 6;
                    byte r = pastelColors[colorIdx, 0];
                    byte g = pastelColors[colorIdx, 1];
                    byte b = pastelColors[colorIdx, 2];

                    // Рисуем прямоугольный диск со строгими углами в стиле приложения
                    FillRect(pixels, width, height, stride, dX, dY, dWidth, diskHeight, r, g, b);
                    // Тонкая аккуратная обводка
                    DrawRectBorder(pixels, width, height, stride, dX, dY, dWidth, diskHeight, 120, 120, 120);

                    currentY -= (diskHeight + 4);
                }
            }
        }

        private void FillRect(byte[] pixels, int width, int height, int stride, int x, int y, int w, int h, byte r, byte g, byte b)
        {
            for (int py = y; py < y + h; py++)
            {
                if (py < 0 || py >= height) continue;
                for (int px = x; px < x + w; px++)
                {
                    if (px < 0 || px >= width) continue;
                    int index = py * stride + px * 4;
                    pixels[index] = b;
                    pixels[index + 1] = g;
                    pixels[index + 2] = r;
                    pixels[index + 3] = 255;
                }
            }
        }

        private void DrawRectBorder(byte[] pixels, int width, int height, int stride, int x, int y, int w, int h, byte r, byte g, byte b)
        {
            for (int px = x; px < x + w; px++)
            {
                SetPixel(pixels, width, height, stride, px, y, r, g, b);
                SetPixel(pixels, width, height, stride, px, y + h - 1, r, g, b);
            }
            for (int py = y; py < y + h; py++)
            {
                SetPixel(pixels, width, height, stride, x, py, r, g, b);
                SetPixel(pixels, width, height, stride, x + w - 1, py, r, g, b);
            }
        }

        private void SetPixel(byte[] pixels, int width, int height, int stride, int x, int y, byte r, byte g, byte b)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                int index = y * stride + x * 4;
                pixels[index] = b;
                pixels[index + 1] = g;
                pixels[index + 2] = r;
                pixels[index + 3] = 255;
            }
        }
    }
}