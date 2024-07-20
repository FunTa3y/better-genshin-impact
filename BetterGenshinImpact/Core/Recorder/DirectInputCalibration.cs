using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Common.Map;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.Core.Recorder;

/// <summary>
/// DirectInput、Расстояние перемещения мыши、Разделить объекты по областям изображения
/// </summary>
[Obsolete]
public class DirectInputCalibration
{
    // Просмотр единицы перемещения смещения
    private const int CharMovingUnit = 500;

    public async void Start()
    {
        var hasLock = false;
        try
        {
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                Logger.LogError("Не удалось запустить функцию калибровки угла обзора.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            Init();

            await Task.Run(() =>
            {
                GetOffsetAngle();
            });
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
            Logger.LogInformation("→ {Text}", "Пожалуйста, не повторяйте задания");

            if (hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    private void Init()
    {
        SystemControl.ActivateWindow();
        Logger.LogInformation("→ {Text}", "Калибровка угла обзора，запускать！");
        TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.OnlyCacheCapture);
        Sleep(TaskContext.Instance().Config.TriggerInterval * 5); // Ожидание кэшированного изображения
    }

    public int GetOffsetAngle()
    {
        var directInputMonitor = new DirectInputMonitor();
        var ms1 = directInputMonitor.GetMouseState();
        Logger.LogInformation("Текущий статус мыши：{X} {Y}", ms1.X, ms1.Y);
        var angle1 = GetCharacterOrientationAngle();
        Simulation.SendInput.Mouse.MoveMouseBy(CharMovingUnit, 0);
        Sleep(500);

        Simulation.SendInput.Keyboard.KeyDown(User32.VK.VK_W).Sleep(100).KeyUp(User32.VK.VK_W);
        Sleep(1000);

        var ms2 = directInputMonitor.GetMouseState();
        Logger.LogInformation("Текущий статус мыши：{X} {Y}", ms2.X, ms2.Y);
        var angle2 = GetCharacterOrientationAngle();
        var angleOffset = angle2 - angle1;
        var directInputXOffset = ms2.X - ms1.X;
        Logger.LogInformation("папка с материалами：панорамирование мыши{CharMovingUnit}единица，Угол поворота{AngleOffset}，DirectInputдвигаться{DirectInputXOffset}",
            CharMovingUnit, angleOffset, directInputXOffset);

        var angle2MouseMoveByX = CharMovingUnit * 1d / angleOffset;
        var angle2DirectInputX = directInputXOffset * 1d / angleOffset;
        Logger.LogInformation("Результаты калибровки：Перспективадвигаться1Тратить，нуждатьсяMouseMoveByрасстояние{Angle2MouseMoveByX}，нуждатьсяDirectInputдвигатьсяизединица{Angle2DirectInputX}",
                       angle2MouseMoveByX, angle2DirectInputX);

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
        Logger.LogInformation("текущий уголТратить：{Angle}", angle);
        // CameraOrientation.DrawDirection(ra, angle);
        return angle;
    }
}
