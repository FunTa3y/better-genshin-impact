using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoFishing.Assets;
using BetterGenshinImpact.GameTask.AutoFishing.Model;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.Helpers;
using BetterGenshinImpact.Helpers.Extensions;
using BetterGenshinImpact.Model;
using BetterGenshinImpact.View.Drawable;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Compunet.YoloV8;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using BetterGenshinImpact.GameTask.Common.BgiVision;
using BetterGenshinImpact.GameTask.Model.Area;
using static Vanara.PInvoke.User32;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Point = OpenCvSharp.Point;

namespace BetterGenshinImpact.GameTask.AutoFishing
{
    public class AutoFishingTrigger : ITaskTrigger
    {
        private readonly ILogger<AutoFishingTrigger> _logger = App.GetLogger<AutoFishingTrigger>();
        private readonly IOcrService _ocrService = OcrFactory.Paddle;
        private readonly YoloV8Predictor _predictor = YoloV8Builder.CreateDefaultBuilder().UseOnnxModel(Global.Absolute("Assets\\Model\\Fish\\bgi_fish.onnx")).WithSessionOptions(BgiSessionOption.Instance.Options).Build();

        public string Name => "автоматическая рыбалка";
        public bool IsEnabled { get; set; }
        public int Priority => 15;

        /// <summary>
        /// Для рыбалки требуется эксклюзивный режим
        /// во время рыбалки，Никаких других задач выполняться не должно.
        /// существовать触发器发现正во время рыбалки，Включить эксклюзивный режим
        /// </summary>
        public bool IsExclusive { get; set; }

        private readonly AutoFishingAssets _autoFishingAssets;

        public AutoFishingTrigger()
        {
            _autoFishingAssets = AutoFishingAssets.Instance;
        }

        public void Init()
        {
            IsEnabled = TaskContext.Instance().Config.AutoFishingConfig.Enabled;
            IsExclusive = false;

            // Инициализация переменной рыбалки
            _findFishBoxTips = false;
            _switchBaitContinuouslyFrameNum = 0;
            _waitBiteContinuouslyFrameNum = 0;
            _noFishActionContinuouslyFrameNum = 0;
            _isThrowRod = false;
            _selectedBaitName = string.Empty;
        }

        private Rect _fishBoxRect = Rect.Empty;

        private DateTime _prevExecute = DateTime.MinValue;

        private CaptureContent _currContent;

        public void OnCapture(CaptureContent content)
        {
            this._currContent = content;
            // Вынести исключительное решение
            if (!IsExclusive)
            {
                if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 200)
                {
                    return;
                }

                _prevExecute = DateTime.Now;

                // Введите решение в эксклюзивном режиме
                CheckFishingUserInterface(content);
            }
            else
            {
                // Автоматическое метание шеста
                ThrowRod(content);
                // Решение о взятии наживки
                FishBite(content);
                // При входе в интерфейс рыбалки сначала попытайтесь узнать местоположение рыболовного ящика.
                if (_fishBoxRect.Width == 0)
                {
                    if ((DateTime.Now - _prevExecute).TotalMilliseconds <= 200)
                    {
                        return;
                    }

                    _prevExecute = DateTime.Now;

                    _fishBoxRect = GetFishBoxArea(content.CaptureRectArea);
                    CheckFishingUserInterface(content);
                }
                else
                {
                    // удочка
                    Fishing(content, new Mat(content.CaptureRectArea.SrcMat, _fishBoxRect));
                }
            }
        }

        // /// <summary>
        // /// существовать“начать ловить рыбу”Устройте один из наших“начинатьавтоматическая рыбалка”кнопка
        // /// 点击кнопка进入独占模式
        // /// </summary>
        // /// <param name="content"></param>
        // /// <returns></returns>
        // [Obsolete]
        // private void DisplayButtonOnStartFishPageForExclusive(CaptureContent content)
        // {
        //     VisionContext.Instance().DrawContent.RemoveRect("StartFishingButton");
        //     var info = TaskContext.Instance().SystemInfo;
        //     var srcMat = content.CaptureRectArea.SrcMat;
        //     var rightBottomMat = CropHelper.CutRightBottom(srcMat, srcMat.Width / 2, srcMat.Height / 2);
        //     var list = CommonRecognition.FindGameButton(rightBottomMat);
        //     if (list.Count > 0)
        //     {
        //         foreach (var rect in list)
        //         {
        //             var ro = new RecognitionObject()
        //             {
        //                 Name = "StartFishingText",
        //                 RecognitionType = RecognitionTypes.OcrMatch,
        //                 RegionOfInterest = new Rect(srcMat.Width / 2, srcMat.Height / 2, srcMat.Width - srcMat.Width / 2,
        //                     srcMat.Height - srcMat.Height / 2),
        //                 AllContainMatchText = new List<string>
        //                 {
        //                     "начинать", "ловит рыбу"
        //                 },
        //                 DrawOnWindow = false
        //             };
        //             var ocrRaRes = content.CaptureRectArea.Find(ro);
        //             if (ocrRaRes.IsEmpty())
        //             {
        //                 WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "RemoveButton", new object(), "начинатьавтоматическая рыбалка"));
        //             }
        //             else
        //             {
        //                 VisionContext.Instance().DrawContent.PutRect("StartFishingButton", rect.ToWindowsRectangleOffset(srcMat.Width / 2, srcMat.Height / 2).ToRectDrawable());
        //
        //                 var btnPosition = new Rect(rect.X + srcMat.Width / 2, rect.Y + srcMat.Height / 2 - rect.Height - 10, rect.Width, rect.Height);
        //                 var maskButton = new MaskButton("начинатьавтоматическая рыбалка", btnPosition, () =>
        //                 {
        //                     VisionContext.Instance().DrawContent.RemoveRect("StartFishingButton");
        //                     _logger.LogInformation("→ {Text}", "автоматическая рыбалка，запускать！");
        //                     // 点击下面的кнопка
        //                     var rc = info.CaptureAreaRect;
        //                     Simulation.SendInputEx
        //                         .Mouse
        //                         .MoveMouseTo(
        //                             (rc.X + srcMat.Width * 1d / 2 + rect.X + rect.Width * 1d / 2) * 65535 / info.DesktopRectArea.Width,
        //                             (rc.Y + srcMat.Height * 1d / 2 + rect.Y + rect.Height * 1d / 2) * 65535 / info.DesktopRectArea.Height)
        //                         .LeftButtonClick();
        //                     WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "RemoveButton", new object(), "начинатьавтоматическая рыбалка"));
        //                     // запускать要延时一会等待ловит рыбу界面切换
        //                     Sleep(1000);
        //                     IsExclusive = true;
        //                     _switchBaitContinuouslyFrameNum = 0;
        //                     _waitBiteContinuouslyFrameNum = 0;
        //                     _noFishActionContinuouslyFrameNum = 0;
        //                     _isThrowRod = false;
        //                 });
        //                 WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "AddButton", new object(), maskButton));
        //             }
        //         }
        //     }
        //     else
        //     {
        //         WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "RemoveButton", new object(), "начинатьавтоматическая рыбалка"));
        //     }
        // }

