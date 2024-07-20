using OpenCvSharp;
using System.Diagnostics;

namespace BetterGenshinImpact.Core.Recognition.OpenCv;

public class OpenCvCommonHelper
{
    /// <summary>
    ///     Подсчитайте количество пикселей определенного цвета в изображении в оттенках серого.
    ///     Метод быстрого обхода происходит от: https://blog.csdn.net/TyroneKing/article/details/129108838
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static int CountGrayMatColor(Mat mat, byte color)
    {
        Debug.Assert(mat.Depth() == MatType.CV_8U);
        var channels = mat.Channels();
        var nRows = mat.Rows;
        var nCols = mat.Cols * channels;
        if (mat.IsContinuous())
        {
            nCols *= nRows;
            nRows = 1;
        }

        var sum = 0;
        unsafe
        {
            for (var i = 0; i < nRows; i++)
            {
                var p = mat.Ptr(i);
                var b = (byte*)p.ToPointer();
                for (var j = 0; j < nCols; j++)
                    if (b[j] == color)
                        sum++;
            }
        }

        return sum;
    }

    public static int CountGrayMatColor(Mat mat, byte lowColor, byte highColor)
    {
        Debug.Assert(mat.Depth() == MatType.CV_8U);
        var channels = mat.Channels();
        var nRows = mat.Rows;
        var nCols = mat.Cols * channels;
        if (mat.IsContinuous())
        {
            nCols *= nRows;
            nRows = 1;
        }

        var sum = 0;
        unsafe
        {
            for (var i = 0; i < nRows; i++)
            {
                var p = mat.Ptr(i);
                var b = (byte*)p.ToPointer();
                for (var j = 0; j < nCols; j++)
                    if (b[j] >= lowColor && b[j] <= highColor)
                        sum++;
            }
        }

        return sum;
    }

    public static Mat Threshold(Mat src, Scalar low, Scalar high)
    {
        using var mask = new Mat();
        using var rgbMat = new Mat();

        Cv2.CvtColor(src, rgbMat, ColorConversionCodes.BGR2RGB);
        Cv2.InRange(rgbMat, low, high, mask);
        Cv2.Threshold(mask, mask, 0, 255, ThresholdTypes.Binary); //Бинаризация
        return mask.Clone();
    }

    public static Mat InRangeHsv(Mat src, Scalar low, Scalar high)
    {
        using var mask = new Mat();
        using var rgbMat = new Mat();

        Cv2.CvtColor(src, rgbMat, ColorConversionCodes.BGR2HSV);
        Cv2.InRange(rgbMat, low, high, mask);
        return mask.Clone();
    }

    public static Mat Threshold(Mat src, Scalar s)
    {
        return Threshold(src, s, s);
    }

    /// <summary>
    ///     иБинаризацияЦвет как раз наоборот
    /// </summary>
    /// <param name="src"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Mat CreateMask(Mat src, Scalar s)
    {
        var mask = new Mat();
        Cv2.InRange(src, s, s, mask);
        return 255 - mask;
    }
}
