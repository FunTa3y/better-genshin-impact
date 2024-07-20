using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Helpers.DpiAwareness;
using BetterGenshinImpact.View.Drawable;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Serilog.Sinks.RichTextBox.Abstraction;
using System;
using System.Diagnostics;
using PresentMonFps;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Vanara.PInvoke;
using FontFamily = System.Windows.Media.FontFamily;
using BetterGenshinImpact.ViewModel;
using Microsoft.Extensions.Logging;
using BetterGenshinImpact.Genshin.Settings;

namespace BetterGenshinImpact.View;

/// <summary>
/// Окно, используемое для наложения на окно игры.，Используется для отображения результатов распознавания.、Показать журнал、Установить местоположение области и т. д.
/// пожалуйста, используйте Instance Способ получения синглтона
/// </summary>
public partial class MaskWindow : Window
{
    private static MaskWindow? _maskWindow;

    private static readonly Typeface _typeface;

    private nint _hWnd;

    private IRichTextBox? _richTextBox;

    private readonly ILogger<MaskWindow> _logger = App.GetLogger<MaskWindow>();

    static MaskWindow()
    {
        if (Application.Current.TryFindResource("TextThemeFontFamily") is FontFamily fontFamily)
        {
            _typeface = fontFamily.GetTypefaces().First();
        }
        else
        {
            _typeface = new FontFamily("Microsoft Yahei UI").GetTypefaces().First();
        }

        DefaultStyleKeyProperty.OverrideMetadata(typeof(MaskWindow), new FrameworkPropertyMetadata(typeof(MaskWindow)));
    }

    public static MaskWindow Instance()
    {
        if (_maskWindow == null)
        {
            throw new Exception("MaskWindow Неинициализированный");
        }

        return _maskWindow;
    }

    public bool IsExist()
    {
        return _maskWindow != null && PresentationSource.FromVisual(_maskWindow) != null;
    }

