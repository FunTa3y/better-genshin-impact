using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation;
using BetterGenshinImpact.GameTask.AutoWood;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.Model.Enum;
using BetterGenshinImpact.Genshin.Settings;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.View;
using Fischless.GameCapture;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.AutoSkip;
using BetterGenshinImpact.GameTask.AutoSkip.Model;
using Vanara.PInvoke;
using BetterGenshinImpact.GameTask.AutoMusicGame;
using BetterGenshinImpact.GameTask.AutoTrackPath;

namespace BetterGenshinImpact.GameTask
{
    public class TaskTriggerDispatcher : IDisposable
    {
        private readonly ILogger<TaskTriggerDispatcher> _logger = App.GetLogger<TaskTriggerDispatcher>();

        private static TaskTriggerDispatcher? _instance;

        private readonly System.Timers.Timer _timer = new();
        private List<ITaskTrigger>? _triggers;

        public IGameCapture? GameCapture { get; private set; }

        private static readonly object _locker = new();
        private int _frameIndex = 0;

        private RECT _gameRect = RECT.Empty;
        private bool _prevGameActive;

        private DateTime _prevManualGc = DateTime.MinValue;

        /// <summary>
        /// захват очереди результатов
        /// </summary>
        private Bitmap _bitmap = new(10, 10);

        /// <summary>
        /// Режим только съемки
        /// </summary>
        private DispatcherCaptureModeEnum _dispatcherCacheCaptureMode = DispatcherCaptureModeEnum.NormalTrigger;

        private static readonly object _bitmapLocker = new();

        public event EventHandler UiTaskStopTickEvent;

        public event EventHandler UiTaskStartTickEvent;

        public TaskTriggerDispatcher()
        {
            _instance = this;
            _timer.Elapsed += Tick;
            //_timer.Tick += Tick;
        }

        public static TaskTriggerDispatcher Instance()
        {
            if (_instance == null)
            {
                throw new Exception("Пожалуйста, сначала начните со стартовой страницы.BetterGI，Если он уже запущен, перезапустите его.");
            }

            return _instance;
        }

        public static IGameCapture GlobalGameCapture
        {
            get
            {
                _instance = Instance();

                if (_instance.GameCapture == null)
                {
                    throw new Exception("Скриншоттер не инициализируется!");
                }

                return _instance.GameCapture;
            }
        }

        public void Start(IntPtr hWnd, CaptureModes mode, int interval = 50)
        {
            // Инициализировать средство создания скриншотов
            GameCapture = GameCaptureFactory.Create(mode);
            // Активировать окно Убедитесь, что информацию об окне можно будет получить позже в обычном режиме.
            SystemControl.ActivateWindow(hWnd);

            // Инициализировать контекст задачи(Необходимо выполнить до инициализации триггера.)
            TaskContext.Instance().Init(hWnd);

            // инициализировать триггер(Должен использоваться после инициализации контекста задачи.)
            _triggers = GameTaskManager.LoadTriggers();

            // Начать скриншот
            GameCapture.Start(hWnd,
                new Dictionary<string, object>()
                {
                    { "useBitmapCache", TaskContext.Instance().Config.WgcUseBitmapCache }
                }
            );

            // Конфигурация инициализации режима захвата
            if (TaskContext.Instance().Config.CommonConfig.ScreenshotEnabled || TaskContext.Instance().Config.MacroConfig.CombatMacroEnabled)
            {
                _dispatcherCacheCaptureMode = DispatcherCaptureModeEnum.CacheCaptureWithTrigger;
            }

            // Запустить таймер
            _frameIndex = 0;
            _timer.Interval = interval;
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            _timer.Stop();
            GameCapture?.Stop();
            _gameRect = RECT.Empty;
            _prevGameActive = false;
        }

