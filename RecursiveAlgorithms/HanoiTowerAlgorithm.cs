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

        public const int SubStepsPerMove = 12; // Количество кадров анимации на одно перемещение

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

            return await Task.Run(() => CalculateStepBitmap(width, height, depth, depth * SubStepsPerMove));
        }

        public WriteableBitmap CalculateStepBitmap(int width, int height, int disksCount, int totalSubStepIndex)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = bitmap.BackBufferStride;
            byte[] pixels = new byte[stride * height];

            ClearBackground(pixels, width, height, stride, 255, 255, 255);
            DrawBaseAndPegs(pixels, width, height, stride);

            var pegs = new List<int>[3];
            for (int i = 0; i < 3; i++) pegs[i] = new List<int>();
            for (int i = disksCount; i >= 1; i--) pegs[0].Add(i);

            var moves = new List<(int from, int to)>();
            GetHanoiMoves(disksCount, 0, 2, 1, moves);

            int moveIndex = totalSubStepIndex / SubStepsPerMove;
            int subStep = totalSubStepIndex % SubStepsPerMove;

            // Выполняем все полностью завершенные предыдущие ходы
            int completedMoves = Math.Min(moveIndex, moves.Count);
            for (int s = 0; s < completedMoves; s++)
            {
                var m = moves[s];
                if (pegs[m.from].Count > 0)
                {
                    int disk = pegs[m.from][pegs[m.from].Count - 1];
                    pegs[m.from].RemoveAt(pegs[m.from].Count - 1);
                    pegs[m.to].Add(disk);
                }
            }

            // Если есть активный текущий ход — анимируем его
            int movingDisk = -1;
            int movingDiskX = 0, movingDiskY = 0;

            if (moveIndex < moves.Count && subStep > 0)
            {
                var m = moves[moveIndex];
                if (pegs[m.from].Count > 0)
                {
                    movingDisk = pegs[m.from][pegs[m.from].Count - 1];
                    pegs[m.from].RemoveAt(pegs[m.from].Count - 1);

                    // Координаты источников и целей
                    int[] pegX = { width / 4, width / 2, (width * 3) / 4 };
                    int baseY = height - 140;
                    int diskHeight = 22;

                    int startX = pegX[m.from];
                    int startY = baseY - (pegs[m.from].Count) * (diskHeight + 4);

                    int endX = pegX[m.to];
                    int endY = baseY - (pegs[m.to].Count) * (diskHeight + 4);

                    int topY = baseY - 240; // Высота подъема над стержнями

                    double progress = (double)subStep / SubStepsPerMove;

                    // Анимация из 3 фаз: подъем вверх -> сдвиг по горизонтали -> опускание вниз
                    if (progress < 0.33)
                    {
                        double p = progress / 0.33;
                        movingDiskX = startX;
                        movingDiskY = (int)(startY + (topY - startY) * p);
                    }
                    else if (progress < 0.66)
                    {
                        double p = (progress - 0.33) / 0.33;
                        movingDiskX = (int)(startX + (endX - startX) * p);
                        movingDiskY = topY;
                    }
                    else
                    {
                        double p = (progress - 0.66) / 0.34;
                        movingDiskX = endX;
                        movingDiskY = (int)(topY + (endY - topY) * p);
                    }
                }
            }

            // Отрисовываем статичные диски на стержнях
            DrawPegsContent(pixels, width, height, stride, pegs);

            // Отрисовываем движущийся диск поверх
            if (movingDisk != -1)
            {
                DrawSingleDisk(pixels, width, height, stride, movingDisk, movingDiskX, movingDiskY);
            }

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

            FillRect(pixels, width, height, stride, baseX, baseY, baseWidth, baseHeight, 216, 216, 216);
            DrawRectBorder(pixels, width, height, stride, baseX, baseY, baseWidth, baseHeight, 180, 180, 180);

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

            for (int p = 0; p < 3; p++)
            {
                int currentY = baseY;
                for (int i = 0; i < pegs[p].Count; i++)
                {
                    int diskSize = pegs[p][i];
                    DrawSingleDisk(pixels, width, height, stride, diskSize, pegX[p], currentY);
                    currentY -= (diskHeight + 4);
                }
            }
        }

        private void DrawSingleDisk(byte[] pixels, int width, int height, int stride, int diskSize, int centerX, int topY)
        {
            int diskHeight = 22;
            int dWidth = 50 + diskSize * 28;
            int dX = centerX - dWidth / 2;

            byte[,] pastelColors = {
                { 220, 150, 150 },
                { 225, 180, 140 },
                { 225, 215, 140 },
                { 150, 200, 160 },
                { 140, 180, 215 },
                { 180, 160, 210 }
            };

            int colorIdx = (diskSize - 1) % 6;
            byte r = pastelColors[colorIdx, 0];
            byte g = pastelColors[colorIdx, 1];
            byte b = pastelColors[colorIdx, 2];

            FillRect(pixels, width, height, stride, dX, topY, dWidth, diskHeight, r, g, b);
            DrawRectBorder(pixels, width, height, stride, dX, topY, dWidth, diskHeight, 120, 120, 120);
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