    public void RefreshPosition()
    {
        nint targetHWnd = TaskContext.Instance().GameHandle;
        _ = User32.GetClientRect(targetHWnd, out RECT targetRect);
        float x = DpiHelper.GetScale(targetHWnd).X;
        _ = User32.SetWindowPos(_hWnd, IntPtr.Zero, 0, 0, (int)(targetRect.Width * x), (int)(targetRect.Height * x), User32.SetWindowPosFlags.SWP_SHOWWINDOW);

        Invoke(() =>
        {
            Canvas.SetTop(LogTextBoxWrapper, Height - LogTextBoxWrapper.Height - 65);
            Canvas.SetLeft(LogTextBoxWrapper, 20);
            Canvas.SetTop(StatusWrapper, Height - LogTextBoxWrapper.Height - 90);
            Canvas.SetLeft(StatusWrapper, 20);
        });

        // Пересчитать положение управления
        // shit code Забронировано
        WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "RefreshSettings", new object(), "Пересчитать положение управления"));
    }

    public MaskWindow()
    {
        _maskWindow = this;

        this.SetResourceReference(StyleProperty, typeof(MaskWindow));
        InitializeComponent();
        this.InitializeDpiAwareness();

        LogTextBox.TextChanged += LogTextBoxTextChanged;
        //AddAreaSettingsControl("окно идентификации теста");
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _richTextBox = App.GetService<IRichTextBox>();
        if (_richTextBox != null)
        {
            _richTextBox.RichTextBox = LogTextBox;
        }

        _hWnd = new WindowInteropHelper(this).Handle;
        nint targetHWnd = TaskContext.Instance().GameHandle;
        _ = User32.SetParent(_hWnd, targetHWnd);

        RefreshPosition();
        PrintSystemInfo();
    }

    private void PrintSystemInfo()
    {
        var systemInfo = TaskContext.Instance().SystemInfo;
        var width = systemInfo.GameScreenSize.Width;
        var height = systemInfo.GameScreenSize.Height;
        var dpiScale = TaskContext.Instance().DpiScale;
        _logger.LogInformation("Окно маски активировано，размер игры{Width}x{Height}，Увеличение материала{Scale}，DPIУвеличить{Dpi}",
            width, height, systemInfo.AssetScale.ToString("F"), dpiScale);

        if (width * 9 != height * 16)
        {
            _logger.LogWarning("Текущее разрешение игры не16:9，Некоторые функции могут работать некорректно");
        }

        // Чтение конфигурации реестра игры
        ReadGameSettings();
    }

    private void ReadGameSettings()
    {
        try
        {
            SettingsContainer settings = new();
            TaskContext.Instance().GameSettings = settings;
            var lang = settings.Language?.TextLang;
            if (lang != null && lang != TextLanguage.SimplifiedChinese)
            {
                _logger.LogWarning("Текущий язык игры{Lang}Не упрощенный китайский，Некоторые функции могут работать некорректно。The game language is not Simplified Chinese, some functions may not work properly", lang);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning("Не удалось прочитать информацию о конфигурации реестра игры.：" + e.Source + "\r\n--" + Environment.NewLine + e.StackTrace + "\r\n---" + Environment.NewLine + e.Message);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        this.SetLayeredWindow();
        this.SetChildWindow();
        this.HideFromAltTab();
    }

    private void LogTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        var textRange = new TextRange(LogTextBox.Document.ContentStart, LogTextBox.Document.ContentEnd);
        if (textRange.Text.Length > 10000)
        {
            LogTextBox.Document.Blocks.Clear();
        }

        LogTextBox.ScrollToEnd();
    }

    public void Refresh()
    {
        Dispatcher.Invoke(InvalidateVisual);
    }

    public void Invoke(Action action)
    {
        Dispatcher.Invoke(action);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        try
        {
            var cnt = VisionContext.Instance().DrawContent.RectList.Count + VisionContext.Instance().DrawContent.LineList.Count + VisionContext.Instance().DrawContent.TextList.Count;
            if (cnt == 0)
            {
                return;
            }

            // Основанием для решения свыше является то, что，возможныйRenderкогда，Конфигурация такжеНеинициализированный
            if (!TaskContext.Instance().Config.MaskWindowConfig.DisplayRecognitionResultsOnMask)
            {
                return;
            }

            foreach (var kv in VisionContext.Instance().DrawContent.RectList)
            {
                foreach (var drawable in kv.Value)
                {
                    if (!drawable.IsEmpty)
                    {
                        drawingContext.DrawRectangle(Brushes.Transparent,
                            new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width),
                            drawable.Rect);
                    }
                }
            }

            foreach (var kv in VisionContext.Instance().DrawContent.LineList)
            {
                foreach (var drawable in kv.Value)
                {
                    drawingContext.DrawLine(new Pen(new SolidColorBrush(drawable.Pen.Color.ToWindowsColor()), drawable.Pen.Width), drawable.P1, drawable.P2);
                }
            }

            foreach (var kv in VisionContext.Instance().DrawContent.TextList)
            {
                foreach (var drawable in kv.Value)
                {
                    if (!drawable.IsEmpty)
                    {
                        drawingContext.DrawText(new FormattedText(drawable.Text,
                            CultureInfo.GetCultureInfo("zh-cn"),
                            FlowDirection.LeftToRight,
                            _typeface,
                            36, Brushes.Black, 1), drawable.Point);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        base.OnRender(drawingContext);
    }

    public RichTextBox LogBox => LogTextBox;
}

file static class MaskWindowExtension
{
    public static void HideFromAltTab(this Window window)
    {
        HideFromAltTab(new WindowInteropHelper(window).Handle);
    }

    public static void HideFromAltTab(nint hWnd)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        style |= (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW;
        User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetLayeredWindow(this Window window, bool isLayered = true)
    {
        SetLayeredWindow(new WindowInteropHelper(window).Handle, isLayered);
    }

    private static void SetLayeredWindow(nint hWnd, bool isLayered = true)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE);

        if (isLayered)
        {
            style |= (int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style |= (int)User32.WindowStylesEx.WS_EX_LAYERED;
        }
        else
        {
            style &= ~(int)User32.WindowStylesEx.WS_EX_TRANSPARENT;
            style &= ~(int)User32.WindowStylesEx.WS_EX_LAYERED;
        }

        _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_EXSTYLE, style);
    }

    public static void SetChildWindow(this Window window)
    {
        SetChildWindow(new WindowInteropHelper(window).Handle);
    }

    private static void SetChildWindow(nint hWnd)
    {
        int style = User32.GetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE);

        style |= (int)User32.WindowStyles.WS_CHILD;
        _ = User32.SetWindowLong(hWnd, User32.WindowLongFlags.GWL_STYLE, style);
    }
}
