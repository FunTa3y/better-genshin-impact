using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.Model.Area;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

/// <summary>
/// подражатьOpenCvстатический класс
/// Используется для различных операций по идентификации и контролю Genshin Impact.
///
/// В основном это необходимо для определения некоторых состояний в игре.
/// </summary>
public static partial class Bv
{
    /// <summary>
    /// Нажмите белую кнопку подтверждения.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickWhiteConfirmButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnWhiteConfirm);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите белую кнопку отмены.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickWhiteCancelButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnWhiteCancel);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите черную кнопку подтверждения.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickBlackConfirmButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnBlackConfirm);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите черную кнопку отмены.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickBlackCancelButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnBlackCancel);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите кнопку онлайн-подтверждения.
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickOnlineYesButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnOnlineYes);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите кнопку «Отменить онлайн».
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickOnlineNoButton(ImageRegion captureRa)
    {
        var ra = captureRa.Find(ElementAssets.Instance.BtnOnlineNo);
        if (ra.IsExist())
        {
            ra.Click();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Нажмите кнопку подтверждения（Отдайте предпочтение нажатию кнопки подтверждения на белом фоне.）
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickConfirmButton(ImageRegion captureRa)
    {
        return ClickBlackConfirmButton(captureRa) || ClickWhiteConfirmButton(captureRa) || ClickOnlineYesButton(captureRa);
    }

    /// <summary>
    /// Нажмите кнопку отмены（Отдайте предпочтение нажатию кнопки подтверждения на белом фоне.）
    /// </summary>
    /// <param name="captureRa"></param>
    /// <returns></returns>
    public static bool ClickCancelButton(ImageRegion captureRa)
    {
        return ClickBlackCancelButton(captureRa) || ClickWhiteCancelButton(captureRa) || ClickOnlineNoButton(captureRa);
    }
}
