using BetterGenshinImpact.GameTask.AutoFight.Model;
using BetterGenshinImpact.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using TimeSpan = System.TimeSpan;

namespace BetterGenshinImpact.GameTask.AutoFight.Script;

public class CombatCommand
{
    public string Name { get; set; }

    public Method Method { get; set; }

    public List<string>? Args { get; set; }

    public CombatCommand(string name, string command)
    {
        Name = name.Trim();
        command = command.Trim();
        var startIndex = command.IndexOf('(');
        if (startIndex > 0)
        {
            var endIndex = command.IndexOf(')');
            var method = command[..startIndex];
            method = method.Trim();
            Method = Method.GetEnumByCode(method);

            var parameters = command.Substring(startIndex + 1, endIndex - startIndex - 1);
            Args = new List<string>(parameters.Split(',', StringSplitOptions.TrimEntries));
            // Проверить параметры
            if (Method == Method.Walk)
            {
                AssertUtils.IsTrue(Args.Count == 2, "walkМетод должен иметь два входных параметра，Первый параметр — направление，Второй параметр — время ходьбы。пример：walk(s, 0.2)");
                var s = double.Parse(Args[1]);
                AssertUtils.IsTrue(s > 0, "Время ходьбы должно быть больше0");
            }
            else if (Method == Method.W || Method == Method.A || Method == Method.S || Method == Method.D)
            {
                AssertUtils.IsTrue(Args.Count == 1, "w/a/s/dМетод должен иметь входной параметр，Представляет время ходьбы。пример：d(0.5)");
            }
            else if (Method == Method.MoveBy)
            {
                AssertUtils.IsTrue(Args.Count == 2, "movebyМетод должен иметь два входных параметра，Они естьxиy。пример：moveby(100, 100))");
            }
            else if (Method == Method.KeyDown || Method == Method.KeyUp || Method == Method.KeyPress)
            {
                AssertUtils.IsTrue(Args.Count == 1, $"{Method.Alias[0]}Метод должен иметь входной параметр，Представляет кнопку");
                try
                {
                    User32Helper.ToVk(Args[0]);
                }
                catch
                {
                    throw new ArgumentException($"{Method.Alias[0]}Входные параметры метода должны бытьVirtualKeyCodesзначение в перечислении，Текущие входные параметры {Args[0]} незаконный");
                }
            }
        }
        else
        {
            Method = Method.GetEnumByCode(command);
        }
    }

    public void Execute(CombatScenes combatScenes)
    {
        var avatar = combatScenes.SelectAvatar(Name);
        if (avatar == null)
        {
            return;
        }

        // не-макроскрипт，Подождите, пока переключение ролей пройдет успешно.
        if (Method != Method.Wait
            && Method != Method.MouseDown
            && Method != Method.MouseUp
            && Method != Method.Click
            && Method != Method.MoveBy
            && Method != Method.KeyDown
            && Method != Method.KeyUp
            && Method != Method.KeyPress)
        {
            avatar.Switch();
        }

        Execute(avatar);
    }

    public void Execute(Avatar avatar)
    {
        if (Method == Method.Skill)
        {
            var hold = Args != null && Args.Contains("hold");
            avatar.UseSkill(hold);
        }
        else if (Method == Method.Burst)
        {
            avatar.UseBurst();
        }
        else if (Method == Method.Attack)
        {
            if (Args is { Count: > 0 })
            {
                var s = double.Parse(Args![0]);
                avatar.Attack((int)TimeSpan.FromSeconds(s).TotalMilliseconds);
            }
            else
            {
                avatar.Attack();
            }
        }
        else if (Method == Method.Charge)
        {
            if (Args is { Count: > 0 })
            {
                var s = double.Parse(Args![0]);
                avatar.Charge((int)TimeSpan.FromSeconds(s).TotalMilliseconds);
            }
            else
            {
                avatar.Charge();
            }
        }
        else if (Method == Method.Walk)
        {
            var s = double.Parse(Args![1]);
            avatar.Walk(Args![0], (int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.W)
        {
            var s = double.Parse(Args![0]);
            avatar.Walk("w", (int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.A)
        {
            var s = double.Parse(Args![0]);
            avatar.Walk("a", (int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.S)
        {
            var s = double.Parse(Args![0]);
            avatar.Walk("s", (int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.D)
        {
            var s = double.Parse(Args![0]);
            avatar.Walk("d", (int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.Wait)
        {
            var s = double.Parse(Args![0]);
            avatar.Wait((int)TimeSpan.FromSeconds(s).TotalMilliseconds);
        }
        else if (Method == Method.Aim)
        {
            throw new NotImplementedException();
        }
        else if (Method == Method.Dash)
        {
            if (Args is { Count: > 0 })
            {
                var s = double.Parse(Args![0]);
                avatar.Dash((int)TimeSpan.FromSeconds(s).TotalMilliseconds);
            }
            else
            {
                avatar.Dash();
            }
        }
        else if (Method == Method.Jump)
        {
            avatar.Jump();
        }
        // Макрос
        else if (Method == Method.MouseDown)
        {
            if (Args is { Count: > 0 })
            {
                avatar.MouseDown(Args![0]);
            }
            else
            {
                avatar.MouseDown();
            }
        }
        else if (Method == Method.MouseUp)
        {
            if (Args is { Count: > 0 })
            {
                avatar.MouseUp(Args![0]);
            }
            else
            {
                avatar.MouseUp();
            }
        }
        else if (Method == Method.Click)
        {
            if (Args is { Count: > 0 })
            {
                avatar.Click(Args![0]);
            }
            else
            {
                avatar.Click();
            }
        }
        else if (Method == Method.MoveBy)
        {
            if (Args is { Count: 2 })
            {
                var x = int.Parse(Args![0]);
                var y = int.Parse(Args[1]);
                avatar.MoveBy(x, y);
            }
            else
            {
                throw new ArgumentException("movebyМетод должен иметь два входных параметра，Они естьxиy。пример：moveby(100, 100)");
            }
        }
        else if (Method == Method.KeyDown)
        {
            avatar.KeyDown(Args![0]);
        }
        else if (Method == Method.KeyUp)
        {
            avatar.KeyUp(Args![0]);
        }
        else if (Method == Method.KeyPress)
        {
            avatar.KeyPress(Args![0]);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}
