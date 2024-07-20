using OpenCvSharp;
using System;

namespace BetterGenshinImpact.GameTask.Common.Map;

/// <summary>
/// Расчет угла в системе координат изображения с левым верхним углом в качестве начала координат.
/// </summary>
public class CharacterOrientation
{
    public static int Compute(Mat mat)
    {
        var splitMat = mat.Split();

        // 1. Побитовое И красного и синего каналов
        var red = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[0], new Scalar(250), new Scalar(255), red);
        var blue = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[2], new Scalar(0), new Scalar(10), blue);
        var andMat = new Mat(mat.Size(), MatType.CV_8UC1);

        Cv2.BitwiseAnd(red, blue, andMat);

        // найти контуры
        Cv2.FindContours(andMat, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Mat dst = Mat.Zeros(andMat.Size(), MatType.CV_8UC3);

        // Вычислить наибольший ограничивающий прямоугольник

        if (contours.Length > 0)
        {
            var maxRect = Rect.Empty;
            var maxIndex = 0;
            for (int i = 0; i < contours.Length; i++)
            {
                var box = Cv2.BoundingRect(contours[i]);
                if (box.Width * box.Height > maxRect.Width * maxRect.Height)
                {
                    maxRect = box;
                    maxIndex = i;
                }
            }

            var maxContour = contours[maxIndex];

            // Вычислить периметр контура
            var perimeter = Cv2.ArcLength(maxContour, true);

            // Приблизительная подгонка многоугольника
            var approx = Cv2.ApproxPolyDP(maxContour, 0.08 * perimeter, true);

            // Если подобранный многоугольник имеет три вершины，Думайте об этом как о треугольнике
            if (approx.Length == 3)
            {
                // Вырезаем место, где находится треугольник.
                var newSrcMat = new Mat(mat, maxRect);

                // HSV Порог для удаления центральной стрелы
                var hsvMat = new Mat();
                Cv2.CvtColor(newSrcMat, hsvMat, ColorConversionCodes.BGR2HSV);
                // var lowScalar = new Scalar(95, 255, 255);
                // var highScalar = new Scalar(255, 255, 255);
                var lowScalar = new Scalar(93, 155, 170);
                var highScalar = new Scalar(255, 255, 255);
                var hsvThresholdMat = new Mat();
                Cv2.InRange(hsvMat, lowScalar, highScalar, hsvThresholdMat);

                // Цикл для расчета середин трех сторон,И посчитаем количество последовательных черных пикселей во всех точках от середины до вершины.
                var maxBlackCount = 0;
                Point correctP1 = new(), correctP2 = new();
                var offset = new Point(maxRect.X, maxRect.Y);
                for (int i = 0; i < 3; i++)
                {
                    var midPoint = Midpoint(approx[i], approx[(i + 1) % 3]);
                    var targetPoint = approx[(i + 2) % 3];

                    // Все точки от середины до вершины
                    var lineIterator = new LineIterator(hsvThresholdMat, midPoint - offset, targetPoint - offset, PixelConnectivity.Connectivity8);

                    // Подсчитайте количество последовательных черных пикселей
                    var blackCount = 0;
                    foreach (var item in lineIterator)
                    {
                        if (item.GetValue<Vec2b>().Item0 == 255)
                        {
                            break;
                        }

                        blackCount++;
                    }

                    if (blackCount > maxBlackCount)
                    {
                        maxBlackCount = blackCount;
                        correctP1 = midPoint; // середина низа
                        correctP2 = targetPoint; // ежедневно

                        // Рассчитать радианы
                        double radians = Math.Atan2(correctP2.Y - correctP1.Y, correctP2.X - correctP1.X);

                        // Перевести радианы в градусы
                        double angle = radians * (180.0 / Math.PI);
                        return (int)angle;
                    }
                }

                // VisionContext.Instance().DrawContent.PutLine("co", new LineDrawable(correctP1, correctP2 + (correctP2 - correctP1) * 3));
            }
        }
        return -1;
    }

    static Point Midpoint(Point p1, Point p2)
    {
        var midX = (p1.X + p2.X) / 2;
        var midY = (p1.Y + p2.Y) / 2;
        return new Point(midX, midY);
    }

    public static int GameAngle2(string path)
    {
        var mat = Cv2.ImRead(path);
        return Compute(mat);
    }
}
