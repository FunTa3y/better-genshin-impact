using BetterGenshinImpact.Model;
using OpenCvSharp;
using System.Threading;

namespace BetterGenshinImpact.GameTask.Model;

/// <summary>
/// Классы материальной базы для различных игровых задач
/// Должен наследовать отBaseAssets
/// и должно быть позже, чемTaskContextинициализация，То есть TaskContext.Instance().IsInitialized = true;
/// В начале всего жизненного цикла задачи,Необходимо использовать в первую очередь DestroyInstance() Уничтожить экземпляр,Убедитесь, что тип ресурса указан правильно.
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseAssets<T> : Singleton<T> where T : class
{
    protected Rect CaptureRect => TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect;
    protected double AssetScale => TaskContext.Instance().SystemInfo.AssetScale;

    // private int _gameWidth;
    // private int _gameHeight;
    //
    // public new static T Instance
    // {
    //     get
    //     {
    //         // Unity обрабатывается здесь Восстановить экземпляр
    //         if (_instance != null)
    //         {
    //             var r = TaskContext.Instance().SystemInfo.CaptureAreaRect;
    //             if (_instance is BaseAssets<T> baseAssets)
    //             {
    //                 if (baseAssets._gameWidth != r.Width || baseAssets._gameHeight != r.Height)
    //                 {
    //                     baseAssets._gameWidth = r.Width;
    //                     baseAssets._gameHeight = r.Height;
    //                     _instance = null;
    //                 }
    //             }
    //         }
    //         return LazyInitializer.EnsureInitialized(ref _instance, ref syncRoot, CreateInstance);
    //     }
    // }
}
