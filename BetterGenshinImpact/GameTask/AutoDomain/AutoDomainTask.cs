﻿using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Assets;
using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.GameTask.AutoFight.Script;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoPick.Assets;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Compunet.YoloV8;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.GameTask.AutoDomain;

public class AutoDomainTask
{
    private readonly AutoDomainParam _taskParam;

    private readonly PostMessageSimulator _simulator;

    private readonly YoloV8Predictor _predictor;

    private readonly AutoDomainConfig _config;

    private readonly CombatScriptBag _combatScriptBag;

    public AutoDomainTask(AutoDomainParam taskParam)
    {
        _taskParam = taskParam;
        _simulator = AutoFightContext.Instance.Simulator;

        _predictor = YoloV8Builder.CreateDefaultBuilder()
            .UseOnnxModel(Global.Absolute("Assets\\Model\\Domain\\bgi_tree.onnx"))
            .WithSessionOptions(BgiSessionOption.Instance.Options)
            .Build();

        _config = TaskContext.Instance().Config.AutoDomainConfig;

        _combatScriptBag = CombatScriptParser.ReadAndParse(_taskParam.CombatStrategyPath);
    }

    public async void Start()
    {
        var hasLock = false;
        try
        {
            AutoFightAssets.DestroyInstance();
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                Logger.LogError("Не удалось запустить автоматическую функцию секретной области.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            Init();
            NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Started().Build());

            var combatScenes = new CombatScenes().InitializeTeam(GetRectAreaFromDispatcher());

            // Войдите в секретное царство заранее
            EnterDomain();

            for (var i = 0; i < _taskParam.DomainRoundNum; i++)
            {
                // 0. Закрыть подсказку о секретном мире
                Logger.LogDebug("0. Закрыть подсказку о секретном мире");
                CloseDomainTip();

                // Если команда не инициализируется успешно, повторите попытку.
                RetryTeamInit(combatScenes);

                // 0. Переключиться на первый символ
                var combatCommands = FindCombatScriptAndSwitchAvatar(combatScenes);

                // 1. Подойди к ключу и запусти его
                Logger.LogInformation("Автоматическое секретное царство：{Text}", "1. Подойди к ключу и запусти его");
                await WalkToPressF();

                // 2. Выполнить бой（боевая нить、перспективная нить、Обнаружить цепочку завершения боя）
                Logger.LogInformation("Автоматическое секретное царство：{Text}", "2. Выполнить бойСтратегия");
                await StartFight(combatScenes, combatCommands);
                EndFightWait();

                // 3. Ищем окаменелые древние деревья и двигайтесь влево и вправо, пока окаменевшее древнее дерево не окажется в центре экрана.
                Logger.LogInformation("Автоматическое секретное царство：{Text}", "3. Ищем окаменелые древние деревья");
                await FindPetrifiedTree();

                // 4. Прогулка к окаменевшему древнему дереву
                Logger.LogInformation("Автоматическое секретное царство：{Text}", "4. Прогулка к окаменевшему древнему дереву");
                await WalkToPressF();

                // 5. Быстро получайте награды и определяйте, будет ли следующий раунд.
                Logger.LogInformation("Автоматическое секретное царство：{Text}", "5. Получить награду");
                if (!GettingTreasure(_taskParam.DomainRoundNum == 9999, i == _taskParam.DomainRoundNum - 1))
                {
                    if (i == _taskParam.DomainRoundNum - 1)
                    {
                        Logger.LogInformation("настроен{Cnt}Секретное царство завершено，ЗаканчиватьАвтоматическое секретное царство", _taskParam.DomainRoundNum);
                    }
                    else
                    {
                        Logger.LogInformation("Физические силы исчерпаны，ЗаканчиватьАвтоматическое секретное царство");
                    }
                    NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Success().Build());
                    break;
                }
                NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Progress().Build());
            }
        }
        catch (NormalEndException e)
        {
            Logger.LogInformation("Автоматическое секретное царствопрерывать:" + e.Message);
            NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Cancelled().Build());
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogDebug(e.StackTrace);
            NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Failure().Build());
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.NormalTrigger);
            TaskSettingsPageViewModel.SetSwitchAutoDomainButtonText(false);
            Logger.LogInformation("→ {Text}", "Автоматическое секретное царствоЗаканчивать");

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private void Init()
    {
        LogScreenResolution();
        if (_taskParam.DomainRoundNum == 9999)
        {
            Logger.LogInformation("→ {Text} Завершите после исчерпания всей вашей энергии.", "Автоматическое секретное царство，запускать！");
        }
        else
        {
            Logger.LogInformation("→ {Text} Установите общее количество раз：{Cnt}", "Автоматическое секретное царство，запускать！", _taskParam.DomainRoundNum);
        }

        SystemControl.ActivateWindow();
        TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.OnlyCacheCapture);
        Sleep(TaskContext.Instance().Config.TriggerInterval * 5, _taskParam.Cts); // Ожидание кэшированного изображения
    }

    private void LogScreenResolution()
    {
        var gameScreenSize = SystemControl.GetGameScreenRect(TaskContext.Instance().GameHandle);
        if (gameScreenSize.Width * 9 != gameScreenSize.Height * 16)
        {
            Logger.LogWarning("Разрешение окна игры не 16:9 ！Текущее разрешение {Width}x{Height} , Нет 16:9 Игры с разными разрешениями могут работать некорректноАвтоматическое секретное царствоФункция !", gameScreenSize.Width, gameScreenSize.Height);
        }
        if (gameScreenSize.Width < 1920 || gameScreenSize.Height < 1080)
        {
            Logger.LogWarning("Разрешение окна игры меньше, чем 1920x1080 ！Текущее разрешение {Width}x{Height} , меньше, чем 1920x1080 的Игры с разными разрешениями могут работать некорректноАвтоматическое секретное царствоФункция !", gameScreenSize.Width, gameScreenSize.Height);
        }
    }

    private void RetryTeamInit(CombatScenes combatScenes)
    {
        if (!combatScenes.CheckTeamInitialized())
        {
            combatScenes.InitializeTeam(GetRectAreaFromDispatcher());
            if (!combatScenes.CheckTeamInitialized())
            {
                throw new Exception("Не удалось определить роль в команде.，Пожалуйста, попробуйте еще раз на более темном фоне.，Например, настроить время игры на ночь.。Или напрямую используйте функцию принудительного назначения текущей командной роли.。");
            }
        }
    }

    private void EnterDomain()
    {
        var fightAssets = AutoFightContext.Instance.FightAssets;

        using var fRectArea = GetRectAreaFromDispatcher().Find(AutoPickAssets.Instance.FRo);
        if (!fRectArea.IsEmpty())
        {
            Simulation.SendInput.Keyboard.KeyPress(VK.VK_F);
            Logger.LogInformation("Автоматическое секретное царство：{Text}", "Войдите в секретное царство");
            // Анимация открытия секретного мира 5s
            Sleep(5000, _taskParam.Cts);
        }

        int retryTimes = 0, clickCount = 0;
        while (retryTimes < 20 && clickCount < 2)
        {
            retryTimes++;
            using var confirmRectArea = GetRectAreaFromDispatcher().Find(fightAssets.ConfirmRa);
            if (!confirmRectArea.IsEmpty())
            {
                confirmRectArea.Click();
                clickCount++;
            }

            Sleep(1500, _taskParam.Cts);
        }

        // анимация загрузки
        Sleep(3000, _taskParam.Cts);
    }

    private void CloseDomainTip()
    {
        // 2minВремени загрузки достаточно, не так ли?
        var retryTimes = 0;
        while (retryTimes < 120)
        {
            retryTimes++;
            using var cactRectArea = GetRectAreaFromDispatcher().Find(AutoFightContext.Instance.FightAssets.ClickAnyCloseTipRa);
            if (!cactRectArea.IsEmpty())
            {
                Sleep(1000, _taskParam.Cts);
                cactRectArea.Click();
                break;
            }

            // todo Добавлено определение положения углового маркера мини-карты. Чтобы никто не прикоснулся к нему
            Sleep(1000, _taskParam.Cts);
        }

        Sleep(1500, _taskParam.Cts);
    }

    private List<CombatCommand> FindCombatScriptAndSwitchAvatar(CombatScenes combatScenes)
    {
        var combatCommands = _combatScriptBag.FindCombatScript(combatScenes.Avatars);
        var avatar = combatScenes.SelectAvatar(combatCommands[0].Name);
        avatar?.SwitchWithoutCts();
        Sleep(200);
        return combatCommands;
    }

    /// <summary>
    /// Подойди к ключу и запусти его
    /// </summary>
    private async Task WalkToPressF()
    {
        if (_taskParam.Cts.Token.IsCancellationRequested)
        {
            return;
        }

        await Task.Run(() =>
        {
            _simulator.KeyDown(VK.VK_W);
            Sleep(20);
            // Кажется, что комбинацию клавиш нельзя использовать напрямую postmessage
            if (!_config.WalkToF)
            {
                Simulation.SendInput.Keyboard.KeyDown(VK.VK_SHIFT);
            }

            try
            {
                while (!_taskParam.Cts.Token.IsCancellationRequested)
                {
                    using var fRectArea = GetRectAreaFromDispatcher().Find(AutoPickAssets.Instance.FRo);
                    if (fRectArea.IsEmpty())
                    {
                        Sleep(100, _taskParam.Cts);
                    }
                    else
                    {
                        Logger.LogInformation("Обнаружен ключ взаимодействия");
                        Simulation.SendInput.Keyboard.KeyPress(VK.VK_F);
                        break;
                    }
                }
            }
            finally
            {
                _simulator.KeyUp(VK.VK_W);
                Sleep(50);
                if (!_config.WalkToF)
                {
                    Simulation.SendInput.Keyboard.KeyUp(VK.VK_SHIFT);
                }
            }
        });
    }

    private Task StartFight(CombatScenes combatScenes, List<CombatCommand> combatCommands)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        _taskParam.Cts.Token.Register(cts.Cancel);
        combatScenes.BeforeTask(cts);
        // боевые действия
        var combatTask = new Task(() =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Универсальная боевая стратегия
                    foreach (var command in combatCommands)
                    {
                        command.Execute(combatScenes);
                    }
                }
            }
            catch (NormalEndException e)
            {
                Logger.LogInformation("боевые действияпрерывать：{Msg}", e.Message);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e.Message);
                throw;
            }
            finally
            {
                Logger.LogInformation("автоматическийбоевая нитьЗаканчивать");
            }
        }, cts.Token);

        // Перспективная операция

        // Обнаружение окончания игры
        var domainEndTask = DomainEndDetectionTask(cts);

        combatTask.Start();
        domainEndTask.Start();

        return Task.WhenAll(combatTask, domainEndTask);
    }

    private void EndFightWait()
    {
        if (_taskParam.Cts.Token.IsCancellationRequested)
        {
            return;
        }

        var s = TaskContext.Instance().Config.AutoDomainConfig.FightEndDelay;
        if (s > 0)
        {
            Logger.LogInformation("Подожди после боя {Second} Второй", s);
            Sleep((int)(s * 1000), _taskParam.Cts);
        }
    }

    /// <summary>
    /// Обнаружение окончания игры
    /// </summary>
    private Task DomainEndDetectionTask(CancellationTokenSource cts)
    {
        return new Task(() =>
        {
            while (!_taskParam.Cts.Token.IsCancellationRequested)
            {
                if (IsDomainEnd())
                {
                    cts.Cancel();
                    break;
                }

                Sleep(1000);
            }
        });
    }

    private bool IsDomainEnd()
    {
        using var ra = GetRectAreaFromDispatcher();

        var endTipsRect = ra.DeriveCrop(AutoFightContext.Instance.FightAssets.EndTipsUpperRect);
        var text = OcrFactory.Paddle.Ocr(endTipsRect.SrcGreyMat);
        if (text.Contains("испытание") || text.Contains("достигать"))
        {
            Logger.LogInformation("Обнаружено приглашение на завершение секретной области(испытаниедостигать)，Покончить с секретным царством");
            return true;
        }

        endTipsRect = ra.DeriveCrop(AutoFightContext.Instance.FightAssets.EndTipsRect);
        text = OcrFactory.Paddle.Ocr(endTipsRect.SrcGreyMat);
        if (text.Contains("автоматический") || text.Contains("покидать"))
        {
            Logger.LogInformation("Обнаружено приглашение на завершение секретной области(xxxВторойназадавтоматическийпокидать)，Покончить с секретным царством");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 旋转视角назадИщем окаменелые древние деревья
    /// </summary>
    private Task FindPetrifiedTree()
    {
        CancellationTokenSource treeCts = new CancellationTokenSource();
        _taskParam.Cts.Token.Register(treeCts.Cancel);
        // Щелкните средней кнопкой мыши, чтобы вернуться к обычному виду.
        Simulation.SendInput.Mouse.MiddleButtonClick();
        Sleep(900, _taskParam.Cts);

        // Двигайтесь влево и вправо, пока окаменевшее древнее дерево не окажется в центре экрана.
        var moveAvatarTask = MoveAvatarHorizontallyTask(treeCts);

        // Блокировка восточного направленияперспективная нить
        var lockCameraToEastTask = LockCameraToEastTask(treeCts, moveAvatarTask);
        lockCameraToEastTask.Start();
        return Task.WhenAll(moveAvatarTask, lockCameraToEastTask);
    }

    private Task MoveAvatarHorizontallyTask(CancellationTokenSource treeCts)
    {
        return new Task(() =>
        {
            var captureArea = TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect;
            var middleX = captureArea.Width / 2;
            var leftKeyDown = false;
            var rightKeyDown = false;
            var noDetectCount = 0;
            var prevKey = VK.VK_A;
            var backwardsAndForwardsCount = 0;
            while (!_taskParam.Cts.Token.IsCancellationRequested)
            {
                var treeRect = DetectTree(GetRectAreaFromDispatcher());
                if (treeRect != Rect.Empty)
                {
                    var treeMiddleX = treeRect.X + treeRect.Width / 2;
                    if (treeRect.X + treeRect.Width < middleX && !_config.ShortMovement)
                    {
                        backwardsAndForwardsCount = 0;
                        // дерево слева иди налево
                        Debug.WriteLine($"дерево слева иди налево {treeMiddleX}  {middleX}");
                        if (rightKeyDown)
                        {
                            // сначала ослабьDключ
                            _simulator.KeyUp(VK.VK_D);
                            rightKeyDown = false;
                        }

                        if (!leftKeyDown)
                        {
                            _simulator.KeyDown(VK.VK_A);
                            leftKeyDown = true;
                        }
                    }
                    else if (treeRect.X > middleX && !_config.ShortMovement)
                    {
                        backwardsAndForwardsCount = 0;
                        // дерево справа Иди направо
                        Debug.WriteLine($"дерево справа Иди направо {treeMiddleX}  {middleX}");
                        if (leftKeyDown)
                        {
                            // сначала ослабьAключ
                            _simulator.KeyUp(VK.VK_A);
                            leftKeyDown = false;
                        }

                        if (!rightKeyDown)
                        {
                            _simulator.KeyDown(VK.VK_D);
                            rightKeyDown = true;
                        }
                    }
                    else
                    {
                        // дерево посередине ослабить всеключ
                        if (rightKeyDown)
                        {
                            _simulator.KeyUp(VK.VK_D);
                            prevKey = VK.VK_D;
                            rightKeyDown = false;
                        }

                        if (leftKeyDown)
                        {
                            _simulator.KeyUp(VK.VK_A);
                            prevKey = VK.VK_A;
                            leftKeyDown = false;
                        }

                        // Отпустите кнопкуключназад使用小碎步移动
                        if (treeMiddleX < middleX)
                        {
                            if (prevKey == VK.VK_D)
                            {
                                backwardsAndForwardsCount++;
                            }

                            _simulator.KeyPress(VK.VK_A, 60);
                            prevKey = VK.VK_A;
                        }
                        else if (treeMiddleX > middleX)
                        {
                            if (prevKey == VK.VK_A)
                            {
                                backwardsAndForwardsCount++;
                            }

                            _simulator.KeyPress(VK.VK_D, 60);
                            prevKey = VK.VK_D;
                        }
                        else
                        {
                            _simulator.KeyPress(VK.VK_W, 60);
                            Sleep(500, _taskParam.Cts);
                            treeCts.Cancel();
                            break;
                        }
                    }
                }
                else
                {
                    backwardsAndForwardsCount = 0;
                    // Патрулировать направо и налево
                    noDetectCount++;
                    if (noDetectCount > 40)
                    {
                        if (leftKeyDown)
                        {
                            _simulator.KeyUp(VK.VK_A);
                            leftKeyDown = false;
                        }

                        if (!rightKeyDown)
                        {
                            _simulator.KeyDown(VK.VK_D);
                            rightKeyDown = true;
                        }
                    }
                    else
                    {
                        if (rightKeyDown)
                        {
                            _simulator.KeyUp(VK.VK_D);
                            rightKeyDown = false;
                        }

                        if (!leftKeyDown)
                        {
                            _simulator.KeyDown(VK.VK_A);
                            leftKeyDown = true;
                        }
                    }
                }

                if (backwardsAndForwardsCount >= _config.LeftRightMoveTimes)
                {
                    // Двигайтесь влево и вправо5Это описание уже находится в центре дерева
                    _simulator.KeyPress(VK.VK_W, 60);
                    Sleep(500, _taskParam.Cts);
                    treeCts.Cancel();
                    break;
                }

                Sleep(60, _taskParam.Cts);
            }

            VisionContext.Instance().DrawContent.ClearAll();
        });
    }

    private Rect DetectTree(ImageRegion region)
    {
        using var memoryStream = new MemoryStream();
        region.SrcBitmap.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var result = _predictor.Detect(memoryStream);
        var list = new List<RectDrawable>();
        foreach (var box in result.Boxes)
        {
            var rect = new Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
            list.Add(region.ToRectDrawable(rect, "tree"));
        }

        VisionContext.Instance().DrawContent.PutOrRemoveRectList("TreeBox", list);

        if (list.Count > 0)
        {
            var box = result.Boxes[0];
            return new Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
        }

        return Rect.Empty;
    }

    private Task LockCameraToEastTask(CancellationTokenSource cts, Task moveAvatarTask)
    {
        return new Task(() =>
        {
            var continuousCount = 0; // Количество последовательных направлений на восток
            var started = false;
            while (!cts.Token.IsCancellationRequested)
            {
                using var captureRegion = GetRectAreaFromDispatcher();
                var angle = CameraOrientation.Compute(captureRegion.SrcGreyMat);
                CameraOrientation.DrawDirection(captureRegion, angle);
                if (angle is >= 356 or <= 4)
                {
                    // Считается выровненным
                    continuousCount++;
                }

                if (angle < 180)
                {
                    // Сместить перспективу влево
                    var moveAngle = angle;
                    if (moveAngle > 2)
                    {
                        moveAngle *= 2;
                    }

                    Simulation.SendInput.Mouse.MoveMouseBy(-moveAngle, 0);
                    continuousCount = 0;
                }
                else if (angle is > 180 and < 360)
                {
                    // Сдвинуть перспективу вправо
                    var moveAngle = 360 - angle;
                    if (moveAngle > 2)
                    {
                        moveAngle *= 2;
                    }

                    Simulation.SendInput.Mouse.MoveMouseBy(moveAngle, 0);
                    continuousCount = 0;
                }
                else
                {
                    // 360 Тратить Восточная перспектива
                    if (continuousCount > 5)
                    {
                        if (!started && moveAvatarTask.Status != TaskStatus.Running)
                        {
                            started = true;
                            moveAvatarTask.Start();
                        }
                    }
                }

                Sleep(100);
            }

            Logger.LogInformation("Блокировка восточного направленияперспективная нитьЗаканчивать");
            VisionContext.Instance().DrawContent.ClearAll();
        });
    }

    /// <summary>
    /// Получить награду
    /// </summary>
    /// <param name="recognizeResin">Стоит ли идентифицировать смолу</param>
    /// <param name="isLastTurn">Это последний раунд?</param>
    private bool GettingTreasure(bool recognizeResin, bool isLastTurn)
    {
        // Подождите, пока появится окно
        Sleep(1500, _taskParam.Cts);

        // Отдавайте предпочтение концентрированной смоле.
        var retryTimes = 0;
        while (true)
        {
            retryTimes++;
            if (retryTimes > 3)
            {
                Logger.LogInformation("Больше никакой концентрированной смолы");
                break;
            }

            var useCondensedResinRa = GetRectAreaFromDispatcher().Find(AutoFightContext.Instance.FightAssets.UseCondensedResinRa);
            if (!useCondensedResinRa.IsEmpty())
            {
                useCondensedResinRa.Click();
                // Не умею идентифицировать предметы #224 #218
                // Чтобы решить проблему Короля водяных драконов, нажмите влево.ключназад没松开，Затем, когда я нажимаю и нажимаю его позже, ничего не происходит.
                Sleep(400, _taskParam.Cts);
                useCondensedResinRa.Click();
                break;
            }

            Sleep(800, _taskParam.Cts);
        }

        Sleep(1000, _taskParam.Cts);

        var captureArea = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        for (var i = 0; i < 30; i++)
        {
            // Пропустить получение анимации
            GameCaptureRegion.GameRegionClick((size, scale) => (size.Width - 140 * scale, 53 * scale));
            Sleep(200, _taskParam.Cts);
            GameCaptureRegion.GameRegionClick((size, scale) => (size.Width - 140 * scale, 53 * scale));

            // Расставить приоритеты, нажмите, чтобы продолжить
            var ra = GetRectAreaFromDispatcher();
            var confirmRectArea = ra.Find(AutoFightContext.Instance.FightAssets.ConfirmRa);
            if (!confirmRectArea.IsEmpty())
            {
                if (isLastTurn)
                {
                    // последний раунд покидать
                    var exitRectArea = ra.Find(AutoFightContext.Instance.FightAssets.ExitRa);
                    if (!exitRectArea.IsEmpty())
                    {
                        exitRectArea.Click();
                        return false;
                    }
                }

                if (!recognizeResin)
                {
                    confirmRectArea.Click();
                    return true;
                }

                var (condensedResinCount, fragileResinCount) = GetRemainResinStatus();
                if (condensedResinCount == 0 && fragileResinCount < 20)
                {
                    // Нет больше энергиипокидать
                    var exitRectArea = ra.Find(AutoFightContext.Instance.FightAssets.ExitRa);
                    if (!exitRectArea.IsEmpty())
                    {
                        exitRectArea.Click();
                        return false;
                    }
                }
                else
                {
                    // Иметь силы продолжать
                    confirmRectArea.Click();
                    return true;
                }
            }

            Sleep(300, _taskParam.Cts);
        }

        throw new NormalEndException("Конец секретного мира не обнаружен，Возможно, рюкзак полон。");
    }

    /// <summary>
    /// Получить статус оставшейся смолы
    /// </summary>
    private (int, int) GetRemainResinStatus()
    {
        var condensedResinCount = 0;
        var fragileResinCount = 0;

        var ra = GetRectAreaFromDispatcher();
        // Концентрированная смола
        var condensedResinCountRa = ra.Find(AutoFightContext.Instance.FightAssets.CondensedResinCountRa);
        if (!condensedResinCountRa.IsEmpty())
        {
            // В правой части изображения находитсяКонцентрированная смолаНаш клик
            var countArea = ra.DeriveCrop(condensedResinCountRa.X + condensedResinCountRa.Width, condensedResinCountRa.Y, condensedResinCountRa.Width, condensedResinCountRa.Height);
            // Cv2.ImWrite($"log/resin_{DateTime.Now.ToString("yyyy-MM-dd HH：mm：ss：ffff")}.png", countArea.SrcGreyMat);
            var count = OcrFactory.Paddle.OcrWithoutDetector(countArea.SrcGreyMat);
            condensedResinCount = StringUtils.TryParseInt(count);
        }

        // хрупкая смола
        var fragileResinCountRa = ra.Find(AutoFightContext.Instance.FightAssets.FragileResinCountRa);
        if (!fragileResinCountRa.IsEmpty())
        {
            // В правой части изображения находитсяхрупкая смолаНаш клик
            var countArea = ra.DeriveCrop(fragileResinCountRa.X + fragileResinCountRa.Width, fragileResinCountRa.Y, (int)(fragileResinCountRa.Width * 3), fragileResinCountRa.Height);
            var count = OcrFactory.Paddle.Ocr(countArea.SrcGreyMat);
            fragileResinCount = StringUtils.TryParseInt(count);
        }

        Logger.LogInformation("Оставшийся：Концентрированная смола {CondensedResinCount} хрупкая смола {FragileResinCount}", condensedResinCount, fragileResinCount);
        return (condensedResinCount, fragileResinCount);
    }
}
