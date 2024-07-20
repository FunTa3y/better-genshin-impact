﻿using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace BetterGenshinImpact.Core.Recognition.OpenCv;

/// <summary>
///     Соответствие нескольких целевых шаблоновdemo
///     https://github.com/shimat/opencvsharp/issues/182
/// </summary>
public class MatchTemplateHelper
{
    private static readonly ILogger<MatchTemplateHelper> _logger = App.GetLogger<MatchTemplateHelper>();

    /// <summary>
    ///  соответствие шаблону
    /// </summary>
    /// <param name="srcMat">исходное изображение</param>
    /// <param name="dstMat">шаблон</param>
    /// <param name="matchMode">Метод сопоставления</param>
    /// <param name="maskMat">маска</param>
    /// <param name="threshold">порог</param>
    /// <returns>знак препинания в левом верхнем углу,потому что(0,0)баллы как непревзойденные результаты，Поэтому вы не можете сделать то же самоесоответствие шаблону</returns>
    public static Point MatchTemplate(Mat srcMat, Mat dstMat, TemplateMatchModes matchMode, Mat? maskMat = null, double threshold = 0.8)
    {
        try
        {
            using var result = new Mat();
            Cv2.MatchTemplate(srcMat, dstMat, result, matchMode, maskMat!);

            if (matchMode is TemplateMatchModes.SqDiff or TemplateMatchModes.CCoeff or TemplateMatchModes.CCorr)
            {
                Cv2.Normalize(result, result, 0, 1, NormTypes.MinMax);
            }

            Cv2.MinMaxLoc(result, out var minValue, out var maxValue, out var minLoc, out var maxLoc);

            if (matchMode is TemplateMatchModes.SqDiff or TemplateMatchModes.SqDiffNormed)
            {
                if (minValue <= 1 - threshold)
                {
                    return minLoc;
                }
            }
            else
            {
                if (maxValue >= threshold)
                {
                    return maxLoc;
                }
            }

            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            _logger.LogDebug(ex, ex.Message);
            return default;
        }
    }

    /// <summary>
    ///     соответствие шаблонунесколько результатов
    ///     трудно использовать
    /// </summary>
    /// <param name="srcMat"></param>
    /// <param name="dstMat"></param>
    /// <param name="maskMat"></param>
    /// <param name="threshold"></param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    [Obsolete]
    public static List<Point> MatchTemplateMulti(Mat srcMat, Mat dstMat, Mat? maskMat = null, double threshold = 0.8, int maxCount = 8)
    {
        var points = new List<Point>();
        try
        {
            using var result = new Mat();
            Cv2.MatchTemplate(srcMat, dstMat, result, TemplateMatchModes.CCoeffNormed, maskMat!);

            var mask = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.White);
            var maskSub = new Mat(result.Height, result.Width, MatType.CV_8UC1, Scalar.Black);
            while (true)
            {
                Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out var maxLoc, mask);
                var maskRect = new Rect(maxLoc.X, maxLoc.Y, dstMat.Width, dstMat.Height);
                maskSub.Rectangle(maskRect, Scalar.White, -1);
                mask -= maskSub;
                if (maxValue >= threshold)
                    points.Add(maxLoc);
                else
                    break;
            }

