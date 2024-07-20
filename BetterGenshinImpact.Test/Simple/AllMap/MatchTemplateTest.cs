using BetterGenshinImpact.Core.Recognition.OpenCv;
using OpenCvSharp;

namespace BetterGenshinImpact.Test.Simple.AllMap;

public class MatchTemplateTest
{
    public static readonly Size TemplateSize = new(240, 135);

    // Обрезать ненужные части（Левый160，начальство80，Вниз96）
    public static readonly Rect TemplateSizeRoi = new Rect(20, 10, TemplateSize.Width - 20, TemplateSize.Height - 22);

    public static void Test()
    {
        var tar = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Улучшенный Genshin Impact\Сравнивать\Наложение\Невозможно сопоставить.png", ImreadModes.Grayscale);
        var sTar = tar.Resize(new Size(240, 135), 0, 0, InterpolationFlags.Cubic);
        // sTar = new Mat(sTar, TemplateSizeRoi);
        Cv2.ImShow("sTar", sTar);
        var src = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Улучшенный Genshin Impact\combined_image_small.png", ImreadModes.Grayscale);
        var src2 = src.Clone();
        var p = MatchTemplateWithGaussianBlur(src, sTar, TemplateMatchModes.CCoeffNormed, null, 0.1);

        Cv2.Rectangle(src2, new Rect(p.X, p.Y, sTar.Width, sTar.Height), new Scalar(0, 0, 255));

        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\Улучшенный Genshin Impact\x1.png", src2);
    }

    public static void TestTrack()
    {
        var tar = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Сюжет миссии и отслеживание карты\blue_task_point_28x.png", ImreadModes.Grayscale);
        var src = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Сюжет миссии и отслеживание карты\202404050232291883.png", ImreadModes.Grayscale);
        var src2 = src.Clone();
        var p = MatchTemplateHelper.MatchTemplate(src, tar, TemplateMatchModes.CCoeffNormed, null, 0.2);
        Cv2.Rectangle(src2, new Rect(p.X, p.Y, tar.Width, tar.Height), new Scalar(0, 0, 255), 1);
        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Сюжет миссии и отслеживание карты\rec_b1.png", src2);
    }

    public static void TestMenu()
    {
        var tar = new Mat(@"D:\HuiPrograming\Projects\CSharp\MiHoYo\BetterGenshinImpact\BetterGenshinImpact\GameTask\Common\Element\Assets\1920x1080\paimon_menu.png", ImreadModes.Color);
        var src = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Улучшенный Genshin Impact\Сравнивать\меню\Clip_20240331_155210.png", ImreadModes.Color);
        var src2 = src.Clone();
        var p = MatchTemplateHelper.MatchTemplate(src, tar, TemplateMatchModes.CCoeffNormed, null, 0.1);
        Cv2.Rectangle(src2, new Rect(p.X, p.Y, tar.Width, tar.Height), new Scalar(0, 0, 255), 1);
        // Нарисуйте местоположение на мини-карте
        // Эта картинка38x40 мини-карта210x210 мини-картаЛевыйначальствоУгловое положение 24,-15
        Cv2.Rectangle(src2, new Rect(p.X + 24, p.Y - 15, 210, 210), Scalar.Blue, 1);
        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\Улучшенный Genshin Impact\Сравнивать\меню\rec2.png", src2);
    }

    public static void TestTeamAvatar()
    {
        var tar = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Авто бой\Идентификация команды\Определите материал аватара\UI_AvatarIcon_Side_Zhongli_2.png", ImreadModes.Unchanged);
        // tar = tar.Resize(new Size(108, 108), 0, 0, InterpolationFlags.Cubic);
        var channels = tar.Split();
        for (int i = 0; i < 3; i++)
        {
            channels[i] &= channels[3];
        }
        Cv2.Merge(channels[..3], tar);
        Cv2.ImShow("tar.png", tar);
        var src = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Авто бой\Идентификация команды\1.png", ImreadModes.Color);
        var src2 = src.Clone();
        var p = MatchTemplateWithGaussianBlur(src, tar, TemplateMatchModes.CCoeffNormed, null, 0.1);
        Cv2.Rectangle(src2, new Rect(p.X, p.Y, tar.Width, tar.Height), new Scalar(0, 0, 255), 1);
        // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Авто бой\Идентификация команды\rec_1.png", src2);
        Cv2.ImShow("src2", src2);
    }

    public static Point MatchTemplateWithGaussianBlur(Mat srcMat, Mat dstMat, TemplateMatchModes matchMode, Mat? maskMat = null, double threshold = 0.8)
    {
        try
        {
            var result = new Mat();
            if (maskMat == null)
            {
                Cv2.MatchTemplate(srcMat, dstMat, result, matchMode);
            }
            else
            {
                Cv2.MatchTemplate(srcMat, dstMat, result, matchMode, maskMat);
            }

            if (matchMode is TemplateMatchModes.SqDiff or TemplateMatchModes.CCoeff or TemplateMatchModes.CCorr)
            {
                Cv2.Normalize(result, result, 0, 1, NormTypes.MinMax, -1, null);
            }
            using var blurResult = new Mat();
            Cv2.GaussianBlur(result, blurResult, new Size(1, 1), 0);
            Cv2.ImShow("blurResult", blurResult);
            Cv2.Subtract(result, blurResult, result);

            Cv2.MinMaxLoc(result, out var minValue, out var maxValue, out var minLoc, out var maxLoc);
            Cv2.ImShow("result", result);
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

            return new Point();
        }
        catch (Exception ex)
        {
            return new Point();
        }
    }
}
