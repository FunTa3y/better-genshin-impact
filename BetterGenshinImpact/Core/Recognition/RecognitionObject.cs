using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Helpers.Extensions;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BetterGenshinImpact.Core.Recognition;

/// <summary>
///     Определить объекты
/// </summary>
[Serializable]
public class RecognitionObject
{
    public RecognitionTypes RecognitionType { get; set; }

    /// <summary>
    ///     область интересов
    /// </summary>
    public Rect RegionOfInterest { get; set; }

    public string? Name { get; set; }

    #region соответствие шаблону

    /// <summary>
    ///     соответствие шаблонуОбъект(цвет)
    /// </summary>
    public Mat? TemplateImageMat { get; set; }

    /// <summary>
    ///     соответствие шаблонуОбъект(серый)
    /// </summary>
    public Mat? TemplateImageGreyMat { get; set; }

    /// <summary>
    ///     соответствие шаблонупорог。Необязательный，по умолчанию 0.8 。
    /// </summary>
    public double Threshold { get; set; } = 0.8;

    /// <summary>
    ///     использовать или нет 3 согласование каналов。Необязательный，по умолчанию false 。
    /// </summary>
    public bool Use3Channels { get; set; } = false;

    /// <summary>
    ///     соответствие шаблонуалгоритм。Необязательный，по умолчанию CCoeffNormed 。
    ///     https://docs.opencv.org/4.x/df/dfb/group__imgproc__object.html
    /// </summary>
    public TemplateMatchModes TemplateMatchMode { get; set; } = TemplateMatchModes.CCoeffNormed;

    /// <summary>
    ///     Маска шаблона соответствия，Укажите, что определенный цвет на изображении не обязательно должен совпадать.
    ///     при его использовании，Вам необходимо установить цвет фона изображения шаблона на сплошной зеленый.，Прямо сейчас (0, 255, 0)
    /// </summary>
    public bool UseMask { get; set; } = false;

    /// <summary>
    ///     Соответствующие цвета не требуются，по умолчаниюзеленый
    ///     UseMask = true Полезно, когда
    /// </summary>
    public Color MaskColor { get; set; } = Color.FromArgb(0, 255, 0);

    public Mat? MaskMat { get; set; }

    /// <summary>
    ///     Когда матч успешен，Рисовать ли прямоугольную рамку на экране。Необязательный，по умолчанию false 。
    ///     true час Name должно иметь ценность。
    /// </summary>
    public bool DrawOnWindow { get; set; } = false;

    /// <summary>
    ///     DrawOnWindow для true час，Цвет нарисованного прямоугольника。Необязательный，по умолчаниюкрасный。
    /// </summary>
    public Pen DrawOnWindowPen = new(Color.Red, 2);

    /// <summary>
    ///    одинсоответствие шаблонунесколько результатовчасмаксимальное количество совпадений。Необязательный，по умолчанию -1，Прямо сейчасне ограничен。
    /// </summary>
    public int MaxMatchCount { get; set; } = -1;

    public RecognitionObject InitTemplate()
    {
        if (TemplateImageMat != null && TemplateImageGreyMat == null)
        {
            TemplateImageGreyMat = new Mat();
            Cv2.CvtColor(TemplateImageMat, TemplateImageGreyMat, ColorConversionCodes.BGR2GRAY);
        }

        if (UseMask && TemplateImageMat != null && MaskMat == null) MaskMat = OpenCvCommonHelper.CreateMask(TemplateImageMat, MaskColor.ToScalar());
        return this;
    }

    #endregion соответствие шаблону

    #region подбор цвета

    /// <summary>
    ///     подбор цветаСпособ。Прямо сейчас cv::ColorConversionCodes。Необязательный，по умолчанию 4 (RGB)。
    ///     Общие ценности：4 (RGB, 3 ряд), 40 (HSV, 3 ряд), 6 (GRAY, 1 ряд)。
    ///     https://docs.opencv.org/4.x/d8/d01/group__imgproc__color__conversions.html
    /// </summary>
    public ColorConversionCodes ColorConversionCode { get; set; } = ColorConversionCodes.BGR2RGB;

    public Scalar LowerColor { get; set; }
    public Scalar UpperColor { get; set; }

    /// <summary>
    ///     Удовлетворить необходимое количество баллов。Необязательный，по умолчанию 1
    /// </summary>
    public int MatchCount { get; set; } = 1;

    #endregion подбор цвета

    #region OCRраспознавание текста

    /// <summary>
    ///     OCR двигатель。Необязательный，только Paddle。
    /// </summary>
    public OcrEngineTypes OcrEngine { get; set; } = OcrEngineTypes.Paddle;

    /// <summary>
    ///     частьраспознавание текстаНеточные результаты，Сделать замену。Необязательный。
    /// </summary>
    public Dictionary<string, string[]> ReplaceDictionary { get; set; } = new();

    /// <summary>
    ///     содержит совпадения
    ///     Успех достигается только при совпадении нескольких значений.
    ///     В сложных ситуациях используйте следующее обычное сопоставление
    /// </summary>
    public List<string> AllContainMatchText { get; set; } = new();

    /// <summary>
    ///     содержит совпадения
    ///     Соответствие значений считается успешным.
    /// </summary>
    public List<string> OneContainMatchText { get; set; } = new();

    /// <summary>
    ///     Обычный матч
    ///     Успех достигается только при совпадении нескольких значений.
    /// </summary>
    public List<string> RegexMatchText { get; set; } = new();

    #endregion OCRраспознавание текста
}
