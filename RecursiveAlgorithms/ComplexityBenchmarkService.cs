using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RecursiveAlgorithms
{
    public class BenchmarkDataPoint
    {
        public int N { get; set; }
        public double EmpiricalTimeMs { get; set; }
        public double TheoreticalTimeMs { get; set; }
        public double ErrorPercent { get; set; }
    }

    public class BenchmarkAlgorithmInfo
    {
        public string Name { get; set; } = "";
        public string ComplexityBigO { get; set; } = "";
        public string FormulaDescription { get; set; } = "";
        public int DefaultMinN { get; set; } = 1;
        public int DefaultMaxN { get; set; } = 15;
        public Func<int, double> TheoreticalBaseFunc { get; set; } = n => Math.Pow(2, n);
    }

    public static class ComplexityBenchmarkService
    {
        private static readonly Dictionary<string, BenchmarkAlgorithmInfo> _algoInfo = new()
        {
            {
                "Треугольник Серпинского",
                new BenchmarkAlgorithmInfo
                {
                    Name = "Треугольник Серпинского",
                    ComplexityBigO = "O(3^N)",
                    FormulaDescription = "T(N) = C · 3^N",
                    DefaultMinN = 1,
                    DefaultMaxN = 14,
                    TheoreticalBaseFunc = n => Math.Pow(3.0, n)
                }
            },
            {
                "Кривая Гильберта",
                new BenchmarkAlgorithmInfo
                {
                    Name = "Кривая Гильберта",
                    ComplexityBigO = "O(4^N)",
                    FormulaDescription = "T(N) = C · 4^N",
                    DefaultMinN = 1,
                    DefaultMaxN = 10,
                    TheoreticalBaseFunc = n => Math.Pow(4.0, n)
                }
            },
            {
                "Ханойская башня",
                new BenchmarkAlgorithmInfo
                {
                    Name = "Ханойская башня",
                    ComplexityBigO = "O(2^N)",
                    FormulaDescription = "T(N) = C · (2^N - 1)",
                    DefaultMinN = 1,
                    DefaultMaxN = 22,
                    TheoreticalBaseFunc = n => Math.Pow(2.0, n) - 1.0
                }
            },
            {
                "Кривая Леви",
                new BenchmarkAlgorithmInfo
                {
                    Name = "Кривая Леви",
                    ComplexityBigO = "O(2^N)",
                    FormulaDescription = "T(N) = C · 2^N",
                    DefaultMinN = 1,
                    DefaultMaxN = 22,
                    TheoreticalBaseFunc = n => Math.Pow(2.0, n)
                }
            },
            {
                "Фрактал Вичека",
                new BenchmarkAlgorithmInfo
                {
                    Name = "Фрактал Вичека",
                    ComplexityBigO = "O(5^N)",
                    FormulaDescription = "T(N) = C · 5^N",
                    DefaultMinN = 1,
                    DefaultMaxN = 9,
                    TheoreticalBaseFunc = n => Math.Pow(5.0, n)
                }
            }
        };

        public static IEnumerable<string> GetAvailableAlgorithms() => _algoInfo.Keys;

        public static BenchmarkAlgorithmInfo GetAlgorithmInfo(string name)
        {
            if (_algoInfo.TryGetValue(name, out var info)) return info;
            return _algoInfo["Кривая Леви"];
        }

        /// <summary>
        /// Выполняет точный замер времени чистого рекурсивного алгоритма на заданной глубине N (в миллисекундах).
        /// Без искусственных ограничений глубины.
        /// </summary>
        public static async Task<double> MeasureStepTimeMsAsync(string algorithmName, int depth, int repetitions, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                // Прогрев JIT-компилятора
                RunAlgorithm(algorithmName, Math.Min(depth, 3), ct);

                var times = new List<double>();
                for (int r = 0; r < repetitions; r++)
                {
                    ct.ThrowIfCancellationRequested();

                    long start = Stopwatch.GetTimestamp();
                    RunAlgorithm(algorithmName, depth, ct);
                    long end = Stopwatch.GetTimestamp();

                    double elapsedMs = (end - start) * 1000.0 / Stopwatch.Frequency;
                    times.Add(elapsedMs);
                }

                times.Sort();
                double median = times[times.Count / 2];

                // Если время меньше 0.05 мс, делаем адаптивный пакетный замер для микросекундной точности
                if (median < 0.05)
                {
                    int loopCount = 100;
                    long start = Stopwatch.GetTimestamp();
                    for (int i = 0; i < loopCount; i++)
                    {
                        if ((i & 0x1F) == 0) ct.ThrowIfCancellationRequested();
                        RunAlgorithm(algorithmName, depth, ct);
                    }
                    long end = Stopwatch.GetTimestamp();
                    median = ((end - start) * 1000.0 / Stopwatch.Frequency) / loopCount;
                }

                return median;
            }, ct);
        }

        /// <summary>
        /// Метод наименьших квадратов (МНК) для вычисления масштабного коэффициента C:
        /// min sum (T_i - C * f(N_i))^2 => C = sum(T_i * f_i) / sum(f_i^2)
        /// </summary>
        public static double CalculateLeastSquaresC(List<BenchmarkDataPoint> points, Func<int, double> theorFunc)
        {
            if (points == null || points.Count == 0) return 0.0;

            double sumNumerator = 0.0;
            double sumDenominator = 0.0;

            foreach (var pt in points)
            {
                double f = theorFunc(pt.N);
                sumNumerator += pt.EmpiricalTimeMs * f;
                sumDenominator += f * f;
            }

            if (sumDenominator <= 0) return 0.0;
            return sumNumerator / sumDenominator;
        }

        /// <summary>
        /// Коэффициент детерминации R^2 (от 0 до 1), отражающий точность соответствия теории и практики.
        /// </summary>
        public static double CalculateR2(List<BenchmarkDataPoint> points, Func<int, double> theorFunc, double c)
        {
            if (points == null || points.Count < 2) return 1.0;

            double sumY = 0;
            foreach (var pt in points) sumY += pt.EmpiricalTimeMs;
            double meanY = sumY / points.Count;

            double ssTot = 0;
            double ssRes = 0;

            foreach (var pt in points)
            {
                double y = pt.EmpiricalTimeMs;
                double yTheor = c * theorFunc(pt.N);
                ssTot += (y - meanY) * (y - meanY);
                ssRes += (y - yTheor) * (y - yTheor);
            }

            if (ssTot <= 0) return 1.0;
            double r2 = 1.0 - (ssRes / ssTot);
            return Math.Max(0.0, Math.Min(1.0, r2));
        }

        // --- Чистые рекурсивные исполнители алгоритмов без UI и без ограничений ---

        private static void RunAlgorithm(string algorithmName, int depth, CancellationToken ct)
        {
            long counter = 0;
            switch (algorithmName)
            {
                case "Ханойская башня":
                    HanoiRecursivePure(depth, 0, 2, 1, ref counter, ct);
                    break;
                case "Треугольник Серпинского":
                    SierpinskiRecursivePure(0, 0, 100, 0, 50, 86, depth, ref counter, ct);
                    break;
                case "Кривая Гильберта":
                    HilbertRecursivePure(0, 0, 100, 0, 0, 100, depth, ref counter, ct);
                    break;
                case "Кривая Леви":
                    LevyRecursivePure(0, 0, 100, 0, depth, ref counter, ct);
                    break;
                case "Фрактал Вичека":
                    VicsekRecursivePure(0, 0, 100, depth, ref counter, ct);
                    break;
                default:
                    throw new ArgumentException($"Неизвестный алгоритм: {algorithmName}");
            }
        }

        private static void HanoiRecursivePure(int n, int from, int to, int aux, ref long counter, CancellationToken ct)
        {
            if (n <= 0) return;
            if ((counter++ & 0x1FFF) == 0) ct.ThrowIfCancellationRequested();

            HanoiRecursivePure(n - 1, from, aux, to, ref counter, ct);
            HanoiRecursivePure(n - 1, aux, to, from, ref counter, ct);
        }

        private static void SierpinskiRecursivePure(double x1, double y1, double x2, double y2, double x3, double y3, int depth, ref long counter, CancellationToken ct)
        {
            if (depth <= 0) return;
            if ((counter++ & 0x1FFF) == 0) ct.ThrowIfCancellationRequested();

            double m12x = (x1 + x2) * 0.5, m12y = (y1 + y2) * 0.5;
            double m23x = (x2 + x3) * 0.5, m23y = (y2 + y3) * 0.5;
            double m31x = (x3 + x1) * 0.5, m31y = (y3 + y1) * 0.5;

            SierpinskiRecursivePure(x1, y1, m12x, m12y, m31x, m31y, depth - 1, ref counter, ct);
            SierpinskiRecursivePure(m12x, m12y, x2, y2, m23x, m23y, depth - 1, ref counter, ct);
            SierpinskiRecursivePure(m31x, m31y, m23x, m23y, x3, y3, depth - 1, ref counter, ct);
        }

        private static void HilbertRecursivePure(double x, double y, double xi, double xj, double yi, double yj, int depth, ref long counter, CancellationToken ct)
        {
            if (depth <= 0) return;
            if ((counter++ & 0x1FFF) == 0) ct.ThrowIfCancellationRequested();

            double hxi = xi * 0.5, hxj = xj * 0.5;
            double hyi = yi * 0.5, hyj = yj * 0.5;

            HilbertRecursivePure(x, y, hyi, hyj, hxi, hxj, depth - 1, ref counter, ct);
            HilbertRecursivePure(x + hxi, y + hxj, hxi, hxj, hyi, hyj, depth - 1, ref counter, ct);
            HilbertRecursivePure(x + hxi + hyi, y + hxj + hyj, hxi, hxj, hyi, hyj, depth - 1, ref counter, ct);
            HilbertRecursivePure(x + hxi + yi, y + hxj + yj, -hyi, -hyj, -hxi, -hxj, depth - 1, ref counter, ct);
        }

        private static void LevyRecursivePure(double x1, double y1, double x2, double y2, int depth, ref long counter, CancellationToken ct)
        {
            if (depth <= 0) return;
            if ((counter++ & 0x1FFF) == 0) ct.ThrowIfCancellationRequested();

            double mx = (x1 + x2 - (y2 - y1)) * 0.5;
            double my = (y1 + y2 + (x2 - x1)) * 0.5;

            LevyRecursivePure(x1, y1, mx, my, depth - 1, ref counter, ct);
            LevyRecursivePure(mx, my, x2, y2, depth - 1, ref counter, ct);
        }

        private static void VicsekRecursivePure(double x, double y, double size, int depth, ref long counter, CancellationToken ct)
        {
            if (depth <= 0) return;
            if ((counter++ & 0x1FFF) == 0) ct.ThrowIfCancellationRequested();

            double s = size / 3.0;

            VicsekRecursivePure(x + s, y + s, s, depth - 1, ref counter, ct);
            VicsekRecursivePure(x + s, y, s, depth - 1, ref counter, ct);
            VicsekRecursivePure(x + s, y + 2.0 * s, s, depth - 1, ref counter, ct);
            VicsekRecursivePure(x, y + s, s, depth - 1, ref counter, ct);
            VicsekRecursivePure(x + 2.0 * s, y + s, s, depth - 1, ref counter, ct);
        }
    }
}
