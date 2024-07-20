using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Assets;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.Helpers.Extensions;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.Model.Area;
using Point = OpenCvSharp.Point;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation;

/// <summary>
/// Используется для управления играми
/// </summary>
public class GeniusInvokationControl
{
    private readonly ILogger<GeniusInvokationControl> _logger = App.GetLogger<GeniusInvokationControl>();

    // Определите статическую переменную для хранения экземпляра класса.
    private static GeniusInvokationControl? _uniqueInstance;

    // Определите идентификатор для обеспечения синхронизации потоков.
    private static readonly object _locker = new();

    private AutoGeniusInvokationConfig _config;

    // Определить частный конструктор，Сделать невозможным создание экземпляров этого класса посторонними лицами.
    private GeniusInvokationControl()
    {
        _config = TaskContext.Instance().Config.AutoGeniusInvokationConfig;
    }

    /// <summary>
    /// Определите общедоступные методы для предоставления глобальной точки доступа.,Вы также можете определить общедоступные свойства для предоставления глобальных точек доступа.
    /// </summary>
    /// <returns></returns>
    public static GeniusInvokationControl GetInstance()
    {
        if (_uniqueInstance == null)
        {
            lock (_locker)
            {
                _uniqueInstance ??= new GeniusInvokationControl();
            }
        }

        return _uniqueInstance;
    }

    public static bool OutputImageWhenError = true;

    private CancellationTokenSource? _cts;

    private readonly AutoGeniusInvokationAssets _assets = AutoGeniusInvokationAssets.Instance;

    // private IGameCapture? _gameCapture;

    public void Init(GeniusInvokationTaskParam taskParam)
    {
        _cts = taskParam.Cts;
        // _gameCapture = taskParam.Dispatcher.GameCapture;
    }

    public void Sleep(int millisecondsTimeout)
    {
        CheckTask();
        Thread.Sleep(millisecondsTimeout);
        var sleepDelay = TaskContext.Instance().Config.AutoGeniusInvokationConfig.SleepDelay;
        if (sleepDelay > 0)
        {
            Thread.Sleep(sleepDelay);
        }
    }

    public Mat CaptureGameMat()
    {
        return CaptureToRectArea().SrcMat;
    }

    public Mat CaptureGameGreyMat()
    {
        return CaptureToRectArea().SrcGreyMat;
    }

    public ImageRegion CaptureGameRectArea()
    {
        return CaptureToRectArea();
    }

    public void CheckTask()
    {
        NewRetry.Do(() =>
        {
            if (_cts is { IsCancellationRequested: true })
            {
                return;
            }

            if (!SystemControl.IsGenshinImpactActiveByProcess())
            {
                _logger.LogWarning("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.，Пауза");
                throw new RetryException("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.");
            }
        }, TimeSpan.FromSeconds(1), 100);

        if (_cts is { IsCancellationRequested: true })
        {
            throw new TaskCanceledException("Задача отменена");
        }
    }

    public void CommonDuelPrepare()
    {
        // 1. Выберите начальную руку
        Sleep(1000);
        _logger.LogInformation("начинатьВыберите начальную руку");
        while (!ClickConfirm())
        {
            // Циклическое ожидание экрана выбора карты
            Sleep(1000);
        }

        _logger.LogInformation("Нажмите, чтобы подтвердить");

        // 2. Выберите роль для игры
        // Выберите здесь2роль Тор
        _logger.LogInformation("ждать3sПодготовка к игре...");
        Sleep(3000);

        // Это интерфейс выбора персонажа?
        NewRetry.Do(IsInCharacterPickRetryThrowable, TimeSpan.FromSeconds(0.8), 20);
        _logger.LogInformation("Признано, что персонаж уже находится в боевом интерфейсе.，ждать1.5s");
        Sleep(1500);
    }

    public void SortActionPhaseDiceMats(HashSet<ElementalType> elementSet)
    {
        _assets.ActionPhaseDiceMats = _assets.ActionPhaseDiceMats.OrderByDescending(kvp =>
            {
                for (var i = 0; i < elementSet.Count; i++)
                {
                    if (kvp.Key == elementSet.ElementAt(i).ToLowerString())
                    {
                        return i;
                    }
                }

                return -1;
            })
            .ToDictionary(x => x.Key, x => x.Value);
        // Распечатать отсортированный порядок
        var msg = _assets.ActionPhaseDiceMats.Aggregate("", (current, kvp) => current + $"{kvp.Key.ToElementalType().ToChinese()}| ");
        _logger.LogDebug("Текущая сортировка кубиков：{Msg}", msg);
    }

