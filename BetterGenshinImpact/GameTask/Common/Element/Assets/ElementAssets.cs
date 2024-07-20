using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Common.Element.Assets;

public class ElementAssets : BaseAssets<ElementAssets>
{
    public RecognitionObject BtnWhiteConfirm;
    public RecognitionObject BtnWhiteCancel;
    public RecognitionObject BtnBlackConfirm;
    public RecognitionObject BtnBlackCancel;
    public RecognitionObject BtnOnlineYes;
    public RecognitionObject BtnOnlineNo;

    public RecognitionObject PaimonMenuRo;
    public RecognitionObject BlueTrackPoint;

    public RecognitionObject UiLeftTopCookIcon;

    public RecognitionObject SpaceKey;
    public RecognitionObject XKey;

    public RecognitionObject FriendChat;

    private ElementAssets()
    {
        // кнопка
        BtnWhiteConfirm = new RecognitionObject
        {
            Name = "BtnWhiteConfirm",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_white_confirm.png"),
            Use3Channels = true
        }.InitTemplate();
        BtnWhiteCancel = new RecognitionObject
        {
            Name = "BtnWhiteCancel",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_white_cancel.png"),
            Use3Channels = true
        }.InitTemplate();
        BtnBlackConfirm = new RecognitionObject
        {
            Name = "BtnBlackConfirm",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_black_confirm.png"),
            Use3Channels = true
        }.InitTemplate();
        BtnBlackCancel = new RecognitionObject
        {
            Name = "BtnBlackCancel",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_black_cancel.png"),
            Use3Channels = true
        }.InitTemplate();
        BtnOnlineYes = new RecognitionObject
        {
            Name = "BtnOnlineYes",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_online_yes.png"),
            Use3Channels = true
        }.InitTemplate();
        BtnOnlineNo = new RecognitionObject
        {
            Name = "BtnOnlineNo",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "btn_online_no.png"),
            Use3Channels = true
        }.InitTemplate();

        // Паймон меню
        // Эта картинка38x40 мини-карта210x210 мини-картав левом верхнем углуРасположение 24,-15
        PaimonMenuRo = new RecognitionObject
        {
            Name = "PaimonMenu",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "paimon_menu.png"),
            RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 4, CaptureRect.Height / 4),
            DrawOnWindow = false
        }.InitTemplate();

        // Точки отслеживания миссии
        BlueTrackPoint = new RecognitionObject
        {
            Name = "BlueTrackPoint",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "blue_track_point_28x.png"),
            RegionOfInterest = new Rect((int)(300 * AssetScale), 0, CaptureRect.Width - (int)(600 * AssetScale), CaptureRect.Height),
            Threshold = 0.6,
            DrawOnWindow = true
        }.InitTemplate();

        // в левом верхнем углуUIэлемент
        UiLeftTopCookIcon = new RecognitionObject
        {
            Name = "UiLeftTopCookIcon",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "ui_left_top_cook_icon.png"),
            RegionOfInterest = new Rect(0, 0, (int)(150 * AssetScale), (int)(120 * AssetScale)),
            DrawOnWindow = false
        }.InitTemplate();

        // Ключевые подсказки в правом нижнем углу
        SpaceKey = new RecognitionObject
        {
            Name = "SpaceKey",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "key_space.png"),
            RegionOfInterest = new Rect(CaptureRect.Width - (int)(130 * AssetScale), CaptureRect.Height - (int)(70 * AssetScale), (int)(130 * AssetScale), (int)(70 * AssetScale)),
            DrawOnWindow = false
        }.InitTemplate();
        XKey = new RecognitionObject
        {
            Name = "XKey",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "key_x.png"),
            RegionOfInterest = new Rect(CaptureRect.Width - (int)(210 * AssetScale), CaptureRect.Height - (int)(70 * AssetScale), (int)(60 * AssetScale), (int)(70 * AssetScale)),
            DrawOnWindow = false
        }.InitTemplate();

        // Чат с друзьями в левом нижнем углуicon
        FriendChat = new RecognitionObject
        {
            Name = "FriendChat",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage(@"Common\Element", "friend_chat.png"),
            RegionOfInterest = new Rect(0, CaptureRect.Height - (int)(70 * AssetScale), (int)(83 * AssetScale), (int)(70 * AssetScale)),
            DrawOnWindow = false
        }.InitTemplate();
    }
}
