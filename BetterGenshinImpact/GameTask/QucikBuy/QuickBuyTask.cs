using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;

namespace BetterGenshinImpact.GameTask.QucikBuy;

public class QuickBuyTask
{
    public static void Done()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("Пожалуйста, начните сначала");
            return;
        }
        if (!SystemControl.IsGenshinImpactActiveByProcess())
        {
            return;
        }

        try
        {
            // Нажмите, чтобы купить/обмен Нижний правый225x60
            GameCaptureRegion.GameRegionClick((size, scale) => (size.Width - 225 * scale, size.Height - 60 * scale));
            TaskControl.CheckAndSleep(100); // Подождите, пока появится окно

            // Выберите левую точку 742x601
            GameCaptureRegion.GameRegion1080PPosMove(742, 601);
            TaskControl.CheckAndSleep(100);
            Simulation.SendInput.Mouse.LeftButtonDown();
            TaskControl.CheckAndSleep(50);

            // проведите пальцем вправо
            Simulation.SendInput.Mouse.MoveMouseBy(1000, 0);
            TaskControl.CheckAndSleep(200);
            Simulation.SendInput.Mouse.LeftButtonUp();
            TaskControl.CheckAndSleep(100);

            // Нажмите «Купить» на всплывающей странице./обмен 1100x780
            GameCaptureRegion.GameRegion1080PPosClick(1100, 780);
            TaskControl.CheckAndSleep(200); // Подождите, пока окно исчезнет
            GameCaptureRegion.GameRegionClick((size, scale) => (size.Width - 225 * scale, size.Height - 60 * scale));
            TaskControl.CheckAndSleep(200);
        }
        catch (Exception e)
        {
            TaskControl.Logger.LogWarning(e.Message);
        }
    }
}
