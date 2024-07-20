// using BetterGenshinImpact.GameTask;
// using BetterGenshinImpact.Helpers.Extensions;
// using System;
//
// namespace BetterGenshinImpact.Helpers;
//
// /// <summary>
// /// Не рекомендуется
// /// пожалуйста, используйте GameCaptureRegion.GameRegionClick или GameCaptureRegion.GameRegion1080PPosClick заменять
// /// </summary>
// [Obsolete]
// public class ClickOffset
// {
//     public int OffsetX { get; set; }
//     public int OffsetY { get; set; }
//     public double AssetScale { get; set; }
//
//     // public double CaptureAreaScale { get; set; }
//
//     public ClickOffset()
//     {
//         if (!TaskContext.Instance().IsInitialized)
//         {
//             throw new Exception("Пожалуйста, начните сначала");
//         }
//         var captureArea = TaskContext.Instance().SystemInfo.CaptureAreaRect;
//         var assetScale = TaskContext.Instance().SystemInfo.AssetScale;
//         OffsetX = captureArea.X;
//         OffsetY = captureArea.Y;
//         AssetScale = assetScale;
//         // CaptureAreaScale = TaskContext.Instance().SystemInfo.CaptureAreaScale;
//     }
//
//     public ClickOffset(int offsetX, int offsetY, double assetScale)
//     {
//         if (!TaskContext.Instance().IsInitialized)
//         {
//             throw new Exception("Пожалуйста, начните сначала");
//         }
//         // CaptureAreaScale = TaskContext.Instance().SystemInfo.CaptureAreaScale;
//
//         OffsetX = offsetX;
//         OffsetY = offsetY;
//         AssetScale = assetScale;
//     }
//
//     public void Click(int x, int y)
//     {
//         ClickExtension.Click(OffsetX + (int)(x * AssetScale), OffsetY + (int)(y * AssetScale));
//     }
//
//     /// <summary>
//     /// Входx,y Обратите внимание на обработку масштабирования.
//     /// </summary>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     public void ClickWithoutScale(int x, int y)
//     {
//         ClickExtension.Click(OffsetX + x, OffsetY + y);
//     }
//
//     public void Move(int x, int y)
//     {
//         ClickExtension.Move(OffsetX + (int)(x * AssetScale), OffsetY + (int)(y * AssetScale));
//     }
// }
