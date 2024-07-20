using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Model;
using Gma.System.MouseKeyHook;
using Microsoft.Extensions.Logging;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BetterGenshinImpact.Core.Recorder;

public class GlobalKeyMouseRecord : Singleton<GlobalKeyMouseRecord>
{
    private readonly ILogger<GlobalKeyMouseRecord> _logger = App.GetLogger<GlobalKeyMouseRecord>();

    private KeyMouseRecorder? _recorder;

    private readonly Dictionary<Keys, bool> _keyDownState = new();

    private DirectInputMonitor? _directInputMonitor;

    private readonly System.Timers.Timer _timer = new();

    private bool _isInMainUi = false; // Это в основном интерфейсе?

    public GlobalKeyMouseRecord()
    {
        _timer.Elapsed += Tick;
        _timer.Interval = 50; // ms
    }

    public void StartRecord()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("Пожалуйста, сначала перейдите на стартовую страницу，Запустите программу создания снимков экрана, а затем используйте эту функцию");
            return;
        }

        TaskTriggerDispatcher.Instance().StopTimer();
        _timer.Start();

        _recorder = new KeyMouseRecorder();
        _directInputMonitor = new DirectInputMonitor();
        _directInputMonitor.Start();

        _logger.LogInformation("Записывать：{Text}", "Задача реального времени приостановлена，ЗаписыватьНачал");
        _logger.LogInformation("Уведомление：ЗаписыватьПри встрече с основным интерфейсом（Мышь всегда находится в центре интерфейса）и другие интерфейсы（Мышь может свободно перемещаться，Например, карты и т. д.）переключение，Пожалуйста, уберите руки от мыши и подождите.ЗаписыватьЖурнал переключения режимов");
    }

    public string StopRecord()
    {
        var macro = _recorder?.ToJsonMacro() ?? string.Empty;
        _recorder = null;
        _directInputMonitor?.Stop();
        _directInputMonitor?.Dispose();
        _directInputMonitor = null;

        _timer.Stop();

        _logger.LogInformation("Записывать：{Text}", "ЗаканчиватьЗаписывать");

        TaskTriggerDispatcher.Instance().StartTimer();
        return macro;
    }

    public void Tick(object? sender, EventArgs e)
    {
        var ra = TaskControl.CaptureToRectArea();
        var iconRa = ra.Find(ElementAssets.Instance.FriendChat);
        var exist = iconRa.IsExist();
        if (exist != _isInMainUi)
        {
            _logger.LogInformation("Записывать：{Text}", exist ? "Войдите в основной интерфейс，Захват относительного движения мыши" : "Выйти из основного интерфейса，инициализация");
        }
        _isInMainUi = exist;
        iconRa.Dispose();
        ra.Dispose();
    }

    public void GlobalHookKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode.ToString() == TaskContext.Instance().Config.HotKeyConfig.Test1Hotkey)
        {
            return;
        }

        if (_keyDownState.TryGetValue(e.KeyCode, out var v))
        {
            if (v)
            {
                return; // Нажатое состояние больше не будет записываться
            }
            else
            {
                _keyDownState[e.KeyCode] = true;
            }
        }
        else
        {
            _keyDownState.Add(e.KeyCode, true);
        }
        // Debug.WriteLine($"KeyDown: {e.KeyCode}");
        _recorder?.KeyDown(e);
    }

    public void GlobalHookKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode.ToString() == TaskContext.Instance().Config.HotKeyConfig.Test1Hotkey)
        {
            return;
        }

        if (_keyDownState.ContainsKey(e.KeyCode) && _keyDownState[e.KeyCode])
        {
            // Debug.WriteLine($"KeyUp: {e.KeyCode}");
            _keyDownState[e.KeyCode] = false;
            _recorder?.KeyUp(e);
        }
    }

    public void GlobalHookMouseDown(MouseEventExtArgs e)
    {
        // Debug.WriteLine($"MouseDown: {e.Button}");
        _recorder?.MouseDown(e);
    }

    public void GlobalHookMouseUp(MouseEventExtArgs e)
    {
        // Debug.WriteLine($"MouseUp: {e.Button}");
        _recorder?.MouseUp(e);
    }

    public void GlobalHookMouseMoveTo(MouseEventExtArgs e)
    {
        if (_isInMainUi)
        {
            return;
        }
        // Debug.WriteLine($"MouseMove: {e.X}, {e.Y}");
        _recorder?.MouseMoveTo(e);
    }

    public void GlobalHookMouseMoveBy(MouseState state)
    {
        if (state is { X: 0, Y: 0 } || !_isInMainUi)
        {
            return;
        }
        // Debug.WriteLine($"MouseMoveBy: {state.X}, {state.Y}");
        _recorder?.MouseMoveBy(state);
    }
}
