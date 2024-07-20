using System.Text.RegularExpressions;

namespace BetterGenshinImpact.Helpers;

public class StringUtils
{
    /// <summary>
    ///  Удалить все пустые строки
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveAllSpace(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return str.Replace(" ", "").Replace("\t", "");
    }

    /// <summary>
    ///  Удалить все новые строки
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveAllEnter(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return str.Replace("\n", "").Replace("\r", "");
    }

    /// <summary>
    /// Определите, является ли строка китайской
    /// </summary>
    public static bool IsChinese(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return false;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
    }

    /// <summary>
    /// Сохраняйте китайские иероглифы
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ExtractChinese(string str)
    {
        //Объявите строку, в которой будет сохранен результат
        string chineseString = "";

        //Добавить китайские иероглифы во входящих параметрах в результирующую строку
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] >= 0x4E00 && str[i] <= 0x9FA5) //китайский символ
            {
                chineseString += str[i];
            }
        }

        //Возврат результатов обработки, в которых сохраняются китайские иероглифы.
        return chineseString;
    }

    public static double TryParseDouble(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        try
        {
            return double.Parse(text);
        }
        catch
        {
            return 0;
        }
    }

    public static int TryParseInt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        try
        {
            return int.Parse(text);
        }
        catch
        {
            return 0;
        }
    }

    public static int TryExtractPositiveInt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return -1;
        }

        try
        {
            text = Regex.Replace(text, @"[^0-9]+", "");
            return int.Parse(text);
        }
        catch
        {
            return -1;
        }
    }
}