    /// <summary>
    /// Получите нашу сторону триролькартаобласть
    /// </summary>
    /// <returns></returns>
    public List<Rect> GetCharacterRects()
    {
        var srcMat = CaptureGameMat();
        var halfHeight = srcMat.Height / 2;
        var bottomMat = new Mat(srcMat, new Rect(0, halfHeight, srcMat.Width, srcMat.Height - halfHeight));

        var lowPurple = new Scalar(235, 245, 198);
        var highPurple = new Scalar(255, 255, 236);
        var gray = OpenCvCommonHelper.Threshold(bottomMat, lowPurple, highPurple);

        // Проецируйте горизонтально, чтобыyось Обычно имеется только одна непрерывная область.
        var h = ArithmeticHelper.HorizontalProjection(gray);

        // yось Подтвердите непрерывные области сверху вниз
        int y1 = 0, y2 = 0;
        int start = 0;
        var inLine = false;
        for (int i = 0; i < h.Length; i++)
        {
            // Гистограмма
            if (OutputImageWhenError)
            {
                Cv2.Line(bottomMat, 0, i, h[i], i, Scalar.Yellow);
            }

            if (h[i] > h.Average() * 10)
            {
                if (!inLine)
                {
                    //Введенная область символов из пустого места，рекордная отметка
                    inLine = true;
                    start = i;
                }
            }
            else if (inLine)
            {
                //От сплошной области к пустой области
                inLine = false;

                if (y1 == 0)
                {
                    y1 = start;
                    if (OutputImageWhenError)
                    {
                        Cv2.Line(bottomMat, 0, y1, bottomMat.Width, y1, Scalar.Red);
                    }
                }
                else if (y2 == 0 && i - y1 > 20)
                {
                    y2 = i;
                    if (OutputImageWhenError)
                    {
                        Cv2.Line(bottomMat, 0, y2, bottomMat.Width, y2, Scalar.Red);
                    }

                    break;
                }
            }
        }

        if (y1 == 0 || y2 == 0)
        {
            _logger.LogWarning("Область карты персонажа не распознается（Yось）");
            if (OutputImageWhenError)
            {
                Cv2.ImWrite("log\\character_card_error.jpg", bottomMat);
            }

            throw new RetryException("Область символов не получена");
        }

        //if (y1 < windowRect.Height / 2 || y2 < windowRect.Height / 2)
        //{
        //    MyLogger.Warn("Область карты распознанного персонажа（Yось）ошибка：y1:{} y2:{}", y1, y2);
        //    if (OutputImageWhenError)
        //    {
        //        Cv2.ImWrite("log\\character_card_error.jpg", bottomMat);
        //    }

        //    throw new RetryException("Область символов не получена");
        //}

        // вертикальная проекция
        var v = ArithmeticHelper.VerticalProjection(gray);

        inLine = false;
        start = 0;
        var colLines = new List<int>();
        //Начните определять точки сегментации на основе значений проекции
        for (int i = 0; i < v.Length; ++i)
        {
            if (OutputImageWhenError)
            {
                Cv2.Line(bottomMat, i, 0, i, v[i], Scalar.Yellow);
            }

            if (v[i] > h.Average() * 5)
            {
                if (!inLine)
                {
                    //Введенная область символов из пустого места，рекордная отметка
                    inLine = true;
                    start = i;
                }
            }
            else if (i - start > 30 && inLine)
            {
                //От сплошной области к пустой области
                inLine = false;
                if (OutputImageWhenError)
                {
                    Cv2.Line(bottomMat, start, 0, start, bottomMat.Height, Scalar.Red);
                }

                colLines.Add(start);
            }
        }

        if (colLines.Count != 6)
        {
            _logger.LogWarning("Область карты персонажа не распознается（Xосьидентификационный пункт{Count}индивидуальный）", colLines.Count);
            if (OutputImageWhenError)
            {
                Cv2.ImWrite("log\\character_card_error.jpg", bottomMat);
            }

            throw new RetryException("Область символов не получена");
        }

        var rects = new List<Rect>();
        for (var i = 0; i < colLines.Count - 1; i++)
        {
            if (i % 2 == 0)
            {
                var r = new Rect(colLines[i], halfHeight + y1, colLines[i + 1] - colLines[i],
                    y2 - y1);
                rects.Add(r);
            }
        }

        if (rects == null || rects.Count != 3)
        {
            throw new RetryException("Область символов не получена");
        }

        //_logger.LogInformation("Определите область карты персонажа:{Rects}", rects);

        //Cv2.ImWrite("log\\character_card_success.jpg", bottomMat);
        return rects;
    }

    /// <summary>
    /// Нажмите, чтобы зафиксировать относительное положение области
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public void ClickCaptureArea(int x, int y)
    {
        var rect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        ClickExtension.Click(rect.X + x, rect.Y + y);
    }

    /// <summary>
    ///  Нажмите на центральную точку игрового экрана.
    /// </summary>
    public void ClickGameWindowCenter()
    {
        var p = TaskContext.Instance().SystemInfo.CaptureAreaRect.GetCenterPoint();
        p.Click();
    }

    /*public static Dictionary<string, List<Point>> FindMultiPicFromOneImage(Mat srcMat, Dictionary<string, Mat> imgSubDictionary, double threshold = 0.8)
    {
        var dictionary = new Dictionary<string, List<Point>>();
        foreach (var kvp in imgSubDictionary)
        {
            var list = MatchTemplateHelper.MatchTemplateMulti(srcMat, kvp.Value, threshold);
            dictionary.Add(kvp.Key, list);
            // скрыть результат，Избегайте дублирования идентификации
            foreach (var point in list)
            {
                Cv2.Rectangle(srcMat, point, new Point(point.X + kvp.Value.Width, point.Y + kvp.Value.Height), Scalar.Black, -1);
            }
        }

        return dictionary;
    }*/

