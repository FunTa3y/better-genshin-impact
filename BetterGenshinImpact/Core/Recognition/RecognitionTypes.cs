namespace BetterGenshinImpact.Core.Recognition;

public enum RecognitionTypes
{
    None,
    TemplateMatch, // соответствие шаблону
    ColorMatch, // подбор цвета
    OcrMatch, // Распознавание и сопоставление текста
    Ocr, // только распознавание текста
    ColorRangeAndOcr, // Извлеките указанный цвет и выполните распознавание текста
    Detect
}
