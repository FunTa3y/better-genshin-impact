using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoSkip.Model;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.ViewModel.Pages;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using WinRT;

namespace BetterGenshinImpact.GameTask.AutoSkip;

public class AutoTrackTask(AutoTrackParam param) : BaseIndependentTask
{
    // /// <summary>
    // /// готов двигаться вперед
    // /// </summary>
    // private bool _readyMoveForward = false;

    /// <summary>
    /// расстояние задачи
    /// </summary>
    private Rect _missionDistanceRect = Rect.Empty;

    public async void Start()
    {
        var hasLock = false;
        try
        {
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                Logger.LogError("Не удалось запустить функцию автоматического отслеживания.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            SystemControl.ActivateWindow();

            Logger.LogInformation("→ {Text}", "Автоматическое отслеживание，запускать！");

            TrackMission();
        }
        catch (NormalEndException e)
        {
            Logger.LogInformation("Автоматическое отслеживаниепрерывать:" + e.Message);
            // NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Cancelled().Build());
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogDebug(e.StackTrace);
            // NotificationHelper.SendTaskNotificationWithScreenshotUsing(b => b.Domain().Failure().Build());
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskSettingsPageViewModel.SetSwitchAutoTrackButtonText(false);
            Logger.LogInformation("→ {Text}", "Автоматическое отслеживаниеЗаканчивать");

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private void TrackMission()
    {
        // Убедитесь, что следующая задача будет выполняться только в главном интерфейсе.
        var ra = GetRectAreaFromDispatcher();
        var paimonMenuRa = ra.Find(ElementAssets.Instance.PaimonMenuRo);
        if (!paimonMenuRa.IsExist())
        {
            Sleep(5000, param.Cts);
            return;
        }

        // Текст задачи имеет анимационные эффекты，ждать2sСделать снимок экрана заново
        Simulation.SendInput.Mouse.MoveMouseBy(0, 7000);
        Sleep(2000, param.Cts);

        // OCR Текст задачи Под миникартой
        var textRaList = OcrMissionTextRaList(paimonMenuRa);
        if (textRaList.Count == 0)
        {
            Logger.LogInformation("не найденоТекст задачи");
            Sleep(5000, param.Cts);
            return;
        }

        // отТекст задачиизвлечь израсстояние
        var distance = GetDistanceFromMissionText(textRaList);
        Logger.LogInformation("Отслеживание задач：{Text}", "расстояние" + distance + "m");
        if (distance >= 150)
        {
            // расстояниебольше, чем150рис，Сначала телепортируйтесь в ближайшую точку телепортации.
            // J Открыть задачу Переключить отслеживание на открытую карту Центральная точка — это точка миссии.
            Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_J);
            Sleep(800, param.Cts);
            // TODO Определите, находитесь ли вы в интерфейсе задачи
            // Переключить отслеживание
            var btn = ra.Derive(CaptureRect.Width - 250, CaptureRect.Height - 60);
            btn.Click();
            Sleep(200, param.Cts);
            btn.Click();
            Sleep(1500, param.Cts);

            // Найдите все точки телепортации.
            ra = GetRectAreaFromDispatcher();
            var tpPointList = MatchTemplateHelper.MatchMultiPicForOnePic(ra.SrcGreyMat, QuickTeleportAssets.Instance.MapChooseIconGreyMatList);
            if (tpPointList.Count > 0)
            {
                // Выберите точку телепортации, ближайшую к центральной точке.
                var centerX = ra.Width / 2;
                var centerY = ra.Height / 2;
                var minDistance = double.MaxValue;
                var nearestRect = Rect.Empty;
                foreach (var tpPoint in tpPointList)
                {
                    var distanceTp = Math.Sqrt(Math.Pow(Math.Abs(tpPoint.X - centerX), 2) + Math.Pow(Math.Abs(tpPoint.Y - centerY), 2));
                    if (distanceTp < minDistance)
                    {
                        minDistance = distanceTp;
                        nearestRect = tpPoint;
                    }
                }

                ra.Derive(nearestRect).Click();
                // ждатьАвтоматический перенос завершен
                Sleep(2000, param.Cts);

                if (Bv.IsInBigMapUi(GetRectAreaFromDispatcher()))
                {
                    Logger.LogWarning("Все еще в интерфейсе большой карты，Передача не удалась");
                }
                else
                {
                    Sleep(500, param.Cts);
                    NewRetry.Do(() =>
                    {
                        if (!Bv.IsInMainUi(GetRectAreaFromDispatcher()))
                        {
                            Logger.LogInformation("Не зашёл в основной интерфейс，продолжатьждать");
                            throw new RetryException("Не зашёл в основной интерфейс");
                        }
                    }, TimeSpan.FromSeconds(1), 100);
                    StartTrackPoint();
                }
            }
            else
            {
                Logger.LogWarning("высокий");
            }
        }
        else
        {
            StartTrackPoint();
        }
    }

    private void StartTrackPoint()
    {
        // VКлючевое прямое отслеживание
        Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_V);
        Sleep(3000, param.Cts);

        var ra = GetRectAreaFromDispatcher();
        var blueTrackPointRa = ra.Find(ElementAssets.Instance.BlueTrackPoint);
        if (blueTrackPointRa.IsExist())
        {
            MakeBlueTrackPointDirectlyAbove();
        }
        else
        {
            Logger.LogWarning("Точка отслеживания не найдена впервые");
        }
    }

    /// <summary>
    /// Найдите точку отслеживания и отрегулируйте направление
    /// </summary>
    private void MakeBlueTrackPointDirectlyAbove()
    {
        // return new Task(() =>
        // {
        int prevMoveX = 0;
        bool wDown = false;
        while (!param.Cts.Token.IsCancellationRequested)
        {
            var ra = GetRectAreaFromDispatcher();
            var blueTrackPointRa = ra.Find(ElementAssets.Instance.BlueTrackPoint);
            if (blueTrackPointRa.IsExist())
            {
                // Расположите точку отслеживания над видом с высоты птичьего полета
                var centerY = blueTrackPointRa.Y + blueTrackPointRa.Height / 2;
                if (centerY > CaptureRect.Height / 2)
                {
                    Simulation.SendInput.Mouse.MoveMouseBy(-50, 0);
                    if (wDown)
                    {
                        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_W);
                        wDown = false;
                    }
                    Debug.WriteLine("Расположите точку отслеживания над видом с высоты птичьего полета");
                    continue;
                }

                // Отрегулировать направление
                var centerX = blueTrackPointRa.X + blueTrackPointRa.Width / 2;
                var moveX = (centerX - CaptureRect.Width / 2) / 8;
                moveX = moveX switch
                {
                    > 0 and < 10 => 10,
                    > -10 and < 0 => -10,
                    _ => moveX
                };
                if (moveX != 0)
                {
                    Simulation.SendInput.Mouse.MoveMouseBy(moveX, 0);
                    Debug.WriteLine("Отрегулировать направление:" + moveX);
                }

                if (moveX == 0 || prevMoveX * moveX < 0)
                {
                    if (!wDown)
                    {
                        Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_W);
                        wDown = true;
                    }
                }

                if (Math.Abs(moveX) < 50 && Math.Abs(centerY - CaptureRect.Height / 2) < 200)
                {
                    if (wDown)
                    {
                        Simulation.SendInput.Keyboard.KeyUp(User32.VK.VK_W);
                        wDown = false;
                    }
                    // идентифицироватьрасстояние
                    var text = OcrFactory.Paddle.OcrWithoutDetector(ra.SrcGreyMat[_missionDistanceRect]);
                    if (StringUtils.TryExtractPositiveInt(text) is > -1 and <= 3)
                    {
                        Logger.LogInformation("Отслеживание задач：достичь цели,Результаты распознавания[{Text}]", text);
                        break;
                    }
                    Logger.LogInformation("Отслеживание задач：достичь цели");
                    break;
                }

                prevMoveX = moveX;
            }
            else
            {
                // случайный ход
                Logger.LogInformation("Точка отслеживания не найдена");
            }

            Simulation.SendInput.Mouse.MoveMouseBy(0, 500); // Гарантированный вид с высоты птичьего полета
            Sleep(100);
        }
        // });
    }

    private int GetDistanceFromMissionText(List<Region> textRaList)
    {
        // распечатать всеТекст задачи
        var text = textRaList.Aggregate(string.Empty, (current, textRa) => current + textRa.Text.Trim() + "|");
        Logger.LogInformation("Отслеживание задач：{Text}", text);

        foreach (var textRa in textRaList)
        {
            if (textRa.Text.Length < 8 && (textRa.Text.Contains("m") || textRa.Text.Contains("M")))
            {
                _missionDistanceRect = textRa.ConvertSelfPositionToGameCaptureRegion();
                return StringUtils.TryExtractPositiveInt(textRa.Text);
            }
        }

        return -1;
    }

    private List<Region> OcrMissionTextRaList(Region paimonMenuRa)
    {
        return GetRectAreaFromDispatcher().FindMulti(new RecognitionObject
        {
            RecognitionType = RecognitionTypes.Ocr,
            RegionOfInterest = new Rect(paimonMenuRa.X, paimonMenuRa.Y - 15 + 210,
                (int)(300 * AssetScale), (int)(100 * AssetScale))
        });
    }
}