        //private bool OcrStartFishingForExclusive(CaptureContent content)
        //{
        //    var srcMat = content.CaptureRectArea.SrcMat;
        //    var rightBottomMat = CutHelper.CutRightBottom(srcMat, srcMat.Width / 2, srcMat.Height / 2);
        //    var text = _ocrService.Ocr(rightBottomMat.ToBitmap());
        //    if (!string.IsNullOrEmpty(text) && StringUtils.RemoveAllSpace(text).Contains("начинать") && StringUtils.RemoveAllSpace(text).Contains("ловит рыбу"))
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// попытаться найти右下角的退出ловит рыбукнопка
        /// 用于判断是否进入ловит рыбу界面
        /// 进入ловит рыбу界面时该触发器进入独占模式
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private bool FindButtonForExclusive(CaptureContent content)
        {
            return !content.CaptureRectArea.Find(_autoFishingAssets.ExitFishingButtonRo).IsEmpty();
        }

        private int _throwRodWaitFrameNum = 0; // Время ожидания кастинга(Рамки)
        private int _switchBaitContinuouslyFrameNum = 0; // 切换鱼饵кнопка图标的持续时间(Рамки)
        private int _waitBiteContinuouslyFrameNum = 0; // Продолжительность ожидания подключения(Рамки)
        private int _noFishActionContinuouslyFrameNum = 0; // 无ловит рыбу三вид сцены的持续时间(Рамки)
        private bool _isThrowRod = false; // Стержень был отлит?

        /// <summary>
        /// ловит рыбу有3вид сцены
        /// 1. Не бросая удочку BaitButtonRo存существовать && WaitBiteButtonRo不存существовать
        /// 2. Не тянуть удочку после броска удочки WaitBiteButtonRo存существовать && BaitButtonRo不存существовать
        /// 3. Крючок-тяга _isFishingProcess && _biteTipsExitCount > 0
        ///
        /// новыйAIловит рыбу
        /// помещение：Вы должны повернуться лицом к пруду с рыбой.，没有идентифицировать到鱼的时候不会Автоматическое метание шеста
        /// 1. Наблюдайте за своим окружением，Определитесь с местом расположения пруда с рыбками，Вид из центра пруда с рыбками
        /// 2. На основе наблюдений на первом этапе，Выбирайте приманку заранее
        /// 3.
        /// </summary>
        /// <param name="content"></param>
        private void ThrowRod(CaptureContent content)
        {
            // Когда нет тяг или подъемных стоек，Автоматическое метание шеста
            if (!_isFishingProcess && _biteTipsExitCount == 0 && TaskContext.Instance().Config.AutoFishingConfig.AutoThrowRodEnabled)
            {
                var baitRectArea = content.CaptureRectArea.Find(_autoFishingAssets.BaitButtonRo);
                var waitBiteArea = content.CaptureRectArea.Find(_autoFishingAssets.WaitBiteButtonRo);
                if (!baitRectArea.IsEmpty() && waitBiteArea.IsEmpty())
                {
                    _switchBaitContinuouslyFrameNum++;
                    _waitBiteContinuouslyFrameNum = 0;
                    _noFishActionContinuouslyFrameNum = 0;

                    if (_switchBaitContinuouslyFrameNum >= content.FrameRate)
                    {
                        _isThrowRod = false;
                        _switchBaitContinuouslyFrameNum = 0;
                        _logger.LogInformation("当前处于Не бросая удочку状态");
                    }

                    if (!_isThrowRod)
                    {
                        // 1. Наблюдайте за своим окружением，Определитесь с местом расположения пруда с рыбками，Вид из центра пруда с рыбками
                        using var memoryStream = new MemoryStream();
                        content.CaptureRectArea.SrcBitmap.Save(memoryStream, ImageFormat.Bmp);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        var result = _predictor.Detect(memoryStream);
                        Debug.WriteLine($"YOLOv8идентифицировать: {result.Speed}");
                        var fishpond = new Fishpond(result);
                        if (fishpond.FishpondRect == Rect.Empty)
                        {
                            Sleep(500);
                            return;
                        }
                        else
                        {
                            var centerX = content.CaptureRectArea.SrcBitmap.Width / 2;
                            var centerY = content.CaptureRectArea.SrcBitmap.Height / 2;
                            // Перемещение влево — положительное число.，Движение вправо — отрицательное число.
                            if (fishpond.FishpondRect.Left > centerX)
                            {
                                Simulation.SendInput.Mouse.MoveMouseBy(100, 0);
                            }

                            if (fishpond.FishpondRect.Right < centerX)
                            {
                                Simulation.SendInput.Mouse.MoveMouseBy(-100, 0);
                            }

                            // 鱼塘尽量существовать上半屏幕
                            if (fishpond.FishpondRect.Bottom > centerY)
                            {
                                Simulation.SendInput.Mouse.MoveMouseBy(0, -100);
                            }

                            if ((fishpond.FishpondRect.Left < centerX && fishpond.FishpondRect.Right > centerX && fishpond.FishpondRect.Bottom >= centerY) || fishpond.FishpondRect.Width < content.CaptureRectArea.SrcBitmap.Width / 4)
                            {
                                // 鱼塘существовать中心，Выберите наживку
                                if (string.IsNullOrEmpty(_selectedBaitName))
                                {
                                    _selectedBaitName = ChooseBait(content, fishpond);
                                }

                                // Метательная удочка
                                Sleep(2000);
                                ApproachFishAndThrowRod(content);
                                Sleep(2000);
                            }
                        }
                    }
                }

                if (baitRectArea.IsEmpty() && !waitBiteArea.IsEmpty() && _isThrowRod)
                {
                    _switchBaitContinuouslyFrameNum = 0;
                    _waitBiteContinuouslyFrameNum++;
                    _noFishActionContinuouslyFrameNum = 0;
                    _throwRodWaitFrameNum++;

                    if (_waitBiteContinuouslyFrameNum >= content.FrameRate)
                    {
                        _isThrowRod = true;
                        _waitBiteContinuouslyFrameNum = 0;
                    }

                    if (_isThrowRod)
                    {
                        // 30s Не попался на удочку，重новыйМетательная удочка
                        if (_throwRodWaitFrameNum >= content.FrameRate * TaskContext.Instance().Config.AutoFishingConfig.AutoThrowRodTimeOut)
                        {
                            Simulation.SendInput.Mouse.LeftButtonClick();
                            _throwRodWaitFrameNum = 0;
                            _waitBiteContinuouslyFrameNum = 0;
                            Debug.WriteLine("Автоматическое закрытие полюса после тайм-аута");
                            Sleep(2000);
                            _isThrowRod = false;
                        }
                    }
                }

                if (baitRectArea.IsEmpty() && waitBiteArea.IsEmpty())
                {
                    _switchBaitContinuouslyFrameNum = 0;
                    _waitBiteContinuouslyFrameNum = 0;
                    _noFishActionContinuouslyFrameNum++;
                    if (_noFishActionContinuouslyFrameNum > content.FrameRate)
                    {
                        CheckFishingUserInterface(content);
                    }
                }
            }
            else
            {
                _switchBaitContinuouslyFrameNum = 0;
                _waitBiteContinuouslyFrameNum = 0;
                _noFishActionContinuouslyFrameNum = 0;
                _throwRodWaitFrameNum = 0;
                _isThrowRod = false;
            }
        }

