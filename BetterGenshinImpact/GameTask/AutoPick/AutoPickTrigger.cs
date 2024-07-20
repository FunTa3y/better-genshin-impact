using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX.SVTR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoPick.Assets;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BetterGenshinImpact.Core.Recognition;
using Vanara.PInvoke;
using System.Windows.Input;

namespace BetterGenshinImpact.GameTask.AutoPick;

public class AutoPickTrigger : ITaskTrigger
{
    private readonly ILogger<AutoPickTrigger> _logger = App.GetLogger<AutoPickTrigger>();
    private readonly ITextInference _pickTextInference = TextInferenceFactory.Pick;

    public string Name => "автоматический сбор";
    public bool IsEnabled { get; set; }
    public int Priority => 30;
    public bool IsExclusive => false;

    private readonly AutoPickAssets _autoPickAssets;

    /// <summary>
    /// Взять черный список
    /// </summary>
    private List<string> _blackList = new();

    /// <summary>
    /// Получить белый список
    /// </summary>
    private List<string> _whiteList = new();

    // Пользовательская кнопка выбора
    private string _pickKeyName = "F";

    private User32.VK _pickVk = User32.VK.VK_F;
    private RecognitionObject _pickRo;

    public AutoPickTrigger()
    {
        _autoPickAssets = AutoPickAssets.Instance;
    }

    public void Init()
    {
        _pickRo = _autoPickAssets.FRo;
        var keyName = TaskContext.Instance().Config.AutoPickConfig.PickKey;
        if (!string.IsNullOrEmpty(keyName))
        {
            try
            {
                _pickRo = _autoPickAssets.LoadCustomPickKey(keyName);
                _pickVk = User32Helper.ToVk(keyName);
                _pickKeyName = keyName;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "нагрузкаПользовательская кнопка выбораИсключение возникает, когда");
                _logger.LogError("нагрузкаПользовательская кнопка выборанеудача，Продолжайте использовать значение по умолчаниюFключ");
                TaskContext.Instance().Config.AutoPickConfig.PickKey = "F";
                return;
            }
            if (_pickKeyName != "F")
            {
                _logger.LogInformation("Пользовательская кнопка выбора：{Key}", _pickKeyName);
            }
        }

