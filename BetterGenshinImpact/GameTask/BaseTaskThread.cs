using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask;

/// <summary>
/// Разница между триггерами：Задача не требует непрерывного захвата игровых изображений.
/// </summary>
public class BaseTaskThread
{
    private readonly ILogger<BaseTaskThread> _logger = App.GetLogger<BaseTaskThread>();

    protected BaseTaskParam _taskParam;

    protected BaseTaskThread(BaseTaskParam taskParam)
    {
        _taskParam = taskParam;
    }

    /// <summary>
    /// Блокируйте и запускайте задачи независимо
    /// </summary>
    /// <param name="useLock"></param>
    /// <returns></returns>
    public async Task StandaloneRunAsync(bool useLock = true)
    {
        // Замок
        var hasLock = false;
        if (useLock)
        {
            hasLock = await TaskSemaphore.WaitAsync(0);
            if (!hasLock)
            {
                _logger.LogError("{Name} Запуск не удался：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！", _taskParam.Name);
                return;
            }
        }

        try
        {
            _logger.LogInformation("→ {Text}", _taskParam.Name + "запускать！");

            // инициализация
            Init();

            // Отправлять уведомления о запущенных задачах
            SendNotification();

            await OnRunAsync();
        }
        catch (NormalEndException e)
        {
            _logger.LogInformation("{Name} прерывать:{Msg}", _taskParam.Name, e.Message);
            SendNotification();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            _logger.LogDebug(e.StackTrace);
            SendNotification();
        }
        finally
        {
            End();
            _logger.LogInformation("→ {Text}", _taskParam.Name + "Заканчивать");

            // разблокировать замок
            if (useLock && hasLock)
            {
                TaskSemaphore.Release();
            }
        }
    }

    /// <summary>
    /// Логика выполнения задач
    /// </summary>
    /// <returns></returns>
    public virtual async Task OnRunAsync()
    {
        await Task.Delay(0);
    }

    public void Init()
    {
        if (_taskParam.TriggerOperation == DispatcherTimerOperationEnum.StopTimer)
        {
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.Stop);
        }
        else if (_taskParam.TriggerOperation == DispatcherTimerOperationEnum.UseCacheImage)
        {
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.OnlyCacheCapture);
        }
    }

    public void End()
    {
        VisionContext.Instance().DrawContent.ClearAll();
        if (_taskParam.TriggerOperation == DispatcherTimerOperationEnum.StopTimer)
        {
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.Start);
        }
        else if (_taskParam.TriggerOperation == DispatcherTimerOperationEnum.UseCacheImage)
        {
            TaskTriggerDispatcher.Instance().SetCacheCaptureMode(DispatcherCaptureModeEnum.CacheCaptureWithTrigger);
        }
    }

    public void SendNotification()
    {
    }
}