    public static Dictionary<string, List<Point>> FindMultiPicFromOneImage2OneByOne(Mat srcMat, Dictionary<string, Mat> imgSubDictionary, double threshold = 0.8)
    {
        var dictionary = new Dictionary<string, List<Point>>();
        foreach (var kvp in imgSubDictionary)
        {
            var list = new List<Point>();

            while (true)
            {
                var point = MatchTemplateHelper.MatchTemplate(srcMat, kvp.Value, TemplateMatchModes.CCoeffNormed, null, threshold);
                if (point != new Point())
                {
                    // скрыть результат，Избегайте дублирования идентификации
                    Cv2.Rectangle(srcMat, point, new Point(point.X + kvp.Value.Width, point.Y + kvp.Value.Height), Scalar.Black, -1);
                    list.Add(point);
                }
                else
                {
                    break;
                }
            }

            dictionary.Add(kvp.Key, list);
        }

        return dictionary;
    }

    /// <summary>
    /// перебросить кубик
    /// </summary>
    /// <param name="holdElementalTypes">Зарезервированные типы элементов</param>
    public bool RollPhaseReRoll(params ElementalType[] holdElementalTypes)
    {
        var gameSnapshot = CaptureGameMat();
        Cv2.CvtColor(gameSnapshot, gameSnapshot, ColorConversionCodes.BGRA2BGR);
        var dictionary = FindMultiPicFromOneImage2OneByOne(gameSnapshot, _assets.RollPhaseDiceMats, 0.73);

        var count = dictionary.Sum(kvp => kvp.Value.Count);

        if (count != 8)
        {
            _logger.LogDebug("Кости были израсходованы{Count}индивидуальныйигральная кость,ждатьПовторить попытку", count);
            return false;
        }
        else
        {
            _logger.LogInformation("Кости были израсходованы{Count}индивидуальныйигральная кость", count);
        }

        int upper = 0, lower = 0;
        foreach (var kvp in dictionary)
        {
            foreach (var point in kvp.Value)
            {
                if (point.Y < gameSnapshot.Height / 2)
                {
                    upper++;
                }
                else
                {
                    lower++;
                }
            }
        }

        if (upper != 4 || lower != 4)
        {
            _logger.LogInformation("игральная костьОпределить местоположениеошибка,Повторить попытку");
            return false;
        }

        foreach (var kvp in dictionary)
        {
            // Сражаться или нетЗарезервированные типы элементов
            if (holdElementalTypes.Contains(kvp.Key.ToElementalType()))
            {
                continue;
            }

            // Выберите, чтобы проголосовать повторно
            foreach (var point in kvp.Value)
            {
                ClickCaptureArea(point.X + _assets.RollPhaseDiceMats[kvp.Key].Width / 2, point.Y + _assets.RollPhaseDiceMats[kvp.Key].Height / 2);
                Sleep(100);
            }
        }

        return true;
    }

    /// <summary>
    ///  Выберите руку/перебросить кубик подтверждать
    /// </summary>
    public bool ClickConfirm()
    {
        var foundRectArea = CaptureGameRectArea().Find(_assets.ConfirmButtonRo);
        if (!foundRectArea.IsEmpty())
        {
            foundRectArea.Click();
            foundRectArea.Dispose();
            return true;
        }

        return false;
    }

    public void ReRollDice(params ElementalType[] holdElementalTypes)
    {
        // 3.перебросить кубик
        _logger.LogInformation("ждать5sАнимация броска кубиков...");

        var msg = holdElementalTypes.Aggregate(" ", (current, elementalType) => current + (elementalType.ToChinese() + " "));

        _logger.LogInformation("бронировать{Msg}игральная кость", msg);
        Sleep(5000);
        var retryCount = 0;
        // бронировать x、универсальный игральная кость
        while (!RollPhaseReRoll(holdElementalTypes))
        {
            retryCount++;

            if (IsDuelEnd())
            {
                throw new NormalEndException("Битва окончена,Остановить автоматическое воспроизведение！");
            }

            //MyLogger.Debug("идентифицироватьигральная костьНеправильное количество,Нет.{}ВторосортныйПовторить попыткусередина...", retryCount);
            Sleep(500);
            if (retryCount > 35)
            {
                throw new System.Exception("идентифицироватьигральная костьНеправильное количество,Повторить попыткутайм-аут,Остановить автоматическое воспроизведение！");
            }
        }

        ClickConfirm();
        _logger.LogInformation("Выбор требует реинвестированияизигральная костьназадНажмите, чтобы подтвердитьполный");

        Sleep(1000);
        // Переместите мышь в центр
        ClickGameWindowCenter();

        _logger.LogInformation("ждать5sПротивник перебрасывает");
        Sleep(5000);
    }

    public Point MakeOffset(Point p)
    {
        var rect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        return new Point(rect.X + p.X, rect.Y + p.Y);
    }

    /// <summary>
    /// Посчитайте те, которые есть на данный моментигральная кость
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, int> ActionPhaseDice()
    {
        var srcMat = CaptureGameMat();
        Cv2.CvtColor(srcMat, srcMat, ColorConversionCodes.BGRA2BGR);
        // Узнаем после вырезания картинки Способствовать росту Местоположение бесполезно，Так удобнее после резки
        var dictionary = FindMultiPicFromOneImage2OneByOne(CutRight(srcMat, srcMat.Width / 5), _assets.ActionPhaseDiceMats, 0.7);

        var msg = "";
        var result = new Dictionary<string, int>();
        foreach (var kvp in dictionary)
        {
            result.Add(kvp.Key, kvp.Value.Count);
            msg += $"{kvp.Key.ToElementalType().ToChinese()} {kvp.Value.Count}| ";
        }

        _logger.LogInformation("текущийигральная костьсостояние：{Res}", msg);
        return result;
    }

