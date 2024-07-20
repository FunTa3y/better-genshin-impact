﻿using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.View.Drawable;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace BetterGenshinImpact.GameTask.Placeholder;

/// <summary>
/// Идентификатор для тестирования разработки、Или глобальный триггер-заполнитель
/// Когда этот триггер активируется，прямая монополия
/// </summary>
public class TestTrigger : ITaskTrigger
{
    public string Name => "Пользовательский триггер-заполнитель";
    public bool IsEnabled { get; set; }
    public int Priority => 9999;
    public bool IsExclusive { get; private set; }

    private readonly Pen _pen = new(Color.Coral, 1);

    //private readonly AutoGeniusInvokationAssets _autoGeniusInvokationAssets;

    // private readonly YoloV8 _predictor = new(Global.Absolute("Assets\\Model\\Domain\\bgi_tree.onnx"));

    public TestTrigger()
    {
        var info = TaskContext.Instance().SystemInfo;
        //_autoGeniusInvokationAssets = new AutoGeniusInvokationAssets();
    }

    public void Init()
    {
        IsEnabled = false;
        IsExclusive = false;
    }

    public void OnCapture(CaptureContent content)
    {
        // TestArrow(content);
        // TestCamera(content);
        // Detect(content);
        // var angle = CameraOrientation.Compute(content);
        // Debug.WriteLine(angle);
        // if (angle < 180)
        // {
        //     // Сместить перспективу влево
        //     Simulation.SendInputEx.Mouse.MoveMouseBy(-angle, 0);
        // }
        // else if (angle is > 180 and < 360)
        // {
        //     // Сдвинуть перспективу вправо
        //     Simulation.SendInputEx.Mouse.MoveMouseBy(360 - angle, 0);
        // }
        // else
        // {
        //     // 360 Тратить Восточная перспектива
        // }

        //var dictionary = GeniusInvokationControl.FindMultiPicFromOneImage2OneByOne(content.CaptureRectArea.SrcGreyMat, _autoGeniusInvokationAssets.RollPhaseDiceMats, 0.7);
        //if (dictionary.Count > 0)
        //{
        //    int i = 0;
        //    foreach (var pair in dictionary)
        //    {
        //        var list = pair.Value;

        //        foreach (var p in list)
        //        {
        //            i++;
        //            VisionContext.Instance().DrawContent.PutRect("i" + i,
        //                new RectDrawable(new Rect(p.X, p.Y,
        //                    _autoGeniusInvokationAssets.RollPhaseDiceMats[pair.Key].Width,
        //                    _autoGeniusInvokationAssets.RollPhaseDiceMats[pair.Key].Height)));
        //        }
        //    }
        //    Debug.WriteLine("нашел это" + i + "индивидуальный");
        //}

        //var foundRectArea = content.CaptureRectArea.Find(_autoGeniusInvokationAssets.ElementalTuningConfirmButtonRo);
        //if (!foundRectArea.IsEmpty())
        //{
        //    Debug.WriteLine("нашел это");
        //}
        //else
        //{
        //    Debug.WriteLine("не нашел");
        //}

        // Потому что на изучение картинок уходит больше времени
        // var tar = ElementAssets.Instance.PaimonMenuRo.TemplateImageGreyMat!;
        // var p = MatchTemplateHelper.MatchTemplate(content.CaptureRectArea.SrcGreyMat, tar, TemplateMatchModes.CCoeffNormed, null, 0.9);
        // if (p.X == 0 || p.Y == 0)
        // {
        //     return;
        // }
        //
        // var miniMapMat = new Mat(content.CaptureRectArea.SrcGreyMat, new Rect(p.X + 24, p.Y - 15, 210, 210));
        // var mask = new Mat(new Size(miniMapMat.Width, miniMapMat.Height), MatType.CV_8UC1, Scalar.Black);
        // Cv2.Circle(mask, new Point(miniMapMat.Width / 2, miniMapMat.Height / 2), 90, Scalar.White, -1);
        // var res = new Mat();
        // Cv2.BitwiseAnd(miniMapMat, miniMapMat, res, mask);
        // EntireMap.Instance.GetMapPositionAndDrawByFeatureMatch(res);
        // Cv2.ImWrite(Global.Absolute(@"log\minimap.png"), res);

        // Тест на большой карте
        var mat = content.CaptureRectArea.SrcGreyMat;
        // mat = mat.Resize(new Size(240, 135));
        Rect rect = BigMap.Instance.GetBigMapPositionByFeatureMatch(mat);

        var s = 2.56;
        WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "UpdateBigMapRect", new object(),
            new System.Windows.Rect(rect.X / s, rect.Y / s, rect.Width / s, rect.Height / s)));

        // Bv.BigMapIsUnderground(content.CaptureRectArea);
    }

    // private void Detect(CaptureContent content)
    // {
    //     using var memoryStream = new MemoryStream();
    //     content.CaptureRectArea.SrcBitmap.Save(memoryStream, ImageFormat.Bmp);
    //     memoryStream.Seek(0, SeekOrigin.Begin);
    //     var result = _predictor.Detect(memoryStream);
    //     Debug.WriteLine(result);
    //     var list = new List<RectDrawable>();
    //     foreach (var box in result.Boxes)
    //     {
    //         var rect = new System.Windows.Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
    //         list.Add(new RectDrawable(rect, _pen));
    //     }
    //
    //     VisionContext.Instance().DrawContent.PutOrRemoveRectList("TreeBox", list);
    // }

    public static void TestArrow(CaptureContent content)
    {
        var mat = new Mat(content.CaptureRectArea.SrcMat, new Rect(0, 0, 300, 240));

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

            // Если подобранный многоугольник имеет трииндивидуальныйвершина，Думайте об этом как о треугольнике
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

                // Цикл для расчета середин трех сторон,и рассчитатьВсе точки от середины до вершинысплошных черных пикселей виндивидуальныйчисло
                var maxBlackCount = 0;
                Point correctP1 = new(), correctP2 = new();
                var offset = new Point(maxRect.X, maxRect.Y);
                for (int i = 0; i < 3; i++)
                {
                    var midPoint = Midpoint(approx[i], approx[(i + 1) % 3]);
                    var targetPoint = approx[(i + 2) % 3];

                    // Все точки от середины до вершины
                    var lineIterator = new LineIterator(hsvThresholdMat, midPoint - offset, targetPoint - offset, PixelConnectivity.Connectivity8);

                    // Подсчитать количество последовательных черных пикселейиндивидуальныйчисло
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

                // VisionContext.Instance().DrawContent.PutLine("co", new LineDrawable(correctP1, correctP2 + (correctP2 - correctP1) * 3));
            }
        }
    }

    static Point Midpoint(Point p1, Point p2)
    {
        var midX = (p1.X + p2.X) / 2;
        var midY = (p1.Y + p2.Y) / 2;
        return new Point(midX, midY);
    }

    public void TestCamera(CaptureContent content)
    {
        var mat = new Mat(content.CaptureRectArea.SrcGreyMat, new Rect(62, 19, 212, 212));
        Cv2.GaussianBlur(mat, mat, new Size(3, 3), 0);
        // разложение полярных координат
        var centerPoint = new Point2f(mat.Width / 2f, mat.Height / 2f);
        var polarMat = new Mat();
        Cv2.WarpPolar(mat, polarMat, new Size(360, 360), centerPoint, 360d, InterpolationFlags.Linear, WarpPolarMode.Linear);
        // Cv2.ImShow("polarMat", polarMat);
        var polarRoiMat = new Mat(polarMat, new Rect(10, 0, 70, polarMat.Height));
        Cv2.Rotate(polarRoiMat, polarRoiMat, RotateFlags.Rotate90Counterclockwise);

        var scharrResult = new Mat();
        Cv2.Scharr(polarRoiMat, scharrResult, MatType.CV_32F, 1, 0);

        // Найдите гребень волны
        var left = new int[360];
        var right = new int[360];

        scharrResult.GetArray<float>(out var array);
        var leftPeaks = FindPeaks(array);
        leftPeaks.ForEach(i => left[i % 360]++);

        var reversedArray = array.Select(x => -x).ToArray();
        var rightPeaks = FindPeaks(reversedArray);
        rightPeaks.ForEach(i => right[i % 360]++);

        // оптимизация
        var left2 = left.Zip(right, (x, y) => Math.Max(x - y, 0)).ToArray();
        var right2 = right.Zip(left, (x, y) => Math.Max(x - y, 0)).ToArray();

        // Сдвиньте влево и умножьте Рядом2°Найдите максимальное значение в пределах
        var sum = new int[360];
        for (var i = -2; i <= 2; i++)
        {
            var all = left2.Zip(Shift(right2, -90 + i), (x, y) => x * y * (3 - Math.Abs(i)) / 3).ToArray();
            sum = sum.Zip(all, (x, y) => x + y).ToArray();
        }

        // свертка
        var result = new int[360];
        for (var i = -2; i <= 2; i++)
        {
            var all = Shift(sum, i);
            for (var j = 0; j < all.Length; j++)
            {
                all[j] = all[j] * (3 - Math.Abs(i)) / 3;
            }

            result = result.Zip(all, (x, y) => x + y).ToArray();
        }

        // Рассчитанный угол результатаТратить
        var maxIndex = result.ToList().IndexOf(result.Max());
        var angle = maxIndex + 45;

        const int r = 100;
        var center = new Point(168, 125);
        var x1 = center.X + r * Math.Cos(angle * Math.PI / 180);
        var y1 = center.Y + r * Math.Sin(angle * Math.PI / 180);

        // var line = new LineDrawable(center, new Point(x1, y1));
        // line.Pen = new Pen(Color.Yellow, 1);
        // VisionContext.Instance().DrawContent.PutLine("camera", line);
    }

    static List<int> FindPeaks(float[] data)
    {
        List<int> peakIndices = new List<int>();

        for (int i = 1; i < data.Length - 1; i++)
        {
            if (data[i] > data[i - 1] && data[i] > data[i + 1])
            {
                peakIndices.Add(i);
            }
        }

        return peakIndices;
    }

    public static int[] RightShift(int[] array, int k)
    {
        return array.Skip(array.Length - k)
            .Concat(array.Take(array.Length - k))
            .ToArray();
    }

    public static int[] LeftShift(int[] array, int k)
    {
        return array.Skip(k)
            .Concat(array.Take(k))
            .ToArray();
    }

    public static int[] Shift(int[] array, int k)
    {
        if (k > 0)
        {
            return RightShift(array, k);
        }
        else
        {
            return LeftShift(array, -k);
        }
    }
}