        public void StartTimer()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void StopTimer()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        /// <summary>
        /// Запуск независимых задач
        /// </summary>
        public void StartIndependentTask(IndependentTaskEnum taskType, BaseTaskParam param)
        {
            if (!_timer.Enabled)
            {
                throw new Exception("Пожалуйста, сначала начните со стартовой страницы.BetterGI，Если он уже запущен, перезапустите его.");
            }

            var maskWindow = MaskWindow.Instance();
            maskWindow.LogBox.IsHitTestVisible = false;
            maskWindow.Invoke(() => { maskWindow.Show(); });
            if (taskType == IndependentTaskEnum.AutoGeniusInvokation)
            {
                AutoGeniusInvokationTask.Start((GeniusInvokationTaskParam)param);
            }
            else if (taskType == IndependentTaskEnum.AutoWood)
            {
                Task.Run(() => { new AutoWoodTask().Start((WoodTaskParam)param); });
            }
            else if (taskType == IndependentTaskEnum.AutoFight)
            {
                Task.Run(() => { new AutoFightTask((AutoFightParam)param).Start(); });
            }
            else if (taskType == IndependentTaskEnum.AutoDomain)
            {
                Task.Run(() => { new AutoDomainTask((AutoDomainParam)param).Start(); });
            }
            else if (taskType == IndependentTaskEnum.AutoTrack)
            {
                Task.Run(() => { new AutoTrackTask((AutoTrackParam)param).Start(); });
            }
            else if (taskType == IndependentTaskEnum.AutoTrackPath)
            {
                Task.Run(() => { new AutoTrackPathTask((AutoTrackPathParam)param).Start(); });
            }
            else if (taskType == IndependentTaskEnum.AutoMusicGame)
            {
                Task.Run(() => { new AutoMusicGameTask((AutoMusicGameParam)param).Start(); });
            }
        }

        public void Dispose() => Stop();

