using System.Diagnostics;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX.SVTR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using OpenCvSharp;

namespace BetterGenshinImpact.Test.Simple;

public class OcrTest
{
    public static void TestYap()
    {
        Mat mat = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Временные файлы\fuben_jueyuan.png", ImreadModes.Grayscale);
        var text = TextInferenceFactory.Pick.Inference(PreProcessForInference(mat));
        Debug.WriteLine(text);

        Mat mat2 = Cv2.ImRead(@"E:\HuiTask\Улучшенный Genshin Impact\Временные файлы\fuben_jueyuan.png", ImreadModes.Grayscale);
        var text2 = OcrFactory.Paddle.Ocr(mat2);
        Debug.WriteLine(text2);
    }

    private static Mat PreProcessForInference(Mat mat)
    {
        // Yap Уже перешёл на оттенки серого https://github.com/Alex-Beng/Yap/commit/c2ad1e7b1442aaf2d80782a032e00876cd1c6c84
        // Бинаризация
        // Cv2.Threshold(mat, mat, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
        //Cv2.AdaptiveThreshold(mat, mat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 31, 3); // Эффект хороший Но это не соответствует модели.
        //mat = OpenCvCommonHelper.Threshold(mat, Scalar.FromRgb(235, 235, 235), Scalar.FromRgb(255, 255, 255)); // Не умею идентифицировать предметы
        // Я не знаю, почему он вынужден растягиваться до 221x32
        mat = ResizeHelper.ResizeTo(mat, 221, 32);
        // заполнить до 384x32
        var padded = new Mat(new Size(384, 32), MatType.CV_8UC1, Scalar.Black);
        padded[new Rect(0, 0, mat.Width, mat.Height)] = mat;
        //Cv2.ImWrite(Global.Absolute("padded.png"), padded);
        return padded;
    }
}
