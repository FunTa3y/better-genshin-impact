using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoTrackPath.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using static Vanara.PInvoke.Gdi32;
using Point = OpenCvSharp.Point;

namespace BetterGenshinImpact.GameTask.AutoTrackPath;

/// <summary>
/// Принцип расчета координат
/// 1. Автоматический выбор ветки приглашения，Установите приоритет преобразования во внутриигровую систему координат Genshin Impact.
/// 2. Все операции с прямоугольниками，Установите приоритет преобразования в полную систему координат карты.
/// 3. Все операции, связанные с расчетом угла перспективы мини-карты.，Приоритет преобразован вwarpPolarиспользуемый стандарт степени
/// </summary>
public class AutoTrackPathTask
{
    private readonly AutoTrackPathParam _taskParam;
    private readonly Random _rd = new Random();

    private readonly List<GiWorldPosition> _tpPositions;

    private readonly Dictionary<string, double[]> _countryPositions = MapAssets.Instance.CountryPositions;

    private GiPath _way;

    // Просмотр единицы перемещения смещения
    private const int CharMovingUnit = 500;

    public AutoTrackPathTask(AutoTrackPathParam taskParam)
    {
        _taskParam = taskParam;
        _tpPositions = MapAssets.Instance.TpPositions;

        var wayJson = File.ReadAllText(Global.Absolute(@"log\way\way2.json"));
        _way = JsonSerializer.Deserialize<GiPath>(wayJson, ConfigService.JsonOptions) ?? throw new Exception("way json deserialize failed");
    }