        private string _selectedBaitName = string.Empty;

        /// <summary>
        /// Выберите наживку
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fishpond"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string ChooseBait(CaptureContent content, Fishpond fishpond)
        {
            // Откройте интерфейс смены наживки
            Simulation.SendInput.Mouse.RightButtonClick();
            Sleep(100);
            Simulation.SendInput.Mouse.MoveMouseBy(0, 200); // Мышь удалена，предотвратить вмешательство
            Sleep(500);

            _selectedBaitName = fishpond.Fishes[0].FishType.BaitName; // Выберите наживку, которую съест большинство рыб.
            _logger.LogInformation("Выберите наживку {Text}", BaitType.FromName(_selectedBaitName).ChineseName);

            // ищу наживку
            var ro = new RecognitionObject
            {
                Name = "ChooseBait",
                RecognitionType = RecognitionTypes.TemplateMatch,
                TemplateImageMat = GameTaskManager.LoadAssetImage("AutoFishing", $"bait\\{_selectedBaitName}.png"),
                Threshold = 0.8,
                Use3Channels = true,
                DrawOnWindow = false
            }.InitTemplate();

            // Скриншот
            using var captureRegion = TaskControl.CaptureToRectArea();
            using var resRa = captureRegion.Find(ro);
            if (resRa.IsEmpty())
            {
                _logger.LogWarning("Целевая приманка не найдена");
                _selectedBaitName = string.Empty;
                throw new Exception("Целевая приманка не найдена");
            }
            else
            {
                resRa.Click();
                Sleep(700);
                // Можно нажимать неоднократно，Итак, нажмите на фиксированный интерфейс
                captureRegion.ClickTo((int)(captureRegion.Width * 0.675), (int)(captureRegion.Height / 3d));
                Sleep(200);
                // Нажмите ОК
                Bv.ClickWhiteConfirmButton(captureRegion);
                Sleep(500); // Ожидание переключения интерфейса
            }

            return _selectedBaitName;
        }

        private readonly Random _rd = new();