    /// <summary>
    ///  сжигать карты
    /// </summary>
    public void ActionPhaseElementalTuning(int currentCardCount)
    {
        var rect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var m = Simulation.SendInput.Mouse;
        ClickExtension.Click(rect.X + rect.Width / 2d, rect.Y + rect.Height - 50);
        Sleep(1500);
        if (currentCardCount == 1)
        {
            // Последняя карта справа.，а не посередине
            ClickExtension.Move(rect.X + rect.Width / 2d + 120, rect.Y + rect.Height - 50);
        }

        m.LeftButtonDown();
        Sleep(100);
        m = ClickExtension.Move(rect.X + rect.Width - 50, rect.Y + rect.Height / 2d);
        Sleep(100);
        m.LeftButtonUp();
    }

    /// <summary>
    ///  сжигать картыподтверждать（кнопка сверки элементов）
    /// </summary>
    public bool ActionPhaseElementalTuningConfirm()
    {
        var ra = CaptureGameRectArea();
        // Cv2.ImWrite("log\\" + DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff") + ".png", ra.SrcMat);
        var foundRectArea = ra.Find(_assets.ElementalTuningConfirmButtonRo);
        if (!foundRectArea.IsEmpty())
        {
            foundRectArea.Click();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Нажмите кнопку вырезать
    /// </summary>
    /// <returns></returns>
    public void ActionPhasePressSwitchButton()
    {
        var info = TaskContext.Instance().SystemInfo;
        var x = info.CaptureAreaRect.X + info.CaptureAreaRect.Width - 100 * info.AssetScale;
        var y = info.CaptureAreaRect.Y + info.CaptureAreaRect.Height - 120 * info.AssetScale;

        ClickExtension.Move(x, y).LeftButtonClick();
        Sleep(800); // ждатьАнимация полностью выскакивает

        ClickExtension.Move(x, y).LeftButtonClick();
    }

    /// <summary>
    /// Используйте навыки
    /// </summary>
    /// <param name="skillIndex">Номер навыка,Считаем справа налево,от1начинать</param>
    /// <returns>элементигральная костьСерийный номер должен быть в</returns>
    public bool ActionPhaseUseSkill(int skillIndex)
    {
        ClickGameWindowCenter(); // перезагрузить
        Sleep(500);
        // Координаты навыков жестко закодированы (w - 100 * n, h - 120)
        var info = TaskContext.Instance().SystemInfo;
        var x = info.CaptureAreaRect.X + info.CaptureAreaRect.Width - 100 * info.AssetScale * skillIndex;
        var y = info.CaptureAreaRect.Y + info.CaptureAreaRect.Height - 120 * info.AssetScale;
        ClickExtension.Click(x, y);
        Sleep(1200); // ждатьАнимация полностью выскакивает

        var foundRectArea = CaptureGameRectArea().Find(_assets.ElementalDiceLackWarningRo);
        if (foundRectArea.IsEmpty())
        {
            // Нажмите несколько раз, чтобы убедиться, что вы получили щелчок
            _logger.LogInformation("Используйте навыки{SkillIndex}", skillIndex);
            ClickExtension.Click(x, y);
            Sleep(500);
            ClickGameWindowCenter(); // перезагрузить
            return true;
        }

        return false;
    }

    /// <summary>
    /// Используйте навыки（элементигральная костьнедостаточноизслучай，Список не выполненсжигать карты）
    /// </summary>
    /// <param name="skillIndex">Номер навыка,Считаем справа налево,от1начинать</param>
    /// <param name="diceCost">Расход навыковигральная костьчисло</param>
    /// <param name="elementalType">потреблятьигральная костьэлемент类型</param>
    /// <param name="duel">Игровой объект</param>
    /// <returns>手牌或者элементигральная костьСерийный номер должен быть в</returns>
    public bool ActionPhaseAutoUseSkill(int skillIndex, int diceCost, ElementalType elementalType, Duel duel)
    {
        var dice9RetryCount = 0;
        var retryCount = 0;
        var diceStatus = ActionPhaseDice();
        while (true)
        {
            int dCount = diceStatus.Sum(x => x.Value);
            if (dCount != duel.CurrentDiceCount)
            {
                if (retryCount > 20)
                {
                    throw new System.Exception("игральная костьчисло量与预期不符，Повторить попыткуВторосортныйчисло过много，Могут быть неизвестныеошибка！");
                }

                if (dCount == 9 && duel.CurrentDiceCount == 8 && diceStatus[ElementalType.Omni.ToLowerString()] > 0)
                {
                    dice9RetryCount++;
                    if (dice9RetryCount > 5)
                    {
                        // Зона поддержки существует Брат Китовый колодец случайигральная костьчисло量增加导致идентифицировать出错извопрос #1
                        // 5ВторосортныйПовторить попыткуназад仍然是9индивидуальныйигральная костьи есть хотя бы одининдивидуальныйуниверсальныйигральная кость，Множественное распознавание происходит очень редко.，На данный момент это в принципе можно считать Зона поддержки существует Брат Китовый колодец
                        // TODO : Но этоиндивидуальныйМетод не100%точный，В будущем необходимо добавить оценку области поддержки.
                        _logger.LogInformation("ожидатьизигральная костьчисло量8，Должно быть начальное ожидание，Повторить попыткумногоВторосортныйназад累计实际идентифицировать9индивидуальныйигральная костьизСитуация такова5Второсортный");
                        duel.CurrentDiceCount = 9; // 修正текущийигральная костьчисло量
                        break;
                    }
                }

                _logger.LogInformation("текущийигральная костьчисло量{Count}与ожидатьизигральная костьчисло量{Expect}не равный，Повторить попытку", dCount, duel.CurrentDiceCount);
                diceStatus = ActionPhaseDice();
                retryCount++;
                Sleep(1000);
            }
            else
            {
                break;
            }
        }

        int needSpecifyElementDiceCount = diceCost - diceStatus[ElementalType.Omni.ToLowerString()] - diceStatus[elementalType.ToLowerString()];
        if (needSpecifyElementDiceCount > 0)
        {
            if (duel.CurrentCardCount < needSpecifyElementDiceCount)
            {
                _logger.LogInformation("Текущее количество карт на руке{Current}меньше, чем нужносжигать картычисло量{Expect}，Невозможно раскрыть навыки", duel.CurrentCardCount, needSpecifyElementDiceCount);
                return false;
            }

            _logger.LogInformation("текущий需要изэлементигральная костьчисло量不足{Cost}индивидуальный，До сих пор отсутствует{Lack}индивидуальный，Текущее количество карт на руке{Current}，сжигать карты", diceCost, needSpecifyElementDiceCount, duel.CurrentCardCount);

            for (var i = 0; i < needSpecifyElementDiceCount; i++)
            {
                _logger.LogInformation("- горетьНет.{Count}карта", i + 1);
                ActionPhaseElementalTuning(duel.CurrentCardCount);
                Sleep(1200);
                var res = ActionPhaseElementalTuningConfirm();
                if (res == false)
                {
                    _logger.LogWarning("сжигать картынеудача，Повторить попытку");
                    i--;
                    ClickGameWindowCenter(); // перезагрузить
                    Sleep(1000);
                    continue;
                }

                Sleep(1000); // сжигать картыанимация
                ClickGameWindowCenter(); // перезагрузить
                Sleep(500);
                duel.CurrentCardCount--;
                // 最назадодинкартаизМедленная скорость восстановления.，подожди еще немного
                if (duel.CurrentCardCount <= 1)
                {
                    ClickGameWindowCenter(); // перезагрузить
                    Sleep(500);
                }
            }
        }

        return ActionPhaseUseSkill(skillIndex);
    }

    /// <summary>
    /// конец раунда
    /// </summary>
    public void RoundEnd()
    {
        CaptureGameRectArea().Find(_assets.RoundEndButtonRo, foundRectArea =>
        {
            foundRectArea.Click();
            Sleep(1000); // Есть всплывающая анимация
            foundRectArea.Click();
            Sleep(300);
        });

        ClickGameWindowCenter(); // перезагрузить
    }

    /// <summary>
    /// Это интерфейс выбора персонажа?
    /// МожетПовторить попыткуметод
    /// </summary>
    public void IsInCharacterPickRetryThrowable()
    {
        if (!IsInCharacterPick())
        {
            throw new RetryException("В настоящее время нет в интерфейсе выбора персонажа.");
        }
    }

    /// <summary>
    /// Это интерфейс выбора персонажа?
    /// </summary>
    /// <returns></returns>
    public bool IsInCharacterPick()
    {
        return !CaptureGameRectArea().Find(_assets.InCharacterPickRo).IsEmpty();
    }

    /// <summary>
    /// это моя очередь
    /// </summary>
    /// <returns></returns>
    public bool IsInMyAction()
    {
        return !CaptureGameRectArea().Find(_assets.RoundEndButtonRo).IsEmpty();
    }

    /// <summary>
    /// Настала очередь противника?
    /// </summary>
    /// <returns></returns>
    public bool IsInOpponentAction()
    {
        return !CaptureGameRectArea().Find(_assets.InOpponentActionRo).IsEmpty();
    }

    /// <summary>
    /// Это стадия круглого урегулирования?
    /// </summary>
    /// <returns></returns>
    public bool IsEndPhase()
    {
        return !CaptureGameRectArea().Find(_assets.EndPhaseRo).IsEmpty();
    }

    /// <summary>
    /// Был ли побеждён персонаж в бою
    /// </summary>
    /// <returns></returns>
    public bool IsActiveCharacterTakenOut()
    {
        return !CaptureGameRectArea().Find(_assets.CharacterTakenOutRo).IsEmpty();
    }

    /// <summary>
    /// Какие персонажи в бою потерпели поражение?
    /// </summary>
    /// <returns>true был сбит с ног</returns>
    public bool[] WhatCharacterDefeated(List<Rect> rects)
    {
        if (rects == null || rects.Count != 3)
        {
            throw new System.Exception("Не удалось получить местоположение карты нашего персонажа.");
        }

        var pList = MatchTemplateHelper.MatchTemplateMulti(CaptureGameGreyMat(), _assets.CharacterDefeatedMat, 0.8);

        var res = new bool[3];
        foreach (var p in pList)
        {
            for (var i = 0; i < rects.Count; i++)
            {
                if (IsOverlap(rects[i], new Rect(p.X, p.Y, _assets.CharacterDefeatedMat.Width, _assets.CharacterDefeatedMat.Height)))
                {
                    res[i] = true;
                }
            }
        }

        return res;
    }

    /// <summary>
    /// Определите, перекрываются ли прямоугольники
    /// </summary>
    /// <param name="rc1"></param>
    /// <param name="rc2"></param>
    /// <returns></returns>
    public bool IsOverlap(Rect rc1, Rect rc2)
    {
        if (rc1.X + rc1.Width > rc2.X &&
            rc2.X + rc2.Width > rc1.X &&
            rc1.Y + rc1.Height > rc2.Y &&
            rc2.Y + rc2.Height > rc1.Y
           )
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Игра полностью окончена?
    /// </summary>
    /// <returns></returns>
    public bool IsDuelEnd()
    {
        return !CaptureGameRectArea().Find(_assets.ExitDuelButtonRo).IsEmpty();
    }

    public Mat CutRight(Mat srcMat, int saveRightWidth)
    {
        srcMat = new Mat(srcMat, new Rect(srcMat.Width - saveRightWidth, 0, saveRightWidth, srcMat.Height));
        return srcMat;
    }

    /// <summary>
    /// ждатьяизкруглый
    /// Наш персонаж может умереть в этот период
    /// </summary>
    public void WaitForMyTurn(Duel duel, int waitTime = 0)
    {
        if (waitTime > 0)
        {
            _logger.LogInformation("ждатьДействия противника{Time}s", waitTime / 1000);
            Sleep(waitTime);
        }

        // Определить, закончилось ли действие противника
        var retryCount = 0;
        var inMyActionCount = 0;
        while (true)
        {
            if (IsInMyAction())
            {
                if (IsActiveCharacterTakenOut())
                {
                    DoWhenCharacterDefeated(duel);
                }
                else
                {
                    // большая задержка2s // Гарантировано, что отобразилось приглашение о проигрыше
                    inMyActionCount++;
                    if (inMyActionCount == 3)
                    {
                        break;
                    }
                }
            }
            else if (IsDuelEnd())
            {
                throw new NormalEndException("Битва окончена,Остановить автоматическое воспроизведение！");
            }

            retryCount++;
            if (retryCount >= 60)
            {
                throw new System.Exception("ждатьДействия противникатайм-аут,Остановить автоматическое воспроизведение！");
            }

            _logger.LogInformation("Другая сторона все еще в действии,продолжатьждать(Второсортныйчисло{Count})...", retryCount);
            Sleep(1000);
        }
    }

    /// <summary>
    /// ждать对方круглый и конец раундаэтап
    /// Наш персонаж может умереть в этот период
    /// </summary>
    public void WaitOpponentAction(Duel duel)
    {
        var rd = new Random();
        Sleep(3000 + rd.Next(1, 1000));
        // Определить, закончилось ли действие противника
        var retryCount = 0;
        while (true)
        {
            if (IsInOpponentAction())
            {
                _logger.LogInformation("Другая сторона все еще в действии,продолжатьждать(Второсортныйчисло{Count})...", retryCount);
            }
            else if (IsEndPhase())
            {
                _logger.LogInformation("являютсяконец раундаэтап,продолжатьждать(Второсортныйчисло{Count})...", retryCount);
            }
            else if (IsInMyAction())
            {
                if (IsActiveCharacterTakenOut())
                {
                    DoWhenCharacterDefeated(duel);
                }
            }
            else if (IsDuelEnd())
            {
                throw new NormalEndException("Битва окончена,Остановить автоматическое воспроизведение！");
            }
            else
            {
                // Иди хотя бы триВторосортный判断才能确定Действия противника结束
                if (retryCount > 2)
                {
                    break;
                }
                else
                {
                    _logger.LogError("ждать对方круглый и конец раундаэтап Программа не распознала действительный контент(Второсортныйчисло{Count})...", retryCount);
                }
            }

            retryCount++;
            if (retryCount >= 60)
            {
                throw new System.Exception("ждатьДействия противникатайм-аут,Остановить автоматическое воспроизведение！");
            }

            Sleep(1000 + rd.Next(1, 500));
        }
    }

    /// <summary>
    /// После того, как персонаж будет побеждён, вам необходимо сменить персонажа.
    /// </summary>
    /// <param name="duel"></param>
    /// <exception cref="NormalEndException"></exception>
    public void DoWhenCharacterDefeated(Duel duel)
    {
        _logger.LogInformation("Персонаж, играющий в данный момент, побеждён，Нужно выбрать новую роль для игры");
        var defeatedArray = WhatCharacterDefeated(duel.CharacterCardRects);

        for (var i = defeatedArray.Length - 1; i >= 0; i--)
        {
            duel.Characters[i + 1].IsDefeated = defeatedArray[i];
        }

        var orderList = duel.GetCharacterSwitchOrder();
        if (orderList.Count == 0)
        {
            throw new NormalEndException("Стратегия последующих действий,Длительное нажатие, чтобы повернуть перспективу,Завершить автоматическую игру в карты(Предложить добавить дополнительные действия)");
        }

        foreach (var j in orderList)
        {
            if (!duel.Characters[j].IsDefeated)
            {
                duel.Characters[j].SwitchWhenTakenOut();
                break;
            }
        }

        ClickGameWindowCenter();
        Sleep(2000); // Вырезание анимации людей
    }

    public void AppendCharacterStatus(Character character, Mat greyMat, int hp = -2)
    {
        // Расширение области перехватываемого боевого персонажа
        using var characterMat = new Mat(greyMat, new Rect(character.Area.X,
            character.Area.Y,
            character.Area.Width + 40,
            character.Area.Height + 10));
        // Определить ненормальное состояние персонажа
        var pCharacterStatusFreeze = MatchTemplateHelper.MatchTemplate(characterMat, _assets.CharacterStatusFreezeMat, TemplateMatchModes.CCoeffNormed);
        if (pCharacterStatusFreeze != new Point())
        {
            character.StatusList.Add(CharacterStatusEnum.Frozen);
        }

        var pCharacterStatusDizziness = MatchTemplateHelper.MatchTemplate(characterMat, _assets.CharacterStatusDizzinessMat, TemplateMatchModes.CCoeffNormed);
        if (pCharacterStatusDizziness != new Point())
        {
            character.StatusList.Add(CharacterStatusEnum.Frozen);
        }

        // Определить энергетику персонажа
        var energyPointList = MatchTemplateHelper.MatchTemplateMulti(characterMat.Clone(), _assets.CharacterEnergyOnMat, 0.8);
        character.EnergyByRecognition = energyPointList.Count;

        character.Hp = hp;

        _logger.LogInformation("Сейчас играю{Character}", character);
    }

    public Character WhichCharacterActiveWithRetry(Duel duel)
    {
        // Проверьте, побежден ли персонаж // Проверьте здесь еще разВторосортный是因为最назадодинрольвыживатьизслучай，Автоматически пойдет в бой
        var defeatedArray = WhatCharacterDefeated(duel.CharacterCardRects);
        for (var i = defeatedArray.Length - 1; i >= 0; i--)
        {
            duel.Characters[i + 1].IsDefeated = defeatedArray[i];
        }

        return WhichCharacterActiveByHpOcr(duel);
    }

    public Character WhichCharacterActiveByHpWord(Duel duel)
    {
        if (duel.CharacterCardRects == null || duel.CharacterCardRects.Count != 3)
        {
            throw new System.Exception("Не удалось получить местоположение карты нашего персонажа.");
        }

        var srcMat = CaptureGameMat();

        int halfHeight = srcMat.Height / 2;
        Mat bottomMat = new Mat(srcMat, new Rect(0, halfHeight, srcMat.Width, srcMat.Height - halfHeight));

        var lowPurple = new Scalar(239, 239, 239);
        var highPurple = new Scalar(255, 255, 255);
        Mat gray = OpenCvCommonHelper.Threshold(bottomMat, lowPurple, highPurple);

        var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(15, 10), new OpenCvSharp.Point(-1, -1));
        Cv2.Dilate(gray, gray, kernel); //Расширение

        Cv2.FindContours(gray, out var contours, out _, RetrievalModes.External,
            ContourApproximationModes.ApproxSimple, null);

        if (contours.Length > 0)
        {
            // .Where(w => w.Width > 1 && w.Height >= 5)
            var rects = contours.Select(Cv2.BoundingRect).ToList();

            // в соответствии сYосьВысокая сортировка
            rects = rects.OrderBy(r => r.Y).ToList();

            // Нет.одининдивидуальныйиРольПерекрытие картизпрямоугольник
            foreach (var rect in rects)
            {
                for (var i = 0; i < duel.CharacterCardRects.Count; i++)
                {
                    // Увеличение высоты，Убедитесь, что оно пересекается
                    var rect1 = new Rect(rect.X, halfHeight + rect.Y, rect.Width + 20,
                        rect.Height + 20);
                    if (IsOverlap(rect1, duel.CharacterCardRects[i]) &&
                        halfHeight + rect.Y < duel.CharacterCardRects[i].Y)
                    {
                        // головаиндивидуальный相交прямоугольник就是идти воеватьРоль
                        duel.CurrentCharacter = duel.Characters[i + 1];
                        var grayMat = new Mat();
                        Cv2.CvtColor(srcMat, grayMat, ColorConversionCodes.BGR2GRAY);
                        AppendCharacterStatus(duel.CurrentCharacter, grayMat);

                        Cv2.Rectangle(srcMat, rect1, Scalar.Yellow);
                        Cv2.Rectangle(srcMat, duel.CharacterCardRects[i], Scalar.Blue, 2);
                        OutputImage(duel, rects, bottomMat, halfHeight, "log\\active_character2_success.jpg");
                        return duel.CurrentCharacter;
                    }
                }
            }

            OutputImage(duel, rects, bottomMat, halfHeight, "log\\active_character2_no_overlap_error.jpg");
        }
        else
        {
            if (OutputImageWhenError)
            {
                Cv2.ImWrite("log\\active_character2_no_rects_error.jpg", gray);
            }
        }

        throw new RetryException("未идентифицировать到индивидуальныйидти воеватьРоль");
    }

    public Character WhichCharacterActiveByHpOcr(Duel duel)
    {
        if (duel.CharacterCardRects == null || duel.CharacterCardRects.Count != 3)
        {
            throw new System.Exception("Не удалось получить местоположение карты нашего персонажа.");
        }

        var srcMat = CaptureGameGreyMat();

        var hpArray = new int[3]; // 1 Представитель не играл 2 Идти на войну от имени
        for (var i = 0; i < duel.CharacterCardRects.Count; i++)
        {
            if (duel.Characters[i + 1].IsDefeated)
            {
                // Персонажи, потерпевшие поражение, не должны появляться в бою.
                hpArray[i] = 1;
                continue;
            }

            var cardRect = duel.CharacterCardRects[i];
            // Персонажи, не игравшие в боюhpобласть
            var hpMat = new Mat(srcMat, new Rect(cardRect.X + _config.CharacterCardExtendHpRect.X,
                cardRect.Y + _config.CharacterCardExtendHpRect.Y,
                _config.CharacterCardExtendHpRect.Width, _config.CharacterCardExtendHpRect.Height));
            var text = OcrFactory.Paddle.Ocr(hpMat);
            //Cv2.ImWrite($"log\\hp_n_{i}.jpg", hpMat);
            Debug.WriteLine($"Роль{i}Не игралHPРезультаты распознавания местоположения{text}");
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Объясни эторольНе играл
                hpArray[i] = 1;
            }
            else
            {
                hpMat = new Mat(srcMat, new Rect(cardRect.X + _config.CharacterCardExtendHpRect.X,
                    cardRect.Y + _config.CharacterCardExtendHpRect.Y - _config.ActiveCharacterCardSpace,
                    _config.CharacterCardExtendHpRect.Width, _config.CharacterCardExtendHpRect.Height));
                text = OcrFactory.Paddle.Ocr(hpMat);
                //Cv2.ImWrite($"log\\hp_active_{i}.jpg", hpMat);
                Debug.WriteLine($"Роль{i}идти воеватьHPРезультаты распознавания местоположения{text}");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var hp = -2;
                    if (Regex.IsMatch(text, @"^[0-9]+$"))
                    {
                        hp = int.Parse(text);
                    }

                    hpArray[i] = 2;
                    duel.CurrentCharacter = duel.Characters[i + 1];
                    AppendCharacterStatus(duel.CurrentCharacter, srcMat, hp);
                    return duel.CurrentCharacter;
                }
            }
        }

        if (hpArray.Count(x => x == 1) == 2)
        {
            // найди и подожди1из
            var index = hpArray.ToList().FindIndex(x => x != 1);
            Debug.WriteLine($"проходитьOCR HPиз方式没有идентифицировать到идти воеватьРоль，нопроходитьИсключениеподтверждатьРоль{index + 1}видти воеватьсостояние！");
            duel.CurrentCharacter = duel.Characters[index + 1];
            AppendCharacterStatus(duel.CurrentCharacter, srcMat);
            return duel.CurrentCharacter;
        }

        // Вышеупомянутое решение недействительно
        _logger.LogWarning("проходитьOCR HPиз方式未идентифицировать到идти воеватьРоль {Array}", hpArray);
        return NewRetry.Do(() => WhichCharacterActiveByHpWord(duel), TimeSpan.FromSeconds(0.3), 2);
    }

