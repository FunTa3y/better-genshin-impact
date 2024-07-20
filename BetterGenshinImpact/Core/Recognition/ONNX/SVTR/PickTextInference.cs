using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace BetterGenshinImpact.Core.Recognition.ONNX.SVTR;

/// <summary>
///     От Yap Распознавание выбранного текста
///     https://github.com/Alex-Beng/Yap
/// </summary>
public class PickTextInference : ITextInference
{
    private readonly InferenceSession _session;
    private readonly Dictionary<int, string> _wordDictionary;

    public PickTextInference()
    {
        var modelPath = Global.Absolute("Assets\\Model\\Yap\\model_training.onnx");
        if (!File.Exists(modelPath)) throw new FileNotFoundException("YapФайл модели не существует", modelPath);

        _session = new InferenceSession(modelPath, BgiSessionOption.Instance.Options);

        var wordJsonPath = Global.Absolute("Assets\\Model\\Yap\\index_2_word.json");
        if (!File.Exists(wordJsonPath)) throw new FileNotFoundException("YapФайл словаря не существует", wordJsonPath);

        var json = File.ReadAllText(wordJsonPath);
        _wordDictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? throw new Exception("index_2_word.json deserialize failed");
    }

    public string Inference(Mat mat)
    {
        long startTime = Stopwatch.GetTimestamp();
        // Настройте входные данные так, чтобы (1, 1, 32, 384) Тензор формы
        var reshapedInputData = ToTensorUnsafe(mat, out var owner);

        IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results;

        using (owner)
        {
            // Создать ввод NamedOnnxValue, Запустить вывод модели
            results = _session.Run([NamedOnnxValue.CreateFromTensor("input", reshapedInputData)]);
        }

        using (results)
        {
            // Получить выходные данные
            var boxes = results[0].AsTensor<float>();

            var ans = new StringBuilder();
            var lastWord = default(string);
            for (var i = 0; i < boxes.Dimensions[0]; i++)
            {
                var maxIndex = 0;
                var maxValue = -1.0;
                for (var j = 0; j < _wordDictionary.Count; j++)
                {
                    var value = boxes[[i, 0, j]];
                    if (value > maxValue)
                    {
                        maxValue = value;
                        maxIndex = j;
                    }
                }

                var word = _wordDictionary[maxIndex];
                if (word != lastWord && word != "|")
                {
                    ans.Append(word);
                }

                lastWord = word;
            }

            TimeSpan time = Stopwatch.GetElapsedTime(startTime);
            string result = ans.ToString();
            Debug.WriteLine($"YapИдентификация модели кропотливый{time.TotalMilliseconds}ms результат: {result}");
            return result;
        }
    }

    public static Tensor<float> ToTensorUnsafe(Mat src, out IMemoryOwner<float> tensorMemoryOwnser)
    {
        var channels = src.Channels();
        var nRows = src.Rows;
        var nCols = src.Cols * channels;
        if (src.IsContinuous())
        {
            nCols *= nRows;
            nRows = 1;
        }

        //var inputData = new float[nCols];
        tensorMemoryOwnser = MemoryPool<float>.Shared.Rent(nCols);
        var memory = tensorMemoryOwnser.Memory[..nCols];
        unsafe
        {
            for (var i = 0; i < nRows; i++)
            {
                var b = (byte*)src.Ptr(i);
                for (var j = 0; j < nCols; j++)
                {
                    memory.Span[j] = b[j] / 255f;
                }
            }
        }

        return new DenseTensor<float>(memory, [1, 1, 32, 384]);
    }
}
