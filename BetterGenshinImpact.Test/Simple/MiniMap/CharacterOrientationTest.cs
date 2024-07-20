using System.Diagnostics;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using OpenCvSharp;

namespace BetterGenshinImpact.Test.Simple.MiniMap;

public class CharacterOrientationTest
{
    public static void TestArrow()
    {
        var mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Распознавание стрелок\3.png", ImreadModes.Color);
        var lowScalar = new Scalar(0, 207, 255);
        var highScalar = new Scalar(0, 208, 255);
        var gray = OpenCvCommonHelper.Threshold(mat, lowScalar, highScalar);
        Cv2.ImShow("gray", gray);
    }

    static double Distance(Point pt1, Point pt2)
    {
        int deltaX = Math.Abs(pt2.X - pt1.X);
        int deltaY = Math.Abs(pt2.Y - pt1.Y);

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    static double Distance(Point2f pt1, Point2f pt2)
    {
        var deltaX = Math.Abs(pt2.X - pt1.X);
        var deltaY = Math.Abs(pt2.Y - pt1.Y);

        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    // static Point2f Midpoint(Point2f p1, Point2f p2)
    // {
    //     var midX = (p1.X + p2.X) / 2;
    //     var midY = (p1.Y + p2.Y) / 2;
    //     return new Point2f(midX, midY);
    // }

    static Point Midpoint(Point p1, Point p2)
    {
        var midX = (p1.X + p2.X) / 2;
        var midY = (p1.Y + p2.Y) / 2;
        return new Point(midX, midY);
    }

    public static void Triangle(Mat src, Mat gray)
    {
        Cv2.FindContours(gray, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);
        Mat dst = Mat.Zeros(gray.Size(), MatType.CV_8UC3);
        for (int i = 0; i < contours.Length; i++)
        {
            Cv2.DrawContours(src, contours, i, Scalar.Red, 1, LineTypes.Link4, hierarchy);
        }
        // Cv2.ImShow("Цель", dst);

        Mat dst2 = Mat.Zeros(gray.Size(), MatType.CV_8UC3);
        // Траверсные контуры
        for (int i = 0; i < contours.Length; i++)
        {
            // Вычислить периметр контура
            double perimeter = Cv2.ArcLength(contours[i], true);

            // Приблизительная подгонка многоугольника
            Point[] approx = Cv2.ApproxPolyDP(contours[i], 0.04 * perimeter, true);

            // Если подобранный многоугольник имеет три вершины，Думайте об этом как о треугольнике
            if (approx.Length == 3)
            {
                // Нарисуйте контур треугольника на изображении.
                Cv2.DrawContours(src, new Point[][] { approx }, -1, Scalar.Green, 1);
                // Вычислить длину трех сторон
                var sideLengths = new double[3];
                sideLengths[0] = Distance(approx[1], approx[2]);
                sideLengths[1] = Distance(approx[2], approx[0]);
                sideLengths[2] = Distance(approx[0], approx[1]);

                var result = sideLengths
                    .Select((value, index) => new { Value = value, Index = index })
                    .OrderBy(item => item.Value)
                    .First();

                // Вычислить середину самой короткой линии
                var residue = approx.ToList();
                residue.RemoveAt(result.Index);
                var midPoint = new Point((residue[0].X + residue[1].X) / 2, (residue[0].Y + residue[1].Y) / 2);

                // Нарисуйте прямую линию на изображении
                Cv2.Line(src, midPoint, approx[result.Index] + (approx[result.Index] - midPoint) * 3, Scalar.Red, 1);

                Debug.WriteLine(CalculateAngle(midPoint, approx[result.Index]));
            }
        }

        Cv2.ImShow("Цель2", src);
    }

    public static void TestArrow2()
    {
        var mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Распознавание стрелок\s1.png", ImreadModes.Color);
        Cv2.GaussianBlur(mat, mat, new Size(3, 3), 0);
        var splitMat = mat.Split();

        //for (int i = 0; i < splitMat.Length; i++)
        //{
        //    Cv2.ImShow($"splitMat{i}", splitMat[i]);
        //}

        // Побитовое И красного и синего каналов
        var red = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[0], new Scalar(250), new Scalar(255), red);
        //Cv2.ImShow("red", red);
        var blue = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[2], new Scalar(0), new Scalar(10), blue);
        //Cv2.ImShow("blue", blue);
        var andMat = red & blue;
        Cv2.ImShow("andMat2", andMat);
        Triangle(mat, andMat);
    }

