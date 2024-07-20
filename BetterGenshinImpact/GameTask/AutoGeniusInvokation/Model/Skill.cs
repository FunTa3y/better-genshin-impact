namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;

public class Skill
{
    /// <summary>
    /// 1-4 То же, что и индекс массива，В игре счет начинается справа налево.！
    /// </summary>
    public short Index { get; set; }

    /// <summary>
    /// китайское имя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public ElementalType Type { get; set; }

    /// <summary>
    /// Поглотите указанное количество кубиков стихий.
    /// </summary>
    public int SpecificElementCost { get; set; }

    /// <summary>
    /// Количество израсходованных цветных кубиков
    /// </summary>
    public int AnyElementCost { get; set; } = 0;

    /// <summary>
    /// Поглотите указанное количество кубиков стихий. + Количество израсходованных цветных кубиков = Общее количество израсходованных кубиков
    /// </summary>
    public int AllCost { get; set; }
}