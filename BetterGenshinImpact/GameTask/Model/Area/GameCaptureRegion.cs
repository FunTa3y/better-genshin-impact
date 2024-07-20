using BetterGenshinImpact.GameTask.Model.Area.Converter;
using BetterGenshinImpact.View.Drawable;
using OpenCvSharp;
using System;
using System.Drawing;
using Size = OpenCvSharp.Size;

namespace BetterGenshinImpact.GameTask.Model.Area;

/// <summary>
/// Класс зоны захвата игры
/// В основном используется для преобразования в координаты окна маски.
/// </summary>
public class GameCaptureRegion(Bitmap bitmap, int initX, int initY, Region? owner = null, INodeConverter? converter = null) : ImageRegion(bitmap, initX, initY, owner, converter)
{
    /// <summary>
    /// Преобразуйте координатные размеры изображения захвата игры в координатные размеры окна маски.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="pen"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public RectDrawable ConvertToRectDrawable(int x, int y, int w, int h, Pen? pen = null, string? name = null)
    {
        var scale = TaskContext.Instance().DpiScale;
        System.Windows.Rect newRect = new(x / scale, y / scale, w / scale, h / scale);
        return new RectDrawable(newRect, pen, name);
    }

    /// <summary>
    /// Преобразуйте координатные размеры изображения захвата игры в координатные размеры окна маски.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="pen"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public LineDrawable ConvertToLineDrawable(int x1, int y1, int x2, int y2, Pen? pen = null, string? name = null)
    {
        var scale = TaskContext.Instance().DpiScale;
        var drawable = new LineDrawable(x1 / scale, y1 / scale, x2 / scale, y2 / scale);
        if (pen != null)
        {
            drawable.Pen = pen;
        }
        return drawable;
    }

    // public void DrawRect(int x, int y, int w, int h, Pen? pen = null, string? name = null)
    // {
    //     VisionContext.Instance().DrawContent.PutRect(name ?? "None", ConvertToRectDrawable(x, y, w, h, pen, name));
    // }

    /// <summary>
    /// Начальный скриншот окна игры больше, чем1080Pравномерное преобразование в1080P
    /// </summary>
    /// <returns></returns>
    public ImageRegion DeriveTo1080P()
    {
        if (Width <= 1920)
        {
            return this;
        }
        var scale = Width / 1920d;

        var newMat = new Mat();
        Cv2.Resize(SrcMat, newMat, new Size(1920, Height / scale));
        _srcGreyMat?.Dispose();
        _srcMat?.Dispose();
        _srcBitmap?.Dispose();
        return new ImageRegion(newMat, 0, 0, this, new ScaleConverter(scale));
        // return new ImageRegion(newMat, 0, 0, this, new TranslationConverter(0, 0));
    }

    /// <summary>
    /// статический метод,Нажмите в игровой форме размер области захвата.
    /// </summary>
    /// <param name="posFunc">
    /// Реализовать метод вывода координат клика(Относительные координаты зоны захвата игры),Для использования в расчетах предусмотрены следующие параметры
    /// Size = Текущий размер области захвата игры
    /// double = Текущая область захвата игры, в которую нужно 1080P Коэффициент масштабирования
    /// Другими словами, магическое число внутри метода должно быть 1080PНомера под
    /// </param>
    public static void GameRegionClick(Func<Size, double, (double, double)> posFunc)
    {
        var captureAreaRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var assetScale = TaskContext.Instance().SystemInfo.ScaleTo1080PRatio;
        var (cx, cy) = posFunc(new Size(captureAreaRect.Width, captureAreaRect.Height), assetScale);
        DesktopRegion.DesktopRegionClick(captureAreaRect.X + cx, captureAreaRect.Y + cy);
    }

    public static void GameRegionMove(Func<Size, double, (double, double)> posFunc)
    {
        var captureAreaRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var assetScale = TaskContext.Instance().SystemInfo.ScaleTo1080PRatio;
        var (cx, cy) = posFunc(new Size(captureAreaRect.Width, captureAreaRect.Height), assetScale);
        DesktopRegion.DesktopRegionMove(captureAreaRect.X + cx, captureAreaRect.Y + cy);
    }

    /// <summary>
    /// статический метод,входить1080PКоординаты ниже,Метод автоматически преобразуется вТекущий размер области захвата игрыКоординаты нижеи нажмите
    /// </summary>
    /// <param name="cx"></param>
    /// <param name="cy"></param>
    public static void GameRegion1080PPosClick(double cx, double cy)
    {
        // 1080Pкоординировать Переключиться в настоящее окно игрыкоординировать
        GameRegionClick((_, scale) => (cx * scale, cy * scale));
    }

    public static void GameRegion1080PPosMove(double cx, double cy)
    {
        // 1080Pкоординировать Переключиться в настоящее окно игрыкоординировать
        GameRegionMove((_, scale) => (cx * scale, cy * scale));
    }
}
