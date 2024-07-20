using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoSkip.Assets;
using BetterGenshinImpact.GameTask.AutoSkip.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Vanara.PInvoke;
using Region = BetterGenshinImpact.GameTask.Model.Area.Region;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// Есть возможность нажать на автоматический график
/// </summary>
public class AutoSkipTrigger : ITaskTrigger
{
    private readonly ILogger<AutoSkipTrigger> _logger = App.GetLogger<AutoSkipTrigger>();

    public string Name => "автоматический сюжет";
    public bool IsEnabled { get; set; }
    public int Priority => 20;
    public bool IsExclusive => false;

    public bool IsBackgroundRunning { get; set; }

    private readonly AutoSkipAssets _autoSkipAssets;

    private readonly AutoSkipConfig _config;

    /// <summary>
    /// Возможность не нажимать автоматически，Приоритет ниже, чем нажатие оранжевого текста
    /// </summary>
    private List<string> _defaultPauseList = new();

    /// <summary>
    /// Возможность не нажимать автоматически
    /// </summary>
    private List<string> _pauseList = new();

    /// <summary>
    /// Отдайте приоритет параметрам автоматического клика
    /// </summary>
    private List<string> _selectList = new();

    private PostMessageSimulator? _postMessageSimulator;

    public AutoSkipTrigger()
    {
        _autoSkipAssets = AutoSkipAssets.Instance;
        _config = TaskContext.Instance().Config.AutoSkipConfig;
    }