    public async void Start()
    {
        var hasLock = false;
        try
        {
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                Logger.LogError("Не удалось запустить функцию автоматического маршрута.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            Init();

            // Tp(_tpPositions[260].X, _tpPositions[260].Y);

            await DoTask();
        }
        catch (NormalEndException)
        {
            Logger.LogInformation("Ручное прерывание автоматического маршрута");
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogDebug(e.StackTrace);
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.NormalTrigger);
            TaskSettingsPageViewModel.SetSwitchAutoFightButtonText(false);
            Logger.LogInformation("→ {Text}", "Автоматическое завершение маршрута");

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private void Init()
    {
        SystemControl.ActivateWindow();
        Logger.LogInformation("→ {Text}", "автоматический маршрут，запускать！");
        TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.OnlyCacheCapture);
        Sleep(TaskContext.Instance().Config.TriggerInterval * 5, _taskParam.Cts); // Ожидание кэшированного изображения
    }

    public void Stop()
    {
        _taskParam.Cts.Cancel();
    }

    public async Task DoTask()
    {
        // 1. Телепортироваться в ближайшую точку телепортации
        var first = _way.WayPointList[0]; // анализировать маршрут，Первая точка – это отправная точка
        Tp(first.Pt.X, first.Pt.Y);

        // 2. Дождитесь завершения передачи
        Sleep(1000);
        NewRetry.Do(() =>
        {
            var ra = GetRectAreaFromDispatcher();
            var miniMapMat = GetMiniMapMat(ra);
            if (miniMapMat == null)
            {
                throw new RetryException("Дождитесь завершения передачи");
            }
        }, TimeSpan.FromSeconds(1), 100);
        Logger.LogInformation("Передача завершена");
        Sleep(1000);

        // 3. Калибровка смещения бокового перемещения，Переместить указанное смещение、нажиматьWОсознайте ориентацию
        var angleOffset = GetOffsetAngle();
        if (angleOffset == 0)
        {
            throw new InvalidOperationException("Калибровка смещения бокового перемещениянеудача");
        }

        // 4. Линейное отслеживание баллов

        var trackCts = new CancellationTokenSource();
        _taskParam.Cts.Token.Register(trackCts.Cancel);
        var trackTask = Track(_way.WayPointList, angleOffset, trackCts);
        trackTask.Start();
        var refreshStatusTask = RefreshStatus(trackCts);
        refreshStatusTask.Start();
        var jumpTask = Jump(trackCts);
        jumpTask.Start();
        await Task.WhenAll(trackTask, refreshStatusTask, jumpTask);
    }

    private MotionStatus _motionStatus = MotionStatus.Normal;

    public Task Jump(CancellationTokenSource trackCts)
    {
        return new Task(() =>
        {
            while (!_taskParam.Cts.Token.IsCancellationRequested && !trackCts.Token.IsCancellationRequested)
            {
                if (_motionStatus == MotionStatus.Normal)
                {
                    MovementControl.Instance.SpacePress();
                    Sleep(300);
                    if (_motionStatus == MotionStatus.Normal)
                    {
                        MovementControl.Instance.SpacePress();
                        Sleep(3500);
                    }
                    else
                    {
                        Sleep(1600);
                    }
                }
                else
                {
                    Sleep(1600);
                }
            }
        });
    }

    private double _targetAngle = 0;

    public Task RefreshStatus(CancellationTokenSource trackCts)
    {
        return new Task(() =>
        {
            while (!_taskParam.Cts.Token.IsCancellationRequested && !trackCts.Token.IsCancellationRequested)
            {
                using var ra = GetRectAreaFromDispatcher();
                _motionStatus = Bv.GetMotionStatus(ra);

                // var miniMapMat = GetMiniMapMat(ra);
                // if (miniMapMat == null)
                // {
                //     throw new InvalidOperationException("В настоящее время нет в основном интерфейсе");
                // }
                //
                // var angle = CharacterOrientation.Compute(miniMapMat);
                // CameraOrientation.DrawDirection(ra, angle, "avatar", new Pen(Color.Blue, 1));
                // Debug.WriteLine($"Угол системы координат текущего изображения персонажа：{angle}");

                // var moveAngle = (int)(_targetAngle - angle);
                // Debug.WriteLine($"Поворот на целевой угол：{_targetAngle}，панорамирование мыши{moveAngle}единица");
                // Simulation.SendInput.Mouse.MoveMouseBy(moveAngle, 0);
                Sleep(60);
            }
        });
    }

    public Task Track(List<GiPathPoint> pList, int angleOffsetUnit, CancellationTokenSource trackCts)
    {
        return new Task(() =>
        {
            var currIndex = 0;
            while (!_taskParam.Cts.IsCancellationRequested)
            {
                var ra = GetRectAreaFromDispatcher();
                var miniMapMat = GetMiniMapMat(ra);
                if (miniMapMat == null)
                {
                    throw new InvalidOperationException("В настоящее время нет в основном интерфейсе");
                }

                // Обратите внимание, что угол игровой системы координат направлен по часовой стрелке.
                var miniMapRect = EntireMap.Instance.GetMiniMapPositionByFeatureMatch(miniMapMat);
                if (miniMapRect == Rect.Empty)
                {
                    Debug.WriteLine("Не удалось определить местоположение на мини-карте.");
                    continue;
                }

                var currMapImageAvatarPos = miniMapRect.GetCenterPoint();
                var (nextMapImagePathPoint, nextPointIndex) = GetNextPoint(currMapImageAvatarPos, pList, currIndex); // Динамический расчет следующей точки
                var nextMapImagePathPos = nextMapImagePathPoint.MatchRect.GetCenterPoint();
                Logger.LogInformation("следующий пункт[{Index}]：{nextMapImagePathPos}", nextPointIndex, nextMapImagePathPos);

                var angle = CharacterOrientation.Compute(miniMapMat);
                CameraOrientation.DrawDirection(ra, angle, "avatar", new Pen(Color.Blue, 1));
                Debug.WriteLine($"Угол системы координат текущего изображения персонажа：{angle}，Расположение：{currMapImageAvatarPos}");

                var nextAngle = Math.Round(Math.Atan2(nextMapImagePathPos.Y - currMapImageAvatarPos.Y, nextMapImagePathPos.X - currMapImageAvatarPos.X) * 180 / Math.PI);
                var nextDistance = MathHelper.Distance(nextMapImagePathPos, currMapImageAvatarPos);
                Debug.WriteLine($"Угол системы координат изображения текущей целевой точки：{nextAngle}，расстояние：{nextDistance}");
                CameraOrientation.DrawDirection(ra, nextAngle, "target", new Pen(Color.Red, 1));

                if (nextDistance < 10)
                {
                    Logger.LogInformation("Достичь целевой точки");
                    currIndex = nextPointIndex;
                    MovementControl.Instance.WUp();
                    if (currIndex == pList.Count - 1)
                    {
                        Logger.LogInformation("добраться до места назначения");
                        trackCts.Cancel();
                        break;
                    }
                }

                // Преобразовать в движения мышиединица
                _targetAngle = nextAngle;
                var moveAngle = (int)(nextAngle - angle);
                moveAngle = (int)(moveAngle * 1d / angleOffsetUnit * CharMovingUnit);
                Debug.WriteLine($"Поворот на целевой угол：{nextAngle}，панорамирование мыши{moveAngle}единица");
                Simulation.SendInput.Mouse.MoveMouseBy(moveAngle, 0);
                Sleep(100);

                miniMapMat = GetMiniMapMat(ra);
                if (miniMapMat == null)
                {
                    throw new InvalidOperationException("В настоящее время нет в основном интерфейсе");
                }
                angle = CharacterOrientation.Compute(miniMapMat);
                CameraOrientation.DrawDirection(ra, angle, "avatar", new Pen(Color.Blue, 1));

                Sleep(100);
                MovementControl.Instance.WDown();

                Sleep(50);

                // MovementControl.Instance.WDown();
                // Sleep(80);
            }
        });
    }

    /// <summary>
    ///  точка изображения карты
    ///  оглядываясь назад20в пунктах，Следующая ближайшая точка，Ключевая точка должна быть достигнута
    /// </summary>
    /// <param name="currPoint"></param>
    /// <param name="pList"></param>
    /// <param name="currIndex"></param>
    /// <returns></returns>
    public (GiPathPoint, int) GetNextPoint(Point currPoint, List<GiPathPoint> pList, int currIndex)
    {
        var nextNum = Math.Min(currIndex + 20, pList.Count - 1); // Самый последний поиск20точка
        var minDistance = double.MaxValue;
        var minDistancePoint = pList[currIndex];
        var minDistanceIndex = currIndex;
        // var minDistanceButGt = double.MaxValue;
        // var minDistancePointButGt = pList[currIndex];
        // var minDistanceIndexButGt = currIndex;
        for (var i = currIndex; i < nextNum; i++)
        {
            var nextPoint = pList[i + 1];
            var nextMapImagePos = nextPoint.MatchRect.GetCenterPoint();
            var distance = MathHelper.Distance(nextMapImagePos, currPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                minDistancePoint = nextPoint;
                minDistanceIndex = i + 1;
                // if (distance > 5)
                // {
                //     minDistanceButGt = distance;
                //     minDistancePointButGt = nextPoint;
                //     minDistanceIndexButGt = i;
                // }
            }

            if (GiPathPoint.IsKeyPoint(nextPoint))
            {
                break;
            }
        }

        // return minDistanceButGt >= double.MaxValue ? (minDistancePointButGt, minDistanceIndexButGt) : (minDistancePoint, minDistanceIndex);
        return (minDistancePoint, minDistanceIndex);
    }

    public int GetOffsetAngle()
    {
        var angle1 = GetCharacterOrientationAngle();
        Simulation.SendInput.Mouse.MoveMouseBy(CharMovingUnit, 0);
        Sleep(500);
        Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_W).Sleep(100).KeyUp(User32.VK.VK_W);
        Sleep(1000);
        var angle2 = GetCharacterOrientationAngle();
        var angleOffset = angle2 - angle1;
        Logger.LogInformation("Калибровка смещения бокового перемещения：панорамирование мыши{CharMovingUnit}единица，Угол поворота{AngleOffset}", CharMovingUnit, angleOffset);
        return angleOffset;
    }

