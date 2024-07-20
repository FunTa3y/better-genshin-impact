using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.GameTask.AutoSkip.Model;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.View.Drawable;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sdcb.PaddleOCR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BetterGenshinImpact.GameTask.AutoSkip;

/// <summary>
/// Повторное открытие диспетчеризации
///
/// Должен использоваться после завершения отправки разведки.
///
/// В 4.3 Версия устарела
/// </summary>
[Obsolete]
public class ExpeditionTask
{
    private static readonly List<string> ExpeditionCharacterList = new();

    private int _expeditionCount = 0;

    public void Run(CaptureContent content)
    {
        InitConfig();
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;
        ReExplorationGameArea(content);
        for (var i = 0; i <= 4; i++)
        {
            if (_expeditionCount >= 5)
            {
                // Наиболее отправлено5люди
                break;
            }
            else
            {
                content.CaptureRectArea
                    .Derive(new Rect((int)(110 * assetScale), (int)((145 + 70 * i) * assetScale),
                        (int)(60 * assetScale), (int)(33 * assetScale)))
                    .Click();
                TaskControl.Sleep(500);
                ReExplorationGameArea(content);
            }
        }

        TaskControl.Logger.LogInformation("Исследуйте диспетчеризацию：{Text}", "Перепланирование завершено");
        VisionContext.Instance().DrawContent.ClearAll();
    }

    private void InitConfig()
    {
        var str = TaskContext.Instance().Config.AutoSkipConfig.AutoReExploreCharacter;
        if (!string.IsNullOrEmpty(str))
        {
            ExpeditionCharacterList.Clear();
            str = str.Replace("，", ",");
            str.Split(',').ToList().ForEach(x => ExpeditionCharacterList.Add(x.Trim()));
            TaskContext.Instance().Config.AutoSkipConfig.AutoReExploreCharacter = string.Join(",", ExpeditionCharacterList);
        }
    }

    private void ReExplorationGameArea(CaptureContent content)
    {
        var captureRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        for (var i = 0; i < 5; i++)
        {
            var result = CaptureAndOcr(content, new Rect(0, 0, captureRect.Width - (int)(480 * assetScale), captureRect.Height));
            var rect = result.FindRectByText("Приключение завершено");
            // TODO i>1 когда,Ключевые слова можно использовать“Исследуйте диспетчеризациюпредел 4 / 5 ”Определить, завершена ли отправка？
            if (rect != Rect.Empty)
            {
                // НажмитеПриключение завершеноНижелюдиаватар
                content.CaptureRectArea.Derive(new Rect(rect.X, rect.Y + (int)(50 * assetScale), rect.Width, (int)(80 * assetScale))).Click();
                TaskControl.Sleep(100);
                // Сделать снимок экрана заново Найдите и соберите
                result = CaptureAndOcr(content);
                rect = result.FindRectByText("получать");
                if (rect != Rect.Empty)
                {
                    using var ra = content.CaptureRectArea.Derive(rect);
                    ra.Click();
                    //TaskControl.Logger.LogInformation("Исследуйте диспетчеризацию：Нажмите{Text}", "получать");
                    TaskControl.Sleep(200);
                    // НажмитеПродолжить в пустой области
                    ra.Click();
                    TaskControl.Sleep(250);

                    // Выберите роль
                    result = CaptureAndOcr(content);
                    rect = result.FindRectByText("Выберите роль");
                    if (rect != Rect.Empty)
                    {
                        content.CaptureRectArea.Derive(rect).Click();
                        TaskControl.Sleep(400); // подожди анимация
                        var success = SelectCharacter(content);
                        if (success)
                        {
                            _expeditionCount++;
                        }
                    }
                }
                else
                {
                    TaskControl.Logger.LogWarning("Исследуйте диспетчеризацию：не найдено {Text} Слово", "получать");
                }
            }
            else
            {
                break;
            }
        }
    }

