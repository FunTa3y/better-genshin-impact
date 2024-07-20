using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// Автоматически пропускать настройку графика
/// </summary>
[Serializable]
public partial class AutoSkipConfig : ObservableObject
{
    /// <summary>
    /// Триггер включен?
    /// Текущее разрешение：
    /// 1. Быстро пропускать разговоры
    /// 2. Автоматически нажимать на распознанный вариант
    /// 3. Автоматически нажмите, чтобы пропустить, если черный экран слишком длинный
    /// </summary>
    [ObservableProperty] private bool _enabled = true;

    /// <summary>
    /// Быстро пропускать разговоры
    /// </summary>
    [ObservableProperty] private bool _quicklySkipConversationsEnabled = true;

    public int ChatOptionTextWidth { get; set; } = 280;

    public int ExpeditionOptionTextWidth { get; set; } = 130;

    /// <summary>
    /// Задержка перед выбором опции（миллисекунда）
    /// </summary>
    [ObservableProperty] private int _afterChooseOptionSleepDelay = 0;

    /// <summary>
    /// Автоматически получайте ежедневные комиссионные вознаграждения
    /// </summary>
    [ObservableProperty] private bool _autoGetDailyRewardsEnabled = true;

    /// <summary>
    /// автоматическая переотправка
    /// </summary>
    [ObservableProperty] private bool _autoReExploreEnabled = true;

    /// <summary>
    /// автоматическая переотправкаИспользовать конфигурацию ролей，Разделенные запятой
    /// </summary>
    [Obsolete]
    [ObservableProperty] private string _autoReExploreCharacter = "";

    /// <summary>
    /// Отдайте предпочтение первому варианту
    /// Отдайте предпочтение последнему варианту
    /// Не выбирать вариант
    /// </summary>
    [ObservableProperty] private string _clickChatOption = "Отдайте предпочтение последнему варианту";

    /// <summary>
    /// Автоматическое приглашение включено
    /// </summary>
    [ObservableProperty] private bool _autoHangoutEventEnabled = false;

    /// <summary>
    /// Автоматический выбор ветки приглашения
    /// </summary>
    [ObservableProperty] private string _autoHangoutEndChoose = string.Empty;

    /// <summary>
    /// Автоматическое приглашениеЗадержка перед выбором опции（миллисекунда）
    /// </summary>
    [ObservableProperty] private int _autoHangoutChooseOptionSleepDelay = 0;

    /// <summary>
    /// Автоматическое приглашение автоматически нажимает кнопку «Пропустить»
    /// </summary>
    [ObservableProperty] private bool _autoHangoutPressSkipEnabled = true;

    public bool IsClickFirstChatOption()
    {
        return ClickChatOption == "Отдайте предпочтение первому варианту";
    }

    public bool IsClickRandomChatOption()
    {
        return ClickChatOption == "Случайный выбор вариантов";
    }

    public bool IsClickNoneChatOption()
    {
        return ClickChatOption == "Не выбирать вариант";
    }

    /// <summary>
    /// Фоновый процесс
    /// </summary>
    [ObservableProperty] private bool _runBackgroundEnabled = false;

    /// <summary>
    /// Отправить элементы
    /// </summary>
    [ObservableProperty] private bool _submitGoodsEnabled = true;

    /// <summary>
    /// Закрыть всплывающий слой
    /// </summary>
    [ObservableProperty] private bool _closePopupPagedEnabled = true;
}
