﻿using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.GameLoading.Assets;
using System;
using System.Diagnostics;

namespace BetterGenshinImpact.GameTask.GameLoading;

public class GameLoadingTrigger : ITaskTrigger
{
    public string Name => "Автоматически открыть дверь";

    public bool IsEnabled { get; set; }

    public int Priority => 999;

    public bool IsExclusive => false;

    public bool IsBackgroundRunning => true;

    private readonly GameLoadingAssets _assets;

    private readonly GenshinStartConfig _config = TaskContext.Instance().Config.GenshinStartConfig;

    private int _enterGameClickCount = 0;
    private int _welkinMoonClickCount = 0;
    private int _noneClickCount, _wmNoneClickCount;

    private DateTime _prevExecuteTime = DateTime.MinValue;

    public GameLoadingTrigger()
    {
        GameLoadingAssets.DestroyInstance();
        _assets = GameLoadingAssets.Instance;
    }

    public void Init()
    {
        IsEnabled = _config.AutoEnterGameEnabled;
        // Нет никакой привязки к запуску Genshin Impact раньше.，Нет необходимости запускать эту задачу
        if ((DateTime.Now - TaskContext.Instance().LinkedStartGenshinTime).TotalMinutes >= 5)
        {
            IsEnabled = false;
        }

        _enterGameClickCount = 0;
    }

    public void OnCapture(CaptureContent content)
    {
        // 5s Целевая рыба не найдена
        if ((DateTime.Now - _prevExecuteTime).TotalMilliseconds <= 5000)
        {
            return;
        }
        _prevExecuteTime = DateTime.Now;
        // 5min автоматически останавливаться после
        if ((DateTime.Now - TaskContext.Instance().LinkedStartGenshinTime).TotalMinutes >= 5)
        {
            IsEnabled = false;
            return;
        }

        using var ra = content.CaptureRectArea.Find(_assets.EnterGameRo);
        if (!ra.IsEmpty())
        {
            // Просто найдите относительное положение щелчка
            TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
            _enterGameClickCount++;
        }
        else
        {
            if (_enterGameClickCount > 0 && !_config.AutoClickBlessingOfTheWelkinMoonEnabled)
            {
                _noneClickCount++;
                if (_noneClickCount > 5)
                {
                    IsEnabled = false;
                }
            }
        }

        if (_enterGameClickCount > 0 && _config.AutoClickBlessingOfTheWelkinMoonEnabled)
        {
            var wmRa = content.CaptureRectArea.Find(_assets.WelkinMoonRo);
            if (!wmRa.IsEmpty())
            {
                // wmRa.BackgroundClick();
                TaskContext.Instance().PostMessageSimulator.LeftButtonClickBackground();
                _welkinMoonClickCount++;
                Debug.WriteLine("[GameLoading] Click blessing of the welkin moon");
                if (_welkinMoonClickCount > 2)
                {
                    IsEnabled = false;
                }
            }
            else
            {
                if (_welkinMoonClickCount > 0)
                {
                    _wmNoneClickCount++;
                    if (_wmNoneClickCount > 1)
                    {
                        IsEnabled = false;
                    }
                }
            }
        }
    }
}