        /// <summary>
        /// Метательная удочка
        /// </summary>
        /// <param name="content"></param>
        private void ApproachFishAndThrowRod(CaptureContent content)
        {
            // 预Метательная удочка
            Simulation.SendInput.Mouse.LeftButtonDown();
            _logger.LogInformation("长按预Метательная удочка");
            Sleep(3000);

            var noPlacementTimes = 0; // Нет просмотров
            var noTargetFishTimes = 0; // Правая часть изображения — количество хрупкой смолы.
            var prevTargetFishRect = Rect.Empty; // Запишите положение предыдущей целевой рыбы.

            while (IsEnabled)
            {
                // Скриншот
                var ra = TaskControl.CaptureToRectArea();

                // попытаться найти Место посадки приманки
                using var memoryStream = new MemoryStream();
                ra.SrcBitmap.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var result = _predictor.Detect(memoryStream);
                Debug.WriteLine($"YOLOv8идентифицировать: {result.Speed}");
                var fishpond = new Fishpond(result);
                if (fishpond.TargetRect == Rect.Empty)
                {
                    noPlacementTimes++;
                    Sleep(50);
                    Debug.WriteLine("历次未попытаться найти到Место посадки приманки");

                    var cX = ra.SrcBitmap.Width / 2;
                    var cY = ra.SrcBitmap.Height / 2;
                    var rdX = _rd.Next(0, ra.SrcBitmap.Width);
                    var rdY = _rd.Next(0, ra.SrcBitmap.Height);

                    var moveX = 100 * (cX - rdX) / ra.SrcBitmap.Width;
                    var moveY = 100 * (cY - rdY) / ra.SrcBitmap.Height;

                    Simulation.SendInput.Mouse.MoveMouseBy(moveX, moveY);

                    if (noPlacementTimes > 25)
                    {
                        _logger.LogInformation("未попытаться найти到Место посадки приманки，Повторить попытку");
                        // Simulation.SendInputEx.Mouse.LeftButtonUp();
                        // // Sleep(2000);
                        // // Simulation.SendInputEx.Mouse.LeftButtonClick();
                        // _selectedBaitName = string.Empty;
                        // _isThrowRod = false;
                        // // Sleep(2000);
                        // // MoveViewpointDown();
                        // Sleep(300);
                        break;
                    }

                    continue;
                }

                // попытаться найти到落点最近的鱼
                OneFish? currentFish = null;
                if (prevTargetFishRect == Rect.Empty)
                {
                    var list = fishpond.FilterByBaitName(_selectedBaitName);
                    if (list.Count > 0)
                    {
                        currentFish = list[0];
                        prevTargetFishRect = currentFish.Rect;
                    }
                }
                else
                {
                    currentFish = fishpond.FilterByBaitNameAndRecently(_selectedBaitName, prevTargetFishRect);
                    if (currentFish != null)
                    {
                        prevTargetFishRect = currentFish.Rect;
                    }
                }

                if (currentFish == null)
                {
                    Debug.WriteLine("Бесцелевая рыба");
                    noTargetFishTimes++;
                    //if (noTargetFishTimes == 30)
                    //{
                    //    Simulation.SendInputEx.Mouse.MoveMouseBy(0, 100);
                    //}

                    if (noTargetFishTimes > 10)
                    {
                        // 没有попытаться найти到目标鱼，重новыйВыберите наживку
                        _logger.LogInformation("没有попытаться найти到目标鱼，1.直接Метательная удочка");
                        Simulation.SendInput.Mouse.LeftButtonUp();
                        Sleep(1500);
                        _logger.LogInformation("没有попытаться найти到目标鱼，2.Заканчивать");
                        Simulation.SendInput.Mouse.LeftButtonClick();
                        Sleep(800);
                        _logger.LogInformation("没有попытаться найти到目标鱼，3.准备重новыйВыберите наживку");
                        _selectedBaitName = string.Empty;
                        _isThrowRod = false;
                        MoveViewpointDown();
                        Sleep(300);
                        break;
                    }

                    continue;
                }
                else
                {
                    noTargetFishTimes = 0;
                    _currContent.CaptureRectArea.DrawRect(fishpond.TargetRect, "Target");
                    _currContent.CaptureRectArea.Derive(currentFish.Rect).DrawSelf("Fish");
                    // VisionContext.Instance().DrawContent.PutRect("Target", fishpond.TargetRect.ToRectDrawable());
                    // VisionContext.Instance().DrawContent.PutRect("Fish", currentFish.Rect.ToRectDrawable());

                    // var min = MoveMouseToFish(fishpond.TargetRect, currentFish.Rect);
                    // // Потому что угол обзора смотрит на рыбу по диагонали.，такY轴Метательная удочка距离要近一点
                    // if ((_selectedBaitName != "fruit paste bait" && min is { Item1: <= 50, Item2: <= 25 })
                    //     || _selectedBaitName == "fruit paste bait" && min is { Item1: <= 40, Item2: <= 25 })
                    // {
                    //     Sleep(100);
                    //     Simulation.SendInputEx.Mouse.LeftButtonUp();
                    //     _logger.LogInformation("попробовать порыбачить {Text}", currentFish.FishType.ChineseName);
                    //     _isThrowRod = true;
                    //     VisionContext.Instance().DrawContent.RemoveRect("Target");
                    //     VisionContext.Instance().DrawContent.RemoveRect("Fish");
                    //     break;
                    // }

                    // от HutaoFisher 的Метательная удочка技术
                    var rod = fishpond.TargetRect;
                    var fish = currentFish.Rect;
                    var dx = NormalizeXTo1024(fish.Left + fish.Right - rod.Left - rod.Right) / 2.0;
                    var dy = NormalizeYTo576(fish.Top + fish.Bottom - rod.Top - rod.Bottom) / 2.0;
                    var state = RodNet.GetRodState(new RodInput
                    {
                        rod_x1 = NormalizeXTo1024(rod.Left),
                        rod_x2 = NormalizeXTo1024(rod.Right),
                        rod_y1 = NormalizeYTo576(rod.Top),
                        rod_y2 = NormalizeYTo576(rod.Bottom),
                        fish_x1 = NormalizeXTo1024(fish.Left),
                        fish_x2 = NormalizeXTo1024(fish.Right),
                        fish_y1 = NormalizeYTo576(fish.Top),
                        fish_y2 = NormalizeYTo576(fish.Bottom),
                        fish_label = BigFishType.GetIndex(currentFish.FishType)
                    });
                    if (state == -1)
                    {
                        // неудача Перемещайте мышь случайным образом
                        var cX = content.CaptureRectArea.SrcBitmap.Width / 2;
                        var cY = content.CaptureRectArea.SrcBitmap.Height / 2;
                        var rdX = _rd.Next(0, content.CaptureRectArea.SrcBitmap.Width);
                        var rdY = _rd.Next(0, content.CaptureRectArea.SrcBitmap.Height);

                        var moveX = 100 * (cX - rdX) / content.CaptureRectArea.SrcBitmap.Width;
                        var moveY = 100 * (cY - rdY) / content.CaptureRectArea.SrcBitmap.Height;

                        _logger.LogInformation("неудача случайный ход {DX}, {DY}", moveX, moveY);
                        Simulation.SendInput.Mouse.MoveMouseBy(moveX, moveY);
                    }
                    else if (state == 0)
                    {
                        // успех Метательная удочка
                        Simulation.SendInput.Mouse.LeftButtonUp();
                        _logger.LogInformation("попробовать порыбачить {Text}", currentFish.FishType.ChineseName);
                        _isThrowRod = true;
                        VisionContext.Instance().DrawContent.RemoveRect("Target");
                        VisionContext.Instance().DrawContent.RemoveRect("Fish");
                        break;
                    }
                    else if (state == 1)
                    {
                        // слишком близко
                        var dl = Math.Sqrt(dx * dx + dy * dy);
                        // set a minimum step
                        dx = dx / dl * 30;
                        dy = dy / dl * 30;
                        // _logger.LogInformation("слишком близко двигаться {DX}, {DY}", dx, dy);
                        Simulation.SendInput.Mouse.MoveMouseBy((int)(-dx / 1.5), (int)(-dy * 1.5));
                    }
                    else if (state == 2)
                    {
                        // очень далеко
                        // _logger.LogInformation("очень далеко двигаться {DX}, {DY}", dx, dy);
                        Simulation.SendInput.Mouse.MoveMouseBy((int)(dx / 1.5), (int)(dy * 1.5));
                    }
                }

                Sleep(20);
            }
        }

