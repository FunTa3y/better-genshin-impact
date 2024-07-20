﻿using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.AutoFight.Assets;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Helpers;
using Compunet.YoloV8;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sdcb.PaddleOCR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask.Model.Area;
using static BetterGenshinImpact.GameTask.Common.TaskControl;

namespace BetterGenshinImpact.GameTask.AutoFight.Model;

/// <summary>
/// сцена боя
/// </summary>
public class CombatScenes : IDisposable
{
    /// <summary>
    /// Текущий состав
    /// </summary>
    public Avatar[] Avatars { get; set; } = new Avatar[1];

    public Dictionary<string, Avatar> AvatarMap { get; set; } = new();

    public int AvatarCount { get; set; }

    private readonly YoloV8Predictor _predictor =
        YoloV8Builder.CreateDefaultBuilder()
            .UseOnnxModel(Global.Absolute("Assets\\Model\\Common\\avatar_side_classify_sim.onnx"))
            .WithSessionOptions(BgiSessionOption.Instance.Options)
            .Build();

    /// <summary>
    /// проходитьYOLOКлассификатор определяет роли внутри команды
    /// </summary>
    /// <param name="imageRegion">Скриншоты полной версии игры.</param>
    public CombatScenes InitializeTeam(ImageRegion imageRegion)
    {
        // Приоритизация конфигурации
        if (!string.IsNullOrEmpty(TaskContext.Instance().Config.AutoFightConfig.TeamNames))
        {
            InitializeTeamFromConfig(TaskContext.Instance().Config.AutoFightConfig.TeamNames);
            return this;
        }

        // Определите команду
        var names = new string[4];
        var displayNames = new string[4];
        try
        {
            for (var i = 0; i < AutoFightAssets.Instance.AvatarSideIconRectList.Count; i++)
            {
                var ra = imageRegion.DeriveCrop(AutoFightAssets.Instance.AvatarSideIconRectList[i]);
                var pair = ClassifyAvatarCnName(ra.SrcBitmap, i + 1);
                names[i] = pair.Item1;
                if (!string.IsNullOrEmpty(pair.Item2))
                {
                    var costumeName = pair.Item2;
                    if (AutoFightAssets.Instance.AvatarCostumeMap.ContainsKey(costumeName))
                    {
                        costumeName = AutoFightAssets.Instance.AvatarCostumeMap[costumeName];
                    }

                    displayNames[i] = $"{pair.Item1}({costumeName})";
                }
                else
                {
                    displayNames[i] = pair.Item1;
                }
            }
            Logger.LogInformation("Признанная командная роль:{Text}", string.Join(",", displayNames));
            Avatars = BuildAvatars(names.ToList());
            AvatarMap = Avatars.ToDictionary(x => x.Name);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e.Message);
        }
        return this;
    }

    public (string, string) ClassifyAvatarCnName(Bitmap src, int index)
    {
        var className = ClassifyAvatarName(src, index);

        var nameEn = className;
        var costumeName = "";
        var i = className.IndexOf("Costume", StringComparison.Ordinal);
        if (i > 0)
        {
            nameEn = className[..i];
            costumeName = className[(i + 7)..];
        }

        var avatar = DefaultAutoFightConfig.CombatAvatarNameEnMap[nameEn];
        return (avatar.Name, costumeName);
    }

    public string ClassifyAvatarName(Bitmap src, int index)
    {
        SpeedTimer speedTimer = new();
        using var memoryStream = new MemoryStream();
        src.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Seek(0, SeekOrigin.Begin);
        speedTimer.Record("Преобразование изображения аватара на стороне персонажа");
        var result = _predictor.Classify(memoryStream);
        speedTimer.Record("Классификация и распознавание аватаров на стороне персонажа");
        Debug.WriteLine($"Результаты распознавания аватаров персонажей：{result}");
        speedTimer.DebugPrint();
        if (result.TopClass.Confidence < 0.8)
        {
            Cv2.ImWrite(@"log\avatar_side_classify_error.png", src.ToMat());
            throw new Exception($"Невозможно распознать{index}роль，Уверенность{result.TopClass.Confidence}，результат：{result.TopClass.Name.Name}");
        }

        return result.TopClass.Name.Name;
    }

    private void InitializeTeamFromConfig(string teamNames)
    {
        var names = teamNames.Split(new[] { "，", "," }, StringSplitOptions.TrimEntries);
        if (names.Length != 4)
        {
            throw new Exception($"Неверное количество обязательных командных персонажей.，должно быть4индивидуальный，текущий{names.Length}индивидуальный");
        }

        // Псевдонимы преобразуются в полные имена.
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = DefaultAutoFightConfig.AvatarAliasToStandardName(names[i]);
        }

        Logger.LogInformation("Принудительно назначенные командные роли:{Text}", string.Join(",", names));
        TaskContext.Instance().Config.AutoFightConfig.TeamNames = string.Join(",", names);
        Avatars = BuildAvatars(names.ToList());
        AvatarMap = Avatars.ToDictionary(x => x.Name);
    }

    public bool CheckTeamInitialized()
    {
        if (Avatars.Length < 4)
        {
            return false;
        }

        return true;
    }

    private Avatar[] BuildAvatars(List<string> names, List<Rect>? nameRects = null)
    {
        AvatarCount = names.Count;
        var avatars = new Avatar[AvatarCount];
        for (var i = 0; i < AvatarCount; i++)
        {
            var nameRect = nameRects?[i] ?? Rect.Empty;
            avatars[i] = new Avatar(this, names[i], i + 1, nameRect)
            {
                IndexRect = AutoFightContext.Instance.FightAssets.AvatarIndexRectList[i]
            };
        }

        return avatars;
    }

    public void BeforeTask(CancellationTokenSource cts)
    {
        for (var i = 0; i < AvatarCount; i++)
        {
            Avatars[i].Cts = cts;
        }
    }

    public Avatar? SelectAvatar(string name)
    {
        return AvatarMap.TryGetValue(name, out var avatar) ? avatar : null;
    }

    #region OCRОпределите команду（Устарело）

    /// <summary>
    /// проходитьOCRОпределите командувнутренняя роль
    /// </summary>
    /// <param name="content">Скриншоты полной версии игры.</param>
    [Obsolete]
    public CombatScenes InitializeTeamOldOcr(CaptureContent content)
    {
        // Приоритизация конфигурации
        if (!string.IsNullOrEmpty(TaskContext.Instance().Config.AutoFightConfig.TeamNames))
        {
            InitializeTeamFromConfig(TaskContext.Instance().Config.AutoFightConfig.TeamNames);
            return this;
        }

        // Вырезать зону команды
        var teamRa = content.CaptureRectArea.DeriveCrop(AutoFightContext.Instance.FightAssets.TeamRectNoIndex);
        // отфильтровать белый цвет
        var hsvFilterMat = OpenCvCommonHelper.InRangeHsv(teamRa.SrcMat, new Scalar(0, 0, 210), new Scalar(255, 30, 255));

        // Определите командувнутренняя роль
        var result = OcrFactory.Paddle.OcrResult(hsvFilterMat);
        ParseTeamOcrResult(result, teamRa);
        return this;
    }

    [Obsolete]
    private void ParseTeamOcrResult(PaddleOcrResult result, ImageRegion rectArea)
    {
        List<string> names = new();
        List<Rect> nameRects = new();
        foreach (var item in result.Regions)
        {
            var name = StringUtils.ExtractChinese(item.Text);
            name = ErrorOcrCorrection(name);
            if (IsGenshinAvatarName(name))
            {
                names.Add(name);
                nameRects.Add(item.Rect.BoundingRect());
            }
        }

        if (names.Count != 4)
        {
            Logger.LogWarning("Признанная командная рольНеправильное количество，текущийидентифицироватьрезультат:{Text}", string.Join(",", names));
        }

        if (names.Count == 3)
        {
            // Особое отношение к бездомным
            // 4Команда более 100 человек，Идентификация Странника не поддерживается
            var wanderer = rectArea.Find(AutoFightContext.Instance.FightAssets.WandererIconRa);
            if (wanderer.IsEmpty())
            {
                wanderer = rectArea.Find(AutoFightContext.Instance.FightAssets.WandererIconNoActiveRa);
            }

            if (wanderer.IsEmpty())
            {
                // Дополнительная идентификация бездомных
                Logger.LogWarning("Вторая попытка идентификации не удалась，текущийидентифицироватьрезультат:{Text}", string.Join(",", names));
            }
            else
            {
                names.Clear();
                foreach (var item in result.Regions)
                {
                    var name = StringUtils.ExtractChinese(item.Text);
                    name = ErrorOcrCorrection(name);
                    if (IsGenshinAvatarName(name))
                    {
                        names.Add(name);
                        nameRects.Add(item.Rect.BoundingRect());
                    }

                    var rect = item.Rect.BoundingRect();
                    if (rect.Y > wanderer.Y && wanderer.Y + wanderer.Height > rect.Y + rect.Height && !names.Contains("странник"))
                    {
                        names.Add("странник");
                        nameRects.Add(item.Rect.BoundingRect());
                    }
                }

                if (names.Count != 4)
                {
                    Logger.LogWarning("изображениеидентифицироватьприезжатьстранник，Но не удалось определить информацию о местоположении внутри команды");
                }
            }
        }

        Logger.LogInformation("Признанная командная роль:{Text}", string.Join(",", names));
        Avatars = BuildAvatars(names, nameRects);
        AvatarMap = Avatars.ToDictionary(x => x.Name);
    }

    [Obsolete]
    private bool IsGenshinAvatarName(string name)
    {
        if (DefaultAutoFightConfig.CombatAvatarNames.Contains(name))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// верноOCRидентифицироватьрезультатвыполнить исправление ошибок
    /// TODO Осталось одно слово имени（эльф、фортепиано）Нераспознанная проблема
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [Obsolete]
    public string ErrorOcrCorrection(string name)
    {
        if (name.Contains("Наси"))
        {
            return "Насида";
        }

        return name;
    }

    #endregion OCRОпределите команду（Устарело）

    public void Dispose()
    {
        _predictor.Dispose();
    }
}