            return points;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            _logger.LogDebug(ex, ex.Message);
            return points;
        }
    }

    public static List<Point> MatchTemplateMulti(Mat srcMat, Mat dstMat, double threshold)
    {
        return MatchTemplateMulti(srcMat, dstMat, null, threshold);
    }

    /// <summary>
    ///     Найти несколько на одном изображениишаблон
    ///     Я нашел глупый способ прикрыть друг друга，низкая эффективность，но очень точный
    /// </summary>
    /// <param name="srcMat"></param>
    /// <param name="imgSubDictionary"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static Dictionary<string, List<Point>> MatchMultiPicForOnePic(Mat srcMat, Dictionary<string, Mat> imgSubDictionary, double threshold = 0.8)
    {
        var dictionary = new Dictionary<string, List<Point>>();
        foreach (var kvp in imgSubDictionary)
        {
            var list = new List<Point>();

            while (true)
            {
                var point = MatchTemplate(srcMat, kvp.Value, TemplateMatchModes.CCoeffNormed, null, threshold);
                if (point != new Point())
                {
                    // скрыть результат，Избегайте дублирования идентификации
                    Cv2.Rectangle(srcMat, point, new Point(point.X + kvp.Value.Width, point.Y + kvp.Value.Height), Scalar.Black, -1);
                    list.Add(point);
                }
                else
                {
                    break;
                }
            }

            dictionary.Add(kvp.Key, list);
        }

        return dictionary;
    }

    /// <summary>
    ///     Найти несколько на одном изображениишаблон
    ///     Я нашел глупый способ прикрыть друг друга，низкая эффективность，но очень точный
    /// </summary>
    /// <param name="srcMat"></param>
    /// <param name="imgSubList"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static List<Rect> MatchMultiPicForOnePic(Mat srcMat, List<Mat> imgSubList, double threshold = 0.8)
    {
        List<Rect> list = new();
        foreach (var sub in imgSubList)
            while (true)
            {
                var point = MatchTemplate(srcMat, sub, TemplateMatchModes.CCoeffNormed, null, threshold);
                if (point != new Point())
                {
                    // скрыть результат，Избегайте дублирования идентификации
                    Cv2.Rectangle(srcMat, point, new Point(point.X + sub.Width, point.Y + sub.Height), Scalar.Black, -1);
                    list.Add(new Rect(point.X, point.Y, sub.Width, sub.Height));
                }
                else
                {
                    break;
                }
            }

        return list;
    }

    /// <summary>
    ///     Найдите каждого на картинкешаблон
    /// </summary>
    /// <param name="srcMat"></param>
    /// <param name="dstMat"></param>
    /// <param name="maskMat"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static List<Rect> MatchOnePicForOnePic(Mat srcMat, Mat dstMat, Mat? maskMat = null, double threshold = 0.8)
    {
        List<Rect> list = new();

        while (true)
        {
            var point = MatchTemplate(srcMat, dstMat, TemplateMatchModes.CCoeffNormed, maskMat, threshold);
            if (point != new Point())
            {
                // скрыть результат，Избегайте дублирования идентификации
                Cv2.Rectangle(srcMat, point, new Point(point.X + dstMat.Width, point.Y + dstMat.Height), Scalar.Black, -1);
                list.Add(new Rect(point.X, point.Y, dstMat.Width, dstMat.Height));
            }
            else
            {
                break;
            }
        }

        return list;
    }

    /// <summary>
    ///    Найдите каждого на картинкешаблон
    /// </summary>
    /// <param name="srcMat"></param>
    /// <param name="dstMat"></param>
    /// <param name="matchMode"></param>
    /// <param name="maskMat"></param>
    /// <param name="threshold"></param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    public static List<Rect> MatchOnePicForOnePic(Mat srcMat, Mat dstMat, TemplateMatchModes matchMode, Mat? maskMat, double threshold, int maxCount = -1)
    {
        List<Rect> list = new();

        if (maxCount < 0)
        {
            maxCount = srcMat.Width * srcMat.Height / dstMat.Width / dstMat.Height;
        }

        for (int i = 0; i < maxCount; i++)
        {
            var point = MatchTemplate(srcMat, dstMat, matchMode, maskMat, threshold);
            if (point != new Point())
            {
                // скрыть результат，Избегайте дублирования идентификации
                Cv2.Rectangle(srcMat, point, new Point(point.X + dstMat.Width, point.Y + dstMat.Height), Scalar.Black, -1);
                list.Add(new Rect(point.X, point.Y, dstMat.Width, dstMat.Height));
            }
            else
            {
                break;
            }
        }

        return list;
    }
}
