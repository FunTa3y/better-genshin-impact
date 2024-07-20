using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BetterGenshinImpact.View.Drawable;

public class DrawContent
{
    /// <summary>
    /// Прямоугольник, нарисованный в окне маски
    /// </summary>
    public ConcurrentDictionary<string, List<RectDrawable>> RectList { get; set; } = new();

    /// <summary>
    /// Текст, нарисованный в окне маски
    /// </summary>
    public ConcurrentDictionary<string, List<TextDrawable>> TextList { get; set; } = new();

    /// <summary>
    /// Текст, нарисованный в окне маски
    /// </summary>
    public ConcurrentDictionary<string, List<LineDrawable>> LineList { get; set; } = new();

    public void PutRect(string key, RectDrawable newRect)
    {
        if (RectList.TryGetValue(key, out var prevRect))
        {
            if (prevRect.Count == 0 && newRect.Equals(prevRect[0]))
            {
                return;
            }
        }

        RectList[key] = new List<RectDrawable> { newRect };
        MaskWindow.Instance().Refresh();
    }

    public void PutOrRemoveRectList(string key, List<RectDrawable>? list)
    {
        bool changed = false;

        if (RectList.TryGetValue(key, out var prevRect))
        {
            if (list == null)
            {
                RectList.TryRemove(key, out _);
                changed = true;
            }
            else if (prevRect.Count != list.Count)
            {
                RectList[key] = list;
                changed = true;
            }
            else
            {
                // Больше не нужно сравнивать одно за другим，Где использовать этот метод，Они обновляются каждый кадр
                RectList[key] = list;
                changed = true;
            }
        }
        else
        {
            if (list is { Count: > 0 })
            {
                RectList[key] = list;
                changed = true;
            }
        }

        if (changed)
        {
            MaskWindow.Instance().Refresh();
        }
    }

    public void RemoveRect(string key)
    {
        if (RectList.TryGetValue(key, out _))
        {
            RectList.TryRemove(key, out _);
            MaskWindow.Instance().Refresh();
        }
    }

    public void PutLine(string key, LineDrawable newLine)
    {
        if (LineList.TryGetValue(key, out var prev))
        {
            if (prev.Count == 0 && newLine.Equals(prev[0]))
            {
                return;
            }
        }

        LineList[key] = new List<LineDrawable> { newLine };
        MaskWindow.Instance().Refresh();
    }


    public void RemoveLine(string key)
    {
        if (LineList.TryGetValue(key, out _))
        {
            LineList.TryRemove(key, out _);
            MaskWindow.Instance().Refresh();
        }
    }



    /// <summary>
    /// Очистите все содержимое чертежа
    /// </summary>
    public void ClearAll()
    {
        if (RectList.IsEmpty && TextList.IsEmpty && LineList.IsEmpty)
        {
            return;
        }
        RectList.Clear();
        TextList.Clear();
        LineList.Clear();
        MaskWindow.Instance().Refresh();
    }
}