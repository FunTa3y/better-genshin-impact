using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace BetterGenshinImpact.GameTask.AutoPick
{
    /// <summary>
    /// Нет16:9Может работать некорректно при разных разрешениях
    /// </summary>
    [Serializable]
    public partial class AutoPickConfig : ObservableObject
    {
        /// <summary>
        /// Триггер включен?
        /// </summary>
        [ObservableProperty] private bool _enabled = true;

        /// <summary>
        /// 1080pНачальное смещение левой части выделенного текста.
        /// </summary>
        [ObservableProperty] private int _itemIconLeftOffset = 60;

        /// <summary>
        /// 1080pНачальное смещение выделенного текста
        /// </summary>
        [ObservableProperty] private int _itemTextLeftOffset = 115;

        /// <summary>
        /// 1080pКонечное смещение выделенного нижнего текста.
        /// </summary>
        [ObservableProperty] private int _itemTextRightOffset = 400;

        /// <summary>
        /// механизм распознавания текста
        /// - Paddle
        /// - Yap
        /// </summary>
        [ObservableProperty]
        private string _ocrEngine = PickOcrEngineEnum.Paddle.ToString();

        /// <summary>
        /// Быстрый режим
        /// Игнорировать результаты распознавания текста，Забрать напрямую
        /// </summary>

        [ObservableProperty] private bool _fastModeEnabled = false;

        /// <summary>
        /// Индивидуальное получение ключей
        /// </summary>
        [ObservableProperty] private string _pickKey = "F";
    }
}
