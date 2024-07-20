using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoWood.Assets;
using BetterGenshinImpact.GameTask.AutoWood.Utils;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Genshin.Settings;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using static Vanara.PInvoke.User32;
using GC = System.GC;

namespace BetterGenshinImpact.GameTask.AutoWood;

/// <summary>
/// Автоматическое журналирование
/// </summary>
public partial class AutoWoodTask
{
    private readonly AutoWoodAssets _assets;

    private bool _first = true;

    private readonly WoodStatisticsPrinter _printer;

    private readonly Login3rdParty _login3rdParty;

    private VK _zKey = VK.VK_Z;

    public AutoWoodTask()
    {
        _login3rdParty = new();
        AutoWoodAssets.DestroyInstance();
        _assets = AutoWoodAssets.Instance;
        _printer = new WoodStatisticsPrinter(_assets);
    }

    public void Start(WoodTaskParam taskParam)
    {
        var hasLock = false;
        var runTimeWatch = new Stopwatch();
        try
        {
            hasLock = TaskSemaphore.Wait(0);
            if (!hasLock)
            {
                Logger.LogError("запускатьАвтоматическое журналированиеФункция не удалась：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            TaskTriggerDispatcher.Instance().StopTimer();
            Kernel32.SetThreadExecutionState(Kernel32.EXECUTION_STATE.ES_CONTINUOUS | Kernel32.EXECUTION_STATE.ES_SYSTEM_REQUIRED | Kernel32.EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            Logger.LogInformation("→ {Text} Установить общее количество вырубок：{Cnt}，Установить верхний предел количества древесины：{MaxCnt}", "Автоматическое журналирование，запускать！", taskParam.WoodRoundNum, taskParam.WoodDailyMaxCount);

            _login3rdParty.RefreshAvailabled();
            if (_login3rdParty.Type == Login3rdParty.The3rdPartyType.Bilibili)
            {
                Logger.LogInformation("Автоматическое журналированиедавать возможностьBрежим сервера");
            }

            SettingsContainer settingsContainer = new();

            if (settingsContainer.OverrideController?.KeyboardMap?.ActionElementMap.Where(item => item.ActionId == ActionId.Gadget).FirstOrDefault()?.ElementIdentifierId is ElementIdentifierId key)
            {
                if (key != ElementIdentifierId.Z)
                {
                    _zKey = key.ToVK();
                    Logger.LogInformation($"Автоматическое журналированиеобнаруженИзменение ключа пользователя {ElementIdentifierId.Z.ToName()} Изменить на {key.ToName()}");
                    if (key == ElementIdentifierId.LeftShift || key == ElementIdentifierId.RightShift)
                    {
                        Logger.LogInformation($"Изменение ключа пользователя {key.ToName()} Может не поддерживаться эмуляцией，Игнорировать, если используется нормально");
                    }
                }
            }

            SystemControl.ActivateWindow();
            // Начинается отсчет времени для регистрации
            runTimeWatch.Start();
            for (var i = 0; i < taskParam.WoodRoundNum; i++)
            {
                if (TaskContext.Instance().Config.AutoWoodConfig.WoodCountOcrEnabled)
                {
                    if (_printer.WoodStatisticsAlwaysEmpty())
                    {
                        Logger.LogInformation("непрерывный{Cnt}Количество древесины, полученное каждый раз, равно0。Установлено, что поблизости нет ответа「Ван Шуруйю」деревья！Или достигнут дневной лимит количества", _printer.NothingCount);
                        break;
                    }

                    if (_printer.ReachedWoodMaxCount)
                    {
                        Logger.LogInformation("{Names}Установленный верхний предел достигнут.：{MaxCnt}", _printer.WoodTotalDict.Keys, taskParam.WoodDailyMaxCount);
                        break;
                    }
                }

                Logger.LogInformation("Нет.{Cnt}Вторичная регистрация", i + 1);
                if (taskParam.Cts.IsCancellationRequested)
                {
                    break;
                }

                Felling(taskParam, i + 1 == taskParam.WoodRoundNum);
                VisionContext.Instance().DrawContent.ClearAll();
                Sleep(500, taskParam.Cts);
            }
        }
        catch (NormalEndException e)
        {
            Logger.LogInformation(e.Message);
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogDebug(e.StackTrace);
            System.Windows.MessageBox.Show("Автоматическое журналированиеАномальный：" + e.Source + "\r\n--" + Environment.NewLine + e.StackTrace + "\r\n---" + Environment.NewLine + e.Message);
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskSettingsPageViewModel.SetSwitchAutoWoodButtonText(false);
            // Таймер окончания регистрации
            runTimeWatch.Stop();
            Kernel32.SetThreadExecutionState(Kernel32.EXECUTION_STATE.ES_CONTINUOUS);
            var elapsedTime = runTimeWatch.Elapsed;
            Logger.LogInformation(@"КнигаВторичная регистрацияОбщее время потрачено：{Time:hh\:mm\:ss}", elapsedTime);
            Logger.LogInformation("← {Text}", "изАвтоматическое журналирование");
            TaskTriggerDispatcher.Instance().StartTimer();

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private partial class WoodStatisticsPrinter(AutoWoodAssets assert)
    {
        public bool ReachedWoodMaxCount;
        public int NothingCount;
        public readonly ConcurrentDictionary<string, int> WoodTotalDict = new();

        private bool _firstWoodOcr = true;
        private string _firstWoodOcrText = "";
        private readonly Dictionary<string, int> _woodMetricsDict = new();
        private readonly Dictionary<string, bool> _woodNotPrintDict = new();

        // from:https://api-static.mihoyo.com/common/blackboard/ys_obc/v1/home/content/list?app_sn=ys_obc&channel_id=13
        private static readonly List<string> ExistWoods =
        [
            "платан", "белый ясень", "Торчвуд", "Липа", "Кедр", "тернвуд", "Тамаки", "Хуа дерево", "Йе Гому", "просветление древесины", "клен", "ароматное дерево",
            "Китайская пихта", "Бамбуковый узел", "Кеша дерево", "сосна", "Дерево Цуйхуа", "береза", "Полезный материал", "мечтать о дереве", "Огаму"
        ];

        [GeneratedRegex("([^\\d\\n]+)[×x](\\d+)")]
        private static partial Regex _parseWoodStatisticsRegex();

        public bool WoodStatisticsAlwaysEmpty()
        {
            return NothingCount >= 3;
        }

        public void PrintWoodStatistics(WoodTaskParam taskParam)
        {
            var woodStatisticsText = GetWoodStatisticsText(taskParam);
            if (string.IsNullOrEmpty(woodStatisticsText))
            {
                NothingCount++;
                Logger.LogWarning("Не удалось определить статистику журналирования.");
                return;
            }

            ParseWoodStatisticsText(taskParam, woodStatisticsText);
            CheckAndPrintWoodQuantities(taskParam);
        }

        private string GetWoodStatisticsText(WoodTaskParam taskParam)
        {
            var firstOcrResultList = new List<string>();
            // Создать таймер，Текст распознавания цикла，до таймаута
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 3500)
            {
                // OCRначальство
                var recognizedText = WoodTextAreaOcr();
                if (_firstWoodOcr)
                {
                    // повторю в первый разOCRидентифицировать，тогда найди лучшееOCRрезультат（то есть самый длинный）
                    var isFound = HasDetectedWoodText(recognizedText);
                    if (isFound) firstOcrResultList.Add(recognizedText);
                    if (firstOcrResultList.Count != 0 && !isFound) break;
                    SleepDurationBetweenOcrs(taskParam);
                }
                else
                {
                    var isFound = HasDetectedWoodText(recognizedText);
                    if (!isFound)
                    {
                        SleepDurationBetweenOcrs(taskParam);
                        continue;
                    }

                    NothingCount = 0;
                    // Количество древесины, ожидающей заготовки, отображается полностью.，сноваOCRидентифицировать。
                    // SleepDurationBetweenOcrs(taskParam);
                    // return WoodTextAreaOcr();

                    // Возврат напрямуюпервыйизидентифицироватьрезультат
                    return _firstWoodOcrText;
                }
            }
            stopwatch.Stop(); // Остановить время
            _firstWoodOcrText = FindBestOcrResult(firstOcrResultList);
            return _firstWoodOcrText;
        }

        private void SleepDurationBetweenOcrs(WoodTaskParam taskParam)
        {
            Sleep(_firstWoodOcr ? 300 : 100, taskParam.Cts);
        }

        private string WoodTextAreaOcr()
        {
            // OCRидентифицировать文Книга区域
            var woodCountRect = CaptureToRectArea().DeriveCrop(assert.WoodCountUpperRect);
            return OcrFactory.Paddle.Ocr(woodCountRect.SrcGreyMat);
        }

        private bool HasDetectedWoodText(string recognizedText)
        {
            if (!_firstWoodOcr)
            {
                return !string.IsNullOrEmpty(recognizedText) &&
                       recognizedText.Contains("получать");
            }
            return !string.IsNullOrEmpty(recognizedText) &&
                   recognizedText.Contains("получать") &&
                   (recognizedText.Contains('×') || recognizedText.Contains('x'));
        }

        private void ParseWoodStatisticsText(WoodTaskParam taskParam, string text)
        {
            // отидентифицироватьиз文Книга中提取древесинаимяиколичество
            // Пример формата："получать\nБамбуковый узел×30\nКитайская пихта×20"
            if (!text.Contains('×') && !text.Contains('X'))
            {
                Logger.LogWarning("Не удалось правильно проанализировать формат информации о древесине.：{woodText}", text);
                return;
            }

            // шаблон соответствия "имя×количество"
            var matches = _parseWoodStatisticsRegex().Matches(text);

            // еслиOCRидентифицироватьдревесинаиз种类小于等于первый保存из一样时，Используйте напрямуюпервыйиздревесинаколичество。
            if (!_firstWoodOcr && 1 <= matches.Count && matches.Count <= _woodMetricsDict.Count)
            {
                foreach (var entry in _woodMetricsDict.Where(entry => entry.Value <= taskParam.WoodDailyMaxCount))
                {
                    UpdateWoodCount(entry.Key, entry.Value);
                }
            }
            else
            {
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        var materialName = match.Groups[1].Value.Trim();
                        var quantityStr = match.Groups[2].Value.Trim();
                        var quantity = int.Parse(quantityStr);
                        Debug.WriteLine($"первыйПолучатьдревесинаизимя：{materialName}, количество：{quantity}");
                        UpdateWoodCount(materialName, quantity);
                    }
                    else
                    {
                        Logger.LogWarning("идентифицировать到изколичество不是有效из整数：{woodText}", text);
                    }
                }

                // После того, как все данные будут сохранены,，первыйOCRидентифицироватьЗаканчивать
                _firstWoodOcr = false;
            }
        }

        private void UpdateWoodCount(string materialName, int quantity)
        {
            // Проверьте, есть ли это в словаредревесинаимя
            if (!ExistWoods.Contains(materialName))
            {
                Logger.LogWarning("Неизвестное название дерева：{woodName}，количество{Cnt}", materialName, quantity);
                return;
            }
            WoodTotalDict.AddOrUpdate(
                key: materialName,
                addValue: quantity,
                updateValueFactory: (_, existingValue) => existingValue + quantity
            );
            if (_firstWoodOcr)
            {
                // Запишите стоимость одного приобретения древесины
                _woodMetricsDict.TryAdd(materialName, quantity);
            }
        }

        private static string FindBestOcrResult(List<string> firstOcrResultList)
        {
            // return firstOcrResultList.Count == 0 ? "" : firstOcrResultList.OrderByDescending(s => s.Length).First();
            if (firstOcrResultList.Count == 0) return "";

            // Сначала отсортируйте, а потом ищите
            var sortedOcrResults = firstOcrResultList.OrderByDescending(s => s.Length).ToList();
            int? targetLength = null;

            foreach (var ocrResult in sortedOcrResults)
            {
                if (targetLength == null)
                {
                    targetLength = ocrResult.Length;
                }
                else if (ocrResult.Length != targetLength)
                {
                    // еслитекущийрезультатдлина иНет.一个匹配项из长度不同，тогда пропусти
                    continue;
                }

                // авария OCR результат中из多个条目
                var matches = _parseWoodStatisticsRegex().Matches(ocrResult);
                var isFound = true;
                foreach (Match match in matches)
                {
                    if (!match.Success)
                    {
                        isFound = false;
                        continue;
                    }
                    var materialName = match.Groups[1].Value.Trim();
                    Debug.WriteLine($"Нет.一次Получатьиздревесинаимя：{materialName}");
                    if (!ExistWoods.Contains(materialName))
                    {
                        isFound = false;
                    }
                }

                if (isFound) return ocrResult;
            }

            // если没有找到匹配изрезультат
            return "";
        }

        private void CheckAndPrintWoodQuantities(WoodTaskParam taskParam)
        {
            if (WoodTotalDict.IsEmpty)
            {
                ReachedWoodMaxCount = false;
                NothingCount++;
                return;
            }

            foreach (var entry in WoodTotalDict)
            {
                if (_woodNotPrintDict.GetValueOrDefault(entry.Key)) continue;
                // Распечатайте ключ каждой записи（древесинаимя）суммарное значение（количество）
                Logger.LogInformation("древесина{woodName}累积Получатьколичество：{Cnt}", entry.Key, entry.Value);

                // исследоватьдревесинаПревышен ли верхний предел
                if (entry.Value < taskParam.WoodDailyMaxCount) continue;
                Logger.LogInformation("древесина{Name}достигнутоколичество设置из上限：{Count}", entry.Key, taskParam.WoodDailyMaxCount);
                _woodNotPrintDict.TryAdd(entry.Key, true);
            }

            // еслидревесина统计из最小值都大于设置из上限，прекратить регистрацию
            ReachedWoodMaxCount = WoodTotalDict.Values.Min() >= taskParam.WoodDailyMaxCount;
        }
    }

    private void Felling(WoodTaskParam taskParam, bool isLast = false)
    {
        // 1. в соответствии с z курок「Ван Шуруйю」
        PressZ(taskParam);

        if (isLast)
        {
            return;
        }

        // Распечатать статистику регистрации（Необязательный）
        if (TaskContext.Instance().Config.AutoWoodConfig.WoodCountOcrEnabled)
        {
            _printer.PrintWoodStatistics(taskParam);
            if (_printer.WoodStatisticsAlwaysEmpty() || _printer.ReachedWoodMaxCount) return;
        }

        // 2. в соответствии сВниз ESC Открыть меню и выйти из игры
        PressEsc(taskParam);

        // 3. Ожидание входа в игру
        EnterGame(taskParam);

        // Руководство GC
        GC.Collect();
    }

    private void PressZ(WoodTaskParam taskParam)
    {
        // IMPORTANT: MUST try focus before press Z
        SystemControl.Focus(TaskContext.Instance().GameHandle);

        if (_first)
        {
            using var contentRegion = CaptureToRectArea();
            using var ra = contentRegion.Find(_assets.TheBoonOfTheElderTreeRo);
            if (ra.IsEmpty())
            {
#if !TEST_WITHOUT_Z_ITEM
                throw new NormalEndException("Пожалуйста, сначала оборудуйте небольшой реквизит.「Ван Шуруйю」！");
#else
                Thread.Sleep(2000);
                Simulation.SendInputEx.Keyboard.KeyPress(_zKey);
                Debug.WriteLine("[AutoWood] Z");
                _first = false;
#endif
            }
            else
            {
                Simulation.SendInput.Keyboard.KeyPress(_zKey);
                Debug.WriteLine("[AutoWood] Z");
                _first = false;
            }
        }
        else
        {
            NewRetry.Do(() =>
            {
                Sleep(1, taskParam.Cts);
                using var contentRegion = CaptureToRectArea();
                using var ra = contentRegion.Find(_assets.TheBoonOfTheElderTreeRo);
                if (ra.IsEmpty())
                {
#if !TEST_WITHOUT_Z_ITEM
                    throw new RetryException("не найдено「Ван Шуруйю」");
#else
                    Thread.Sleep(15000);
#endif
                }

                Simulation.SendInput.Keyboard.KeyPress(_zKey);
                Debug.WriteLine("[AutoWood] Z");
                Sleep(500, taskParam.Cts);
            }, TimeSpan.FromSeconds(1), 120);
        }

        Sleep(300, taskParam.Cts);
        Sleep(TaskContext.Instance().Config.AutoWoodConfig.AfterZSleepDelay, taskParam.Cts);
    }

    private void PressEsc(WoodTaskParam taskParam)
    {
        SystemControl.Focus(TaskContext.Instance().GameHandle);
        Simulation.SendInput.Keyboard.KeyPress(VK.VK_ESCAPE);
        // if (TaskContext.Instance().Config.AutoWoodConfig.PressTwoEscEnabled)
        // {
        //     Sleep(1500, taskParam.Cts);
        //     Simulation.SendInput.Keyboard.KeyPress(VK.VK_ESCAPE);
        // }
        Debug.WriteLine("[AutoWood] Esc");
        Sleep(800, taskParam.Cts);
        // Подтвердите в интерфейсе меню
        try
        {
            NewRetry.Do(() =>
            {
                Sleep(1, taskParam.Cts);
                using var contentRegion = CaptureToRectArea();
                using var ra = contentRegion.Find(_assets.MenuBagRo);
                if (ra.IsEmpty())
                {
                    Simulation.SendInput.Keyboard.KeyPress(VK.VK_ESCAPE);
                    throw new RetryException("Всплывающее меню не обнаружено");
                }
            }, TimeSpan.FromSeconds(1.2), 5);
        }
        catch (Exception e)
        {
            Logger.LogInformation(e.Message);
            Logger.LogInformation("Все ещеНажмите, чтобы выйтив соответствии скнопка");
        }

        // Нажмите, чтобы выйти
        GameCaptureRegion.GameRegionClick((size, scale) => (50 * scale, size.Height - 50 * scale));

        Debug.WriteLine("[AutoWood] Click exit button");

        Sleep(500, taskParam.Cts);

        // Нажмите, чтобы подтвердить
        using var contentRegion = CaptureToRectArea();
        contentRegion.Find(_assets.ConfirmRo, ra =>
        {
            ra.Click();
            Debug.WriteLine("[AutoWood] Click confirm button");
            ra.Dispose();
        });
    }

    private void EnterGame(WoodTaskParam taskParam)
    {
        if (_login3rdParty.IsAvailabled)
        {
            Sleep(1, taskParam.Cts);
            _login3rdParty.Login(taskParam.Cts);
        }

        var clickCnt = 0;
        for (var i = 0; i < 50; i++)
        {
            Sleep(1, taskParam.Cts);

            using var contentRegion = CaptureToRectArea();
            using var ra = contentRegion.Find(_assets.EnterGameRo);
            if (!ra.IsEmpty())
            {
                clickCnt++;
                GameCaptureRegion.GameRegion1080PPosClick(955, 666);
                Debug.WriteLine("[AutoWood] Click entry");
            }
            else
            {
                if (clickCnt > 2)
                {
                    Sleep(5000, taskParam.Cts);
                    break;
                }
            }

            Sleep(1000, taskParam.Cts);
        }

        if (clickCnt == 0)
        {
            throw new RetryException("Необнаруженный и вход в интерфейс игры");
        }
    }
}
