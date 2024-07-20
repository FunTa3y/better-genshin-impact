// using System;
// using System.Diagnostics;
// using System.Linq;
// using BetterGenshinImpact.Core.Recognition;
// using BetterGenshinImpact.Core.Recognition.OpenCv;
// using BetterGenshinImpact.Core.Simulator;
// using BetterGenshinImpact.GameTask.Common.Element.Assets;
// using BetterGenshinImpact.GameTask.Common.Map;
// using BetterGenshinImpact.GameTask.Model.Area;
// using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
// using Microsoft.Extensions.Logging;
// using OpenCvSharp;
// using Vanara.PInvoke;
// using static BetterGenshinImpact.GameTask.Common.TaskControl;
//
// namespace BetterGenshinImpact.GameTask.Common.BgiVision;
//
// /// <summary>
// /// задача передачи
// /// </summary>
// public static partial class Bv
// {
//     private static readonly Random _rd = new Random();
//
//     /// <summary>
//     /// Телепортируйтесь к ближайшей точке телепорта с указанными координатами через большую карту.，Затем переместите в указанные координаты
//     /// </summary>
//     /// <param name="tpX"></param>
//     /// <param name="tpY"></param>
//     public static void Tp(double tpX, double tpY)
//     {
//         // Получить местоположение ближайшей точки телепорта
//         var (x, y) = GetRecentlyTpPoint(tpX, tpY);
//         Logger.LogInformation("({TpX},{TpY}) ближайшее место телепорта ({X},{Y})", tpX, tpY, x, y);
//
//         // M Открыть карту, чтобы определить текущее местоположение，Центральная точка — это текущая позиция
//         Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_M);
//
//         Sleep(1000);
//
//         // Вычислите, к какой центральной точке точка телепорта находится ближе всего после переключения карты.，Переключиться на эту карту
//         SwitchRecentlyCountryMap(x, y);
//
//         // Переместите карту в указанное место точки телепорта.
//         // Debug.WriteLine("Переместите карту в указанное место точки телепорта.");
//         // MoveMapTo(x, y);
//
//         // Нажмите после расчета координат
//         var bigMapInAllMapRect = GetBigMapRect();
//         while (!bigMapInAllMapRect.Contains((int)x, (int)y))
//         {
//             Debug.WriteLine($"({x},{y}) Не здесь {bigMapInAllMapRect} Внутри，продолжай двигаться");
//             Logger.LogInformation("Точка телепортацииНе здесьТекущий большой диапазон картВнутри，продолжай двигаться");
//             MoveMapTo(x, y);
//             bigMapInAllMapRect = GetBigMapRect();
//         }
//
//         // Debug.WriteLine($"({x},{y}) существовать {bigMapInAllMapRect} Внутри，посчитай этосуществоватьформаВнутриизРасположение");
//         // Обратите внимание, что началом этой координаты является точка в центральной области.，Итак, нам нужно преобразовать координаты щелчка（Координата щелчка — это система координат, в которой верхний левый угол является началом координат.），Не могу просто увеличить
//         var (picX, picY) = MapCoordinate.GameToMain2048(x, y);
//         var picRect = MapCoordinate.GameToMain2048(bigMapInAllMapRect);
//         Debug.WriteLine($"({picX},{picY}) существовать {picRect} Внутри，посчитай этосуществоватьформаВнутриизРасположение");
//         var captureRect = TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect;
//         var clickX = (int)((picX - picRect.X) / picRect.Width * captureRect.Width);
//         var clickY = (int)((picY - picRect.Y) / picRect.Height * captureRect.Height);
//         Logger.LogInformation("Нажмите на точку телепорта：({X},{Y})", clickX, clickY);
//         using var ra = GetRectAreaFromDispatcher();
//         ra.ClickTo(clickX, clickY);
//
//         // Запустить функцию быстрой передачи
//     }
//
//     /// <summary>
//     /// Переместите карту в указанное место точки телепорта.
//     /// Он может двигаться неправильно，Итак, вы можете повторить этот метод
//     /// </summary>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     public static void MoveMapTo(double x, double y)
//     {
//         var bigMapCenterPoint = GetPositionFromBigMap();
//         // движущаяся частьВнутриСмещение движения при проверке емкости
//         var (xOffset, yOffset) = (x - bigMapCenterPoint.X, y - bigMapCenterPoint.Y);
//
//         var diffMouseX = 100; // расстояние перемещается каждый раз
//         if (xOffset < 0)
//         {
//             diffMouseX = -diffMouseX;
//         }
//
//         var diffMouseY = 100; // расстояние перемещается каждый раз
//         if (yOffset < 0)
//         {
//             diffMouseY = -diffMouseY;
//         }
//
//         // Сначала перейдите в случайную точку ближе к центру экрана.，Избегайте недопустимого перемещения карты
//         MouseMoveMapX(diffMouseX);
//         MouseMoveMapY(diffMouseY);
//         var newBigMapCenterPoint = GetPositionFromBigMap();
//         var diffMapX = Math.Abs(newBigMapCenterPoint.X - bigMapCenterPoint.X);
//         var diffMapY = Math.Abs(newBigMapCenterPoint.Y - bigMapCenterPoint.Y);
//         Debug.WriteLine($"Каждый100Расстояние на карте перемещено：({diffMapX},{diffMapY})");
//
//         // 快速移动到目标Точка телепортации所существоватьизобласть
//         if (diffMapX > 10 && diffMapY > 10)
//         {
//             // // Рассчитайте необходимое количество ходов
//             var moveCount = (int)Math.Abs(xOffset / diffMapX); // Округлить Изначально я хотел добавить еще1из，Но однажды его перенесли
//             Debug.WriteLine("XНужно переехатьизчастота：" + moveCount);
//             for (var i = 0; i < moveCount; i++)
//             {
//                 MouseMoveMapX(diffMouseX);
//             }
//
//             moveCount = (int)Math.Abs(yOffset / diffMapY); // Округлить Изначально я хотел добавить еще1из，Но однажды его перенесли
//             Debug.WriteLine("YНужно переехатьизчастота：" + moveCount);
//             for (var i = 0; i < moveCount; i++)
//             {
//                 MouseMoveMapY(diffMouseY);
//             }
//         }
//     }
//
//     public static void MouseMoveMapX(int dx)
//     {
//         var moveUnit = dx > 0 ? 20 : -20;
//         GameCaptureRegion.GameRegionMove((rect, _) => (rect.Width / 2d + _rd.Next(-rect.Width / 6, rect.Width / 6), rect.Height / 2d + _rd.Next(-rect.Height / 6, rect.Height / 6)));
//         Simulation.SendInput.Mouse.LeftButtonDown().Sleep(200);
//         for (var i = 0; i < dx / moveUnit; i++)
//         {
//             Simulation.SendInput.Mouse.MoveMouseBy(moveUnit, 0).Sleep(60); // 60 Обеспечьте отсутствие инерции
//         }
//
//         Simulation.SendInput.Mouse.LeftButtonUp().Sleep(200);
//     }
//
//     public static void MouseMoveMapY(int dy)
//     {
//         var moveUnit = dy > 0 ? 20 : -20;
//         GameCaptureRegion.GameRegionMove((rect, _) => (rect.Width / 2d + _rd.Next(-rect.Width / 6, rect.Width / 6), rect.Height / 2d + _rd.Next(-rect.Height / 6, rect.Height / 6)));
//         Simulation.SendInput.Mouse.LeftButtonDown().Sleep(200);
//         // Карта воздействия Геншинасуществоватьнебольшой диапазонВнутриПеремещение недействительно.из，Так что просто переместите его，Так что определенно двигайтесь меньше раз
//         for (var i = 0; i < dy / moveUnit; i++)
//         {
//             Simulation.SendInput.Mouse.MoveMouseBy(0, moveUnit).Sleep(60);
//         }
//
//         Simulation.SendInput.Mouse.LeftButtonUp().Sleep(200);
//     }
//
//     public static Point GetPositionFromBigMap()
//     {
//         var bigMapRect = GetBigMapRect();
//         Debug.WriteLine("Преобразование местоположения на карте в игровые координаты：" + bigMapRect);
//         var bigMapCenterPoint = bigMapRect.GetCenterPoint();
//         Debug.WriteLine("Координаты центра карты：" + bigMapCenterPoint);
//         return bigMapCenterPoint;
//     }
//
//     public static Rect GetBigMapRect()
//     {
//         // ОпределитьсуществоватьИнтерфейс карты
//         using var ra = GetRectAreaFromDispatcher();
//         using var mapScaleButtonRa = ra.Find(QuickTeleportAssets.Instance.MapScaleButtonRo);
//         if (mapScaleButtonRa.IsExist())
//         {
//             var rect = BigMap.Instance.GetBigMapPositionByFeatureMatch(ra.SrcGreyMat);
//             Debug.WriteLine("Определите большую картусуществовать全地图Расположение矩形：" + rect);
//             const int s = 4 * 2; // относительно1024Делать4Увеличить
//             return MapCoordinate.Main2048ToGame(new Rect(rect.X * s, rect.Y * s, rect.Width * s, rect.Height * s));
//         }
//         else
//         {
//             throw new InvalidOperationException("текущийНе здесьИнтерфейс карты");
//         }
//     }
//
//     /// <summary>
//     /// Получить местоположение ближайшей точки телепорта
//     /// </summary>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     /// <returns></returns>
//     public static (int x, int y) GetRecentlyTpPoint(double x, double y)
//     {
//         var recentX = 0;
//         var recentY = 0;
//         var minDistance = double.MaxValue;
//         foreach (var tpPosition in MapAssets.Instance.TpPositions)
//         {
//             var distance = Math.Sqrt(Math.Pow(tpPosition.X - x, 2) + Math.Pow(tpPosition.Y - y, 2));
//             if (distance < minDistance)
//             {
//                 minDistance = distance;
//                 recentX = (int)Math.Round(tpPosition.X);
//                 recentY = (int)Math.Round(tpPosition.Y);
//             }
//         }
//
//         return (recentX, recentY);
//     }
//
//     public static void SwitchRecentlyCountryMap(double x, double y)
//     {
//         var bigMapCenterPoint = GetPositionFromBigMap();
//         Logger.LogInformation("Определить текущее местоположение：{Pos}", bigMapCenterPoint);
//
//         var minDistance = Math.Sqrt(Math.Pow(bigMapCenterPoint.X - x, 2) + Math.Pow(bigMapCenterPoint.Y - y, 2));
//         var minCountry = "Текущее местоположение";
//         foreach (var (country, position) in MapAssets.Instance.CountryPositions)
//         {
//             var distance = Math.Sqrt(Math.Pow(position[0] - x, 2) + Math.Pow(position[1] - y, 2));
//             if (distance < minDistance)
//             {
//                 minDistance = distance;
//                 minCountry = country;
//             }
//         }
//
//         Logger.LogInformation("离目标Точка телепортации最近изобласть是：{Country}", minCountry);
//         if (minCountry != "Текущее местоположение")
//         {
//             GameCaptureRegion.GameRegionClick((rect, scale) => (rect.Width - 160 * scale, rect.Height - 60 * scale));
//             Sleep(200, _taskParam.Cts);
//             var ra = GetRectAreaFromDispatcher();
//             var list = ra.FindMulti(new RecognitionObject
//             {
//                 RecognitionType = RecognitionTypes.Ocr,
//                 RegionOfInterest = new Rect(ra.Width / 2, 0, ra.Width / 2, ra.Height)
//             });
//             list.FirstOrDefault(r => r.Text.Contains(minCountry))?.Click();
//             Logger.LogInformation("переключиться на регион：{Country}", minCountry);
//             Sleep(500, _taskParam.Cts);
//         }
//     }
//
//     public static void Tp(string name)
//     {
//         // Телепортируйтесь в указанную точку телепорта через большую карту.
//     }
//
//     public static void TpByF1(string name)
//     {
//         // Телепортироваться в указанную точку телепортации
//     }
// }