    public Mat? GetMiniMapMat(ImageRegion ra)
    {
        var paimon = ra.Find(ElementAssets.Instance.PaimonMenuRo);
        if (paimon.IsExist())
        {
            return new Mat(ra.SrcMat, new Rect(paimon.X + 24, paimon.Y - 15, 210, 210));
        }

        return null;
    }

    public int GetCharacterOrientationAngle()
    {
        var ra = GetRectAreaFromDispatcher();
        var miniMapMat = GetMiniMapMat(ra);
        if (miniMapMat == null)
        {
            throw new InvalidOperationException("В настоящее время нет в основном интерфейсе");
        }

        var angle = CharacterOrientation.Compute(miniMapMat);
        Logger.LogInformation("текущий угол：{Angle}", angle);
        // CameraOrientation.DrawDirection(ra, angle);
        return angle;
    }

    /// <summary>
    /// Телепортируйтесь к ближайшей точке телепорта с указанными координатами через большую карту.，Затем переместите в указанные координаты
    /// </summary>
    /// <param name="tpX"></param>
    /// <param name="tpY"></param>
    public void Tp(double tpX, double tpY)
    {
        // Получите последниеизТочка телепортацииРасположение
        var (x, y) = GetRecentlyTpPoint(tpX, tpY);
        Logger.LogInformation("({TpX},{TpY}) недавнийизТочка телепортацииРасположение ({X},{Y})", tpX, tpY, x, y);

        // M Откройте карту, чтобы определить текущийРасположение，Центральная точка – это текущийРасположение
        Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_M);

