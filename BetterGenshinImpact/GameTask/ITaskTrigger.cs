namespace BetterGenshinImpact.GameTask;

/// <summary>
/// триггерный интерфейс
/// * Может использоваться для запуска задач、Управление отображением перед запуском задачи
/// * Это также может быть сама задача
///
/// Необходимость непрерывного цикла для получения игровых изображений за короткий период времени，Используйте триггеры；
/// Нужно спать и ждать и иметь определенный процесс，Должен быть реализован самостоятельноTask
/// </summary>
public interface ITaskTrigger
{
    /// <summary>
    /// Имя триггера
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Это включено
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// приоритет выполнения，Чем больше значение, тем раньше оно выполняется.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// При выходе из конструктора
    /// </summary>
    bool IsExclusive { get; }

    /// <summary>
    /// В состоянии, когда он может работать в фоновом режиме（Окно Genshin Impact не активно）
    /// </summary>
    bool IsBackgroundRunning => false;

    /// <summary>
    /// инициализация
    /// </summary>
    void Init();

    /// <summary>
    /// Действия после захвата изображений
    /// </summary>
    /// <param name="content">Захваченные изображения и многое другое</param>
    void OnCapture(CaptureContent content);
}
