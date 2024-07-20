using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Win32.Foundation;

namespace Vision.WindowCapture.Test
{
    /// <summary>
    /// CaptureTestWindow.xaml логика взаимодействия
    /// </summary>
    public partial class CaptureTestWindow : Window
    {
        private IWindowCapture? _capture;

        private long _captureTime;
        private long _transferTime;
        private long _captureCount;

        public CaptureTestWindow()
        {
            _captureTime = 0;
            _captureCount = 0;
            InitializeComponent();
            Closed += (sender, args) =>
            {
                CompositionTarget.Rendering -= Loop;
                _capture?.StopAsync();

                Debug.WriteLine("Среднее время создания скриншота:" + _captureTime * 1.0 / _captureCount);
                Debug.WriteLine("среднее время конверсии:" + _transferTime * 1.0 / _captureCount);
                Debug.WriteLine("Среднее общее время, затраченное:" + (_captureTime + _transferTime) * 1.0 / _captureCount);
            };
        }

        public async void StartCapture(IntPtr hWnd, CaptureModeEnum captureMode)
        {
            if (hWnd == IntPtr.Zero)
            {
                MessageBox.Show("Пожалуйста, выберите окно");
                return;
            }


            _capture = WindowCaptureFactory.Create(captureMode);
            await _capture.StartAsync((HWND)hWnd);

            CompositionTarget.Rendering += Loop;
        }

        private void Loop(object? sender, EventArgs e)
        {
            var sw = new Stopwatch();
            sw.Start();
            var bitmap = _capture?.Capture();
            sw.Stop();
            Debug.WriteLine("Создание скриншотов требует времени:" + sw.ElapsedMilliseconds);
            _captureTime += sw.ElapsedMilliseconds;
            if (bitmap != null)
            {
                _captureCount++;
                sw.Reset();
                sw.Start();
                DisplayCaptureResultImage.Source = ToBitmapImage(bitmap);
                sw.Stop();
                Debug.WriteLine("Время конвертации:" + sw.ElapsedMilliseconds);
                _transferTime += sw.ElapsedMilliseconds;
            }
            else
            {
                Debug.WriteLine("Снимок экрана не выполнен.");
            }
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            var image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}