using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoFight.Assets;
using BetterGenshinImpact.Model;

namespace BetterGenshinImpact.GameTask.AutoFight;

/// <summary>
/// Контекст автоматического боя
/// Пожалуйста начнитеBetterGIИнициализировать позже
/// </summary>
public class AutoFightContext : Singleton<AutoFightContext>
{
    private AutoFightContext()
    {
        Simulator = Simulation.PostMessage(TaskContext.Instance().GameHandle);
    }

    /// <summary>
    /// findресурс
    /// </summary>
    public AutoFightAssets FightAssets => AutoFightAssets.Instance;

    /// <summary>
    /// Специально для бояPostMessageИмитировать операции с клавиатурой и мышью
    /// </summary>
    public readonly PostMessageSimulator Simulator;
}
