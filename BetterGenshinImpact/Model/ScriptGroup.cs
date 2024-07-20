using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterGenshinImpact.Model;

/// <summary>
/// группа выполнения скриптов
/// </summary>
public partial class ScriptGroup : ObservableObject
{
    /// <summary>
    /// Имя группы
    /// </summary>
    [ObservableProperty]
    private string _groupName = string.Empty;
}
