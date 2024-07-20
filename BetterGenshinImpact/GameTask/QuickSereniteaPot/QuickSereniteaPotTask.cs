using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.QuickSereniteaPot.Assets;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using System;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.GameTask.QuickSereniteaPot;

public class QuickSereniteaPotTask
{
    private static void WaitForBagToOpen()
    {
        NewRetry.Do(() =>
        {
            TaskControl.Sleep(1);
            using var ra2 = TaskControl.CaptureToRectArea().Find(QuickSereniteaPotAssets.Instance.BagCloseButtonRo);
            if (ra2.IsEmpty())
            {
                throw new RetryException("Рюкзак не открывается");
            }
        }, TimeSpan.FromMilliseconds(500), 3);
    }

    private static void FindPotIcon()
    {
        NewRetry.Do(() =>
        {
            TaskControl.Sleep(1);
            using var ra2 = TaskControl.CaptureToRectArea().Find(QuickSereniteaPotAssets.Instance.SereniteaPotIconRo);
            if (ra2.IsEmpty())
            {
                throw new RetryException("Горшок не обнаружен");
            }
            else
            {
                ra2.Click();
            }
        }, TimeSpan.FromMilliseconds(200), 3);
    }

    public static void Done()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            System.Windows.MessageBox.Show("Пожалуйста, начните сначала");
            return;
        }

        if (!SystemControl.IsGenshinImpactActiveByProcess())
        {
            return;
        }

        QuickSereniteaPotAssets.DestroyInstance();

        try
        {
            // открытый рюкзак
            Simulation.SendInput.Keyboard.KeyPress(VK.VK_B);
            TaskControl.CheckAndSleep(500);
            WaitForBagToOpen();

            // Нажмите на страницу реквизита
            GameCaptureRegion.GameRegion1080PPosClick(1050, 50);
            TaskControl.CheckAndSleep(200);

            // Попробуйте поставить горшок
            FindPotIcon();
            TaskControl.CheckAndSleep(200);

            // Нажмите, чтобы разместить Нижний правый225,60
            GameCaptureRegion.GameRegionClick((size, assetScale) => (size.Width - 225 * assetScale, size.Height - 60 * assetScale));
            // Вы также можете использовать следующий методНажмите, чтобы разместитьв соответствии скнопка
            // Bv.ClickWhiteConfirmButton(TaskControl.CaptureToRectArea());
            TaskControl.CheckAndSleep(800);

            // в соответствии сFВходить
            Simulation.SendInput.Keyboard.KeyPress(VK.VK_F);
        }
        catch (Exception e)
        {
            TaskControl.Logger.LogWarning(e.Message);
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
        }
    }
}
