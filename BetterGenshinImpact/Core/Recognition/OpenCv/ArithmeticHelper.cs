using OpenCvSharp;

namespace BetterGenshinImpact.Core.Recognition.OpenCv;

public class ArithmeticHelper
{
    /// <summary>
    ///     горизонтальная проекция
    /// </summary>
    /// <param name="gray"></param>
    /// <returns></returns>
    public static int[] HorizontalProjection(Mat gray)
    {
        var projection = new int[gray.Height];
        //Рассчитайте значение проекции для каждой строки
        for (var y = 0; y < gray.Height; ++y)
            //Перебрать каждый пиксель в этой строке，если это действительно，Совокупные прогнозируемые значения
            for (var x = 0; x < gray.Width; ++x)
            {
                var s = gray.Get<Vec2b>(y, x);
                if (s.Item0 == 255) projection[y]++;
            }

        return projection;
    }

    /// <summary>
    ///     вертикальная проекция
    /// </summary>
    /// <param name="gray"></param>
    /// <returns></returns>
    public static int[] VerticalProjection(Mat gray)
    {
        var projection = new int[gray.Width];
        //Переберите каждый столбец, чтобы вычислить значение проекции.
        for (var x = 0; x < gray.Width; ++x)
            for (var y = 0; y < gray.Height; ++y)
            {
                var s = gray.Get<Vec2b>(y, x);
                if (s.Item0 == 255) projection[x]++;
            }

        return projection;
    }
}