    public void Init()
    {
        IsEnabled = _config.Enabled;
        IsBackgroundRunning = _config.RunBackgroundEnabled;
        _postMessageSimulator = TaskContext.Instance().PostMessageSimulator;

        try
        {
            var defaultPauseListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\default_pause_options.json");
            if (!string.IsNullOrEmpty(defaultPauseListJson))
            {
                _defaultPauseList = JsonSerializer.Deserialize<List<string>>(defaultPauseListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "читатьавтоматический сюжетПауза по умолчаниюНажмитеКлючевые словаСписок не выполнен");
            MessageBox.Show("читатьавтоматический сюжетПауза по умолчаниюНажмитеКлючевые словаСписок не выполнен，Пожалуйста, подтвердите изменениеназадизавтоматический сюжетПауза по умолчаниюНажмитеКлючевые словаПравильный ли формат контента?！", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        try
        {
            var pauseListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\pause_options.json");
            if (!string.IsNullOrEmpty(pauseListJson))
            {
                _pauseList = JsonSerializer.Deserialize<List<string>>(pauseListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "читатьавтоматический сюжетПаузаНажмитеКлючевые словаСписок не выполнен");
            MessageBox.Show("читатьавтоматический сюжетПаузаНажмитеКлючевые словаСписок не выполнен，Пожалуйста, подтвердите изменениеназадизавтоматический сюжетПаузаНажмитеКлючевые словаПравильный ли формат контента?！", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        try
        {
            var selectListJson = Global.ReadAllTextIfExist(@"User\AutoSkip\select_options.json");
            if (!string.IsNullOrEmpty(selectListJson))
            {
                _selectList = JsonSerializer.Deserialize<List<string>>(selectListJson, ConfigService.JsonOptions) ?? new List<string>();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "читатьавтоматический сюжетприоритетНажмите选项Список не выполнен");
            MessageBox.Show("читатьавтоматический сюжетприоритетНажмите选项Список не выполнен，Пожалуйста, подтвердите изменениеназадизавтоматический сюжетприоритетНажмите选项Правильный ли формат контента?！", "ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Кадр из последнего воспроизведения
    /// </summary>
    private DateTime _prevPlayingTime = DateTime.MinValue;

    private DateTime _prevExecute = DateTime.MinValue;
    private DateTime _prevHangoutExecute = DateTime.MinValue;

    private DateTime _prevGetDailyRewardsTime = DateTime.MinValue;

    private DateTime _prevClickTime = DateTime.MinValue;

    public void OnCapture(CaptureContent content)
    {
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 200)
        {
            return;
        }

        _prevExecute = DateTime.Now;

        GetDailyRewardsEsc(_config, content);

        // Найдите кнопку автоматического построения графика в левом верхнем углу.
        using var foundRectArea = content.CaptureRectArea.Find(_autoSkipAssets.DisabledUiButtonRo);

        var isPlaying = !foundRectArea.IsEmpty(); // играя

        if (!isPlaying && (DateTime.Now - _prevPlayingTime).TotalSeconds <= 5)
        {
            // Закрыть всплывающую страницу
            ClosePopupPage(content);

            // автоматический сюжетНажмите3sвнутреннее суждение
            if ((DateTime.Now - _prevPlayingTime).TotalMilliseconds < 3000)
            {
                // Отправить элементы
                if (SubmitGoods(content))
                {
                    return;
                }
            }
        }

        if (isPlaying)
        {
            _prevPlayingTime = DateTime.Now;
            if (TaskContext.Instance().Config.AutoSkipConfig.QuicklySkipConversationsEnabled)
            {
                if (IsBackgroundRunning)
                {
                    _postMessageSimulator?.KeyPressBackground(User32.VK.VK_SPACE);
                }
                else
                {
                    Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_SPACE);
                }
            }

            // Выбор варианта разговора
            var hasOption = ChatOptionChoose(content.CaptureRectArea);

            // Выбор варианта приглашения 1s 1Второсортный
            if (_config.AutoHangoutEventEnabled && !hasOption)
            {
                if ((DateTime.Now - _prevHangoutExecute).TotalMilliseconds < 1200)
                {
                    return;
                }

                _prevHangoutExecute = DateTime.Now;
                HangoutOptionChoose(content.CaptureRectArea);
            }
        }
        else
        {
            ClickBlackGameScreen(content);
        }
    }

    /// <summary>
    /// Решение по клику на черном экране
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private bool ClickBlackGameScreen(CaptureContent content)
    {
        // График черного экрана требует щелчка мышью（многоВторосортный） Не нужно нажимать, когда почти совсем темно
        if ((DateTime.Now - _prevClickTime).TotalMilliseconds > 1200)
        {
            using var grayMat = new Mat(content.CaptureRectArea.SrcGreyMat, new Rect(0, content.CaptureRectArea.SrcGreyMat.Height / 3, content.CaptureRectArea.SrcGreyMat.Width, content.CaptureRectArea.SrcGreyMat.Height / 3));
            var blackCount = OpenCvCommonHelper.CountGrayMatColor(grayMat, 0);
            var rate = blackCount * 1d / (grayMat.Width * grayMat.Height);
            if (rate is >= 0.5 and < 0.98999)
            {
                if (IsBackgroundRunning)
                {
                    TaskContext.Instance().PostMessageSimulator?.LeftButtonClickBackground();
                }
                else
                {
                    Simulation.SendInput.Mouse.LeftButtonClick();
                }

                _logger.LogInformation("автоматический сюжет：{Text} Пропорция {Rate}", "Нажмите на черный экран", rate.ToString("F"));

                _prevClickTime = DateTime.Now;
                return true;
            }
        }
        return false;
    }

    private void HangoutOptionChoose(ImageRegion captureRegion)
    {
        var selectedRects = captureRegion.FindMulti(_autoSkipAssets.HangoutSelectedRo);
        var unselectedRects = captureRegion.FindMulti(_autoSkipAssets.HangoutUnselectedRo);
        if (selectedRects.Count > 0 || unselectedRects.Count > 0)
        {
            List<HangoutOption> hangoutOptionList =
            [
                .. selectedRects.Select(selectedRect => new HangoutOption(selectedRect, true)),
                .. unselectedRects.Select(unselectedRect => new HangoutOption(unselectedRect, false)),
            ];
            // Есть только один вариант нажать напрямую
            // if (hangoutOptionList.Count == 1)
            // {
            //     hangoutOptionList[0].Click(clickOffset);
            //     AutoHangoutSkipLog("Нажмите на единственный вариант приглашения");
            //     return;
            // }

            hangoutOptionList = hangoutOptionList.Where(hangoutOption => hangoutOption.TextRect != null).ToList();
            if (hangoutOptionList.Count == 0)
            {
                return;
            }

            // OCRОпределить текст опции
            foreach (var hangoutOption in hangoutOptionList)
            {
                var text = OcrFactory.Paddle.Ocr(hangoutOption.TextRect!.SrcGreyMat);
                hangoutOption.OptionTextSrc = StringUtils.RemoveAllEnter(text);
            }

            // Предпочитаю варианты филиалов
            if (!string.IsNullOrEmpty(_config.AutoHangoutEndChoose))
            {
                var chooseList = HangoutConfig.Instance.HangoutOptions[_config.AutoHangoutEndChoose];
                foreach (var hangoutOption in hangoutOptionList)
                {
                    foreach (var str in chooseList)
                    {
                        if (hangoutOption.OptionTextSrc.Contains(str))
                        {
                            HangoutOptionClick(hangoutOption);
                            _logger.LogInformation("Это последний раунд?[{Text}]Ключевые слова[{Str}]ударять", _config.AutoHangoutEndChoose, str);
                            AutoHangoutSkipLog(hangoutOption.OptionTextSrc);
                            VisionContext.Instance().DrawContent.RemoveRect("HangoutSelected");
                            VisionContext.Instance().DrawContent.RemoveRect("HangoutUnselected");
                            return;
                        }
                    }
                }
            }

            // Нет возможности остаться Предпочитаю невыбранные варианты
            foreach (var hangoutOption in hangoutOptionList)
            {
                if (!hangoutOption.IsSelected)
                {
                    HangoutOptionClick(hangoutOption);
                    AutoHangoutSkipLog(hangoutOption.OptionTextSrc);
                    VisionContext.Instance().DrawContent.RemoveRect("HangoutSelected");
                    VisionContext.Instance().DrawContent.RemoveRect("HangoutUnselected");
                    return;
                }
            }

            // Нет невыбранных опций Выберите первый выбранный вариант
            HangoutOptionClick(hangoutOptionList[0]);
            AutoHangoutSkipLog(hangoutOptionList[0].OptionTextSrc);
            VisionContext.Instance().DrawContent.RemoveRect("HangoutSelected");
            VisionContext.Instance().DrawContent.RemoveRect("HangoutUnselected");
        }
        else
        {
            // Нет возможности приглашения ищу кнопку пропуска
            if (_config.AutoHangoutPressSkipEnabled)
            {
                using var skipRa = captureRegion.Find(_autoSkipAssets.HangoutSkipRo);
                if (skipRa.IsExist())
                {
                    if (IsBackgroundRunning && !SystemControl.IsGenshinImpactActive())
                    {
                        skipRa.BackgroundClick();
                    }
                    else
                    {
                        skipRa.Click();
                    }
                    AutoHangoutSkipLog("Нажмите кнопку «Пропустить»");
                }
            }
        }
    }

    /// <summary>
    /// Получить текст оранжевой опции
    /// </summary>
    /// <param name="captureMat"></param>
    /// <param name="foundIconRectArea"></param>
    /// <param name="chatOptionTextWidth"></param>
    /// <returns></returns>
    [Obsolete]
    private string GetOrangeOptionText(Mat captureMat, ImageRegion foundIconRectArea, int chatOptionTextWidth)
    {
        var textRect = new Rect(foundIconRectArea.X + foundIconRectArea.Width, foundIconRectArea.Y, chatOptionTextWidth, foundIconRectArea.Height);
        using var mat = new Mat(captureMat, textRect);
        // Извлеките только апельсин
        using var bMat = OpenCvCommonHelper.Threshold(mat, new Scalar(247, 198, 50), new Scalar(255, 204, 54));
        // Cv2.ImWrite("log/ежедневная комиссия.png", bMat);
        var whiteCount = OpenCvCommonHelper.CountGrayMatColor(bMat, 255);
        var rate = whiteCount * 1.0 / (bMat.Width * bMat.Height);
        if (rate < 0.06)
        {
            Debug.WriteLine($"Распознана часть оранжевой текстовой области:{rate}");
            return string.Empty;
        }

        var text = OcrFactory.Paddle.Ocr(bMat);
        return text;
    }

    private bool IsOrangeOption(Mat textMat)
    {
        // Извлеките только апельсин
        // Cv2.ImWrite($"log/text{DateTime.Now:yyyyMMddHHmmssffff}.png", textMat);
        using var bMat = OpenCvCommonHelper.Threshold(textMat, new Scalar(247, 198, 50), new Scalar(255, 204, 54));
        var whiteCount = OpenCvCommonHelper.CountGrayMatColor(bMat, 255);
        var rate = whiteCount * 1.0 / (bMat.Width * bMat.Height);
        Debug.WriteLine($"Распознана часть оранжевой текстовой области:{rate}");
        if (rate > 0.06)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// получатьежедневная комиссиянаграда назад 10s Ищите грубые камни，появится, нажмитеesc
    /// </summary>
    private void GetDailyRewardsEsc(AutoSkipConfig config, CaptureContent content)
    {
        if (!config.AutoGetDailyRewardsEnabled)
        {
            return;
        }

        if ((DateTime.Now - _prevGetDailyRewardsTime).TotalSeconds > 10)
        {
            return;
        }

        content.CaptureRectArea.Find(_autoSkipAssets.PrimogemRo, primogemRa =>
        {
            Thread.Sleep(100);
            Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_ESCAPE);
            _prevGetDailyRewardsTime = DateTime.MinValue;
            primogemRa.Dispose();
        });
    }

    private readonly Regex _enOrNumRegex = new(@"^[a-zA-Z0-9]+$");

    /// <summary>
    /// 新изВыбор варианта разговора
    ///
    /// возвращаться true Указывает на наличие вариантов диалога，Но не обязательно нажимать
    /// </summary>
    private bool ChatOptionChoose(ImageRegion region)
    {
        if (_config.IsClickNoneChatOption())
        {
            return false;
        }
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        // Распознавание восклицательного знака Нажмите непосредственно при встрече
        using var exclamationIconRa = region.Find(_autoSkipAssets.ExclamationIconRo);
        if (!exclamationIconRa.IsEmpty())
        {
            Thread.Sleep(_config.AfterChooseOptionSleepDelay);
            exclamationIconRa.Click();
            AutoSkipLog("Нажмите на опцию восклицательного знака");
            return true;
        }

        // Распознавание пузырьков
        var chatOptionResultList = region.FindMulti(_autoSkipAssets.OptionIconRo);
        if (chatOptionResultList.Count > 0)
        {
            // Первый элемент — нижний
            chatOptionResultList = chatOptionResultList.OrderByDescending(r => r.Y).ToList();

            // Распознавание текста через всплывающее окно внизу
            var lowest = chatOptionResultList[0];
            var ocrRect = new Rect((int)(lowest.X + lowest.Width + 8 * assetScale), region.Height / 12,
                (int)(535 * assetScale), (int)(lowest.Y + lowest.Height + 30 * assetScale - region.Height / 12d));
            var ocrResList = region.FindMulti(new RecognitionObject
            {
                RecognitionType = RecognitionTypes.Ocr,
                RegionOfInterest = ocrRect
            });
            //using var ocrMat = new Mat(region.SrcGreyMat, ocrRect);
            //// Cv2.ImWrite("log/ocrMat.png", ocrMat);
            //var ocrRes = OcrFactory.Paddle.OcrResult(ocrMat);

            // Удалить пустые результаты и Чисто английские результаты
            var rs = new List<Region>();
            // в соответствии сyСортировка координат
            ocrResList = ocrResList.OrderBy(r => r.Y).ToList();
            for (var i = 0; i < ocrResList.Count; i++)
            {
                var item = ocrResList[i];
                if (string.IsNullOrEmpty(item.Text) || (item.Text.Length < 5 && _enOrNumRegex.IsMatch(item.Text)))
                {
                    continue;
                }
                if (i != ocrResList.Count - 1)
                {
                    if (ocrResList[i + 1].Y - ocrResList[i].Y > 150)
                    {
                        Debug.WriteLine($"существоватьYРезультат чрезмерного отклонения оси，пренебрегать:{item.Text}");
                        continue;
                    }
                }

                rs.Add(item);
            }

            if (rs.Count > 0)
            {
                // ОбычайКлючевые слова соответствовать
                foreach (var item in rs)
                {
                    // выбиратьКлючевые слова
                    if (_selectList.Any(s => item.Text.Contains(s)))
                    {
                        ClickOcrRegion(item);
                        return true;
                    }

                    // 不выбиратьКлючевые слова
                    if (_pauseList.Any(s => item.Text.Contains(s)))
                    {
                        return true;
                    }
                }

                // оранжевый вариант
                foreach (var item in rs)
                {
                    var textMat = item.ToImageRegion().SrcMat;
                    if (IsOrangeOption(textMat))
                    {
                        if (_config.AutoGetDailyRewardsEnabled && (item.Text.Contains("ежедневно") || item.Text.Contains("поручить")))
                        {
                            ClickOcrRegion(item, "ежедневная комиссия");
                            _prevGetDailyRewardsTime = DateTime.Now; // Запишите время сбора
                        }
                        else if (_config.AutoReExploreEnabled && (item.Text.Contains("исследовать") || item.Text.Contains("отправлять")))
                        {
                            ClickOcrRegion(item, "исследоватьотправлять");
                            Thread.Sleep(800); // ждатьисследоватьотправлятьОткроется интерфейс
                            new OneKeyExpeditionTask().Run(_autoSkipAssets);
                        }
                        else
                        {
                            ClickOcrRegion(item);
                        }

                        return true;
                    }
                }

                // 默认不выбиратьКлючевые слова
                foreach (var item in rs)
                {
                    // 不выбиратьКлючевые слова
                    if (_defaultPauseList.Any(s => item.Text.Contains(s)))
                    {
                        return true;
                    }
                }

                // большинствоназад，Выберите параметры по умолчанию
                var clickRegion = rs[^1];
                if (_config.IsClickFirstChatOption())
                {
                    clickRegion = rs[0];
                }
                else if (_config.IsClickRandomChatOption())
                {
                    var random = new Random();
                    clickRegion = rs[random.Next(0, rs.Count)];
                }

                ClickOcrRegion(clickRegion);
                AutoSkipLog(clickRegion.Text);
            }
            else
            {
                var clickRect = lowest;
                if (_config.IsClickFirstChatOption())
                {
                    clickRect = chatOptionResultList[^1];
                }

                // безOCRпечатать，Непосредственно выберите вариант пузырька
                Thread.Sleep(_config.AfterChooseOptionSleepDelay);
                clickRect.Click();
                var msg = _config.IsClickFirstChatOption() ? "Первый" : "большинствоназадодин";
                AutoSkipLog($"Нажмите{msg}Параметры пузырька");
            }

            return true;
        }

        return false;
    }

    private void ClickOcrRegion(Region region, string optionType = "")
    {
        if (string.IsNullOrEmpty(optionType))
        {
            Thread.Sleep(_config.AfterChooseOptionSleepDelay);
        }
        if (IsBackgroundRunning && !SystemControl.IsGenshinImpactActive())
        {
            region.BackgroundClick();
        }
        else
        {
            region.Click();
        }
        AutoSkipLog(region.Text);
    }

    private void HangoutOptionClick(HangoutOption option)
    {
        if (_config.AutoHangoutChooseOptionSleepDelay > 0)
        {
            Thread.Sleep(_config.AutoHangoutChooseOptionSleepDelay);
        }
        if (IsBackgroundRunning && !SystemControl.IsGenshinImpactActive())
        {
            option.BackgroundClick();
        }
        else
        {
            option.Click();
        }
    }

    private void AutoHangoutSkipLog(string text)
    {
        if ((DateTime.Now - _prevClickTime).TotalMilliseconds > 1000)
        {
            _logger.LogInformation("Автоматическое приглашение：{Text}", text);
        }

        _prevClickTime = DateTime.Now;
    }

    private void AutoSkipLog(string text)
    {
        if (text.Contains("ежедневная комиссия") || text.Contains("исследоватьотправлять"))
        {
            _logger.LogInformation("автоматический сюжет：{Text}", text);
        }
        else if ((DateTime.Now - _prevClickTime).TotalMilliseconds > 1000)
        {
            _logger.LogInformation("автоматический сюжет：{Text}", text);
        }

        _prevClickTime = DateTime.Now;
    }

    /// <summary>
    /// Закрыть всплывающую страницу
    /// </summary>
    /// <param name="content"></param>
    private void ClosePopupPage(CaptureContent content)
    {
        content.CaptureRectArea.Find(_autoSkipAssets.PageCloseRo, pageCloseRoRa =>
        {
            TaskContext.Instance().PostMessageSimulator.KeyPress(User32.VK.VK_ESCAPE);

            AutoSkipLog("Закрыть всплывающую страницу");
            pageCloseRoRa.Dispose();
        });
    }

    private bool SubmitGoods(CaptureContent content)
    {
        using var exclamationRa = content.CaptureRectArea.Find(_autoSkipAssets.SubmitExclamationIconRo);
        if (!exclamationRa.IsEmpty())
        {
            // var rects = MatchTemplateHelper.MatchOnePicForOnePic(content.CaptureRectArea.SrcMat.CvtColor(ColorConversionCodes.BGRA2BGR),
            //     _autoSkipAssets.SubmitGoodsMat, TemplateMatchModes.SqDiffNormed, null, 0.9, 4);
            var rects = ContoursHelper.FindSpecifyColorRects(content.CaptureRectArea.SrcMat, new Scalar(233, 229, 220), 100, 20);
            if (rects.Count == 0)
            {
                return false;
            }

            // Нарисуйте прямоугольник и сохраните его.
            // foreach (var rect in rects)
            // {
            //     Cv2.Rectangle(content.CaptureRectArea.SrcMat, rect, Scalar.Red, 1);
            // }
            // Cv2.ImWrite("log/Отправить элементы.png", content.CaptureRectArea.SrcMat);

            for (var i = 0; i < rects.Count; i++)
            {
                content.CaptureRectArea.Derive(rects[i]).Click();
                _logger.LogInformation("Отправить элементы：{Text}", "1. Выберите элементы" + i);
                TaskControl.Sleep(800);

                var btnBlackConfirmRa = TaskControl.CaptureToRectArea().Find(ElementAssets.Instance.BtnBlackConfirm);
                if (!btnBlackConfirmRa.IsEmpty())
                {
                    btnBlackConfirmRa.Click();
                    _logger.LogInformation("Отправить элементы：{Text}", "2. положить в" + i);
                    TaskControl.Sleep(200);
                }
            }

            TaskControl.Sleep(500);

            using var ra = TaskControl.CaptureToRectArea();
            using var btnWhiteConfirmRa = ra.Find(ElementAssets.Instance.BtnWhiteConfirm);
            if (!btnWhiteConfirmRa.IsEmpty())
            {
                btnWhiteConfirmRa.Click();
                _logger.LogInformation("Отправить элементы：{Text}", "3. доставлять");

                VisionContext.Instance().DrawContent.ClearAll();
            }

            // большинство4Разница в высоте между двумя прямоугольниками слишком велика. Поддержите одного сейчас
            // var prevGoodsRect = Rect.Empty;
            // for (var i = 1; i <= 4; i++)
            // {
            //     // Постоянно перехватывайте предметы справа
            //     TaskControl.Sleep(200);
            //     content = TaskControl.CaptureToContent();
            //     var gameArea = content.CaptureRectArea;
            //     if (prevGoodsRect != Rect.Empty)
            //     {
            //         var r = content.CaptureRectArea.ToRect();
            //         var newX = prevGoodsRect.X + prevGoodsRect.Width;
            //         gameArea = content.CaptureRectArea.Crop(new Rect(newX, 0, r.Width - newX, r.Height));
            //         Cv2.ImWrite($"log/вещь{i}.png", gameArea.SrcMat);
            //     }
            //
            //     var goods = gameArea.Find(_autoSkipAssets.SubmitGoodsRo);
            //     if (!goods.IsEmpty())
            //     {
            //         prevGoodsRect = goods.ConvertRelativePositionToCaptureArea();
            //         goods.ClickCenter();
            //         _logger.LogInformation("Отправить элементы：{Text}", "1. Выберите элементы" + i);
            //
            //         TaskControl.Sleep(800);
            //         content = TaskControl.CaptureToContent();
            //
            //         var btnBlackConfirmRa = content.CaptureRectArea.Find(ElementAssets.Instance().BtnBlackConfirm);
            //         if (!btnBlackConfirmRa.IsEmpty())
            //         {
            //             btnBlackConfirmRa.ClickCenter();
            //             _logger.LogInformation("Отправить элементы：{Text}", "2. положить в" + i);
            //
            //             TaskControl.Sleep(800);
            //             content = TaskControl.CaptureToContent();
            //
            //             btnBlackConfirmRa = content.CaptureRectArea.Find(ElementAssets.Instance().BtnBlackConfirm);
            //             if (!btnBlackConfirmRa.IsEmpty())
            //             {
            //                 _logger.LogInformation("Отправить элементы：{Text}", "2. Все ещесуществоватьвещь");
            //                 continue;
            //             }
            //             else
            //             {
            //                 var btnWhiteConfirmRa = content.CaptureRectArea.Find(ElementAssets.Instance().BtnWhiteConfirm);
            //                 if (!btnWhiteConfirmRa.IsEmpty())
            //                 {
            //                     btnWhiteConfirmRa.ClickCenter();
            //                     _logger.LogInformation("Отправить элементы：{Text}", "3. доставлять");
            //
            //                     VisionContext.Instance().DrawContent.ClearAll();
            //                     return true;
            //                 }
            //                 break;
            //             }
            //         }
            //     }
            //     else
            //     {
            //         break;
            //     }
            // }
        }

        return false;
    }
}
