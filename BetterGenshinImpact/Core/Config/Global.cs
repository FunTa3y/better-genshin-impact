using System;
using System.IO;

namespace BetterGenshinImpact.Core.Config;

public class Global
{
    public static string Version { get; } = "0.31.3";

    public static string StartUpPath { get; set; } = AppContext.BaseDirectory;

    public static string Absolute(string relativePath)
    {
        return Path.Combine(StartUpPath, relativePath);
    }

    public static string? ReadAllTextIfExist(string relativePath)
    {
        var path = Absolute(relativePath);
        if (File.Exists(path)) return File.ReadAllText(path);
        return null;
    }

    /// <summary>
    ///     Определить объекты，Определите, является ли это новой версией
    /// </summary>
    /// <param name="currentVersion">Недавно полученная версия</param>
    /// <returns></returns>
    public static bool IsNewVersion(string currentVersion)
    {
        return IsNewVersion(Version, currentVersion);
    }

    /// <summary>
    ///     Определить объекты，Определите, является ли это новой версией
    /// </summary>
    /// <param name="oldVersion">старая версия</param>
    /// <param name="currentVersion">Недавно полученная версия</param>
    /// <returns>Нужно ли его обновлять?</returns>
    public static bool IsNewVersion(string oldVersion, string currentVersion)
    {
        try
        {
            Version oldVersionX = new(oldVersion);
            Version currentVersionX = new(currentVersion);

            if (currentVersionX > oldVersionX)
                // необходимо обновить
                return true;
        }
        catch
        {
            ///
        }

        // космоснеобходимо обновить
        return false;
    }

    public static void WriteAllText(string relativePath, string blackListJson)
    {
        var path = Absolute(relativePath);
        File.WriteAllText(path, blackListJson);
    }
}