        IsEnabled = TaskContext.Instance().Config.AutoPickConfig.Enabled;
        try
        {
            var blackListJson = Global.ReadAllTextIfExist(@"User\pick_black_lists.json");
            if (!string.IsNullOrEmpty(blackListJson))
            {
                _blackList = JsonSerializer.Deserialize<List<string>>(blackListJson, ConfigService.JsonOptions) ?? [];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "читатьВзять черный списокнеудача");
            MessageBox.Show("читатьВзять черный списокнеудача，Пожалуйста, подтвердите изменениеВзять черный списокПравильный ли формат контента?！", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        try
        {
            var whiteListJson = Global.ReadAllTextIfExist(@"User\pick_white_lists.json");
            if (!string.IsNullOrEmpty(whiteListJson))
            {
                _whiteList = JsonSerializer.Deserialize<List<string>>(whiteListJson, ConfigService.JsonOptions) ?? [];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "читатьПолучить белый списокнеудача");
            MessageBox.Show("читатьПолучить белый списокнеудача，Пожалуйста, подтвердите изменениеПолучить белый списокПравильный ли формат контента?！", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Используется для вывода журнала только один раз
    /// </summary>
    private string _lastText = string.Empty;

    /// <summary>
    /// Используется для вывода журнала только один раз
    /// </summary>
    private int _prevClickFrameIndex = -1;

    //private int _fastModePickCount = 0;

    public void OnCapture(CaptureContent content)
    {
        var speedTimer = new SpeedTimer();

        content.CaptureRectArea.Find(_pickRo, foundRectArea =>
        {
            speedTimer.Record($"помещать {_pickKeyName} подобратьключ");
            var scale = TaskContext.Instance().SystemInfo.AssetScale;
            var config = TaskContext.Instance().Config.AutoPickConfig;

            // помещатьподобратьключ，Отправка в один клик
            var isExcludeIcon = false;
            _autoPickAssets.ChatIconRo.RegionOfInterest = new Rect(foundRectArea.X + (int)(config.ItemIconLeftOffset * scale), foundRectArea.Y, (int)((config.ItemTextLeftOffset - config.ItemIconLeftOffset) * scale), foundRectArea.Height);
            var chatIconRa = content.CaptureRectArea.Find(_autoPickAssets.ChatIconRo);
            speedTimer.Record("Определите значок чата");
            if (!chatIconRa.IsEmpty())
            {
                // Значок предмета представляет собой пузырь чата.，В целомNPCдиалог，Текст не находится в белом списке и не будет подхвачен.
                isExcludeIcon = true;
            }
            else
            {
                _autoPickAssets.SettingsIconRo.RegionOfInterest = _autoPickAssets.ChatIconRo.RegionOfInterest;
                var settingsIconRa = content.CaptureRectArea.Find(_autoPickAssets.SettingsIconRo);
                speedTimer.Record("Определить значок настроек");
                if (!settingsIconRa.IsEmpty())
                {
                    // Значок элемента является значком настроек.，В целомРешайте головоломки、Активность、Лифт и т. д.
                    isExcludeIcon = true;
                }
            }

            //if (config.FastModeEnabled && !isExcludeIcon)
            //{
            //    _fastModePickCount++;
            //    if (_fastModePickCount > 2)
            //    {
            //        _fastModePickCount = 0;
            //        LogPick(content, "Быстрый самовывоз");
            //        Simulation.SendInput.Keyboard.KeyPress(VirtualKeyCode.VK_F);
            //    }
            //    return;
            //}

            // Этот тип распознавания текста совершенно особенный.，Все они представляют собой распознавание текста для определенной сцены.，Поэтому на данный момент он не абстрагирован в объект распознавания.
            // Вычислить текстовую область
            var textRect = new Rect(foundRectArea.X + (int)(config.ItemTextLeftOffset * scale), foundRectArea.Y,
                (int)((config.ItemTextRightOffset - config.ItemTextLeftOffset) * scale), foundRectArea.Height);
            if (textRect.X + textRect.Width > content.CaptureRectArea.SrcGreyMat.Width
                || textRect.Y + textRect.Height > content.CaptureRectArea.SrcGreyMat.Height)
            {
                Debug.WriteLine("AutoPickTrigger: текстовая область out of range");
                return;
            }

            var textMat = new Mat(content.CaptureRectArea.SrcGreyMat, textRect);

            string text;
            if (config.OcrEngine == PickOcrEngineEnum.Yap.ToString())
            {
                var paddedMat = PreProcessForInference(textMat);
                text = _pickTextInference.Inference(paddedMat);
            }
            else
            {
                text = OcrFactory.Paddle.Ocr(textMat);
            }

            speedTimer.Record("распознавание текста");
            if (!string.IsNullOrEmpty(text))
            {
                text = Regex.Replace(text, @"^[\p{P} ]+|[\p{P} ]+$", "");
                // Единственный динамический предмет подбора.，специальная обработка，Сдвинуть перспективу вправо
                if (text.Contains("время роста"))
                {
                    return;
                }

                // одиночный персонажСдвинуть перспективу вправо
                if (text.Length <= 1)
                {
                    return;
                }

                if (_whiteList.Contains(text))
                {
                    LogPick(content, text);
                    Simulation.SendInput.Keyboard.KeyPress(_pickVk);
                    return;
                }

                speedTimer.Record("Решение по белому списку");

                if (isExcludeIcon)
                {
                    //Debug.WriteLine("AutoPickTrigger: Значок предмета представляет собой пузырь чата.，В целомNPCдиалог，Сдвинуть перспективу вправо");
                    return;
                }

                if (_blackList.Contains(text))
                {
                    return;
                }

                speedTimer.Record("Суд по черному списку");

                LogPick(content, text);
                Simulation.SendInput.Keyboard.KeyPress(_pickVk);
            }
        });
        speedTimer.DebugPrint();
    }

    private Mat PreProcessForInference(Mat mat)
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

    /// <summary>
    /// Один и тот же текст до и после3Выводить только один раз в кадре
    /// </summary>
    /// <param name="content"></param>
    /// <param name="text"></param>
    private void LogPick(CaptureContent content, string text)
    {
        if (_lastText != text || (_lastText == text && Math.Abs(content.FrameIndex - _prevClickFrameIndex) >= 5))
        {
            _logger.LogInformation("Взаимодействуйте или возьмите трубку：{Text}", text);
        }

        _lastText = text;
        _prevClickFrameIndex = content.FrameIndex;
    }
}
