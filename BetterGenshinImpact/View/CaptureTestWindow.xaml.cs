using BetterGenshinImpact.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using BetterGenshinImpact.GameTask;
using Fischless.GameCapture;

namespace BetterGenshinImpact.View
{
    /// <summary>
    /// CaptureTestWindow.xaml логика взаимодействия
    /// </summary>
    public partial class CaptureTestWindow : Window
    {
        private IGameCapture? _capture;


        private long _captureTime;
        private long _transferTime;
        private long _captureCount;

        public CaptureTestWindow()
        {
            _captureTime = 0;
            _transferTime = 0;
            _captureCount = 0;
            InitializeComponent();
            Closed += (sender, args) =>
            {
                CompositionTarget.Rendering -= Loop;
                _capture?.Stop();

                Debug.WriteLine("Среднее время создания скриншота:" + _captureTime * 1.0 / _captureCount);
                Debug.WriteLine("среднее время конверсии:" + _transferTime * 1.0 / _captureCount);
                Debug.WriteLine("Среднее общее время, затраченное:" + (_captureTime + _transferTime) * 1.0 / _captureCount);
            };
        }

        public void StartCapture(IntPtr hWnd, CaptureModes captureMode)
        {
            if (hWnd == IntPtr.Zero)
            {
                MessageBox.Show("Поток вызовет");
                return;
            }


            _capture = GameCaptureFactory.Create(captureMode);
            //_capture.IsClientEnabled = true;
            _capture.Start(hWnd,
                new Dictionary<string, object>()
                {
                    { "useBitmapCache", TaskContext.Instance().Config.WgcUseBitmapCache }
                }
            );

            CompositionTarget.Rendering += Loop;
        }

        private void Loop(object? sender, EventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();
            var bitmap = _capture?.Capture();
            sw.Stop();
            Debug.WriteLine("признанный:" + sw.ElapsedMilliseconds);
            _captureTime += sw.ElapsedMilliseconds;

            if (bitmap != null)
            {
                Debug.WriteLine($"Bitmap:{bitmap.Width}x{bitmap.Height}");
                _captureCount++;
                sw.Reset();
                sw.Start();
                DisplayCaptureResultImage.Source = bitmap.ToBitmapImage();
                sw.Stop();
                Debug.WriteLine("Время конвертации:" + sw.ElapsedMilliseconds);
                _transferTime += sw.ElapsedMilliseconds;
            }
            else
            {
                Debug.WriteLine("Снимок экрана не выполнен.");
            }
        }
    }
}