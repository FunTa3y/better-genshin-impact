using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Linq;

namespace BetterGenshinImpact.GameTask.AutoCook;

public class AutoCookTrigger : ITaskTrigger
{
    private readonly ILogger<AutoCookTrigger> _logger = App.GetLogger<AutoCookTrigger>();

    public string Name => "автоматическое приготовление";
    public bool IsEnabled { get; set; }
    public int Priority => 50;
    public bool IsExclusive { get; set; }

    public void Init()
    {
        IsEnabled = TaskContext.Instance().Config.AutoCookConfig.Enabled;
        IsExclusive = false;
    }

    public void OnCapture(CaptureContent content)
    {
        // Определите, находитесь ли вы в интерфейсе приготовления пищи
        IsExclusive = false;
        content.CaptureRectArea.Find(ElementAssets.Instance.UiLeftTopCookIcon, _ =>
        {
            IsExclusive = true;
            var captureRect = TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect;
            using var region = content.CaptureRectArea.DeriveCrop(0, captureRect.Height / 2, captureRect.Width, captureRect.Height / 2);
            var perfectBarRects = ContoursHelper.FindSpecifyColorRects(region.SrcMat, new Scalar(255, 192, 64), 0, 8);
            if (perfectBarRects.Count >= 2)
            {
                // Нажмите на кнопку приготовления
                var btnList = ContoursHelper.FindSpecifyColorRects(region.SrcMat, new Scalar(255, 255, 192), 12, 12);
                if (btnList.Count >= 1)
                {
                    if (btnList.Count > 1)
                    {
                        _logger.LogWarning("автоматическое приготовление：{Text}", "Распознаются несколько кнопок завершения приготовления");
                        btnList = [.. btnList.OrderByDescending(r => r.Width)];
                    }
                    var btn = btnList[0];
                    var x = btn.X + btn.Width / 2;
                    var y = btn.Y + btn.Height / 2;
                    region.ClickTo(x, y);
                    _logger.LogInformation("автоматическое приготовление：{Text}", "Нажмите кнопку завершения");
                }
            }
        });
    }
}
