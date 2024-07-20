using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area.Converter;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Point = OpenCvSharp.Point;

namespace BetterGenshinImpact.GameTask.Model.Area;

public class ImageRegion : Region
{
    protected Bitmap? _srcBitmap;
    protected Mat? _srcMat;
    protected Mat? _srcGreyMat;

    public Bitmap SrcBitmap
    {
        get
        {
            if (_srcBitmap != null)
            {
                return _srcBitmap;
            }

            if (_srcMat == null)
            {
                throw new Exception("SrcBitmapиSrcMatне может быть пустым одновременно");
            }

            _srcBitmap = _srcMat.ToBitmap();
            return _srcBitmap;
        }
    }

    public Mat SrcMat
    {
        get
        {
            if (_srcMat != null)
            {
                return _srcMat;
            }

            if (_srcBitmap == null)
            {
                throw new Exception("SrcBitmapиSrcMatне может быть пустым одновременно");
            }

            _srcMat = _srcBitmap.ToMat();
            return _srcMat;
        }
    }

    public Mat SrcGreyMat
    {
        get
        {
            _srcGreyMat ??= new Mat();
            Cv2.CvtColor(SrcMat, _srcGreyMat, ColorConversionCodes.BGR2GRAY);
            return _srcGreyMat;
        }
    }

    public ImageRegion(Bitmap bitmap, int x, int y, Region? owner = null, INodeConverter? converter = null) : base(x, y, bitmap.Width, bitmap.Height, owner, converter)
    {
        _srcBitmap = bitmap;
    }

    public ImageRegion(Mat mat, int x, int y, Region? owner = null, INodeConverter? converter = null) : base(x, y, mat.Width, mat.Height, owner, converter)
    {
        _srcMat = mat;
    }

    private bool HasImage()
    {
        return _srcBitmap != null || _srcMat != null;
    }

    /// <summary>
    /// обрезанный вывод
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public ImageRegion DeriveCrop(int x, int y, int w, int h)
    {
        return new ImageRegion(new Mat(SrcMat, new Rect(x, y, w, h)), x, y, this, new TranslationConverter(x, y));
    }

    public ImageRegion DeriveCrop(Rect rect)
    {
        return DeriveCrop(rect.X, rect.Y, rect.Width, rect.Height);
    }

    // public ImageRegion Derive(Mat mat, int x, int y)
    // {
    //     return new ImageRegion(mat, x, y, this, new TranslationConverter(x, y));
    // }