    public static void TestArrow3()
    {
        var mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Распознавание стрелок\s1.png", ImreadModes.Color);
        Cv2.GaussianBlur(mat, mat, new Size(3, 3), 0);
        var splitMat = mat.Split();

        //for (int i = 0; i < splitMat.Length; i++)
        //{
        //    Cv2.ImShow($"splitMat{i}", splitMat[i]);
        //}

        // Побитовое И красного и синего каналов
        var red = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[0], new Scalar(255), new Scalar(255), red);
        //Cv2.ImShow("red", red);
        var blue = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[2], new Scalar(0), new Scalar(0), blue);
        //Cv2.ImShow("blue", blue);
        var andMat = red & blue;
        // Cv2.ImShow("andMat", andMat);

        //коррозия
        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        var res = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.Erode(andMat.ToMat(), res, kernel);
        Cv2.ImShow("erode", res);

        // Расширение
        // var res2 = new Mat(mat.Size(), MatType.CV_8UC1);
        // Cv2.Dilate(res, res2, kernel);
        // Cv2.ImShow("dilate", res2);

        // Обнаружение прямоугольника
        Cv2.FindContours(res, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxNone);
        Mat dst = Mat.Zeros(res.Size(), MatType.CV_8UC3);
        for (int i = 0; i < contours.Length; i++)
        {
            // Минимальный охватывающий прямоугольник
            var rect = Cv2.MinAreaRect(contours[i]);
            // Четыре вершины прямоугольника
            var points = Cv2.BoxPoints(rect);
            // Нарисуйте прямоугольник
            for (int j = 0; j < 4; ++j)
            {
                Cv2.Line(mat, (Point)points[j], (Point)points[(j + 1) % 4], Scalar.Red, 1);
            }

            if (Distance(points[0], points[1]) > Distance(points[1], points[2]))
            {
                Debug.WriteLine(CalculateAngle(points[0], points[1]));
            }
            else
            {
                Debug.WriteLine(CalculateAngle(points[1], points[2]));
            }
        }

        Cv2.ImShow("Цель", mat);

        TestArrow2();
    }

    static double CalculateAngle(Point2f point1, Point2f point2)
    {
        var angleRadians = Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
        var angleDegrees = angleRadians * (180 / Math.PI);

        // Отрегулируйте угол до положительного значения
        if (angleDegrees < 0)
        {
            angleDegrees += 360;
        }

        return angleDegrees;
    }

    public static void FloodFill()
    {
        var mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Распознавание стрелок\s1.png", ImreadModes.Color);
        Cv2.GaussianBlur(mat, mat, new Size(3, 3), 0);
        var splitMat = mat.Split();

        //for (int i = 0; i < splitMat.Length; i++)
        //{
        //    Cv2.ImShow($"splitMat{i}", splitMat[i]);
        //}

        // 1. Побитовое И красного и синего каналов
        var red = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[0], new Scalar(250), new Scalar(255), red);
        //Cv2.ImShow("red", red);
        var blue = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[2], new Scalar(0), new Scalar(10), blue);
        //Cv2.ImShow("blue", blue);
        var andMat = new Mat(mat.Size(), MatType.CV_8UC1);

        Cv2.BitwiseAnd(red, blue, andMat);
        Cv2.ImShow("andMat2", andMat);

        // найти контуры
        Cv2.FindContours(andMat, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Mat dst = Mat.Zeros(andMat.Size(), MatType.CV_8UC3);
        for (int i = 0; i < contours.Length; i++)
        {
            Cv2.DrawContours(dst, contours, i, Scalar.Red, 1, LineTypes.Link4, hierarchy);
        }

        Cv2.ImShow("найти контуры", dst);

        // Вычислить наибольший ограничивающий прямоугольник
        if (contours.Length > 0)
        {
            var boxes = contours.Select(Cv2.BoundingRect).Where(w => w.Height >= 2);
            var boxArray = boxes as Rect[] ?? boxes.ToArray();
            if (boxArray.Count() != 1)
            {
                throw new Exception("Найти несколько ограничивающих прямоугольников");
            }

            var box = boxArray.First();

            // Вырезать участки, подлежащие затоплению（увеличить4временная площадь）
            var newSrcMat = new Mat(mat, new Rect(box.X - box.Width / 2, box.Y - box.Height / 2, box.Width * 2, box.Height * 2));
            Cv2.ImShow("Вырезать участки, подлежащие затоплению", newSrcMat);

            // Центральная точка как исходная точка
            var seedPoint = new Point(newSrcMat.Width / 2, newSrcMat.Height / 2);

            // заливка
            Cv2.FloodFill(newSrcMat, seedPoint, Scalar.White, out _, new Scalar());
        }
    }

    public static void Hsv()
    {
        var mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Распознавание стрелок\e1.png", ImreadModes.Color);
        // Cv2.GaussianBlur(mat, mat, new Size(3, 3), 0);
        var splitMat = mat.Split();

        // 1. Побитовое И красного и синего каналов
        var red = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[0], new Scalar(250), new Scalar(255), red);
        var blue = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.InRange(splitMat[2], new Scalar(0), new Scalar(10), blue);
        var andMat = new Mat(mat.Size(), MatType.CV_8UC1);

        Cv2.BitwiseAnd(red, blue, andMat);
        Cv2.ImShow("andMat2", andMat);

        // найти контуры
        Cv2.FindContours(andMat, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        Mat dst = Mat.Zeros(andMat.Size(), MatType.CV_8UC3);
        for (int i = 0; i < contours.Length; i++)
        {
            Cv2.DrawContours(dst, contours, i, Scalar.Red, 1, LineTypes.Link4, hierarchy);
        }

        Cv2.ImShow("найти контуры", dst);

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
                // Нарисуйте контур треугольника на изображении.
                Cv2.DrawContours(mat, new Point[][] { approx }, -1, Scalar.Green, 1);

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
                Cv2.ImShow("Вырезаем место, где находится треугольник.", hsvMat);
                Cv2.ImShow("HSV Порог для удаления центральной стрелы", hsvThresholdMat);

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
                        correctP1 = midPoint;
                        correctP2 = targetPoint;
                    }
                }
                Cv2.Line(mat, correctP1, correctP2 + (correctP2 - correctP1) * 3, Scalar.Red, 1);
                Cv2.ImShow("Окончательные результаты", mat);
            }
        }
    }
}
