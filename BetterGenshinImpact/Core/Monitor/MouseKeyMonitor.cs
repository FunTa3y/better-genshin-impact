﻿using BetterGenshinImpact.Core.Recorder;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.Model;
using Gma.System.MouseKeyHook;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Vanara.PInvoke;
using Timer = System.Timers.Timer;

namespace BetterGenshinImpact.Core.Monitor;

public class MouseKeyMonitor
{
    /// <summary>
    ///     Определить объектыFИзменятьFлопаться
    /// </summary>
    private readonly Timer _fTimer = new();

    //private readonly Random _random = new();

    /// <summary>
    ///     Определить объектыкосмосИзменятькосмослопаться
    /// </summary>
    private readonly Timer _spaceTimer = new();

    private DateTime _firstFKeyDownTime = DateTime.MaxValue;

    /// <summary>
    ///     DateTime.MaxValue значит не нажимал
    /// </summary>
    private DateTime _firstSpaceKeyDownTime = DateTime.MaxValue;

    private IKeyboardMouseEvents? _globalHook;
    private nint _hWnd;

    public void Subscribe(nint gameHandle)
    {
        _hWnd = gameHandle;
        // Note: for the application hook, use the Hook.AppEvents() instead
        _globalHook = Hook.GlobalEvents();

        _globalHook.KeyDown += GlobalHookKeyDown;
        _globalHook.KeyUp += GlobalHookKeyUp;
        _globalHook.MouseDownExt += GlobalHookMouseDownExt;
        _globalHook.MouseUpExt += GlobalHookMouseUpExt;
        _globalHook.MouseMoveExt += GlobalHookMouseMoveExt;
        //_globalHook.KeyPress += GlobalHookKeyPress;

        _firstSpaceKeyDownTime = DateTime.MaxValue;
        var si = TaskContext.Instance().Config.MacroConfig.SpaceFireInterval;
        _spaceTimer.Interval = si;
        _spaceTimer.Elapsed += (sender, args) => { Simulation.PostMessage(_hWnd).KeyPress(User32.VK.VK_SPACE); };

        var fi = TaskContext.Instance().Config.MacroConfig.FFireInterval;
        _fTimer.Interval = fi;
        _fTimer.Elapsed += (sender, args) => { Simulation.PostMessage(_hWnd).KeyPress(User32.VK.VK_F); };
    }

    private void GlobalHookKeyDown(object? sender, KeyEventArgs e)
    {
        // Debug.WriteLine("KeyDown: \t{0}", e.KeyCode);
        GlobalKeyMouseRecord.Instance.GlobalHookKeyDown(e);

        // событие нажатия горячих клавиш
        HotKeyDown(sender, e);

        if (e.KeyCode == Keys.Space)
        {
            if (_firstSpaceKeyDownTime == DateTime.MaxValue)
            {
                _firstSpaceKeyDownTime = DateTime.Now;
            }
            else
            {
                var timeSpan = DateTime.Now - _firstSpaceKeyDownTime;
                if (timeSpan.TotalMilliseconds > 300 && TaskContext.Instance().Config.MacroConfig.SpacePressHoldToContinuationEnabled)
                    if (!_spaceTimer.Enabled)
                        _spaceTimer.Start();
            }
        }
        else if (e.KeyCode == Keys.F)
        {
            if (_firstFKeyDownTime == DateTime.MaxValue)
            {
                _firstFKeyDownTime = DateTime.Now;
            }
            else
            {
                var timeSpan = DateTime.Now - _firstFKeyDownTime;
                if (timeSpan.TotalMilliseconds > 200 && TaskContext.Instance().Config.MacroConfig.FPressHoldToContinuationEnabled)
                    if (!_fTimer.Enabled)
                        _fTimer.Start();
            }
        }
    }

    private void GlobalHookKeyUp(object? sender, KeyEventArgs e)
    {
        // Debug.WriteLine("KeyUp: \t{0}", e.KeyCode);
        GlobalKeyMouseRecord.Instance.GlobalHookKeyUp(e);

        // событие выпуска горячей клавиши
        HotKeyUp(sender, e);

        if (e.KeyCode == Keys.Space)
        {
            if (_firstSpaceKeyDownTime != DateTime.MaxValue)
            {
                var timeSpan = DateTime.Now - _firstSpaceKeyDownTime;
                Debug.WriteLine($"SpaceВремя прессы：{timeSpan.TotalMilliseconds}ms");
                _firstSpaceKeyDownTime = DateTime.MaxValue;
                _spaceTimer.Stop();
            }
        }
        else if (e.KeyCode == Keys.F)
        {
            if (_firstFKeyDownTime != DateTime.MaxValue)
            {
                var timeSpan = DateTime.Now - _firstFKeyDownTime;
                Debug.WriteLine($"FВремя прессы：{timeSpan.TotalMilliseconds}ms");
                _firstFKeyDownTime = DateTime.MaxValue;
                _fTimer.Stop();
            }
        }
    }

    private void HotKeyDown(object? sender, KeyEventArgs e)
    {
        if (KeyboardHook.AllKeyboardHooks.TryGetValue(e.KeyCode, out var hook)) hook.KeyDown(sender, e);
    }

    private void HotKeyUp(object? sender, KeyEventArgs e)
    {
        if (KeyboardHook.AllKeyboardHooks.TryGetValue(e.KeyCode, out var hook)) hook.KeyUp(sender, e);
    }

    //private void GlobalHookKeyPress(object? sender, KeyPressEventArgs e)
    //{
    //    Debug.WriteLine("KeyPress: \t{0}", e.KeyChar);
    //}

    private void GlobalHookMouseDownExt(object? sender, MouseEventExtArgs e)
    {
        // Debug.WriteLine("MouseDown: {0}; \t Location: {1};\t System Timestamp: {2}", e.Button, e.Location, e.Timestamp);
        GlobalKeyMouseRecord.Instance.GlobalHookMouseDown(e);

        if (e.Button != MouseButtons.Left)
            if (MouseHook.AllMouseHooks.TryGetValue(e.Button, out var hook))
                hook.MouseDown(sender, e);
    }

    private void GlobalHookMouseUpExt(object? sender, MouseEventExtArgs e)
    {
        // Debug.WriteLine("MouseUp: {0}; \t Location: {1};\t System Timestamp: {2}", e.Button, e.Location, e.Timestamp);
        GlobalKeyMouseRecord.Instance.GlobalHookMouseUp(e);

        if (e.Button != MouseButtons.Left)
            if (MouseHook.AllMouseHooks.TryGetValue(e.Button, out var hook))
                hook.MouseUp(sender, e);
    }

    private void GlobalHookMouseMoveExt(object? sender, MouseEventExtArgs e)
    {
        // Debug.WriteLine("MouseMove: {0}; \t Location: {1};\t System Timestamp: {2}", e.Button, e.Location, e.Timestamp);
        GlobalKeyMouseRecord.Instance.GlobalHookMouseMoveTo(e);
    }

    public void Unsubscribe()
    {
        if (_globalHook != null)
        {
            _globalHook.KeyDown -= GlobalHookKeyDown;
            _globalHook.KeyUp -= GlobalHookKeyUp;
            _globalHook.MouseDownExt -= GlobalHookMouseDownExt;
            _globalHook.MouseUpExt -= GlobalHookMouseUpExt;
            _globalHook.MouseMoveExt -= GlobalHookMouseMoveExt;
            //_globalHook.KeyPress -= GlobalHookKeyPress;
            _globalHook.Dispose();
        }
    }
}
