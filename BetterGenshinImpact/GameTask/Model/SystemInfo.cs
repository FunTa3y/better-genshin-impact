using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Helpers;
using System;
using System.Diagnostics;
using OpenCvSharp;
using Vanara.PInvoke;
using Size = System.Drawing.Size;

namespace BetterGenshinImpact.GameTask.Model
{
    public class SystemInfo
    {
        /// <summary>
        /// Разрешение монитора Нет масштабирования
        /// </summary>
        public Size DisplaySize { get; }

        /// <summary>
        /// Разрешение окна в игре
        /// </summary>
        public RECT GameScreenSize { get; }

        /// <summary>
        /// к1080PМасштабировать до стандартных кадров,не будет больше, чем1
        /// и ZoomOutMax1080PRatio равный
        /// </summary>
        public double AssetScale { get; } = 1;

        /// <summary>
        /// соотношение игровой площади1080Pуменьшенное соотношение
        /// Максимальное значение1
        /// </summary>
        public double ZoomOutMax1080PRatio { get; } = 1;

        /// <summary>
        /// Захватите игровую зону в увеличенном масштабе1080Pпропорция
        /// </summary>
        public double ScaleTo1080PRatio { get; }

        /// <summary>
        /// захват области окна Соответствует реальному игровому экрану
        /// CaptureAreaRect = GameScreenSize or GameWindowRect
        /// </summary>
        public RECT CaptureAreaRect { get; set; }

        /// <summary>
        /// захват области окна больше, чем1080PТогда это1920x1080
        /// </summary>
        public Rect ScaleMax1080PCaptureRect { get; set; }

        public Process GameProcess { get; }

        public string GameProcessName { get; }

        public int GameProcessId { get; }

        public DesktopRegion DesktopRectArea { get; }

        public SystemInfo(IntPtr hWnd)
        {
            var p = SystemControl.GetProcessByHandle(hWnd);
            GameProcess = p ?? throw new ArgumentException("Не удалось получить игровой процесс через дескриптор.");
            GameProcessName = GameProcess.ProcessName;
            GameProcessId = GameProcess.Id;

            DisplaySize = PrimaryScreen.WorkingArea;
            DesktopRectArea = new DesktopRegion();

            // минимизация суждений
            if (User32.IsIconic(hWnd))
            {
                throw new ArgumentException("Окно игры нельзя свернуть");
            }

            // Обратите внимание, что площадь скриншота должна соответствовать реальной площади окна игры.
            // todo После перемещения окна？
            GameScreenSize = SystemControl.GetGameScreenRect(hWnd);
            if (GameScreenSize.Width < 800 || GameScreenSize.Height < 600)
            {
                throw new ArgumentException("Разрешение окна игры не должно быть меньше 800x600 ！");
            }

            // 0.28 изменять，Масштабирование материала невозможно.кПревосходить 1，То есть разрешение при распознавании изображенийбольше, чем 1920x1080 Масштабируйте напрямую в случае
            if (GameScreenSize.Width < 1920)
            {
                ZoomOutMax1080PRatio = GameScreenSize.Width / 1920d;
                AssetScale = ZoomOutMax1080PRatio;
            }
            ScaleTo1080PRatio = GameScreenSize.Width / 1920d; // 1080P в стандартной комплектации

            CaptureAreaRect = SystemControl.GetCaptureRect(hWnd);
            if (CaptureAreaRect.Width > 1920)
            {
                var scale = CaptureAreaRect.Width / 1920d;
                ScaleMax1080PCaptureRect = new Rect(CaptureAreaRect.X, CaptureAreaRect.Y, 1920, (int)(CaptureAreaRect.Height / scale));
            }
            else
            {
                ScaleMax1080PCaptureRect = new Rect(CaptureAreaRect.X, CaptureAreaRect.Y, CaptureAreaRect.Width, CaptureAreaRect.Height);
            }
        }
    }
}
