using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.AutoSkip.Assets;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using System;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.GameTask.AutoSkip;

public class OneKeyExpeditionTask
{
    public void Run(AutoSkipAssets assets)
    {
        try
        {
            SystemControl.ActivateWindow();
            // 1.Собери их все
            var region = CaptureToRectArea();
            region.Find(assets.CollectRo, ra =>
            {
                ra.Click();
                Logger.LogInformation("Исследуйте диспетчеризацию：{Text}", "Собери их все");
                Sleep(1100);
                // 2.переотправка
                NewRetry.Do(() =>
                {
                    Sleep(1);
                    region = CaptureToRectArea();
                    var ra2 = region.Find(assets.ReRo);
                    if (ra2.IsEmpty())
                    {
                        throw new RetryException("Всплывающее меню не обнаружено");
                    }
                    else
                    {
                        ra2.Click();
                        Logger.LogInformation("Исследуйте диспетчеризацию：{Text}", "отправить снова");
                    }
                }, TimeSpan.FromSeconds(1), 3);

                // 3.Выйти со страницы отправки ESC
                Sleep(500);
                Simulation.SendInput.Keyboard.KeyPress(VK.VK_ESCAPE);
                Logger.LogInformation("Исследуйте диспетчеризацию：{Text}", "Заканчивать");
            });
        }
        catch (Exception e)
        {
            Logger.LogInformation(e.Message);
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
        }
    }
}
