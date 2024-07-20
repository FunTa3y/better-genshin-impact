using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.QuickTeleport.Assets;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Linq;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Model;
using System.Windows.Forms;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model.Area;

namespace BetterGenshinImpact.GameTask.QuickTeleport;

internal class QuickTeleportTrigger : ITaskTrigger
{
    public string Name => "Быстрая доставка";
    public bool IsEnabled { get; set; }
    public int Priority => 21;
    public bool IsExclusive { get; set; }

    private readonly QuickTeleportAssets _assets;

    private DateTime _prevClickOptionButtonTime = DateTime.MinValue;

    // private DateTime _prevClickTeleportButtonTime = DateTime.MinValue;
    private DateTime _prevExecute = DateTime.MinValue;

    private readonly QuickTeleportConfig _config;
    private readonly HotKeyConfig _hotkeyConfig;

    public QuickTeleportTrigger()
    {
        _assets = QuickTeleportAssets.Instance;
        _config = TaskContext.Instance().Config.QuickTeleportConfig;
        _hotkeyConfig = TaskContext.Instance().Config.HotKeyConfig;
    }

    public void Init()
    {
        IsEnabled = _config.Enabled;
        IsExclusive = false;
    }

    public void OnCapture(CaptureContent content)
    {
        if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 300)
        {
            return;
        }

        // Когда включена конфигурация передачи сочетаний клавиш，и активируется при нажатии клавиши быстрого доступа
        if (_config.HotkeyTpEnabled && !string.IsNullOrEmpty(_hotkeyConfig.QuickTeleportTickHotkey))
        {
            if (!IsHotkeyPressed())
            {
                return;
            }
        }

        _prevExecute = DateTime.Now;

