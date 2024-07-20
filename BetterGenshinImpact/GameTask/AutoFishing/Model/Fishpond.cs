using System;
using System.Collections.Generic;
using System.Linq;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using Compunet.YoloV8.Data;
using OpenCvSharp;
using static OpenCvSharp.Cv2;

namespace BetterGenshinImpact.GameTask.AutoFishing.Model;

public class Fishpond
{
    /// <summary>
    /// Расположение пруда с рыбой
    /// </summary>
    public Rect FishpondRect { get; set; }

    /// <summary>
    /// Положение точки приземления метательного стержня
    /// </summary>
    public Rect TargetRect { get; set; }

    /// <summary>
    /// рыба в пруду с рыбой
    /// </summary>
    public List<OneFish> Fishes { get; set; } = new();

    public Fishpond(DetectionResult result)
    {
        foreach (var box in result.Boxes)
        {
            if (box.Class.Name == "rod")
            {
                TargetRect = new Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
                continue;
            }
            else if (box.Class.Name == "err rod")
            {
                TargetRect = new Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height);
                continue;
            }

            var fish = new OneFish(box.Class.Name, new Rect(box.Bounds.X, box.Bounds.Y, box.Bounds.Width, box.Bounds.Height), box.Confidence);
            Fishes.Add(fish);
        }

        // Рыба с наибольшим авторитетом размещается первой.
        Fishes = Fishes.OrderByDescending(fish => fish.Confidence).ToList();

        FishpondRect = CalculateFishpondRect();
    }

    /// <summary>
    /// Рассчитать расположение пруда с рыбой
    /// </summary>
    /// <returns></returns>
    public Rect CalculateFishpondRect()
    {
        if (Fishes.Count == 0)
        {
            return Rect.Empty;
        }

        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;
        foreach (var fish in Fishes)
        {
            if (fish.Rect.Left < left)
            {
                left = fish.Rect.Left;
            }

            if (fish.Rect.Top < top)
            {
                top = fish.Rect.Top;
            }

            if (fish.Rect.Right > right)
            {
                right = fish.Rect.Right;
            }

            if (fish.Rect.Bottom > bottom)
            {
                bottom = fish.Rect.Bottom;
            }
        }

        return new Rect(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Фильтровать рыбу по названию наживки
    /// </summary>
    /// <param name="baitName"></param>
    /// <returns></returns>
    public List<OneFish> FilterByBaitName(string baitName)
    {
        return Fishes.Where(fish => fish.FishType.BaitName == baitName).OrderByDescending(fish => fish.Confidence).ToList();
    }

    public OneFish? FilterByBaitNameAndRecently(string baitName, Rect prevTargetFishRect)
    {
        var fishes = FilterByBaitName(baitName);
        if (fishes.Count == 0)
        {
            return null;
        }

        var min = double.MaxValue;
        var c1 = prevTargetFishRect.GetCenterPoint();
        OneFish? result = null;
        foreach (var fish in fishes)
        {
            var c2 = fish.Rect.GetCenterPoint();
            var distance = Math.Sqrt(Math.Pow(c1.X - c2.X, 2) + Math.Pow(c1.Y - c2.Y, 2));
            if (distance < min)
            {
                min = distance;
                result = fish;
            }
        }

        return result;
    }

    /// <summary>
    /// Название наживки, которую ест большинство рыб
    /// </summary>
    /// <returns></returns>
    public string MostMatchBait()
    {
        Dictionary<string, int> dict = new();
        foreach (var fish in Fishes)
        {
            if (dict.ContainsKey(fish.FishType.BaitName))
            {
                dict[fish.FishType.BaitName]++;
            }
            else
            {
                dict[fish.FishType.BaitName] = 1;
            }
        }

        var max = 0;
        var result = "";
        foreach (var (key, value) in dict)
        {
            if (value > max)
            {
                max = value;
                result = key;
            }
        }

        return result;
    }
}