        Sleep(1000);

        // 计算Точка телепортацииРасположениеПосле переключения с какой картыиз中心点недавний，Переключиться на эту карту
        SwitchRecentlyCountryMap(x, y);

        // двигатьсякарта到指定Точка телепортацииРасположение
        // Debug.WriteLine("двигатьсякарта到指定Точка телепортацииРасположение");
        // MoveMapTo(x, y);

        // Нажмите после расчета координат
        var bigMapInAllMapRect = GetBigMapRect();
        while (!bigMapInAllMapRect.Contains((int)x, (int)y))
        {
            Debug.WriteLine($"({x},{y}) Не здесь {bigMapInAllMapRect} Внутри，продолжай двигаться");
            Logger.LogInformation("Точка телепортацииНе здесьТекущий большой диапазон картВнутри，продолжай двигаться");
            MoveMapTo(x, y);
            bigMapInAllMapRect = GetBigMapRect();
        }

        // Debug.WriteLine($"({x},{y}) существовать {bigMapInAllMapRect} Внутри，посчитай этосуществоватьформаВнутриизРасположение");
        // Обратите внимание на эту координатуизИсходная точка находится где-то в центральной области.точка，Итак, нам нужно преобразовать координаты щелчка（Координата щелчка — это система координат, в которой верхний левый угол является началом координат.），Не могу просто увеличить
        var (picX, picY) = MapCoordinate.GameToMain2048(x, y);
        var picRect = MapCoordinate.GameToMain2048(bigMapInAllMapRect);
        Debug.WriteLine($"({picX},{picY}) существовать {picRect} Внутри，посчитай этосуществоватьформаВнутриизРасположение");
        var captureRect = TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect;
        var clickX = (int)((picX - picRect.X) / picRect.Width * captureRect.Width);
        var clickY = (int)((picY - picRect.Y) / picRect.Height * captureRect.Height);
        Logger.LogInformation("Нажмите на точку телепорта：({X},{Y})", clickX, clickY);
        using var ra = GetRectAreaFromDispatcher();
        ra.ClickTo(clickX, clickY);