    private static void OutputImage(Duel duel, List<Rect> rects, Mat bottomMat, int halfHeight, string fileName)
    {
        if (OutputImageWhenError)
        {
            foreach (var rect2 in rects)
            {
                Cv2.Rectangle(bottomMat, new OpenCvSharp.Point(rect2.X, rect2.Y),
                    new OpenCvSharp.Point(rect2.X + rect2.Width, rect2.Y + rect2.Height), Scalar.Red, 1);
            }

            foreach (var rc in duel.CharacterCardRects)
            {
                Cv2.Rectangle(bottomMat,
                    new Rect(rc.X, rc.Y - halfHeight, rc.Width, rc.Height), Scalar.Green, 1);
            }

            Cv2.ImWrite(fileName, bottomMat);
        }
    }

    /// <summary>
    /// проходитьOCRидентифицироватьтекущийигральная костьчисло量
    /// </summary>
    /// <returns></returns>
    public int GetDiceCountByOcr()
    {
        var srcMat = CaptureGameGreyMat();
        var diceCountMap = new Mat(srcMat, _config.MyDiceCountRect);
        var text = OcrFactory.Paddle.OcrWithoutDetector(diceCountMap);
        text = text.Replace(" ", "")
            .Replace("①", "1")
            .Replace("②", "2")
            .Replace("③", "3")
            .Replace("④", "4")
            .Replace("⑤", "5")
            .Replace("⑥", "6")
            .Replace("⑦", "7")
            .Replace("⑧", "8")
            .Replace("⑨", "9")
            .Replace("⑩", "10")
            .Replace("⑪", "11")
            .Replace("⑫", "12")
            .Replace("⑬", "13")
            .Replace("⑭", "14")
            .Replace("⑮", "15");
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("проходитьOCRидентифицироватьтекущийигральная костьчисло量результат为空,нет эффекта");
#if DEBUG
            Cv2.ImWrite($"log\\dice_count_empty{DateTime.Now:yyyy-MM-dd HH：mm：ss：ffff}.jpg", diceCountMap);
#endif
            return -10;
        }
        else if (Regex.IsMatch(text, @"^[0-9]+$"))
        {
            _logger.LogInformation("проходитьOCRидентифицироватьтекущийигральная костьчисло量: {Text}", text);
            return int.Parse(text);
        }
        else
        {
            _logger.LogWarning("проходитьOCRидентифицироватьтекущийигральная костьрезультат: {Text}", text);
#if DEBUG
            Cv2.ImWrite($"log\\dice_count_error_{DateTime.Now:yyyy-MM-dd HH：mm：ss：ffff}.jpg", diceCountMap);
#endif
            return -10;
        }
    }
}
