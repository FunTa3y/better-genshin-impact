using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Model;
using Fischless.GameCapture;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;
using BetterGenshinImpact.GameTask.Model.Area;

namespace BetterGenshinImpact.GameTask.Common;

public class TaskControl
{
    public static ILogger Logger { get; } = App.GetLogger<TaskControl>();

    public static readonly SemaphoreSlim TaskSemaphore = new(1, 1);

    public static void CheckAndSleep(int millisecondsTimeout)
    {
        if (!SystemControl.IsGenshinImpactActiveByProcess())
        {
            Logger.LogInformation("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.，Остановить выполнение");
            throw new NormalEndException("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.");
        }

        Thread.Sleep(millisecondsTimeout);
    }

    public static void Sleep(int millisecondsTimeout)
    {
        NewRetry.Do(() =>
        {
            if (!SystemControl.IsGenshinImpactActiveByProcess())
            {
                Logger.LogInformation("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.，Пауза");
                throw new RetryException("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.");
            }
        }, TimeSpan.FromSeconds(1), 100);
        Thread.Sleep(millisecondsTimeout);
    }

    public static void Sleep(int millisecondsTimeout, CancellationTokenSource? cts)
    {
        if (cts is { IsCancellationRequested: true })
        {
            throw new NormalEndException("Отменить автоматические задачи");
        }

        if (millisecondsTimeout <= 0)
        {
            return;
        }

        NewRetry.Do(() =>
        {
            if (cts is { IsCancellationRequested: true })
            {
                throw new NormalEndException("Отменить автоматические задачи");
            }

            if (!SystemControl.IsGenshinImpactActiveByProcess())
            {
                Logger.LogInformation("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.，Пауза");
                throw new RetryException("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.");
            }
        }, TimeSpan.FromSeconds(1), 100);
        Thread.Sleep(millisecondsTimeout);
        if (cts is { IsCancellationRequested: true })
        {
            throw new NormalEndException("Отменить автоматические задачи");
        }
    }

    public static void SleepWithoutThrow(int millisecondsTimeout, CancellationTokenSource cts)
    {
        try
        {
            Sleep(millisecondsTimeout, cts);
        }
        catch
        {
        }
    }

    private static Bitmap CaptureGameBitmap(IGameCapture? gameCapture)
    {
        var bitmap = gameCapture?.Capture();
        // wgc Настройки буфера2 Так хоть скриншот сделай3Второсортный
        if (gameCapture?.Mode == CaptureModes.WindowsGraphicsCapture)
        {
            for (int i = 0; i < 2; i++)
            {
                bitmap = gameCapture?.Capture();
                Sleep(50);
            }
        }

        if (bitmap == null)
        {
            Logger.LogWarning("Снимок экрана не выполнен.!");
            // Повторить попытку5Второсортный
            for (var i = 0; i < 15; i++)
            {
                bitmap = gameCapture?.Capture();
                if (bitmap != null)
                {
                    return bitmap;
                }

                Sleep(30);
            }

            throw new Exception("Попробуйте большеВторосортныйназад,Снимок экрана не выполнен.!");
        }
        else
        {
            return bitmap;
        }
    }

    [Obsolete]
    public static Bitmap CaptureGameBitmap()
    {
        return CaptureGameBitmap(TaskTriggerDispatcher.GlobalGameCapture);
    }

    private static CaptureContent CaptureToContent(IGameCapture? gameCapture)
    {
        var bitmap = CaptureGameBitmap(gameCapture);
        return new CaptureContent(bitmap, 0, 0);
    }

    // [Obsolete]
    // public static CaptureContent CaptureToContent()
    // {
    //     return CaptureToContent(TaskTriggerDispatcher.GlobalGameCapture);
    // }

    public static ImageRegion CaptureToRectArea()
    {
        return CaptureToContent(TaskTriggerDispatcher.GlobalGameCapture).CaptureRectArea;
    }

    // /// <summary>
    // /// Этот метод TaskDispatcherпо крайней мере в DispatcherCaptureModeEnum.CacheCaptureWithTrigger статус можно использовать
    // /// </summary>
    // /// <returns></returns>
    // [Obsolete]
    // public static CaptureContent GetContentFromDispatcher()
    // {
    //     return TaskTriggerDispatcher.Instance().GetLastCaptureContent();
    // }

    /// <summary>
    /// Этот метод TaskDispatcherпо крайней мере в DispatcherCaptureModeEnum.CacheCaptureWithTrigger статус можно использовать
    /// </summary>
    /// <returns></returns>
    public static ImageRegion GetRectAreaFromDispatcher()
    {
        return TaskTriggerDispatcher.Instance().GetLastCaptureContent().CaptureRectArea;
    }
}
