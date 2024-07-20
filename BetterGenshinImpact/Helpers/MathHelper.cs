using System;
using OpenCvSharp;

namespace BetterGenshinImpact.Helpers;

public class MathHelper
{
    /// <summary>
    /// Кратчайшее расстояние от точки до прямой
    /// </summary>
    /// <param name="point"></param>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <returns></returns>
    public static double Distance(Point point, Point point1, Point point2)
    {
        // вектор направления прямой линии
        double a = point2.Y - point1.Y;
        double b = point1.X - point2.X;
        double c = point2.X * point1.Y - point1.X * point2.Y;

        // Рассчитайте по формуле расстоянияКратчайшее расстояние от точки до прямой
        double numerator = Math.Abs(a * point.X + b * point.Y + c);
        double denominator = Math.Sqrt(a * a + b * b);
        double distance = numerator / denominator;

        return distance;
    }

    /// <summary>
    /// расстояние между двумя точками
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    public static double Distance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }
}
