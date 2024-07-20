namespace BetterGenshinImpact.Helpers.Extensions;

public static class BooleanExtension
{
    public static string ToChinese(this bool enabled)
    {
        return enabled ? "включать" : "закрытие";
    }
}