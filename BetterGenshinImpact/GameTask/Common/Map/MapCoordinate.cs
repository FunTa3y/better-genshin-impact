using BetterGenshinImpact.Core.Recognition.OpenCv;
using OpenCvSharp;
using System;

namespace BetterGenshinImpact.GameTask.Common.Map;

/// <summary>
/// Преобразование системы координат карты
/// 1. Система координат игры Genshin Impact Game
/// 2. BetterGIосновная карта1024блочная система координат Main1024
/// </summary>
public class MapCoordinate
{
    public static readonly int GameMapRows = 13; // Количество рядов тайлов карты в игровых координатах
    public static readonly int GameMapCols = 14; // Количество столбцов тайлов карты в игровых координатах
    public static readonly int GameMapUpRows = 5; // Под игровыми координатами Количество строк от верхнего левого угла до начала координат карты.
    public static readonly int GameMapLeftCols = 7; // Под игровыми координатами Количество столбцов от верхнего левого угла до начала координат карты.
    public static readonly int GameMapBlockWidth = 1024; // Длина и ширина тайлов игровой карты.

    /// <summary>
    /// Система координат игры Genshin Impact -> основная карта1024блочная система координат
    /// </summary>
    /// <param name="position">[a,b,c]</param>
    /// <returns></returns>
    public static Point GameToMain1024(decimal[] position)
    {
        // Округлить до четного
        var a = (int)Math.Round(position[0]); // начальство
        var c = (int)Math.Round(position[2]); // Левый

        // Конвертировать1024координаты блока，Положительная ось системы координат большой карты направленаЛевыйначальствонаправленный
        // Пишите сюда больше всегоЛевыйначальствоУгловойкоординаты блока(GameMapUpRows,GameMapLeftCols)/(начальство,Левый),Крайний срок4.5Версия，большинствоЛевыйначальствоУгловойкоординаты блокада(5,7)

        return new Point((GameMapLeftCols + 1) * GameMapBlockWidth - c, (GameMapUpRows + 1) * GameMapBlockWidth - a);
    }

    /// <summary>
    /// основная карта1024блочная система координат -> Система координат игры Genshin Impact
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Point Main1024ToGame(Point point)
    {
        return new Point((GameMapLeftCols + 1) * GameMapBlockWidth - point.X, (GameMapUpRows + 1) * GameMapBlockWidth - point.Y);
    }

    /// <summary>
    /// Система координат игры Genshin Impact -> основная карта2048блочная система координат
    /// </summary>
    /// <param name="position">[a,b,c]</param>
    /// <returns></returns>
    public static Point GameToMain2048(decimal[] position)
    {
        var a = position[0]; // начальство
        var c = position[2]; // Левый

        // Конвертировать1024координаты блока，Положительная ось системы координат большой карты направленаЛевыйначальствонаправленный
        // Пишите сюда больше всегоЛевыйначальствоУгловойкоординаты блока(GameMapUpRows,GameMapLeftCols)/(начальство,Левый),Крайний срок4.5Версия，большинствоЛевыйначальствоУгловойкоординаты блокада(5,7)

        return new Point((int)(((GameMapLeftCols + 1) * GameMapBlockWidth - c) * 2), (int)(((GameMapUpRows + 1) * GameMapBlockWidth - a) * 2));
    }

    /// <summary>
    /// Система координат игры Genshin Impact -> основная карта2048блочная система координат
    /// </summary>
    /// <param name="point">(c,a)</param>
    /// <returns></returns>
    public static Point GameToMain2048(Point point)
    {
        return new Point(((GameMapLeftCols + 1) * GameMapBlockWidth - point.X) * 2, ((GameMapUpRows + 1) * GameMapBlockWidth - point.Y) * 2);
    }

    /// <summary>
    /// Система координат игры Genshin Impact -> основная карта2048блочная система координат
    /// </summary>
    /// <returns></returns>
    public static (double x, double y) GameToMain2048(double c, double a)
    {
        // Конвертировать1024координаты блока，Положительная ось системы координат большой карты направленаЛевыйначальствонаправленный
        // Пишите сюда больше всегоЛевыйначальствоУгловойкоординаты блока(GameMapUpRows,GameMapLeftCols)/(начальство,Левый),Крайний срок4.5Версия，большинствоЛевыйначальствоУгловойкоординаты блокада(5,7)

        return new(((GameMapLeftCols + 1) * GameMapBlockWidth - c) * 2, ((GameMapUpRows + 1) * GameMapBlockWidth - a) * 2);
    }

    public static Rect GameToMain2048(Rect rect)
    {
        var center = rect.GetCenterPoint();
        // КонвертироватьКоординаты центральной точки
        (double newX, double newY) = GameToMain2048(center.X, center.Y);

        // возвращатьсяКонвертироватьКоординаты прямоугольника после
        return new Rect((int)Math.Round(newX) - rect.Width, (int)Math.Round(newY) - rect.Height, rect.Width * 2, rect.Height * 2);
    }

    /// <summary>
    /// основная карта2048блочная система координат -> Система координат игры Genshin Impact
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static Point Main2048ToGame(Point point)
    {
        return new Point((GameMapLeftCols + 1) * GameMapBlockWidth - point.X / 2, (GameMapUpRows + 1) * GameMapBlockWidth - point.Y / 2);
    }

    /// <summary>
    /// основная карта2048блочная система координат -> Система координат игры Genshin Impact
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Rect Main2048ToGame(Rect rect)
    {
        var center = rect.GetCenterPoint();
        var point = Main2048ToGame(center);
        return new Rect(point.X - rect.Width / 4, point.Y - rect.Height / 4, rect.Width / 2, rect.Height / 2);
    }
}