        private double NormalizeXTo1024(int x)
        {
            return x * 1.0 / TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect.Width * 1024;
        }

        private double NormalizeYTo576(int y)
        {
            return y * 1.0 / TaskContext.Instance().SystemInfo.ScaleMax1080PCaptureRect.Height * 576;
        }

        /// <summary>
        /// 向下двигаться视角
        /// </summary>
        private void MoveViewpointDown()
        {
            if (TaskContext.Instance().Config.AutoFishingConfig.AutoThrowRodEnabled)
            {
                // Переместите угол обзора вниз, чтобы лучше видеть рыбу.
                Simulation.SendInput.Mouse.MoveMouseBy(0, 400);
                Sleep(500);
                Simulation.SendInput.Mouse.MoveMouseBy(0, 500);
                Sleep(500);
            }
        }

        [Obsolete]
        private (int, int) MoveMouseToFish(Rect rect1, Rect rect2)
        {
            int minDistance;

            //Сначала вычислите центральные точки двух прямоугольников.
            Point c1, c2;
            c1.X = rect1.X + (rect1.Width / 2);
            c1.Y = rect1.Y + (rect1.Height / 2);
            c2.X = rect2.X + (rect2.Width / 2);
            c2.Y = rect2.Y + (rect2.Height / 2);

            // 分别计算两矩形中心点существоватьXОсь иYрасстояние в направлении оси
            var dx = Math.Abs(c2.X - c1.X);
            var dy = Math.Abs(c2.Y - c1.Y);

            //Два прямоугольника не пересекаются，существоватьXДва прямоугольника с частично перекрывающимися осями
            if (dx < (rect1.Width + rect2.Width) / 2 && dy >= (rect1.Height + rect2.Height) / 2)
            {
                minDistance = dy - ((rect1.Height + rect2.Height) / 2);

                var moveY = 5;
                if (minDistance >= 100)
                {
                    moveY = 50;
                }

                if (c1.Y > c2.Y)
                {
                    moveY = -moveY;
                }

                //_logger.LogInformation("двигаться鼠标 {X} {Y}", 0, moveY);
                Simulation.SendInput.Mouse.MoveMouseBy(0, moveY);
                return (0, minDistance);
            }

            //Два прямоугольника не пересекаются，существоватьYДва прямоугольника с частично перекрывающимися осями
            else if (dx >= (rect1.Width + rect2.Width) / 2 && (dy < (rect1.Height + rect2.Height) / 2))
            {
                minDistance = dx - ((rect1.Width + rect2.Width) / 2);
                var moveX = 10;
                if (minDistance >= 100)
                {
                    moveX = 50;
                }

                if (c1.X > c2.X)
                {
                    moveX = -moveX;
                }

                //_logger.LogInformation("двигаться鼠标 {X} {Y}", moveX, 0);
                Simulation.SendInput.Mouse.MoveMouseBy(moveX, 0);
                return (minDistance, 0);
            }

            //Два прямоугольника не пересекаются，существоватьXОсь иYДва прямоугольника без перекрывающихся осей
            else if ((dx >= ((rect1.Width + rect2.Width) / 2)) && (dy >= ((rect1.Height + rect2.Height) / 2)))
            {
                var dpX = dx - ((rect1.Width + rect2.Width) / 2);
                var dpY = dy - ((rect1.Height + rect2.Height) / 2);
                //minDistance = (int)Math.Sqrt(dpX * dpX + dpY * dpY);
                var moveX = 10;
                if (dpX >= 100)
                {
                    moveX = 50;
                }

                var moveY = 5;
                if (dpY >= 100)
                {
                    moveY = 50;
                }

                if (c1.Y > c2.Y)
                {
                    moveY = -moveY;
                }

                if (c1.X > c2.X)
                {
                    moveX = -moveX;
                }

                //_logger.LogInformation("двигаться鼠标 {X} {Y}", moveX, moveY);
                Simulation.SendInput.Mouse.MoveMouseBy(moveX, moveY);
                return (dpX, dpY);
            }

            //Два прямоугольника пересекаются
            else
            {
                //_logger.LogInformation("无需двигаться鼠标");
                minDistance = -1;
                return (0, 0);
            }
        }