        public void Tick(object? sender, EventArgs e)
        {
            var hasLock = false;
            try
            {
                Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    // Пропустить во время выполнения
                    return;
                }

                // Проверьте, инициализирован ли скриншотер
                var maskWindow = MaskWindow.Instance();
                if (GameCapture == null || !GameCapture.IsCapturing)
                {
                    if (!TaskContext.Instance().SystemInfo.GameProcess.HasExited)
                    {
                        _logger.LogError("Скриншоттер не инициализируется!");
                    }
                    else
                    {
                        _logger.LogInformation("Игра вышла，BetterGI Автоматически останавливать создание скриншотов");
                    }
                    UiTaskStopTickEvent.Invoke(sender, e);
                    maskWindow.Invoke(maskWindow.Hide);
                    return;
                }

                // Проверьте, находится ли игра на переднем плане
                var hasBackgroundTriggerToRun = false;
                var active = SystemControl.IsGenshinImpactActive();
                if (!active)
                {
                    // Проверьте, закончилась ли игра
                    if (TaskContext.Instance().SystemInfo.GameProcess.HasExited)
                    {
                        _logger.LogInformation("Игра вышла，BetterGI Автоматически останавливать создание скриншотов");
                        UiTaskStopTickEvent.Invoke(sender, e);
                        return;
                    }

                    if (_prevGameActive)
                    {
                        Debug.WriteLine("Окно игры не на переднем плане, Больше никаких скриншотов");
                    }

                    // var pName = SystemControl.GetActiveProcessName();
                    // if (pName != "BetterGI" && pName != "YuanShen" && pName != "GenshinImpact" && pName != "Genshin Impact Cloud Game")
                    // {
                    //     maskWindow.Invoke(() => { maskWindow.Hide(); });
                    // }

                    _prevGameActive = active;

                    if (_triggers != null)
                    {
                        var exclusive = _triggers.FirstOrDefault(t => t is { IsEnabled: true, IsExclusive: true });
                        if (exclusive != null)
                        {
                            hasBackgroundTriggerToRun = exclusive.IsBackgroundRunning;
                        }
                        else
                        {
                            hasBackgroundTriggerToRun = _triggers.Any(t => t is { IsEnabled: true, IsBackgroundRunning: true });
                        }
                    }
                    if (!hasBackgroundTriggerToRun)
                    {
                        // Триггеры не работают в фоновом режиме，На этот раз больше скриншотов не будет
                        return;
                    }
                }
                else
                {
                    // if (!_prevGameActive)
                    // {
                    //     maskWindow.Invoke(() =>
                    //     {
                    //         if (maskWindow.IsExist())
                    //         {
                    //             maskWindow.Show();
                    //         }
                    //     });
                    // }

                    _prevGameActive = active;
                    // Синхронизировать положение окна маски при перемещении окна игры.,В настоящее время захват не производится
                    if (SyncMaskWindowPosition())
                    {
                        return;
                    }
                }

                // Серийный номер рамы увеличивается автоматически 1Возврат к нулю через несколько минут(MaxFrameIndexSecond)
                _frameIndex = (_frameIndex + 1) % (int)(CaptureContent.MaxFrameIndexSecond * 1000d / _timer.Interval);

                if (_dispatcherCacheCaptureMode == DispatcherCaptureModeEnum.NormalTrigger
                    && (_triggers == null || !_triggers.Exists(t => t.IsEnabled)))
                {
                    // Debug.WriteLine("Триггер недоступен и не доступен только для скриншотов., Больше никаких скриншотов");
                    return;
                }

                var speedTimer = new SpeedTimer();
                // Снимайте кадры игры
                var bitmap = GameCapture.Capture();
                speedTimer.Record("Скриншот");

                if (bitmap == null)
                {
                    _logger.LogWarning("Скриншотнеудача!");
                    return;
                }

                if (IsOnlyCacheCapture(bitmap))
                {
                    return;
                }

                // Перебрать все триггеры При наличии эксклюзивного триггера будет выполнен только эксклюзивный триггер.
                var content = new CaptureContent(bitmap, _frameIndex, _timer.Interval);
                var exclusiveTrigger = _triggers!.FirstOrDefault(t => t is { IsEnabled: true, IsExclusive: true });
                if (exclusiveTrigger != null)
                {
                    exclusiveTrigger.OnCapture(content);
                    speedTimer.Record(exclusiveTrigger.Name);
                }
                else
                {
                    var runningTriggers = _triggers.Where(t => t.IsEnabled);
                    if (hasBackgroundTriggerToRun)
                    {
                        runningTriggers = runningTriggers.Where(t => t.IsBackgroundRunning);
                    }

                    foreach (var trigger in runningTriggers)
                    {
                        trigger.OnCapture(content);
                        speedTimer.Record(trigger.Name);
                    }
                }

                speedTimer.DebugPrint();
                content.Dispose();
            }
            finally
            {
                if ((DateTime.Now - _prevManualGc).TotalSeconds > 2)
                {
                    GC.Collect();
                    _prevManualGc = DateTime.Now;
                }

                if (hasLock)
                {
                    Monitor.Exit(_locker);
                }
            }
        }

