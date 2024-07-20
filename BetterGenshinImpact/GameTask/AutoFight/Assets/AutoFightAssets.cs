using System.Collections.Generic;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.AutoFight.Assets;

public class AutoFightAssets : BaseAssets<AutoFightAssets>
{
    public Rect TeamRectNoIndex;
    public Rect TeamRect;
    public List<Rect> AvatarSideIconRectList;
    public List<Rect> AvatarIndexRectList;
    public Rect ERect;
    public Rect QRect;
    public Rect EndTipsUpperRect; // Советы по завершению испытаний
    public Rect EndTipsRect;
    public RecognitionObject WandererIconRa;
    public RecognitionObject WandererIconNoActiveRa;
    public RecognitionObject ConfirmRa;
    public RecognitionObject ExitRa;
    public RecognitionObject ClickAnyCloseTipRa;
    public RecognitionObject UseCondensedResinRa;

    // Состояние смолы
    public RecognitionObject CondensedResinCountRa;

    public RecognitionObject FragileResinCountRa;

    public Dictionary<string, string> AvatarCostumeMap;

    private AutoFightAssets()
    {
        TeamRectNoIndex = new Rect(CaptureRect.Width - (int)(355 * AssetScale), (int)(220 * AssetScale),
            (int)((355 - 85) * AssetScale), (int)(465 * AssetScale));
        TeamRect = new Rect(CaptureRect.Width - (int)(355 * AssetScale), (int)(220 * AssetScale),
            (int)(355 * AssetScale), (int)(465 * AssetScale));
        ERect = new Rect(CaptureRect.Width - (int)(267 * AssetScale), CaptureRect.Height - (int)(132 * AssetScale),
            (int)(77 * AssetScale), (int)(77 * AssetScale));
        QRect = new Rect(CaptureRect.Width - (int)(157 * AssetScale), CaptureRect.Height - (int)(165 * AssetScale),
            (int)(110 * AssetScale), (int)(110 * AssetScale));
        // Конечная подсказка начинается с середины, чтобы найти относительное положение.
        EndTipsUpperRect = new Rect(CaptureRect.Width / 2 - (int)(100 * AssetScale), (int)(243 * AssetScale),
            (int)(200 * AssetScale), (int)(50 * AssetScale));
        EndTipsRect = new Rect(CaptureRect.Width / 2 - (int)(200 * AssetScale), CaptureRect.Height - (int)(160 * AssetScale),
            (int)(400 * AssetScale), (int)(80 * AssetScale));

        AvatarIndexRectList =
        [
            new Rect(CaptureRect.Width - (int)(61 * AssetScale), (int)(256 * AssetScale), (int)(28 * AssetScale), (int)(24 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(61 * AssetScale), (int)(352 * AssetScale), (int)(28 * AssetScale), (int)(24 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(61 * AssetScale), (int)(448 * AssetScale), (int)(28 * AssetScale), (int)(24 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(61 * AssetScale), (int)(544 * AssetScale), (int)(28 * AssetScale), (int)(24 * AssetScale)),
        ];

        AvatarSideIconRectList =
        [
            new Rect(CaptureRect.Width - (int)(155 * AssetScale), (int)(225 * AssetScale), (int)(76 * AssetScale), (int)(76 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(155 * AssetScale), (int)(315 * AssetScale), (int)(76 * AssetScale), (int)(76 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(155 * AssetScale), (int)(410 * AssetScale), (int)(76 * AssetScale), (int)(76 * AssetScale)),
            new Rect(CaptureRect.Width - (int)(155 * AssetScale), (int)(500 * AssetScale), (int)(76 * AssetScale), (int)(76 * AssetScale)),
        ];

        AvatarCostumeMap = new Dictionary<string, string>
        {
            { "Flamme", "Инь Хун всю ночь" },
            { "Bamboo", "Дождь превращается в бамбуковое тело" },
            { "Dai", "Холодная цветочная роса" },
            { "Yu", "Сюаньюй Яофан" },
            { "Dancer", "Парусная тень, путешествующая по ветру" },
            { "Witch", "Цихуа Синчжу" },
            { "Wic", "гармоничный" },
            { "Studentin", "Е Иньфанмин" },
            { "Fruhling", "Письма время от времени" },
            { "Highness", "Экстремальный ночной сон" },
            { "Feather", "неоновое платье танцует" },
            { "Floral", "Орхидея в пряже" },
            { "Summertime", "Сияющий концерт" },
            { "Sea", "Морской Бриз Мечта" },
        };

        WandererIconRa = new RecognitionObject
        {
            Name = "WandererIcon",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "wanderer_icon.png"),
            DrawOnWindow = false
        }.InitTemplate();
        WandererIconNoActiveRa = new RecognitionObject
        {
            Name = "WandererIconNoActive",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "wanderer_icon_no_active.png"),
            DrawOnWindow = false
        }.InitTemplate();

        // кнопка в правом нижнем углу
        ConfirmRa = new RecognitionObject
        {
            Name = "Confirm",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "confirm.png"),
            RegionOfInterest = new Rect(CaptureRect.Width / 2, CaptureRect.Height / 2, CaptureRect.Width / 2, CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();

        // Нажмите в любом месте, чтобы закрыть подсказку
        ClickAnyCloseTipRa = new RecognitionObject
        {
            Name = "ClickAnyCloseTip",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "click_any_close_tip.png"),
            RegionOfInterest = new Rect(0, CaptureRect.Height / 2, CaptureRect.Width, CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();

        UseCondensedResinRa = new RecognitionObject
        {
            Name = "UseCondensedResin",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "use_condensed_resin.png"),
            RegionOfInterest = new Rect(0, CaptureRect.Height / 2, CaptureRect.Width / 2, CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();

        ExitRa = new RecognitionObject
        {
            Name = "Exit",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "exit.png"),
            RegionOfInterest = new Rect(0, CaptureRect.Height / 2, CaptureRect.Width / 2, CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();

        CondensedResinCountRa = new RecognitionObject
        {
            Name = "CondensedResinCount",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "condensed_resin_count.png"),
            RegionOfInterest = new Rect(CaptureRect.Width / 2, CaptureRect.Height / 3 * 2, CaptureRect.Width / 2, CaptureRect.Height / 3),
            DrawOnWindow = false
        }.InitTemplate();
        FragileResinCountRa = new RecognitionObject
        {
            Name = "FragileResinCount",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFight", "fragile_resin_count.png"),
            RegionOfInterest = new Rect(CaptureRect.Width / 2, CaptureRect.Height / 3 * 2, CaptureRect.Width / 2, CaptureRect.Height / 3),
            DrawOnWindow = false
        }.InitTemplate();
    }
}
