using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.Model;
using Microsoft.ML.OnnxRuntime;
using System.ComponentModel;

namespace BetterGenshinImpact.Core.Recognition.ONNX;

public class BgiSessionOption : Singleton<BgiSessionOption>
{
    public static string[] InferenceDeviceTypes { get; } = ["CPU", "GPU_DirectML"];

    public SessionOptions Options { get; set; } = TaskContext.Instance().Config.InferenceDevice switch
    {
        "CPU" => new SessionOptions(),
        "GPU_DirectML" => MakeSessionOptionWithDirectMlProvider(),
        _ => throw new InvalidEnumArgumentException("Недопустимое устройство вывода")
    };

    public static SessionOptions MakeSessionOptionWithDirectMlProvider()
    {
        var sessionOptions = new SessionOptions();
        sessionOptions.AppendExecutionProvider_DML(0);
        return sessionOptions;
    }

    // /// <summary>
    // /// Перезагрузите каждую рассуждение（Тестирование бесполезно，Можно только перезапустить）
    // /// </summary>
    // public void RefreshInference()
    // {
    //     // Автоматическое секретное царство будет происходить каждый раз.NEWВсе равно
    //     // Yap、автоматическая рыбалка
    //     GameTaskManager.RefreshTriggerConfigs();
    // }
}
