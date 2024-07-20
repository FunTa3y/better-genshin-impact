namespace BetterGenshinImpact.GameTask.Model.Enum;

public enum DispatcherTimerOperationEnum
{
    // Отключить триггеры в реальном времени
    StopTimer,

    // Режим кэширования графа с использованием триггеров реального времени
    UseCacheImage,

    // Успех, если вы включите один
    None
}
