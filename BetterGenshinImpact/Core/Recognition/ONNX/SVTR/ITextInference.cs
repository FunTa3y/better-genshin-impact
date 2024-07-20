using OpenCvSharp;

namespace BetterGenshinImpact.Core.Recognition.ONNX.SVTR;

/// <summary>
///     рассуждение о распознавании текста(SVTRсеть)
/// </summary>
public interface ITextInference
{
    public string Inference(Mat mat);
}
