using System;

namespace BetterGenshinImpact.Model;

public enum HotKeyTypeEnum
{
    GlobalRegister, // Глобальные горячие клавиши
    KeyboardMonitor, // Мониторинг клавиатуры
}

public static class HotKeyTypeEnumExtension
{
    public static string ToChineseName(this HotKeyTypeEnum type)
    {
        return type switch
        {
            HotKeyTypeEnum.GlobalRegister => "Глобальные горячие клавиши",
            HotKeyTypeEnum.KeyboardMonitor => "Мониторинг клавиатуры и мыши",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}