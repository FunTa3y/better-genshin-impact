using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Helpers;
using System.Threading;
using System.Windows;

namespace BetterGenshinImpact.GameTask.Macro;

/// <summary>
/// Укрепляйте святые мощи одним щелчком мыши
/// </summary>
public class QuickEnhanceArtifactMacro
{
    public static void Done()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("Пожалуйста, начните сначала");
            return;
        }

        // SystemControl.ActivateWindow(TaskContext.Instance().GameHandle);

        var config = TaskContext.Instance().Config.MacroConfig;

        // Быстро вставить 1760x770
        GameCaptureRegion.GameRegion1080PPosClick(1760, 770);
        Thread.Sleep(100);
        // укреплять 1760x1020
        GameCaptureRegion.GameRegion1080PPosClick(1760, 1020);
        Thread.Sleep(100 + config.EnhanceWaitDelay);
        // Подробное меню 150x150
        GameCaptureRegion.GameRegion1080PPosClick(150, 150);
        Thread.Sleep(100);
        // укреплятьменю 150x220
        GameCaptureRegion.GameRegion1080PPosClick(150, 220);
        Thread.Sleep(100);
        // двигаться назадБыстро вставить #30
        GameCaptureRegion.GameRegion1080PPosMove(1760, 770);
    }
}