        IsExclusive = false;
        // 1.Определите, есть ли это в интерфейсе карты
        content.CaptureRectArea.Find(_assets.MapScaleButtonRo, _ =>
        {
            IsExclusive = true;

            // 2. Определите, есть ли кнопка отправки
            var hasTeleportButton = CheckTeleportButton(content.CaptureRectArea);

            if (!hasTeleportButton)
            {
                // Кнопка закрытия карты существует，Указывает, что точка передачи не выбрана，Возврат напрямую
                var mapCloseRa = content.CaptureRectArea.Find(_assets.MapCloseButtonRo);
                if (!mapCloseRa.IsEmpty())
                {
                    return;
                }

                // Есть кнопка выбора карты，Указывает, что точка передачи не выбрана，Возврат напрямую
                var mapChooseRa = content.CaptureRectArea.Find(_assets.MapChooseRo);
                if (!mapChooseRa.IsEmpty())
                {
                    return;
                }

                // 3. Цикл, чтобы определить, есть ли точка телепортации в списке опций.
                var hasMapChooseIcon = CheckMapChooseIcon(content);
                if (hasMapChooseIcon)
                {
                    TaskControl.Sleep(_config.WaitTeleportPanelDelay);
                    CheckTeleportButton(TaskControl.CaptureToRectArea());
                }
            }
        });
    }

    private bool CheckTeleportButton(ImageRegion imageRegion)
    {
        var hasTeleportButton = false;
        imageRegion.Find(_assets.TeleportButtonRo, ra =>
        {
            ra.Click();
            hasTeleportButton = true;
            // if ((DateTime.Now - _prevClickTeleportButtonTime).TotalSeconds > 1)
            // {
            //     TaskControl.Logger.LogInformation("Быстрая доставка：передавать");
            // }
            // _prevClickTeleportButtonTime = DateTime.Now;
        });
        return hasTeleportButton;
    }

    /// <summary>
    /// Сопоставьте все и выполните распознавание текста
    /// 60ms ~200ms
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private bool CheckMapChooseIcon(CaptureContent content)
    {
        var hasMapChooseIcon = false;

        // Сопоставить все
        var rResultList = MatchTemplateHelper.MatchMultiPicForOnePic(content.CaptureRectArea.SrcGreyMat[_assets.MapChooseIconRoi], _assets.MapChooseIconGreyMatList);
        // Сортировать по высоте
        if (rResultList.Count > 0)
        {
            rResultList = rResultList.OrderBy(x => x.Y).ToList();
            // Нажмите на самый высокий
            foreach (var iconRect in rResultList)
            {
                // 200ширина текстовой области
                using var ra = content.CaptureRectArea.DeriveCrop(_assets.MapChooseIconRoi.X + iconRect.X + iconRect.Width, _assets.MapChooseIconRoi.Y + iconRect.Y, 200, iconRect.Height);
                using var textRegion = ra.Find(new RecognitionObject
                {
                    RecognitionType = RecognitionTypes.ColorRangeAndOcr,
                    LowerColor = new Scalar(249, 249, 249),  // Берите только белый текст
                    UpperColor = new Scalar(255, 255, 255),
                });
                if (string.IsNullOrEmpty(textRegion.Text) || textRegion.Text.Length == 1)
                {
                    continue;
                }

                if ((DateTime.Now - _prevClickOptionButtonTime).TotalMilliseconds > 500)
                {
                    TaskControl.Logger.LogInformation("Быстрая доставка：Нажмите {Option}", textRegion.Text);
                }

                _prevClickOptionButtonTime = DateTime.Now;
                TaskControl.Sleep(_config.TeleportListClickDelay);
                ra.Click();
                hasMapChooseIcon = true;
                break;
            }
        }

        // List<RectArea> raResultList = new();
        // foreach (var ro in _assets.MapChooseIconRoList)
        // {
        //     var ra = content.CaptureRectArea.Find(ro);
        //     if (!ra.IsEmpty())
        //     {
        //         var text = GetOptionText(content.CaptureRectArea.SrcGreyMat, ra, 200);
        //         if (string.IsNullOrEmpty(text) || text.Length == 1)
        //         {
        //             continue;
        //         }
        //
        //         if ((DateTime.Now - _prevClickOptionButtonTime).TotalMilliseconds > 500)
        //         {
        //             TaskControl.Logger.LogInformation("Быстрая доставка：Нажмите {Option}", text);
        //         }
        //
        //         _prevClickOptionButtonTime = DateTime.Now;
        //         TaskControl.Sleep(_config.TeleportListClickDelay);
        //         raResultList.Add(ra);
        //     }
        // }

        // if (raResultList.Count > 0)
        // {
        //     var highest = raResultList[0];
        //     foreach (var ra in raResultList)
        //     {
        //         if (ra.Y < highest.Y)
        //         {
        //             highest = ra;
        //         }
        //     }
        //
        //     highest.ClickCenter();
        //     hasMapChooseIcon = true;
        //
        //     foreach (var ra in raResultList)
        //     {
        //         ra.Dispose();
        //     }
        // }

        return hasMapChooseIcon;
    }

    // /// <summary>
    // /// Получить текст опции
    // /// </summary>
    // /// <param name="captureMat"></param>
    // /// <param name="foundIconRect"></param>
    // /// <param name="chatOptionTextWidth"></param>
    // /// <returns></returns>
    // [Obsolete]
    // private string GetOptionText(Mat captureMat, Rect foundIconRect, int chatOptionTextWidth)
    // {
    //     var textRect = new Rect(foundIconRect.X + foundIconRect.Width, foundIconRect.Y, chatOptionTextWidth, foundIconRect.Height);
    //     using var mat = new Mat(captureMat, textRect);
    //     var text = OcrFactory.Paddle.Ocr(mat);
    //     return text;
    // }

    private bool IsHotkeyPressed()
    {
        if (HotKey.IsMouseButton(_hotkeyConfig.QuickTeleportTickHotkey))
        {
            if (MouseHook.AllMouseHooks.TryGetValue((MouseButtons)Enum.Parse(typeof(MouseButtons), _hotkeyConfig.QuickTeleportTickHotkey), out var mouseHook))
            {
                if (mouseHook.IsPressed)
                {
                    return true;
                }
            }
        }
        else
        {
            if (KeyboardHook.AllKeyboardHooks.TryGetValue((Keys)Enum.Parse(typeof(Keys), _hotkeyConfig.QuickTeleportTickHotkey), out var keyboardHook))
            {
                if (keyboardHook.IsPressed)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
