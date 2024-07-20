﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;
using static Vanara.PInvoke.Gdi32;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation;

public class AutoGeniusInvokationTask
{
    public static void Start(GeniusInvokationTaskParam taskParam)
    {
        TaskTriggerDispatcher.Instance().StopTimer();
        // Прочтите информацию о политике
        var duel = ScriptParser.Parse(taskParam.StrategyContent);
        SystemControl.ActivateWindow();
        duel.RunAsync(taskParam);
    }
}