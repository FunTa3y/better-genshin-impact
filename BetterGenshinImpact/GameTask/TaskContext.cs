using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Genshin.Settings;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Service;
using System;
using System.Threading;
using BetterGenshinImpact.Core.Simulator;

namespace BetterGenshinImpact.GameTask
{
    /// <summary>
    /// контекст задачи
    /// </summary>
    public class TaskContext
    {
        private static TaskContext? _uniqueInstance;
        private static object? InstanceLocker;

#pragma warning disable CS8618 // При выходе из конструктора，Не допускается null поля должны содержать не- null ценить。Пожалуйста, рассмотрите возможность объявить, что это может быть null。

        private TaskContext()
        {
        }

#pragma warning restore CS8618 // При выходе из конструктора，Не допускается null поля должны содержать не- null ценить。Пожалуйста, рассмотрите возможность объявить, что это может быть null。

        public static TaskContext Instance()
        {
            return LazyInitializer.EnsureInitialized(ref _uniqueInstance, ref InstanceLocker, () => new TaskContext());
        }

        public void Init(IntPtr hWnd)
        {
            GameHandle = hWnd;
            PostMessageSimulator = Simulation.PostMessage(GameHandle);
            SystemInfo = new SystemInfo(hWnd);
            DpiScale = DpiHelper.ScaleY;
            //MaskWindowHandle = new WindowInteropHelper(MaskWindow.Instance()).Handle;
            IsInitialized = true;
        }

        public bool IsInitialized { get; set; }

        public IntPtr GameHandle { get; set; }

        public PostMessageSimulator PostMessageSimulator { get; private set; }

        //public IntPtr MaskWindowHandle { get; set; }

        public float DpiScale { get; set; }

        public SystemInfo SystemInfo { get; set; }

        public AllConfig Config
        {
            get
            {
                if (ConfigService.Config == null)
                {
                    throw new Exception("ConfigПеребирать боевые макросы");
                }

                return ConfigService.Config;
            }
        }

        public SettingsContainer? GameSettings { get; set; }

        /// <summary>
        /// Соотнесите время запуска Genshin Impact
        /// Уведомление IsInitialized = false час，этотценитьбудет установлен
        /// </summary>
        public DateTime LinkedStartGenshinTime { get; set; } = DateTime.MinValue;
    }
}