        /// <summary>
        /// / Синхронизировать положение окна маски при перемещении окна игры.
        /// </summary>
        /// <returns></returns>
        private bool SyncMaskWindowPosition()
        {
            var hWnd = TaskContext.Instance().GameHandle;
            var currentRect = SystemControl.GetCaptureRect(hWnd);
            if (_gameRect == RECT.Empty)
            {
                _gameRect = new RECT(currentRect);
            }
            else if (_gameRect != currentRect)
            {
                // Вероятно, это решение впоследствии может быть отменено.，Поддерживает перемещение и изменение окон по желанию. —— не поддерживается Слишком много вопросов для рассмотрения
                if ((_gameRect.Width != currentRect.Width || _gameRect.Height != currentRect.Height)
                    && !SizeIsZero(_gameRect) && !SizeIsZero(currentRect))
                {
                    _logger.LogError("► Изменение размера окна игры {W}x{H}->{CW}x{CH}, Автоматический перезапускСкриншотв сосуде...", _gameRect.Width, _gameRect.Height, currentRect.Width, currentRect.Height);
                    UiTaskStopTickEvent.Invoke(null, EventArgs.Empty);
                    UiTaskStartTickEvent.Invoke(null, EventArgs.Empty);
                    _logger.LogInformation("► Изменение размера окна игры，СкриншотПерезапуск сервера завершен！");
                }

                _gameRect = new RECT(currentRect);
                double scale = TaskContext.Instance().DpiScale;
                TaskContext.Instance().SystemInfo.CaptureAreaRect = currentRect;
                MaskWindow.Instance().RefreshPosition();
                return true;
            }

            return false;
        }

        private bool SizeIsZero(RECT rect)
        {
            return rect.Width == 0 || rect.Height == 0;
        }

        /// <summary>
        /// Кэшировать ли толькоСкриншот
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private bool IsOnlyCacheCapture(Bitmap bitmap)
        {
            lock (_bitmapLocker)
            {
                if (_dispatcherCacheCaptureMode is DispatcherCaptureModeEnum.OnlyCacheCapture or DispatcherCaptureModeEnum.CacheCaptureWithTrigger)
                {
                    _bitmap = new Bitmap(bitmap);
                    if (_dispatcherCacheCaptureMode == DispatcherCaptureModeEnum.OnlyCacheCapture)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void SetCacheCaptureMode(DispatcherCaptureModeEnum mode)
        {
            if (mode is DispatcherCaptureModeEnum.Start)
            {
                this.StartTimer();
            }
            else if (mode is DispatcherCaptureModeEnum.Stop)
            {
                this.StopTimer();
            }
            else
            {
                _dispatcherCacheCaptureMode = mode;
            }
        }

        public DispatcherCaptureModeEnum GetCacheCaptureMode()
        {
            return _dispatcherCacheCaptureMode;
        }

        public Bitmap GetLastCaptureBitmap()
        {
            lock (_bitmapLocker)
            {
                return new Bitmap(_bitmap);
            }
        }

        public CaptureContent GetLastCaptureContent()
        {
            var bitmap = GetLastCaptureBitmap();
            return new CaptureContent(bitmap, _frameIndex, _timer.Interval);
        }

        public void TakeScreenshot()
        {
            if (_dispatcherCacheCaptureMode is DispatcherCaptureModeEnum.OnlyCacheCapture or DispatcherCaptureModeEnum.CacheCaptureWithTrigger)
            {
                try
                {
                    var path = Global.Absolute($@"log\screenshot\");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    var bitmap = GetLastCaptureBitmap();
                    var name = $@"{DateTime.Now:yyyyMMddHHmmssffff}.png";
                    var savePath = Global.Absolute($@"log\screenshot\{name}");

                    if (TaskContext.Instance().Config.CommonConfig.ScreenshotUidCoverEnabled)
                    {
                        var mat = bitmap.ToMat();
                        var rect = TaskContext.Instance().Config.MaskWindowConfig.UidCoverRect;
                        mat.Rectangle(rect, Scalar.White, -1);
                        Cv2.ImWrite(savePath, mat);
                    }
                    else
                    {
                        bitmap.Save(savePath, ImageFormat.Png);
                    }

                    _logger.LogInformation("СкриншотСохранено: {Name}", name);
                }
                catch (Exception e)
                {
                    _logger.LogError("Скриншотизнеудача: {Message}", e.Message);
                    _logger.LogDebug("Скриншотизнеудача: {StackTrace}", e.StackTrace);
                }
            }
            else
            {
                _logger.LogWarning("В настоящее время не вСкриншотмодель，Не удалось сохранитьСкриншот");
            }
        }
    }
}
