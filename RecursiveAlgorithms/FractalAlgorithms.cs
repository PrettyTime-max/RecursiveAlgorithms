using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FractalDrawer
{
    public interface IFractalAlgorithm
    {
        Task<StreamGeometry> CalculateGeometryAsync(double canvasWidth, double canvasHeight, int depth);
    }

    public class DragonCurveAlgorithm : IFractalAlgorithm
    {
        public async Task<StreamGeometry> CalculateGeometryAsync(double width, double height, int depth)
        {
            // Ограничиваем максимальную глубину, чтобы приложение не падало по памяти
            if (depth > 22) depth = 22;

            return await Task.Run(() =>
            {
                double x1 = width / 2 - 80;
                double y1 = height / 2;
                double x2 = width / 2 + 80;
                double y2 = height / 2;

                var geometry = new StreamGeometry();
                using (var context = geometry.Open())
                {
                    context.BeginFigure(new Point(x1, y1), false, false);
                    DrawDragonRecursive(context, x1, y1, x2, y2, depth, true);
                }
                geometry.Freeze(); // Замораживаем уже готовый результат
                return geometry;
            });
        }

        private void DrawDragonRecursive(StreamGeometryContext context, double x1, double y1, double x2, double y2, int depth, bool left)
        {
            if (depth == 0)
            {
                context.LineTo(new Point(x2, y2), true, false);
            }
            else
            {
                double x3 = (x1 + x2) / 2 + (y2 - y1) / (left ? 2 : -2);
                double y3 = (y1 + y2) / 2 - (x2 - x1) / (left ? 2 : -2);

                DrawDragonRecursive(context, x1, y1, x3, y3, depth - 1, true);
                DrawDragonRecursive(context, x3, y3, x2, y2, depth - 1, false);
            }
        }
    }
}