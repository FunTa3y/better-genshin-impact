using BetterGenshinImpact.GameTask.Model.Area;
using System;
using System.Drawing;

namespace BetterGenshinImpact.GameTask;

/// <summary>
/// Захваченный контент
/// и несколько кратныхtriggerКопировать построчно
/// </summary>
public class CaptureContent : IDisposable
{
    public static readonly int MaxFrameIndexSecond = 60;
    public Bitmap SrcBitmap { get; }
    public int FrameIndex { get; }
    public double TimerInterval { get; }

    public int FrameRate => (int)(1000 / TimerInterval);

    public ImageRegion CaptureRectArea { get; private set; }

    public CaptureContent(Bitmap srcBitmap, int frameIndex, double interval)
    {
        SrcBitmap = srcBitmap;
        FrameIndex = frameIndex;
        TimerInterval = interval;
        var systemInfo = TaskContext.Instance().SystemInfo;

        var gameCaptureRegion = systemInfo.DesktopRectArea.Derive(srcBitmap, systemInfo.CaptureAreaRect.X, systemInfo.CaptureAreaRect.Y);
        CaptureRectArea = gameCaptureRegion.DeriveTo1080P();
    }

    public void Dispose()
    {
        CaptureRectArea.Dispose();
        SrcBitmap.Dispose();
    }
}
