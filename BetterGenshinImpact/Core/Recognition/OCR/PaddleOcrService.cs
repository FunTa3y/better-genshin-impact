using BetterGenshinImpact.Core.Config;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using BetterGenshinImpact.GameTask;

namespace BetterGenshinImpact.Core.Recognition.OCR;

public class PaddleOcrService : IOcrService
{
    private static readonly object locker = new();

    /// <summary>
    ///     Usage:
    ///     https://github.com/sdcb/PaddleSharp/blob/master/docs/ocr.md
    ///     Список моделей:
    ///     https://github.com/PaddlePaddle/PaddleOCR/blob/release/2.5/doc/doc_ch/models_list.md
    /// </summary>
    private readonly PaddleOcrAll _paddleOcrAll;

    public PaddleOcrService()
    {
        var path = Global.Absolute("Assets\\Model\\PaddleOcr");
        var localDetModel = DetectionModel.FromDirectory(Path.Combine(path, "ch_PP-OCRv4_det"), ModelVersion.V4);
        var localClsModel = ClassificationModel.FromDirectory(Path.Combine(path, "ch_ppocr_mobile_v2.0_cls"));
        var localRecModel = RecognizationModel.FromDirectory(Path.Combine(path, "ch_PP-OCRv4_rec"), Path.Combine(path, "ppocr_keys_v1.txt"), ModelVersion.V4);
        var model = new FullOcrModel(localDetModel, localClsModel, localRecModel);
        // Action<PaddleConfig> device = TaskContext.Instance().Config.InferenceDevice switch
        // {
        //     "CPU" => PaddleDevice.Onnx(),
        //     "GPU_DirectML" => PaddleDevice.Onnx(),
        //     _ => throw new InvalidEnumArgumentException("Недопустимое устройство вывода")
        // };
        _paddleOcrAll = new PaddleOcrAll(model, PaddleDevice.Onnx())
        {
            AllowRotateDetection = false, /* Позволяет распознавать наклонный текст */
            Enable180Classification = false /* Позволяет идентифицировать углы поворота, превышающие90текст степени */
        };

        // System.AccessViolationException
        // https://github.com/babalae/better-genshin-impact/releases/latest
        // Загрузите и распакуйте в тот же каталог.
    }

    public string Ocr(Mat mat)
    {
        return OcrResult(mat).Text;
    }

    public PaddleOcrResult OcrResult(Mat mat)
    {
        lock (locker)
        {
            long startTime = Stopwatch.GetTimestamp();
            var result = _paddleOcrAll.Run(mat);
            TimeSpan time = Stopwatch.GetElapsedTime(startTime);
            Debug.WriteLine($"PaddleOcr кропотливый {time.TotalMilliseconds}ms результат: {result.Text}");
            return result;
        }
    }

    public string OcrWithoutDetector(Mat mat)
    {
        lock (locker)
        {
            var str = _paddleOcrAll.Recognizer.Run(mat).Text;
            Debug.WriteLine($"PaddleOcrWithoutDetector результат: {str}");
            return str;
        }
    }
}