    /// <summary>
    /// Найдите лучший объект идентификации в этой области
    /// Или укажите район
    /// соответствовать
    /// RecognitionTypes.TemplateMatch
    /// RecognitionTypes.OcrMatch
    /// идентифицировать
    /// RecognitionTypes.Ocr
    /// </summary>
    /// <param name="ro"></param>
    /// <param name="successAction">Что делать после того, как вы успешно его нашли</param>
    /// <param name="failAction">Что делать после неудачи</param>
    /// <returns>Верните лучшееидентифицироватьрезультатRectArea</returns>
    /// <exception cref="Exception"></exception>
    public Region Find(RecognitionObject ro, Action<Region>? successAction = null, Action? failAction = null)
    {
        if (!HasImage())
        {
            throw new Exception("В текущем объекте нет изображения.，не могу завершить Find действовать");
        }

        if (ro == null)
        {
            throw new Exception("идентифицироватьОбъект не может бытьnull");
        }

        if (RecognitionTypes.TemplateMatch.Equals(ro.RecognitionType))
        {
            Mat roi;
            Mat? template;
            if (ro.Use3Channels)
            {
                template = ro.TemplateImageMat;
                roi = SrcMat;
                Cv2.CvtColor(roi, roi, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                template = ro.TemplateImageGreyMat;
                roi = SrcGreyMat;
            }

            if (template == null)
            {
                throw new Exception($"[TemplateMatch]идентифицироватьобъект{ro.Name}Изображение шаблона не может бытьnull");
            }

            if (ro.RegionOfInterest != Rect.Empty)
            {
                // TODO roi Его можно кэшировать
                if (!(0 <= ro.RegionOfInterest.X && 0 <= ro.RegionOfInterest.Width && ro.RegionOfInterest.X + ro.RegionOfInterest.Width <= roi.Cols
                      && 0 <= ro.RegionOfInterest.Y && 0 <= ro.RegionOfInterest.Height && ro.RegionOfInterest.Y + ro.RegionOfInterest.Height <= roi.Rows))
                {
                    TaskControl.Logger.LogError("Какое увеличение требуется?{W1}x{H1}Найдите шаблоны в,имя：{Name},ROIРасположение{X2}x{Y2},область{H2}x{W2},Переполнение границы！", roi.Width, roi.Height, ro.Name, ro.RegionOfInterest.X, ro.RegionOfInterest.Y, ro.RegionOfInterest.Width, ro.RegionOfInterest.Height);
                }
                roi = new Mat(roi, ro.RegionOfInterest);
            }

            var p = MatchTemplateHelper.MatchTemplate(roi, template, ro.TemplateMatchMode, ro.MaskMat, ro.Threshold);
            if (p != new Point())
            {
                var newRa = Derive(p.X + ro.RegionOfInterest.X, p.Y + ro.RegionOfInterest.Y, template.Width, template.Height);
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    newRa.DrawSelf(ro.Name, ro.DrawOnWindowPen);
                }

                successAction?.Invoke(newRa);
                return newRa;
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return new Region();
            }
        }
        else if (RecognitionTypes.OcrMatch.Equals(ro.RecognitionType))
        {
            if (ro.AllContainMatchText.Count == 0 && ro.OneContainMatchText.Count == 0 && ro.RegexMatchText.Count == 0)
            {
                throw new Exception($"[OCR]идентифицироватьобъект{ro.Name}изсоответствоватьТекст не может быть полностью пустым");
            }

            var roi = SrcGreyMat;
            if (ro.RegionOfInterest != Rect.Empty)
            {
                roi = new Mat(SrcGreyMat, ro.RegionOfInterest);
            }

            var result = OcrFactory.Paddle.OcrResult(roi);
            var text = StringUtils.RemoveAllSpace(result.Text);
            // Замените возможно ошибочный текст
            foreach (var entry in ro.ReplaceDictionary)
            {
                foreach (var replaceStr in entry.Value)
                {
                    text = text.Replace(entry.Key, replaceStr);
                }
            }

            int successContainCount = 0, successRegexCount = 0;
            bool successOneContain = false;
            // изсоответствовать Успешно, если вы включите их все
            foreach (var s in ro.AllContainMatchText)
            {
                if (text.Contains(s))
                {
                    successContainCount++;
                }
            }

            // изсоответствовать Успех, если вы включите один
            foreach (var s in ro.OneContainMatchText)
            {
                if (text.Contains(s))
                {
                    successOneContain = true;
                    break;
                }
            }

            // обычныйсоответствовать
            foreach (var re in ro.RegexMatchText)
            {
                if (Regex.IsMatch(text, re))
                {
                    successRegexCount++;
                }
            }

            if (successContainCount == ro.AllContainMatchText.Count
                && successRegexCount == ro.RegexMatchText.Count
                && (ro.OneContainMatchText.Count == 0 || successOneContain))
            {
                var newRa = Derive(ro.RegionOfInterest);
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    // рисоватьOCRидентифицировать到изобласть
                    var drawList = result.Regions.Select(item => this.ToRectDrawable(item.Rect.BoundingRect() + ro.RegionOfInterest.Location, ro.Name, ro.DrawOnWindowPen)).ToList();
                    VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, drawList);
                }

                successAction?.Invoke(newRa);
                return newRa;
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return new Region();
            }
        }
        else if (RecognitionTypes.Ocr.Equals(ro.RecognitionType) || RecognitionTypes.ColorRangeAndOcr.Equals(ro.RecognitionType))
        {
            Mat roi;
            if (RecognitionTypes.ColorRangeAndOcr.Equals(ro.RecognitionType))
            {
                roi = SrcMat;
                if (ro.RegionOfInterest != Rect.Empty)
                {
                    roi = new Mat(SrcMat, ro.RegionOfInterest);
                }
                roi = roi.Clone();
                if (ro.ColorConversionCode != ColorConversionCodes.BGRA2BGR)
                {
                    Cv2.CvtColor(roi, roi, ro.ColorConversionCode);
                }
                Cv2.InRange(roi, ro.LowerColor, ro.UpperColor, roi);
            }
            else
            {
                roi = SrcGreyMat;
                if (ro.RegionOfInterest != Rect.Empty)
                {
                    roi = new Mat(SrcGreyMat, ro.RegionOfInterest);
                }
            }

            var result = OcrFactory.Paddle.OcrResult(roi);
            var text = StringUtils.RemoveAllSpace(result.Text);

            if (!string.IsNullOrEmpty(text))
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    // рисоватьOCRидентифицировать到изобласть
                    var drawList = result.Regions.Select(item => this.ToRectDrawable(item.Rect.BoundingRect() + ro.RegionOfInterest.Location, ro.Name, ro.DrawOnWindowPen)).ToList();
                    VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, drawList);
                }
                if (ro.RegionOfInterest != Rect.Empty)
                {
                    var newRa = Derive(ro.RegionOfInterest);
                    newRa.Text = text;
                    successAction?.Invoke(newRa);
                    return newRa;
                }
                else
                {
                    this.Text = text;
                    successAction?.Invoke(this);
                    return this;
                }
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return new Region();
            }
        }
        else
        {
            throw new Exception($"ImageRegion不支持изидентифицироватьтип{ro.RecognitionType}");
        }
    }

    /// <summary>
    /// В этомобластьПоиск внутриидентифицироватьобъект
    /// Вернуть все найденные результаты
    /// Поддерживает только:
    /// RecognitionTypes.TemplateMatch
    /// RecognitionTypes.Ocr
    /// </summary>
    /// <param name="ro"></param>
    /// <param name="successAction">Что делать после того, как вы успешно его нашли</param>
    /// <param name="failAction">Что делать после неудачи</param>
    /// <returns>Без встроенных изображений RectArea List</returns>
    /// <exception cref="Exception"></exception>
    public List<Region> FindMulti(RecognitionObject ro, Action<List<Region>>? successAction = null, Action? failAction = null)
    {
        if (!HasImage())
        {
            throw new Exception("В текущем объекте нет изображения.，не могу завершить Find действовать");
        }

        if (ro == null)
        {
            throw new Exception("идентифицироватьОбъект не может бытьnull");
        }

        if (RecognitionTypes.TemplateMatch.Equals(ro.RecognitionType))
        {
            Mat roi;
            Mat? template;
            if (ro.Use3Channels)
            {
                template = ro.TemplateImageMat;
                roi = SrcMat;
                Cv2.CvtColor(roi, roi, ColorConversionCodes.BGRA2BGR);
            }
            else
            {
                template = ro.TemplateImageGreyMat;
                roi = SrcGreyMat;
            }

            if (template == null)
            {
                throw new Exception($"[TemplateMatch]идентифицироватьобъект{ro.Name}Изображение шаблона не может бытьnull");
            }

            if (ro.RegionOfInterest != Rect.Empty)
            {
                roi = new Mat(roi, ro.RegionOfInterest);
            }

            var rectList = MatchTemplateHelper.MatchOnePicForOnePic(roi, template, ro.TemplateMatchMode, ro.MaskMat, ro.Threshold);
            if (rectList.Count > 0)
            {
                var resRaList = rectList.Select(r => this.Derive(r + ro.RegionOfInterest.Location)).ToList();

                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, resRaList.Select(ra => ra.SelfToRectDrawable(ro.Name)).ToList());
                }

                successAction?.Invoke(resRaList);
                return resRaList;
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return [];
            }
        }
        else if (RecognitionTypes.Ocr.Equals(ro.RecognitionType))
        {
            var roi = SrcGreyMat;
            if (ro.RegionOfInterest != Rect.Empty)
            {
                roi = new Mat(SrcGreyMat, ro.RegionOfInterest);
            }

            var result = OcrFactory.Paddle.OcrResult(roi);

            if (result.Regions.Length > 0)
            {
                var resRaList = result.Regions.Select(r =>
                {
                    var newRa = this.Derive(r.Rect.BoundingRect() + ro.RegionOfInterest.Location);
                    newRa.Text = r.Text;
                    return newRa;
                }).ToList();
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    // рисоватьOCRидентифицировать到изобласть
                    var drawList = result.Regions.Select(item => this.ToRectDrawable(item.Rect.BoundingRect() + ro.RegionOfInterest.Location, ro.Name, ro.DrawOnWindowPen)).ToList();
                    VisionContext.Instance().DrawContent.PutOrRemoveRectList(ro.Name, drawList);
                }

                successAction?.Invoke(resRaList);
                return resRaList;
            }
            else
            {
                if (ro.DrawOnWindow && !string.IsNullOrEmpty(ro.Name))
                {
                    VisionContext.Instance().DrawContent.RemoveRect(ro.Name);
                }

                failAction?.Invoke();
                return [];
            }
        }
        else
        {
            throw new Exception($"RectAreaМного целейидентифицировать不支持изидентифицироватьтип{ro.RecognitionType}");
        }
    }

    public new void Dispose()
    {
        _srcGreyMat?.Dispose();
        _srcMat?.Dispose();
        _srcBitmap?.Dispose();
    }
}
