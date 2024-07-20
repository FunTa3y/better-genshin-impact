using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoFight.Script;

public class Method
{
    public static readonly Method Skill = new(new() { "skill", "e" });
    public static readonly Method Burst = new(new() { "burst", "q" });
    public static readonly Method Attack = new(new() { "attack", "Базовая атака", "Обычная атака" });
    public static readonly Method Charge = new(new() { "charge", "Дуть" });
    public static readonly Method Wait = new(new() { "wait", "after", "ждать" });

    public static readonly Method Walk = new(new() { "walk", "ходить" });
    public static readonly Method W = new(new() { "w" });
    public static readonly Method A = new(new() { "a" });
    public static readonly Method S = new(new() { "s" });
    public static readonly Method D = new(new() { "d" });

    public static readonly Method Aim = new(new() { "aim", "r", "цель" });
    public static readonly Method Dash = new(new() { "dash", "спринт" });
    public static readonly Method Jump = new(new() { "jump", "j", "Прыгать" });

    // Макрос
    public static readonly Method MouseDown = new(new() { "mousedown" });
    public static readonly Method MouseUp = new(new() { "mouseup" });
    public static readonly Method Click = new(new() { "click" });
    public static readonly Method MoveBy = new(new() { "moveby" });
    public static readonly Method KeyDown = new(new() { "keydown" });
    public static readonly Method KeyUp = new(new() { "keyup" });
    public static readonly Method KeyPress = new(new() { "keypress" });

    public static IEnumerable<Method> Values
    {
        get
        {
            yield return Skill;
            yield return Burst;
            yield return Attack;
            yield return Charge;
            yield return Wait;

            yield return Walk;
            yield return W;
            yield return A;
            yield return S;
            yield return D;

            // yield return Aim;
            yield return Dash;
            yield return Jump;

            // Макрос
            yield return MouseDown;
            yield return MouseUp;
            yield return Click;
            yield return MoveBy;
            yield return KeyDown;
            yield return KeyUp;
            yield return KeyPress;
        }
    }

    /// <summary>
    /// Псевдоним
    /// </summary>
    public List<string> Alias { get; private set; }

    public Method(List<string> alias)
    {
        Alias = alias;
    }

    public static Method GetEnumByCode(string method)
    {
        foreach (var m in Values)
        {
            if (m.Alias.Contains(method))
            {
                return m;
            }
        }

        Logger.LogError($"Неизвестный метод появляется в сценарии боевой стратегии.：{method}");
        throw new ArgumentException($"Неизвестный метод появляется в сценарии боевой стратегии.：{method}");
    }
}