        // Запустить функцию быстрой передачи
    }

    /// <summary>
    /// двигатьсякарта到指定Точка телепортацииРасположение
    /// Он может двигаться неправильно，Итак, вы можете повторить этот метод
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void MoveMapTo(double x, double y)
    {
        var bigMapCenterPoint = GetPositionFromBigMap();
        // движущаяся частьВнутриСмещение движения при проверке емкости
        var (xOffset, yOffset) = (x - bigMapCenterPoint.X, y - bigMapCenterPoint.Y);

        var diffMouseX = 100; // Каждыйдвижетсяизрасстояние
        if (xOffset < 0)
        {
            diffMouseX = -diffMouseX;
        }

        var diffMouseY = 100; // Каждыйдвижетсяизрасстояние
        if (yOffset < 0)
        {
            diffMouseY = -diffMouseY;
        }

        // Сначала перейдите в случайную точку ближе к центру экрана.Расположение，Избегайте недопустимого перемещения карты
        MouseMoveMapX(diffMouseX);
        MouseMoveMapY(diffMouseY);
        var newBigMapCenterPoint = GetPositionFromBigMap();
        var diffMapX = Math.Abs(newBigMapCenterPoint.X - bigMapCenterPoint.X);
        var diffMapY = Math.Abs(newBigMapCenterPoint.Y - bigMapCenterPoint.Y);
        Debug.WriteLine($"Каждый100двигатьсяизкартарасстояние：({diffMapX},{diffMapY})");

        // 快速двигаться到目标Точка телепортации所существоватьизОпределить
        if (diffMapX > 10 && diffMapY > 10)
        {
            // // Рассчитайте необходимое количество ходов
            var moveCount = (int)Math.Abs(xOffset / diffMapX); // Округлить Изначально я хотел добавить еще1из，Но однажды его перенесли
            Debug.WriteLine("X需要двигатьсяизчастота：" + moveCount);
            for (var i = 0; i < moveCount; i++)
            {
                MouseMoveMapX(diffMouseX);
            }

            moveCount = (int)Math.Abs(yOffset / diffMapY); // Округлить Изначально я хотел добавить еще1из，Но однажды его перенесли
            Debug.WriteLine("Y需要двигатьсяизчастота：" + moveCount);
            for (var i = 0; i < moveCount; i++)
            {
                MouseMoveMapY(diffMouseY);
            }
        }
    }

    public void MouseMoveMapX(int dx)
    {
        var moveUnit = dx > 0 ? 20 : -20;
        GameCaptureRegion.GameRegionMove((rect, _) => (rect.Width / 2d + _rd.Next(-rect.Width / 6, rect.Width / 6), rect.Height / 2d + _rd.Next(-rect.Height / 6, rect.Height / 6)));
        Simulation.SendInput.Mouse.LeftButtonDown().Sleep(200);
        for (var i = 0; i < dx / moveUnit; i++)
        {
            Simulation.SendInput.Mouse.MoveMouseBy(moveUnit, 0).Sleep(60); // 60 Обеспечьте отсутствие инерции
        }

        Simulation.SendInput.Mouse.LeftButtonUp().Sleep(200);
    }

    public void MouseMoveMapY(int dy)
    {
        var moveUnit = dy > 0 ? 20 : -20;
        GameCaptureRegion.GameRegionMove((rect, _) => (rect.Width / 2d + _rd.Next(-rect.Width / 6, rect.Width / 6), rect.Height / 2d + _rd.Next(-rect.Height / 6, rect.Height / 6)));
        Simulation.SendInput.Mouse.LeftButtonDown().Sleep(200);
        // 原神картасуществоватьнебольшой диапазонВнутридвигаться是无效из，Так что просто переместите его，Так что определенно двигайтесь меньше раз
        for (var i = 0; i < dy / moveUnit; i++)
        {
            Simulation.SendInput.Mouse.MoveMouseBy(0, moveUnit).Sleep(60);
        }

        Simulation.SendInput.Mouse.LeftButtonUp().Sleep(200);
    }

    public static Point GetPositionFromBigMap()
    {
        var bigMapRect = GetBigMapRect();
        Debug.WriteLine("картаРасположениеПреобразовать в игровые координаты：" + bigMapRect);
        var bigMapCenterPoint = bigMapRect.GetCenterPoint();
        Debug.WriteLine("Координаты центра карты：" + bigMapCenterPoint);
        return bigMapCenterPoint;
    }

    public static Rect GetBigMapRect()
    {
        // Определитьсуществоватькарта界面
        using var ra = GetRectAreaFromDispatcher();
        using var mapScaleButtonRa = ra.Find(QuickTeleportAssets.Instance.MapScaleButtonRo);
        if (mapScaleButtonRa.IsExist())
        {
            var rect = BigMap.Instance.GetBigMapPositionByFeatureMatch(ra.SrcGreyMat);
            Debug.WriteLine("识别大картасуществовать全картаРасположениепрямоугольник：" + rect);
            const int s = 4 * 2; // относительно1024Делать4Увеличить
            return MapCoordinate.Main2048ToGame(new Rect(rect.X * s, rect.Y * s, rect.Width * s, rect.Height * s));
        }
        else
        {
            throw new InvalidOperationException("текущийНе здеськарта界面");
        }
    }

    /// <summary>
    /// Получите последниеизТочка телепортацииРасположение
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public (int x, int y) GetRecentlyTpPoint(double x, double y)
    {
        var recentX = 0;
        var recentY = 0;
        var minDistance = double.MaxValue;
        foreach (var tpPosition in _tpPositions)
        {
            var distance = Math.Sqrt(Math.Pow(tpPosition.X - x, 2) + Math.Pow(tpPosition.Y - y, 2));
            if (distance < minDistance)
            {
                minDistance = distance;
                recentX = (int)Math.Round(tpPosition.X);
                recentY = (int)Math.Round(tpPosition.Y);
            }
        }

        return (recentX, recentY);
    }

    public void SwitchRecentlyCountryMap(double x, double y)
    {
        var bigMapCenterPoint = GetPositionFromBigMap();
        Logger.LogInformation("识别текущийРасположение：{Pos}", bigMapCenterPoint);

        var minDistance = Math.Sqrt(Math.Pow(bigMapCenterPoint.X - x, 2) + Math.Pow(bigMapCenterPoint.Y - y, 2));
        var minCountry = "текущийРасположение";
        foreach (var (country, position) in _countryPositions)
        {
            var distance = Math.Sqrt(Math.Pow(position[0] - x, 2) + Math.Pow(position[1] - y, 2));
            if (distance < minDistance)
            {
                minDistance = distance;
                minCountry = country;
            }
        }

        Logger.LogInformation("离目标Точка телепортациинедавнийизОпределить是：{Country}", minCountry);
        if (minCountry != "текущийРасположение")
        {
            GameCaptureRegion.GameRegionClick((rect, scale) => (rect.Width - 160 * scale, rect.Height - 60 * scale));
            Sleep(200, _taskParam.Cts);
            var ra = GetRectAreaFromDispatcher();
            var list = ra.FindMulti(new RecognitionObject
            {
                RecognitionType = RecognitionTypes.Ocr,
                RegionOfInterest = new Rect(ra.Width / 2, 0, ra.Width / 2, ra.Height)
            });
            list.FirstOrDefault(r => r.Text.Contains(minCountry))?.Click();
            Logger.LogInformation("переключиться на регион：{Country}", minCountry);
            Sleep(500, _taskParam.Cts);
        }
    }

    public void Tp(string name)
    {
        // Телепортируйтесь в указанную точку телепорта через большую карту.
    }

    public void TpByF1(string name)
    {
        // Телепортироваться в указанную точку телепортации
    }
}