        public void Sleep(int millisecondsTimeout)
        {
            NewRetry.Do(() =>
            {
                if (IsEnabled && !SystemControl.IsGenshinImpactActiveByProcess())
                {
                    _logger.LogWarning("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.，Пауза");
                    throw new RetryException("Окно, в котором сейчас сосредоточено внимание, не Genshin Impact.");
                }
            }, TimeSpan.FromSeconds(1), 100);
            CheckFishingUserInterface(_currContent);
            Thread.Sleep(millisecondsTimeout);
        }

        /// <summary>
        /// 获取ловит рыбу框的位置
        /// </summary>
        private Rect GetFishBoxArea(ImageRegion captureRegion)
        {
            using var topMat = new Mat(captureRegion.SrcMat, new Rect(0, 0, captureRegion.Width, captureRegion.Height / 2));
            ;
            var rects = AutoFishingImageRecognition.GetFishBarRect(topMat);
            if (rects != null && rects.Count == 2)
            {
                if (Math.Abs(rects[0].Height - rects[1].Height) > 10)
                {
                    TaskControl.Logger.LogError("Разница в высоте между двумя прямоугольниками слишком велика.，未идентифицировать到ловит рыбу框");
                    VisionContext.Instance().DrawContent.RemoveRect("FishBox");
                    return Rect.Empty;
                }

                if (rects[0].Width < rects[1].Width)
                {
                    _cur = rects[0];
                    _left = rects[1];
                }
                else
                {
                    _cur = rects[1];
                    _left = rects[0];
                }

                if (_left.X < _cur.X // cur это позиция курсора, существовать初始状态下，cur 一定существоватьleftлевый
                    || _cur.Width > _left.Width // leftОпределенно лучше, чемcurШирина
                    || _cur.X + _cur.Width > topMat.Width / 2 // cur 一定существовать屏幕левая сторона
                    || _cur.X + _cur.Width > _left.X - _left.Width / 2 // cur 一定существоватьleftлевая сторона+left的一半Ширина度
                    || _cur.X + _cur.Width > topMat.Width / 2 - _left.Width // cur 一定существовать屏幕中轴线减去整个left的Ширина度的位置левая сторона
                    || !(_left.X < topMat.Width / 2 && _left.X + _left.Width > topMat.Width / 2) // leftОпределенно пересекает центральную ось игры.
                   )
                {
                    VisionContext.Instance().DrawContent.RemoveRect("FishBox");
                    return Rect.Empty;
                }

                int hExtra = _cur.Height, vExtra = _cur.Height / 4;
                _fishBoxRect = new Rect(_cur.X - hExtra, _cur.Y - vExtra,
                    (_left.X + _left.Width / 2 - _cur.X) * 2 + hExtra * 2, _cur.Height + vExtra * 2);
                // VisionContext.Instance().DrawContent.PutRect("FishBox", _fishBoxRect.ToRectDrawable(new Pen(Color.LightPink, 2)));
                using var boxRa = captureRegion.Derive(_fishBoxRect);
                boxRa.DrawSelf("FishBox", new Pen(Color.LightPink, 2));
                return _fishBoxRect;
            }

            VisionContext.Instance().DrawContent.RemoveRect("FishBox");
            return Rect.Empty;
        }

        private bool _isFishingProcess = false; // После поднятия стержня он будет установлен в положениеtrue
        private int _biteTipsExitCount = 0; // ловит рыбу提示持续时间
        private int _notFishingAfterBiteCount = 0; // 提竿后没有ловит рыбу的时间
        private Rect _baseBiteTips = Rect.Empty;

