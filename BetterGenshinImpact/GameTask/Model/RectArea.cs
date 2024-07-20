// using BetterGenshinImpact.Core.Recognition;
// using BetterGenshinImpact.Core.Recognition.OCR;
// using BetterGenshinImpact.Core.Recognition.OpenCv;
// using BetterGenshinImpact.Helpers;
// using BetterGenshinImpact.Helpers.Extensions;
// using BetterGenshinImpact.View.Drawable;
// using OpenCvSharp;
// using OpenCvSharp.Extensions;
// using Sdcb.PaddleOCR;
// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Linq;
// using System.Text.RegularExpressions;
// using Point = OpenCvSharp.Point;
//
// namespace BetterGenshinImpact.GameTask.Model;
//
// /// <summary>
// /// Прямоугольная область или точка на экране.，Удобен для идентификации и преобразования системы координат.
// /// Общие уровни следующие:：
// /// рабочий стол -> область захвата окна -> Прямоугольная область внутри окна -> Распознанная область изображения внутри прямоугольной области
// /// </summary>
// [Serializable]
// public class RectArea : IDisposable
// {
//     /// <summary>
//     /// Текущий уровень системы координат
//     /// рабочий стол = 0
//     /// окно игры = 1
//     /// Верхний слой должен бытьрабочий стол
//     /// Desktop -> GameCaptureArea -> Part -> ?
//     /// </summary>
//     public int CoordinateLevelNum { get; set; } = 0;
//
//     public int X { get; set; }
//     public int Y { get; set; }
//     public int Width { get; set; }
//     public int Height { get; set; }
//
//     public RectArea? Owner { get; set; }
//
//     private Bitmap? _srcBitmap;
//     private Mat? _srcMat;
//     private Mat? _srcGreyMat;
//
//     public Bitmap SrcBitmap
//     {
//         get
//         {
//             if (_srcBitmap != null)
//             {
//                 return _srcBitmap;
//             }
//
//             if (_srcMat == null)
//             {
//                 throw new Exception("SrcBitmapиSrcMatне может быть пустым одновременно");
//             }
//
//             _srcBitmap = _srcMat.ToBitmap();
//             return _srcBitmap;
//         }
//     }
//
//     public Mat SrcMat
//     {
//         get
//         {
//             if (_srcMat != null)
//             {
//                 return _srcMat;
//             }
//
//             if (_srcBitmap == null)
//             {
//                 throw new Exception("SrcBitmapиSrcMatне может быть пустым одновременно");
//             }
//
//             _srcMat = _srcBitmap.ToMat();
//             return _srcMat;
//         }
//     }
//
//     public Mat SrcGreyMat
//     {
//         get
//         {
//             _srcGreyMat ??= new Mat();
//             Cv2.CvtColor(SrcMat, _srcGreyMat, ColorConversionCodes.BGR2GRAY);
//             return _srcGreyMat;
//         }
//     }
//
//     /// <summary>
//     /// магазинOCRРаспознанный текст результата
//     /// </summary>
//     public string Text { get; set; } = string.Empty;
//
//     public RectArea()
//     {
//     }
//
//     public RectArea(int x, int y, int width, int height, RectArea? owner = null)
//     {
//         X = x;
//         Y = y;
//         Width = width;
//         Height = height;
//         Owner = owner;
//         CoordinateLevelNum = owner?.CoordinateLevelNum + 1 ?? 0;
//     }
//
//     public RectArea(Bitmap bitmap, int x, int y, RectArea? owner = null) : this(x, y, 0, 0, owner)
//     {
//         _srcBitmap = bitmap;
//         Width = bitmap.Width;
//         Height = bitmap.Height;
//     }
//
//     public RectArea(Mat mat, int x, int y, RectArea? owner = null) : this(x, y, 0, 0, owner)
//     {
//         _srcMat = mat;
//         Width = mat.Width;
//         Height = mat.Height;
//     }
//
//     public RectArea(Mat mat, Point p, RectArea? owner = null) : this(mat, p.X, p.Y, owner)
//     {
//     }
//
//     public RectArea(Rect rect, RectArea? owner = null) : this(rect.X, rect.Y, rect.Width, rect.Height, owner)
//     {
//     }
//
//     //public RectArea(Mat mat, RectArea? owner = null)
//     //{
//     //    _srcMat = mat;
//     //    X = 0;
//     //    Y = 0;
//     //    Width = mat.Width;
//     //    Height = mat.Height;
//     //    Owner = owner;
//     //    CoordinateLevelNum = owner?.CoordinateLevelNum + 1 ?? 0;
//     //}
//
//     public Rect ConvertRelativePositionTo(int coordinateLevelNum)
//     {
//         int newX = X, newY = Y;
//         var father = Owner;
//         while (true)
//         {
//             if (father == null)
//             {
//                 throw new Exception("Соответствующая система координат не найдена");
//             }
//
//             if (father.CoordinateLevelNum == coordinateLevelNum)
//             {
//                 break;
//             }
//
//             newX += father.X;
//             newY += father.Y;
//
//             father = father.Owner;
//         }
//
//         return new Rect(newX, newY, Width, Height);
//     }
//
//     public Rect ConvertRelativePositionToDesktop()
//     {
//         return ConvertRelativePositionTo(0);
//     }
//
//     public Rect ConvertRelativePositionToCaptureArea()
//     {
//         return ConvertRelativePositionTo(1);
//     }
//
//     public Rect ToRect()
//     {
//         return new Rect(X, Y, Width, Height);
//     }
//
//     public bool PositionIsInDesktop()
//     {
//         return CoordinateLevelNum == 0;
//     }
//
//     public bool IsEmpty()
//     {
//         return Width == 0 && Height == 0 && X == 0 && Y == 0;
//     }
//
//     /// <summary>
//     /// Семантическая упаковка
//     /// </summary>
//     /// <returns></returns>
//     public bool IsExist()
//     {
//         return !IsEmpty();
//     }
//
//     public bool HasImage()
//     {
//         return _srcBitmap != null || _srcMat != null;
//     }
//
//     /// <summary>
//     /// Найдите лучший объект идентификации в этой области
//     /// </summary>
//     /// <param name="ro"></param>
//     /// <param name="successAction">Что делать после того, как вы успешно его нашли</param>
//     /// <param name="failAction">Что делать после неудачи</param>
//     /// <returns>Вернуть лучший результат распознаванияRectArea</returns>
//     /// <exception cref="Exception"></exception>
//     public RectArea Find(RecognitionObject ro, Action<RectArea>? successAction = null, Action? failAction = null)
//     {
//         if (!HasImage())
//         {
//             throw new Exception("В текущем объекте нет изображения.，не могу завершить Find действовать");
//         }
//
//         if (ro == null)
//         {
//             throw new Exception("Объект идентификации не может бытьnull");
//         }
//
//         if (RecognitionTypes.TemplateMatch.Equals(ro.RecognitionType))
//         {
//             Mat roi;
//             Mat? template;
//             if (ro.Use3Channels)
//             {
//                 template = ro.TemplateImageMat;
//                 roi = SrcMat;
//                 Cv2.CvtColor(roi, roi, ColorConversionCodes.BGRA2BGR);
//             }
//             else
//             {
//                 template = ro.TemplateImageGreyMat;
//                 roi = SrcGreyMat;
//             }
//
//             if (template == null)
//             {
//                 throw new Exception($"[TemplateMatch]Определить объекты{ro.Name}Изображение шаблона не может бытьnull");
//             }
//
//             if (ro.RegionOfInterest != Rect.Empty)
//             {
//                 // TODO roi Его можно кэшировать
//                 // if (!(0 <= ro.RegionOfInterest.X && 0 <= ro.RegionOfInterest.Width && ro.RegionOfInterest.X + ro.RegionOfInterest.Width <= roi.Cols
//                 //       && 0 <= ro.RegionOfInterest.Y && 0 <= ro.RegionOfInterest.Height && ro.RegionOfInterest.Y + ro.RegionOfInterest.Height <= roi.Rows))
//                 // {
//                 //     Logger.LogError("входное изображение{W1}x{H1},шаблонROIРасположение{X2}x{Y2},область{H2}x{W2},Переполнение границы！", roi.Width, roi.Height, ro.RegionOfInterest.X, ro.RegionOfInterest.Y, ro.RegionOfInterest.Width, ro.RegionOfInterest.Height);
//                 // }
//                 roi = new Mat(roi, ro.RegionOfInterest);
//             }
//
//             var p = MatchTemplateHelper.MatchTemplate(roi, template, ro.TemplateMatchMode, ro.MaskMat, ro.Threshold);
//             if (p != new Point())
//             {
//                 var newRa = new RectArea(template.Clone(), p.X + ro.RegionOfInterest.X, p.Y + ro.RegionOfInterest.Y, this);
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.PutRect(ro.Name, newRa
//                         .ConvertRelativePositionToCaptureArea()
//                         .ToRectDrawable(ro.DrawOnWindowPen, ro.Name));
//                 }
//
//                 successAction?.Invoke(newRa);
//                 return newRa;
//             }
//             else
//             {
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
//                 }
//
//                 failAction?.Invoke();
//                 return new RectArea();
//             }
//         }
//         else if (RecognitionTypes.OcrMatch.Equals(ro.RecognitionType))
//         {
//             if (ro.AllContainMatchText.Count == 0 && ro.OneContainMatchText.Count == 0 && ro.RegexMatchText.Count == 0)
//             {
//                 throw new Exception($"[OCR]Определить объекты{ro.Name}Соответствующий текст не может быть пустым.");
//             }
//
//             var roi = SrcGreyMat;
//             if (ro.RegionOfInterest != Rect.Empty)
//             {
//                 roi = new Mat(SrcGreyMat, ro.RegionOfInterest);
//             }
//
//             var result = OcrFactory.Paddle.OcrResult(roi);
//             var text = StringUtils.RemoveAllSpace(result.Text);
//             // Замените возможно ошибочный текст
//             foreach (var entry in ro.ReplaceDictionary)
//             {
//                 foreach (var replaceStr in entry.Value)
//                 {
//                     text = text.Replace(entry.Key, replaceStr);
//                 }
//             }
//
//             int successContainCount = 0, successRegexCount = 0;
//             bool successOneContain = false;
//             // содержит совпадения Успешно, если вы включите их все
//             foreach (var s in ro.AllContainMatchText)
//             {
//                 if (text.Contains(s))
//                 {
//                     successContainCount++;
//                 }
//             }
//
//             // содержит совпадения Успех, если вы включите один
//             foreach (var s in ro.OneContainMatchText)
//             {
//                 if (text.Contains(s))
//                 {
//                     successOneContain = true;
//                     break;
//                 }
//             }
//
//             // Обычный матч
//             foreach (var re in ro.RegexMatchText)
//             {
//                 if (Regex.IsMatch(text, re))
//                 {
//                     successRegexCount++;
//                 }
//             }
//
//             if (successContainCount == ro.AllContainMatchText.Count
//                 && successRegexCount == ro.RegexMatchText.Count
//                 && (ro.OneContainMatchText.Count == 0 || successOneContain))
//             {
//                 var newRa = new RectArea(roi, X + ro.RegionOfInterest.X, Y + ro.RegionOfInterest.Y, this);
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, result.ToRectDrawableListOffset(ro.RegionOfInterest.X, ro.RegionOfInterest.Y));
//                 }
//
//                 successAction?.Invoke(newRa);
//                 return newRa;
//             }
//             else
//             {
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
//                 }
//
//                 failAction?.Invoke();
//                 return new RectArea();
//             }
//         }
//         else
//         {
//             throw new Exception($"RectAreaПожалуйста, начните сначала{ro.RecognitionType}");
//         }
//     }
//
//     /// <summary>
//     /// В этомобластьПоиск внутриОпределить объекты
//     /// Вернуть все найденные результаты
//     /// Поддерживает только:
//     /// RecognitionTypes.TemplateMatch
//     /// RecognitionTypes.Ocr
//     /// </summary>
//     /// <param name="ro"></param>
//     /// <param name="successAction">Что делать после того, как вы успешно его нашли</param>
//     /// <param name="failAction">Что делать после неудачи</param>
//     /// <returns>Без встроенных изображений RectArea List</returns>
//     /// <exception cref="Exception"></exception>
//     public List<RectArea> FindMulti(RecognitionObject ro, Action<List<RectArea>>? successAction = null, Action? failAction = null)
//     {
//         if (!HasImage())
//         {
//             throw new Exception("В текущем объекте нет изображения.，не могу завершить Find действовать");
//         }
//
//         if (ro == null)
//         {
//             throw new Exception("Объект идентификации не может бытьnull");
//         }
//
//         if (RecognitionTypes.TemplateMatch.Equals(ro.RecognitionType))
//         {
//             Mat roi;
//             Mat? template;
//             if (ro.Use3Channels)
//             {
//                 template = ro.TemplateImageMat;
//                 roi = SrcMat;
//                 Cv2.CvtColor(roi, roi, ColorConversionCodes.BGRA2BGR);
//             }
//             else
//             {
//                 template = ro.TemplateImageGreyMat;
//                 roi = SrcGreyMat;
//             }
//
//             if (template == null)
//             {
//                 throw new Exception($"[TemplateMatch]Определить объекты{ro.Name}Изображение шаблона не может бытьnull");
//             }
//
//             if (ro.RegionOfInterest != Rect.Empty)
//             {
//                 // TODO roi Его можно кэшировать
//                 roi = new Mat(roi, ro.RegionOfInterest);
//             }
//
//             var rectList = MatchTemplateHelper.MatchOnePicForOnePic(roi, template, ro.TemplateMatchMode, ro.MaskMat, ro.Threshold);
//             if (rectList.Count > 0)
//             {
//                 var resRaList = rectList.Select(r => this.Derive(r + new Point(ro.RegionOfInterest.X, ro.RegionOfInterest.Y))).ToList();
//
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name,
//                         resRaList.Select(ra => ra.ConvertRelativePositionToCaptureArea()
//                             .ToRectDrawable(ro.DrawOnWindowPen, ro.Name)).ToList());
//                 }
//
//                 successAction?.Invoke(resRaList);
//                 return resRaList;
//             }
//             else
//             {
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
//                 }
//
//                 failAction?.Invoke();
//                 return [];
//             }
//         }
//         else if (RecognitionTypes.Ocr.Equals(ro.RecognitionType))
//         {
//             var roi = SrcGreyMat;
//             if (ro.RegionOfInterest != Rect.Empty)
//             {
//                 roi = new Mat(SrcGreyMat, ro.RegionOfInterest);
//             }
//
//             var result = OcrFactory.Paddle.OcrResult(roi);
//
//             if (result.Regions.Length > 0)
//             {
//                 var resRaList = result.Regions.Select(r =>
//                 {
//                     var newRa = this.Derive(r.Rect.BoundingRect() + new Point(ro.RegionOfInterest.X, ro.RegionOfInterest.Y));
//                     newRa.Text = r.Text;
//                     return newRa;
//                 }).ToList();
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, result.ToRectDrawableListOffset(ro.RegionOfInterest.X, ro.RegionOfInterest.Y));
//                 }
//
//                 successAction?.Invoke(resRaList);
//                 return resRaList;
//             }
//             else
//             {
//                 if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
//                 {
//                     VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
//                 }
//
//                 failAction?.Invoke();
//                 return [];
//             }
//         }
//         else
//         {
//             throw new Exception($"RectAreaМного целейидентифицироватьПожалуйста, начните сначала{ro.RecognitionType}");
//         }
//     }
//
//     /// <summary>
//     /// оказатьсяОпределить объектыи нажмите на центр
//     /// </summary>
//     /// <param name="ro"></param>
//     /// <returns></returns>
//     public RectArea ClickCenter(RecognitionObject ro)
//     {
//         var ra = Find(ro);
//         if (!ra.IsEmpty())
//         {
//             ra.ClickCenter();
//         }
//
//         return ra;
//     }
//
//     /// <summary>
//     /// Центр щелчка по текущему объекту
//     /// </summary>
//     public void ClickCenter()
//     {
//         // Преобразуйте систему координат врабочий столНажмите еще раз
//         if (CoordinateLevelNum == 0)
//         {
//             ToRect().ClickCenter();
//         }
//         else
//         {
//             ConvertRelativePositionToDesktop().ClickCenter();
//         }
//     }
//
//     /// <summary>
//     /// Кадрирование снимка
//     /// </summary>
//     /// <param name="rect"></param>
//     /// <returns></returns>
//     public RectArea Crop(Rect rect)
//     {
//         return new RectArea(SrcMat[rect], rect.X, rect.Y, this);
//     }
//
//     /// <summary>
//     /// полученныйобласть（Нет фотографий）
//     /// </summary>
//     /// <param name="rect"></param>
//     /// <returns></returns>
//     public RectArea Derive(Rect rect)
//     {
//         return new RectArea(rect, this);
//     }
//
//     /// <summary>
//     /// полученный2x2область（Нет фотографий）
//     /// Удобен для кликов
//     /// </summary>
//     /// <param name="x"></param>
//     /// <param name="y"></param>
//     /// <returns></returns>
//     public RectArea DerivePoint(int x, int y)
//     {
//         return new RectArea(new Rect(x, y, 2, 2), this);
//     }
//
//     /// <summary>
//     /// OCRидентифицировать
//     /// </summary>
//     /// <returns>Все результаты</returns>
//     public PaddleOcrResult OcrResult()
//     {
//         return OcrFactory.Paddle.OcrResult(SrcGreyMat);
//     }
//
//     public void Dispose()
//     {
//         _srcGreyMat?.Dispose();
//         _srcMat?.Dispose();
//         _srcBitmap?.Dispose();
//     }
// }
