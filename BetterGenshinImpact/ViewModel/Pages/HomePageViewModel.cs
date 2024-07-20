using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.Genshin.Paths;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service.Interface;
using BetterGenshinImpact.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Fischless.GameCapture;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Interop;
using Windows.System;
using Wpf.Ui.Controls;

namespace BetterGenshinImpact.ViewModel.Pages;

public partial class HomePageViewModel : ObservableObject, INavigationAware, IViewModel
{
    [ObservableProperty] private string[] _modeNames = GameCaptureFactory.ModeNames();

    [ObservableProperty] private string? _selectedMode = CaptureModes.BitBlt.ToString();

    [ObservableProperty] private bool _taskDispatcherEnabled = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTriggerCommand))]
    private bool _startButtonEnabled = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopTriggerCommand))]
    private bool _stopButtonEnabled = true;

    public AllConfig Config { get; set; }

    private MaskWindow? _maskWindow;
    private readonly ILogger<HomePageViewModel> _logger = App.GetLogger<HomePageViewModel>();

    private readonly TaskTriggerDispatcher _taskDispatcher;
    private readonly MouseKeyMonitor _mouseKeyMonitor = new();

    // Запишите последний дескриптор, использованный для Genshin Impact.
    private IntPtr _hWnd;

    [ObservableProperty]
    private string[] _inferenceDeviceTypes = BgiSessionOption.InferenceDeviceTypes;

    public HomePageViewModel(IConfigService configService, TaskTriggerDispatcher taskTriggerDispatcher)
    {
        _taskDispatcher = taskTriggerDispatcher;
        Config = configService.Get();
        ReadGameInstallPath();

        // WindowsGraphicsCapture Поддерживает только Win10 18362 и выше версии (Windows 10 version 1903 or later)
        // https://github.com/babalae/better-genshin-impact/issues/394
        if (!OsVersionHelper.IsWindows10_1903_OrGreater)
        {
            _modeNames = _modeNames.Where(x => x != CaptureModes.WindowsGraphicsCapture.ToString()).ToArray();
            // DirectML в Windows 10 Версия 1903 и Windows SDK соответствующийВерсиявведено в。
            // https://learn.microsoft.com/zh-cn/windows/ai/directml/dml
            _inferenceDeviceTypes = _inferenceDeviceTypes.Where(x => x != "GPU_DirectML").ToArray();
        }

        WeakReferenceMessenger.Default.Register<PropertyChangedMessage<object>>(this, (sender, msg) =>
        {
            if (msg.PropertyName == "Close")
            {
                OnClosed();
            }
            else if (msg.PropertyName == "SwitchTriggerStatus")
            {
                if (_taskDispatcherEnabled)
                {
                    OnStopTrigger();
                }
                else
                {
                    _ = OnStartTriggerAsync();
                }
            }
        });

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            if (args[1].Contains("start"))
            {
                _ = OnStartTriggerAsync();
            }
        }
    }

    [RelayCommand]
    private void OnLoaded()
    {
        // OnTest();
    }

    private void OnClosed()
    {
        OnStopTrigger();
        // Дождитесь завершения задачи
        _maskWindow?.Close();
    }

    [RelayCommand]
    private async Task OnCaptureModeDropDownChanged()
    {
        // Перезагрузить при запуске
        if (TaskDispatcherEnabled)
        {
            _logger.LogInformation("► Переключите режим захвата на[{Mode}]，Скриншоттер автоматически перезапускается...", Config.CaptureMode);
            OnStopTrigger();
            await OnStartTriggerAsync();
        }
    }

    [RelayCommand]
    private void OnInferenceDeviceTypeDropDownChanged(string value)
    {
    }

    [RelayCommand]
    private void OnStartCaptureTest()
    {
        var picker = new PickerWindow();
        var hWnd = picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle);
        if (hWnd != IntPtr.Zero)
        {
            var captureWindow = new CaptureTestWindow();
            captureWindow.StartCapture(hWnd, Config.CaptureMode.ToCaptureMode());
            captureWindow.Show();
        }
    }

    [RelayCommand]
    private void OnManualPickWindow()
    {
        var picker = new PickerWindow();
        var hWnd = picker.PickCaptureTarget(new WindowInteropHelper(UIDispatcherHelper.MainWindow).Handle);
        if (hWnd != IntPtr.Zero)
        {
            _hWnd = hWnd;
            Start(hWnd);
        }
        else
        {
            System.Windows.MessageBox.Show("Выбранный дескриптор формы пуст！");
        }
    }

    [RelayCommand]
    private async Task OpenDisplayAdvancedGraphicsSettingsAsync()
    {
        // ms-settings:display
        // ms-settings:display-advancedgraphics
        // ms-settings:display-advancedgraphics-default
        await Launcher.LaunchUriAsync(new Uri("ms-settings:display-advancedgraphics"));
    }

    private bool CanStartTrigger() => StartButtonEnabled;

    [RelayCommand(CanExecute = nameof(CanStartTrigger))]
    private async Task OnStartTriggerAsync()
    {
        var hWnd = SystemControl.FindGenshinImpactHandle();
        if (hWnd == IntPtr.Zero)
        {
            if (Config.GenshinStartConfig.LinkedStartEnabled && !string.IsNullOrEmpty(Config.GenshinStartConfig.InstallPath))
            {
                hWnd = await SystemControl.StartFromLocalAsync(Config.GenshinStartConfig.InstallPath);
                if (hWnd != IntPtr.Zero)
                {
                    TaskContext.Instance().LinkedStartGenshinTime = DateTime.Now; // Определяет время, когда ассоциация начинает Genshin Impact.
                }
            }

            if (hWnd == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("Окно Genshin Impact не найдено，Пожалуйста, сначала запустите Genshin Impact！");
                return;
            }
        }

        Start(hWnd);
    }

    private void Start(IntPtr hWnd)
    {
        if (!TaskDispatcherEnabled)
        {
            _hWnd = hWnd;
            _taskDispatcher.Start(hWnd, Config.CaptureMode.ToCaptureMode(), Config.TriggerInterval);
            _taskDispatcher.UiTaskStopTickEvent -= OnUiTaskStopTick;
            _taskDispatcher.UiTaskStartTickEvent -= OnUiTaskStartTick;
            _taskDispatcher.UiTaskStopTickEvent += OnUiTaskStopTick;
            _taskDispatcher.UiTaskStartTickEvent += OnUiTaskStartTick;
            _maskWindow ??= new MaskWindow();
            _maskWindow.Show();
            _mouseKeyMonitor.Subscribe(hWnd);
            TaskDispatcherEnabled = true;
        }
    }

    private bool CanStopTrigger() => StopButtonEnabled;

    [RelayCommand(CanExecute = nameof(CanStopTrigger))]
    private void OnStopTrigger()
    {
        Stop();
    }

    private void Stop()
    {
        if (TaskDispatcherEnabled)
        {
            _taskDispatcher.Stop();
            if (_maskWindow != null && _maskWindow.IsExist())
            {
                _maskWindow?.Hide();
            }
            else
            {
                _maskWindow?.Close();
                _maskWindow = null;
            }
            TaskDispatcherEnabled = false;
            _mouseKeyMonitor.Unsubscribe();
        }
    }

    private void OnUiTaskStopTick(object? sender, EventArgs e)
    {
        UIDispatcherHelper.Invoke(Stop);
    }

    private void OnUiTaskStartTick(object? sender, EventArgs e)
    {
        UIDispatcherHelper.Invoke(() => Start(_hWnd));
    }

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    public void OnGoToWikiUrl()
    {
        Process.Start(new ProcessStartInfo("https://bgi.huiyadan.com/doc.html") { UseShellExecute = true });
    }

    [RelayCommand]
    private void OnTest()
    {
        // var result = OcrFactory.Paddle.OcrResult(new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Авто бой\Идентификация команды\x2.png", ImreadModes.Grayscale));
        // foreach (var region in result.Regions)
        // {
        //     Debug.WriteLine($"{region.Text}");
        // }

        //try
        //{
        //    YoloV8 predictor = new(Global.Absolute("Assets\\Model\\Fish\\bgi_fish.onnx"));
        //    using var memoryStream = new MemoryStream();
        //    new Bitmap(Global.Absolute("test_yolo.png")).Save(memoryStream, ImageFormat.Bmp);
        //    memoryStream.Seek(0, SeekOrigin.Begin);
        //    var result = predictor.Detect(memoryStream);
        //    MessageBox.Show(JsonSerializer.Serialize(result));
        //}
        //catch (Exception e)
        //{
        //    MessageBox.Show(e.StackTrace);
        //}

        // Mat tar = new(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Автоматическое приглашение\selected.png", ImreadModes.Grayscale);
        //  var mask = OpenCvCommonHelper.CreateMask(tar, new Scalar(0, 0, 0));
        // var src = new Mat(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Автоматическое приглашение\Clip_20240309_135839.png", ImreadModes.Grayscale);
        // var src2 = src.Clone();
        // var res = MatchTemplateHelper.MatchOnePicForOnePic(src, mask);
        // // Нарисуйте результат на исходном изображении
        // foreach (var t in res)
        // {
        //     Cv2.Rectangle(src2, t, new Scalar(0, 0, 255));
        // }
        //
        // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\автоматический сюжет\Автоматическое приглашение\x1.png", src2);
    }

    [RelayCommand]
    public async Task SelectInstallPathAsync()
    {
        await Task.Run(() =>
        {
            // Появится диалоговое окно выбора папки
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
            {
                Filter = "Геншин Импакт|YuanShen.exe|Геншин ИмпактМеждународный сервер|GenshinImpact.exe|Все файлы|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                var path = dialog.FileName;
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                Config.GenshinStartConfig.InstallPath = path;
            }
        });
    }

    private void ReadGameInstallPath()
    {
        // Проверьте, настроил ли пользовательГеншин Импактинструкция по установке，если нет，Попробуйте прочитать из реестра
        if (string.IsNullOrEmpty(Config.GenshinStartConfig.InstallPath))
        {
            var path = GameExePath.GetWithoutCloud();
            if (!string.IsNullOrEmpty(path))
            {
                Config.GenshinStartConfig.InstallPath = path;
            }
        }
    }
}