    private bool SelectCharacter(CaptureContent content)
    {
        var captureRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var result = CaptureAndOcr(content, new Rect(0, 0, captureRect.Width / 2, captureRect.Height));
        if (result.RegionHasText("Выбор персонажа"))
        {
            var cards = GetCharacterCards(result);
            if (cards.Count > 0)
            {
                var card = cards.FirstOrDefault(c => c.Idle && c.Name != null && ExpeditionCharacterList.Contains(c.Name));
                if (card == null)
                {
                    card = cards.First(c => c.Idle);
                }

                var rect = card.Rects.First();

                using var ra = content.CaptureRectArea.Derive(rect);
                ra.Click();
                TaskControl.Logger.LogInformation("Исследуйте диспетчеризацию：отправлять {Name}", card.Name);
                TaskControl.Sleep(500);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// в соответствии сСловоРезультаты распознавания Получить все варианты персонажа
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private List<ExpeditionCharacterCard> GetCharacterCards(PaddleOcrResult result)
    {
        var captureRect = TaskContext.Instance().SystemInfo.CaptureAreaRect;
        var assetScale = TaskContext.Instance().SystemInfo.AssetScale;

        var ocrResultRects = result.Regions.Select(x => x.ToOcrResultRect()).ToList();
        ocrResultRects = ocrResultRects.Where(r => r.Rect.X + r.Rect.Width < captureRect.Width / 2)
            .OrderBy(r => r.Rect.Y).ThenBy(r => r.Rect.X).ToList();

        var cards = new List<ExpeditionCharacterCard>();
        foreach (var ocrResultRect in ocrResultRects)
        {
            if (ocrResultRect.Text.Contains("время сокращено") || ocrResultRect.Text.Contains("Увеличение вознаграждений") || ocrResultRect.Text.Contains("Бонуса пока нет"))
            {
                var card = new ExpeditionCharacterCard();
                card.Rects.Add(ocrResultRect.Rect);
                card.Addition = ocrResultRect.Text;
                foreach (var ocrResultRect2 in ocrResultRects)
                {
                    if (ocrResultRect2.Rect.Y > ocrResultRect.Rect.Y - 50 * assetScale
                        && ocrResultRect2.Rect.Y + ocrResultRect2.Rect.Height < ocrResultRect.Rect.Y + ocrResultRect.Rect.Height)
                    {
                        if (ocrResultRect2.Text.Contains("Приключение завершено") || ocrResultRect2.Text.Contains("В приключении"))
                        {
                            card.Idle = false;
                            var name = ocrResultRect2.Text.Replace("Приключение завершено", "").Replace("В приключении", "").Replace("/", "").Trim();
                            if (!string.IsNullOrEmpty(name))
                            {
                                card.Name = name;
                            }
                        }
                        else if (!ocrResultRect2.Text.Contains("время сокращено") && !ocrResultRect2.Text.Contains("Увеличение вознаграждений") && !ocrResultRect2.Text.Contains("Бонуса пока нет"))
                        {
                            card.Name = ocrResultRect2.Text;
                        }

                        card.Rects.Add(ocrResultRect2.Rect);
                    }
                }

                if (!string.IsNullOrEmpty(card.Name))
                {
                    cards.Add(card);
                }
                else
                {
                    TaskControl.Logger.LogWarning("Исследуйте диспетчеризацию：Опознавательных данных о жизни персонажа не найдено.");
                }
            }
        }

        return cards;
    }

    private readonly Pen _pen = new(Color.Red, 1);

    private PaddleOcrResult CaptureAndOcr(CaptureContent content)
    {
        using var ra = TaskControl.CaptureToRectArea();
        var result = OcrFactory.Paddle.OcrResult(ra.SrcGreyMat);
        //VisionContext.Instance().DrawContent.PutOrRemoveRectList("OcrResultRects", result.ToRectDrawableList(_pen));
        return result;
    }

    private PaddleOcrResult CaptureAndOcr(CaptureContent content, Rect rect)
    {
        using var ra = TaskControl.CaptureToRectArea();
        var result = OcrFactory.Paddle.OcrResult(ra.SrcGreyMat);
        //VisionContext.Instance().DrawContent.PutOrRemoveRectList("OcrResultRects", result.ToRectDrawableList(_pen));
        return result;
    }
}