        /// <summary>
        /// Автоматический подъем столба
        /// </summary>
        /// <param name="content"></param>
        private void FishBite(CaptureContent content)
        {
            if (_isFishingProcess)
            {
                return;
            }

            // 自动идентифицировать的ловит рыбу框向下延伸到屏幕中间
            //var liftingWordsAreaRect = new Rect(fishBoxRect.X, fishBoxRect.Y + fishBoxRect.Height * 2,
            //    fishBoxRect.Width, content.CaptureRectArea.SrcMat.Height / 2 - fishBoxRect.Y - fishBoxRect.Height * 5);
            // Верхняя половина экрана и середина1/3Область
            var liftingWordsAreaRect = new Rect(content.CaptureRectArea.SrcMat.Width / 3, 0, content.CaptureRectArea.SrcMat.Width / 3,
                content.CaptureRectArea.SrcMat.Height / 2);
            //VisionContext.Instance().DrawContent.PutRect("liftingWordsAreaRect", liftingWordsAreaRect.ToRectDrawable(new Pen(Color.Cyan, 2)));
            var wordCaptureMat = new Mat(content.CaptureRectArea.SrcMat, liftingWordsAreaRect);
            var currentBiteWordsTips = AutoFishingImageRecognition.MatchFishBiteWords(wordCaptureMat, liftingWordsAreaRect);
            if (currentBiteWordsTips != Rect.Empty)
            {
                if (_baseBiteTips == Rect.Empty)
                {
                    _baseBiteTips = currentBiteWordsTips;
                }
                else
                {
                    if (Math.Abs(_baseBiteTips.X - currentBiteWordsTips.X) < 10
                        && Math.Abs(_baseBiteTips.Y - currentBiteWordsTips.Y) < 10
                        && Math.Abs(_baseBiteTips.Width - currentBiteWordsTips.Width) < 10
                        && Math.Abs(_baseBiteTips.Height - currentBiteWordsTips.Height) < 10)
                    {
                        _biteTipsExitCount++;
                        // VisionContext.Instance().DrawContent.PutRect("FishBiteTips",
                        //     currentBiteWordsTips
                        //         .ToWindowsRectangleOffset(liftingWordsAreaRect.X, liftingWordsAreaRect.Y)
                        //         .ToRectDrawable());
                        using var tipsRa = content.CaptureRectArea.Derive(currentBiteWordsTips + liftingWordsAreaRect.Location);
                        tipsRa.DrawSelf("FishBiteTips");

                        if (_biteTipsExitCount >= content.FrameRate / 4d)
                        {
                            // Решение о снятии имиджевого столба
                            using var liftRodButtonRa = content.CaptureRectArea.Find(_autoFishingAssets.LiftRodButtonRo);
                            if (!liftRodButtonRa.IsEmpty())
                            {
                                Simulation.SendInput.Mouse.LeftButtonClick();
                                _logger.LogInformation(@"┌------------------------┐");
                                _logger.LogInformation("  Автоматический подъем столба(图像идентифицировать)");
                                _isFishingProcess = true;
                                _biteTipsExitCount = 0;
                                _baseBiteTips = Rect.Empty;
                                VisionContext.Instance().DrawContent.RemoveRect("FishBiteTips");
                                return;
                            }

                            // OCR Решение при подъеме шеста
                            var text = _ocrService.Ocr(new Mat(content.CaptureRectArea.SrcGreyMat,
                                new Rect(currentBiteWordsTips.X + liftingWordsAreaRect.X,
                                    currentBiteWordsTips.Y + liftingWordsAreaRect.Y,
                                    currentBiteWordsTips.Width, currentBiteWordsTips.Height)));
                            if (!string.IsNullOrEmpty(text) && StringUtils.RemoveAllSpace(text).Contains("В настоящее время нет в интерфейсе выбора персонажа."))
                            {
                                Simulation.SendInput.Mouse.LeftButtonClick();
                                _logger.LogInformation(@"┌------------------------┐");
                                _logger.LogInformation("  Автоматический подъем столба(OCR)");
                                _isFishingProcess = true;
                                _biteTipsExitCount = 0;
                                _baseBiteTips = Rect.Empty;
                                VisionContext.Instance().DrawContent.RemoveRect("FishBiteTips");
                            }
                        }
                    }
                    else
                    {
                        _biteTipsExitCount = 0;
                        _baseBiteTips = currentBiteWordsTips;
                        VisionContext.Instance().DrawContent.RemoveRect("FishBiteTips");
                    }

                    if (_biteTipsExitCount >= content.FrameRate / 2d)
                    {
                        Simulation.SendInput.Mouse.LeftButtonClick();
                        _logger.LogInformation(@"┌------------------------┐");
                        _logger.LogInformation("  Автоматический подъем столба(блок текста)");
                        _isFishingProcess = true;
                        _biteTipsExitCount = 0;
                        _baseBiteTips = Rect.Empty;
                        VisionContext.Instance().DrawContent.RemoveRect("FishBiteTips");
                    }
                }
            }
        }

        private int _noRectsCount = 0;
        private Rect _cur, _left, _right;
        private MOUSEEVENTF _prevMouseEvent = 0x0;
        private bool _findFishBoxTips;

        /// <summary>
        /// удочка
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fishBarMat"></param>
        private void Fishing(CaptureContent content, Mat fishBarMat)
        {
            var simulator = Simulation.SendInput;
            var rects = AutoFishingImageRecognition.GetFishBarRect(fishBarMat);
            if (rects != null && rects.Count > 0)
            {
                if (rects.Count >= 2 && _prevMouseEvent == 0x0 && !_findFishBoxTips)
                {
                    _findFishBoxTips = true;
                    _logger.LogInformation("  идентифицировать到ловит рыбу框，Автоматически вытягивая...");
                }

                // Превосходить3Прямоугольник – это аномалия，取高度最高的三и выйти из игры进行идентифицировать
                if (rects.Count > 3)
                {
                    rects.Sort((a, b) => b.Height.CompareTo(a.Height));
                    rects.RemoveRange(3, rects.Count - 3);
                }

                //Debug.WriteLine($"идентифицировать到{rects.Count} и выйти из игры");
                if (rects.Count == 2)
                {
                    if (rects[0].Width < rects[1].Width)
                    {
                        _cur = rects[0];
                        _left = rects[1];
                    }
                    else
                    {
                        _cur = rects[1];
                        _left = rects[0];
                    }

                    PutRects(_left, _cur, new Rect());

                    if (_cur.X < _left.X)
                    {
                        if (_prevMouseEvent != MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN)
                        {
                            simulator.Mouse.LeftButtonDown();
                            //Simulator.PostMessage(TaskContext.Instance().GameHandle).LeftButtonDown();
                            _prevMouseEvent = MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;
                            //Debug.WriteLine("Недостаточно прогресса левая кнопка нажата");
                        }
                    }
                    else
                    {
                        if (_prevMouseEvent == MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN)
                        {
                            simulator.Mouse.LeftButtonUp();
                            //Simulator.PostMessage(TaskContext.Instance().GameHandle).LeftButtonUp();
                            _prevMouseEvent = MOUSEEVENTF.MOUSEEVENTF_LEFTUP;
                            //Debug.WriteLine("Прогресс превышен Отпустите левую кнопку");
                        }
                    }
                }
                else if (rects.Count == 3)
                {
                    rects.Sort((a, b) => a.X.CompareTo(b.X));
                    _left = rects[0];
                    _cur = rects[1];
                    _right = rects[2];
                    PutRects(_left, _cur, _right);

                    if (_right.X + _right.Width - (_cur.X + _cur.Width) <= _cur.X - _left.X)
                    {
                        if (_prevMouseEvent == MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN)
                        {
                            simulator.Mouse.LeftButtonUp();
                            //Simulator.PostMessage(TaskContext.Instance().GameHandle).LeftButtonUp();
                            _prevMouseEvent = MOUSEEVENTF.MOUSEEVENTF_LEFTUP;
                            //Debug.WriteLine("Введите середину поля Отпустите левую кнопку");
                        }
                    }
                    else
                    {
                        if (_prevMouseEvent != MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN)
                        {
                            simulator.Mouse.LeftButtonDown();
                            //Simulator.PostMessage(TaskContext.Instance().GameHandle).LeftButtonDown();
                            _prevMouseEvent = MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;
                            //Debug.WriteLine("Не в середине кадра левая кнопка нажата");
                        }
                    }
                }
                else
                {
                    PutRects(new Rect(), new Rect(), new Rect());
                }
            }
            else
            {
                PutRects(new Rect(), new Rect(), new Rect());
                _noRectsCount++;
                // 2s 没有矩形视为已经完成ловит рыбу
                if (_noRectsCount >= content.FrameRate * 2 && _prevMouseEvent != 0x0)
                {
                    _findFishBoxTips = false;
                    _isFishingProcess = false;
                    _isThrowRod = false;
                    _prevMouseEvent = 0x0;
                    _logger.LogInformation("  ловит рыбу结束");
                    _logger.LogInformation(@"└------------------------┘");

                    // Убедитесь, что мышь отпущена
                    simulator.Mouse.LeftButtonUp();

                    Sleep(1000);

                    MoveViewpointDown();
                    Sleep(500);
                }

                CheckFishingUserInterface(content);
            }

            // 提竿后没有ловит рыбу的情况
            if (_isFishingProcess && !_findFishBoxTips)
            {
                _notFishingAfterBiteCount++;
                if (_notFishingAfterBiteCount >= decimal.ToDouble(content.FrameRate) * 2)
                {
                    _isFishingProcess = false;
                    _isThrowRod = false;
                    _notFishingAfterBiteCount = 0;
                    _logger.LogInformation("  X 提竿后没有ловит рыбу，перезагрузить!");
                    _logger.LogInformation(@"└------------------------┘");
                }
            }
            else
            {
                _notFishingAfterBiteCount = 0;
            }
        }

