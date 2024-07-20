using BetterGenshinImpact.GameTask.Model.Area.Converter;
using BetterGenshinImpact.View.Drawable;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Vanara.PInvoke;

namespace BetterGenshinImpact.GameTask.Model.Area;

/// <summary>
/// Базовый класс площади
/// используется для описания территории，может быть прямоугольник，Это также может быть точка
/// </summary>
public class Region : IDisposable
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int Top
    {
        get => Y;
        set => Y = value;
    }

    /// <summary>
    /// Gets the y-coordinate that is the sum of the Y and Height property values of this Rect structure.
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    /// Gets the x-coordinate of the left edge of this Rect structure.
    /// </summary>
    public int Left
    {
        get => X;
        set => X = value;
    }

    /// <summary>
    /// Gets the x-coordinate that is the sum of X and Width property values of this Rect structure.
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// магазинOCRРаспознанный текст результата
    /// </summary>
    public string Text { get; set; } = string.Empty;

    public Region()
    {
    }

    public Region(int x, int y, int width, int height, Region? owner = null, INodeConverter? converter = null)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Prev = owner;
        PrevConverter = converter;
    }

    public Region(Rect rect, Region? owner = null, INodeConverter? converter = null) : this(rect.X, rect.Y, rect.Width, rect.Height, owner, converter)
    {
    }

    public Region? Prev { get; }

    /// <summary>
    /// Конвертер координат узлов этой области в предыдущую область
    /// </summary>
    public INodeConverter? PrevConverter { get; }

    // public List<Region>? NextChildren { get; protected set; }

    /// <summary>
    /// Фоновый щелчок【Собственный】центр
    /// </summary>
    public void BackgroundClick()
    {
        User32.GetCursorPos(out var p);
        this.Move();  // Фактическую мышь необходимо переместить
        TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
        Thread.Sleep(10);
        DesktopRegion.DesktopRegionMove(p.X, p.Y); // Верните мышь в исходное положение
    }

    /// <summary>
    /// Нажмите【Собственный】центр
    /// region.Derive(x,y).Click() Эквивалентно region.ClickTo(x,y)
    /// </summary>
    public void Click()
    {
        // относительноСобственныйда 0, 0 координировать
        ClickTo(0, 0, Width, Height);
    }

    /// <summary>
    /// Нажмитев пределах области【Укажите местоположение】
    /// region.Derive(x,y).Click() Эквивалентно region.ClickTo(x,y)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void ClickTo(int x, int y)
    {
        ClickTo(x, y, 0, 0);
    }

    /// <summary>
    /// Нажмитев пределах области【Укажите прямоугольную область】центр
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <exception cref="Exception"></exception>
    public void ClickTo(int x, int y, int w, int h)
    {
        var res = ConvertRes<DesktopRegion>.ConvertPositionToTargetRegion(x, y, w, h, this);
        res.TargetRegion.DesktopRegionClick(res.X, res.Y, res.Width, res.Height);
    }

    /// <summary>
    /// переехать в【Собственный】центр
    /// region.Derive(x,y).Move() Эквивалентно region.MoveTo(x,y)
    /// </summary>
    public void Move()
    {
        // относительноСобственныйда 0, 0 координировать
        MoveTo(0, 0, Width, Height);
    }

    /// <summary>
    /// переехать вв пределах области【Укажите местоположение】
    /// region.Derive(x,y).Move() Эквивалентно region.MoveTo(x,y)
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void MoveTo(int x, int y)
    {
        MoveTo(x, y, 0, 0);
    }

    /// <summary>
    /// переехать вв пределах области【Укажите прямоугольную область】центр
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <exception cref="Exception"></exception>
    public void MoveTo(int x, int y, int w, int h)
    {
        var res = ConvertRes<DesktopRegion>.ConvertPositionToTargetRegion(x, y, w, h, this);
        res.TargetRegion.DesktopRegionMove(res.X, res.Y, res.Width, res.Height);
    }

    /// <summary>
    /// Рисуйте прямо в окне маски【Собственный】
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    public void DrawSelf(string name, Pen? pen = null)
    {
        // относительноСобственныйда 0, 0 координировать
        DrawRect(0, 0, Width, Height, name, pen);
    }

    /// <summary>
    /// Рисуйте прямо в окне маскипод текущей областью【конкретная область】
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    public void DrawRect(int x, int y, int w, int h, string name, Pen? pen = null)
    {
        var drawable = ToRectDrawable(x, y, w, h, name, pen);
        VisionContext.Instance().DrawContent.PutRect(name, drawable);
    }

    public void DrawRect(Rect rect, string name, Pen? pen = null)
    {
        var drawable = ToRectDrawable(rect.X, rect.Y, rect.Width, rect.Height, name, pen);
        VisionContext.Instance().DrawContent.PutRect(name, drawable);
    }

    /// <summary>
    /// Конвертировать【Собственный】Нарисуйте прямоугольник в окне маски.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <returns></returns>
    public RectDrawable SelfToRectDrawable(string name, Pen? pen = null)
    {
        // относительноСобственныйда 0, 0 координировать
        return ToRectDrawable(0, 0, Width, Height, name, pen);
    }

    /// <summary>
    /// Конвертировать【конкретная область】Нарисуйте прямоугольник в окне маски.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <returns></returns>
    public RectDrawable ToRectDrawable(Rect rect, string name, Pen? pen = null)
    {
        return ToRectDrawable(rect.X, rect.Y, rect.Width, rect.Height, name, pen);
    }

    /// <summary>
    /// Конвертировать【конкретная область】Нарисуйте прямоугольник в окне маски.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public RectDrawable ToRectDrawable(int x, int y, int w, int h, string name, Pen? pen = null)
    {
        var res = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x, y, w, h, this);
        return res.TargetRegion.ConvertToRectDrawable(res.X, res.Y, res.Width, res.Height, pen, name);
    }

    /// <summary>
    /// Конвертировать【Укажите прямую линию】Нарисуйте прямую линию к окну маски.
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <param name="name"></param>
    /// <param name="pen"></param>
    /// <returns></returns>
    public LineDrawable ToLineDrawable(int x1, int y1, int x2, int y2, string name, Pen? pen = null)
    {
        var res1 = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x1, y1, 0, 0, this);
        var res2 = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x2, y2, 0, 0, this);
        return res1.TargetRegion.ConvertToLineDrawable(res1.X, res1.Y, res2.X, res2.Y, pen, name);
    }

    public void DrawLine(int x1, int y1, int x2, int y2, string name, Pen? pen = null)
    {
        var drawable = ToLineDrawable(x1, y1, x2, y2, name, pen);
        VisionContext.Instance().DrawContent.PutLine(name, drawable);
    }

    public Rect ConvertSelfPositionToGameCaptureRegion()
    {
        return ConvertPositionToGameCaptureRegion(X, Y, Width, Height);
    }

    public Rect ConvertPositionToGameCaptureRegion(int x, int y, int w, int h)
    {
        var res = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x, y, w, h, this);
        return res.ToRect();
    }

    public (int, int) ConvertPositionToGameCaptureRegion(int x, int y)
    {
        var res = ConvertRes<GameCaptureRegion>.ConvertPositionToTargetRegion(x, y, 0, 0, this);
        return (res.X, res.Y);
    }

    public (int, int) ConvertPositionToDesktopRegion(int x, int y)
    {
        var res = ConvertRes<DesktopRegion>.ConvertPositionToTargetRegion(x, y, 0, 0, this);
        return (res.X, res.Y);
    }

    public Rect ToRect()
    {
        return new Rect(X, Y, Width, Height);
    }

    /// <summary>
    /// Создать новый регион
    /// пожалуйста, используйте using var newRegion
    /// </summary>
    /// <returns></returns>
    public ImageRegion ToImageRegion()
    {
        if (this is ImageRegion imageRegion)
        {
            Debug.WriteLine("ToImageRegion Но уже ImageRegion");
            return imageRegion;
        }

        var res = ConvertRes<ImageRegion>.ConvertPositionToTargetRegion(0, 0, Width, Height, this);
        var newRegion = new ImageRegion(new Mat(res.TargetRegion.SrcMat, res.ToRect()), X, Y, Prev, PrevConverter);
        return newRegion;
    }

    public bool IsEmpty()
    {
        return Width == 0 && Height == 0 && X == 0 && Y == 0;
    }

    /// <summary>
    /// Семантическая упаковка
    /// </summary>
    /// <returns></returns>
    public bool IsExist()
    {
        return !IsEmpty();
    }

    public void Dispose()
    {
        // Освободить все дочерние узлы
        // NextChildren?.ForEach(x => x.Dispose());
    }

    /// <summary>
    /// Вывести регион точечного типа
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Region Derive(int x, int y)
    {
        return Derive(x, y, 0, 0);
    }

    /// <summary>
    /// Вывести площадь типа прямоугольник
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public Region Derive(int x, int y, int w, int h)
    {
        return new Region(x, y, w, h, this, new TranslationConverter(x, y));
    }

    public Region Derive(Rect rect)
    {
        return Derive(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
