using System.Threading;
using BetterGenshinImpact.GameTask.Model.Enum;

namespace BetterGenshinImpact.GameTask.Model;

/// <summary>
/// Независимый базовый класс параметров задачи
/// </summary>
public class BaseTaskParam
{
    public string Name { get; set; } = string.Empty;

    public CancellationTokenSource Cts { get; set; }

    /// <summary>
    /// Действия над триггерами реального времени
    /// </summary>
    public DispatcherTimerOperationEnum TriggerOperation { get; set; } = DispatcherTimerOperationEnum.None;

    protected BaseTaskParam(CancellationTokenSource cts)
    {
        this.Cts = cts;
    }
}