        /// <summary>
        /// 检查是否退出ловит рыбу界面
        /// </summary>
        /// <param name="content"></param>
        private void CheckFishingUserInterface(CaptureContent content)
        {
            var prevIsExclusive = IsExclusive;
            IsExclusive = FindButtonForExclusive(content);
            if (IsEnabled && !prevIsExclusive && IsExclusive)
            {
                _logger.LogInformation("→ {Text}", "автоматическая рыбалка，запускать！");
                var autoThrowRodEnabled = TaskContext.Instance().Config.AutoFishingConfig.AutoThrowRodEnabled;
                _logger.LogInformation("当前自动选饵Метательная удочка状态[{Enabled}]", autoThrowRodEnabled.ToChinese());
                // if (autoThrowRodEnabled)
                // {
                //     _logger.LogInformation("Фонтейн、须弥地区暂不支持Автоматическое метание шеста，如果существовать这两个地区ловит рыбу请关闭Автоматическое метание шеста功能");
                // }
                _switchBaitContinuouslyFrameNum = 0;
                _waitBiteContinuouslyFrameNum = 0;
                _noFishActionContinuouslyFrameNum = 0;
                _isThrowRod = false;
                _selectedBaitName = string.Empty;
            }
            else if (prevIsExclusive && !IsExclusive)
            {
                _logger.LogInformation("← {Text}", "退出ловит рыбу界面");
                _isThrowRod = false;
                _fishBoxRect = Rect.Empty;
                VisionContext.Instance().DrawContent.ClearAll();
            }
        }

        private readonly Pen _pen = new(Color.Red, 1);

        private void PutRects(Rect left, Rect cur, Rect right)
        {
            //var list = new List<RectDrawable>
            //{
            //    left.ToWindowsRectangleOffset(_fishBoxRect.X, _fishBoxRect.Y).ToRectDrawable(_pen),
            //    cur.ToWindowsRectangleOffset(_fishBoxRect.X, _fishBoxRect.Y).ToRectDrawable(_pen),
            //    right.ToWindowsRectangleOffset(_fishBoxRect.X, _fishBoxRect.Y).ToRectDrawable(_pen)
            //};
            using var fishBoxRa = _currContent.CaptureRectArea.Derive(_fishBoxRect);
            var list = new List<RectDrawable>
            {
                fishBoxRa.ToRectDrawable(left, "left", _pen),
                fishBoxRa.ToRectDrawable(cur, "cur", _pen),
                fishBoxRa.ToRectDrawable(right, "right", _pen),
            };
            VisionContext.Instance().DrawContent.PutOrRemoveRectList("FishingBarAll", list);
        }

        ///// <summary>
        ///// чистый холст
        ///// </summary>
        //public void ClearDraw()
        //{
        //    VisionContext.Instance().DrawContent.PutOrRemoveRectList(new List<(string, RectDrawable)>
        //    {
        //        ("FishingBarLeft", new RectDrawable(System.Windows.Rect.Empty)),
        //        ("FishingBarCur", new RectDrawable(System.Windows.Rect.Empty)),
        //        ("FishingBarRight", new RectDrawable(System.Windows.Rect.Empty))
        //    });
        //    VisionContext.Instance().DrawContent.RemoveRect("FishBiteTips");
        //    VisionContext.Instance().DrawContent.RemoveRect("StartFishingButton");
        //    WeakReferenceMessenger.Default.Send(new PropertyChangedMessage<object>(this, "RemoveButton", new object(), "начинатьавтоматическая рыбалка"));
        //}

        //public void Stop()
        //{
        //    ClearDraw();
        //}
    }
}
