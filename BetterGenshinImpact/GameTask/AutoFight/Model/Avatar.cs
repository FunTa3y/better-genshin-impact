using System;
using System.Diagnostics;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.Helpers;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Linq;
using System.Threading;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Model.Area;
using Vanara.PInvoke;
using static BetterGenshinImpact.GameTask.Common.TaskControl;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.GameTask.AutoFight.Model;

/// <summary>
/// Роль в команде
/// </summary>
public class Avatar
{
    /// <summary>
    /// Имя роли Китайский
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Имя роли Английский
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// Серийный номер внутри команды
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Тип оружия
    /// </summary>
    public string Weapon { get; set; }

    /// <summary>
    /// Элементарные боевые навыкиCD
    /// </summary>
    public double SkillCd { get; set; }

    /// <summary>
    /// длинныйв соответствии сЭлементарные боевые навыкиCD
    /// </summary>
    public double SkillHoldCd { get; set; }

    /// <summary>
    /// Элементальный взрывCD
    /// </summary>
    public double BurstCd { get; set; }

    /// <summary>
    /// Элементальный взрывдаГотовы или нет
    /// </summary>
    public bool IsBurstReady { get; set; }

    /// <summary>
    /// Прямоугольное положение имени
    /// </summary>
    public Rect NameRect { get; set; }

    /// <summary>
    /// Позиция номера справа от имени
    /// </summary>
    public Rect IndexRect { get; set; }

    /// <summary>
    /// Токен отмены задачи
    /// </summary>
    public CancellationTokenSource? Cts { get; set; }

    /// <summary>
    /// сцена боя
    /// </summary>
    public CombatScenes CombatScenes { get; set; }

    public Avatar(CombatScenes combatScenes, string name, int index, Rect nameRect)
    {
        CombatScenes = combatScenes;
        Name = name;
        Index = index;
        NameRect = nameRect;

        var ca = DefaultAutoFightConfig.CombatAvatarMap[name];
        NameEn = ca.NameEn;
        Weapon = ca.Weapon;
        SkillCd = ca.SkillCd;
        SkillHoldCd = ca.SkillHoldCd;
        BurstCd = ca.BurstCd;
    }

    /// <summary>
    /// Побежден ли персонаж
    /// текстовый диапазон
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public void ThrowWhenDefeated(ImageRegion region)
    {
        using var confirmRectArea = region.Find(AutoFightContext.Instance.FightAssets.ConfirmRa);
        if (!confirmRectArea.IsEmpty())
        {
            Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_ESCAPE);
            Sleep(600, Cts);
            Simulation.SendInput.Keyboard.KeyPress(User32.VK.VK_M);
            throw new Exception("Персонаж побеждён，в соответствии с M ключ для открытия карты，Название наживки, которую ест большинство рыб。");
        }
    }

    /// <summary>
    /// Переключиться на эту роль
    /// выключательcdда1Второй，есливыключательнеудача，Попробую еще разВторосортныйвыключатель，Большинство попыток5Второсортный
    /// </summary>
    public void Switch()
    {
        for (var i = 0; i < 30; i++)
        {
            if (Cts is { IsCancellationRequested: true })
            {
                return;
            }

            var region = GetRectAreaFromDispatcher();
            ThrowWhenDefeated(region);

            var notActiveCount = CombatScenes.Avatars.Count(avatar => !avatar.IsActive(region));
            if (IsActive(region) && notActiveCount == 3)
            {
                return;
            }

            AutoFightContext.Instance.Simulator.KeyPress(User32.VK.VK_1 + (byte)Index - 1);
            // Debug.WriteLine($"выключательприезжать{Index}Позиция номера");
            // Cv2.ImWrite($"log/выключатель.png", content.CaptureRectArea.SrcMat);
            Sleep(250, Cts);
        }
    }

    /// <summary>
    /// Переключиться на эту роль
    /// выключательcdда1Второй，есливыключательнеудача，Попробую еще разВторосортныйвыключатель，Большинство попыток5Второсортный
    /// </summary>
    public void SwitchWithoutCts()
    {
        for (var i = 0; i < 10; i++)
        {
            var region = GetRectAreaFromDispatcher();
            ThrowWhenDefeated(region);

            var notActiveCount = CombatScenes.Avatars.Count(avatar => !avatar.IsActive(region));
            if (IsActive(region) && notActiveCount == 3)
            {
                return;
            }

            AutoFightContext.Instance.Simulator.KeyPress(User32.VK.VK_1 + (byte)Index - 1);
            Sleep(250);
        }
    }

    /// <summary>
    /// даСражаться или нет
    /// </summary>
    /// <returns></returns>
    public bool IsActive(ImageRegion region)
    {
        if (IndexRect == Rect.Empty)
        {
            throw new Exception("IndexRectПусто");
        }
        else
        {
            // вырезатьIndexRectобласть
            var indexRa = region.DeriveCrop(IndexRect);
            // Cv2.ImWrite($"log/indexRa_{Name}.png", indexRa.SrcMat);
            var count = OpenCvCommonHelper.CountGrayMatColor(indexRa.SrcGreyMat, 251, 255);
            if (count * 1.0 / (IndexRect.Width * IndexRect.Height) > 0.5)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    /// <summary>
    /// даСражаться или нет
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public bool IsActiveNoIndexRect(ImageRegion region)
    {
        // Судите по номеру символа справа.даСтоит ли бороться
        if (IndexRect == Rect.Empty)
        {
            var assetScale = TaskContext.Instance().SystemInfo.AssetScale;
            // вырезатькомандаобласть
            var teamRa = region.DeriveCrop(AutoFightContext.Instance.FightAssets.TeamRect);
            var blockX = NameRect.X + NameRect.Width * 2 - 10;
            var block = teamRa.DeriveCrop(new Rect(blockX, NameRect.Y, teamRa.Width - blockX, NameRect.Height * 2));
            // Cv2.ImWrite($"block_{Name}.png", block.SrcMat);
            // возьми белоеобласть
            var bMat = OpenCvCommonHelper.Threshold(block.SrcMat, new Scalar(255, 255, 255), new Scalar(255, 255, 255));
            // Cv2.ImWrite($"block_b_{Name}.png", bMat);
            // Распознавание прямоугольника
            Cv2.FindContours(bMat, out var contours, out _, RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);
            if (contours.Length > 0)
            {
                var boxes = contours.Select(Cv2.BoundingRect).Where(w => w.Width >= 20 * assetScale && w.Height >= 18 * assetScale).OrderByDescending(w => w.Width).ToList();
                if (boxes.Any())
                {
                    IndexRect = boxes.First();
                    return false;
                }
            }
        }
        else
        {
            // вырезатьIndexRectобласть
            var teamRa = region.DeriveCrop(AutoFightContext.Instance.FightAssets.TeamRect);
            var blockX = NameRect.X + NameRect.Width * 2 - 10;
            var indexBlock = teamRa.DeriveCrop(new Rect(blockX + IndexRect.X, NameRect.Y + IndexRect.Y, IndexRect.Width, IndexRect.Height));
            // Cv2.ImWrite($"indexBlock_{Name}.png", indexBlock.SrcMat);
            var count = OpenCvCommonHelper.CountGrayMatColor(indexBlock.SrcGreyMat, 255);
            if (count * 1.0 / (IndexRect.Width * IndexRect.Height) > 0.5)
            {
                return false;
            }
        }

        Logger.LogInformation("{Name} Сейчас играю", Name);
        return true;
    }

    /// <summary>
    /// Обычная атака
    /// </summary>
    /// <param name="ms">Продолжительность атаки，предположениеда200кратные</param>
    public void Attack(int ms = 0)
    {
        while (ms >= 0)
        {
            if (Cts is { IsCancellationRequested: true })
            {
                return;
            }

            AutoFightContext.Instance.Simulator.LeftButtonClick();
            ms -= 200;
            Sleep(200, Cts);
        }
    }

    /// <summary>
    /// использоватьЭлементарные боевые навыки E
    /// </summary>
    public void UseSkill(bool hold = false)
    {
        for (var i = 0; i < 1; i++)
        {
            if (Cts is { IsCancellationRequested: true })
            {
                return;
            }

            if (hold)
            {
                if (Name == "Насида")
                {
                    AutoFightContext.Instance.Simulator.KeyDown(User32.VK.VK_E);
                    Sleep(300, Cts);
                    for (int j = 0; j < 10; j++)
                    {
                        Simulation.SendInput.Mouse.MoveMouseBy(1000, 0);
                        Sleep(50); // Непрерывная работа не должнаctsОтмена
                    }

                    Sleep(300); // Непрерывная работа не должнаctsОтмена
                    AutoFightContext.Instance.Simulator.KeyUp(User32.VK.VK_E);
                }
                else
                {
                    AutoFightContext.Instance.Simulator.LongKeyPress(User32.VK.VK_E);
                }
            }
            else
            {
                AutoFightContext.Instance.Simulator.KeyPress(User32.VK.VK_E);
            }

            Sleep(200, Cts);

            var region = GetRectAreaFromDispatcher();
            ThrowWhenDefeated(region);
            var cd = GetSkillCurrentCd(region);
            if (cd > 0)
            {
                Logger.LogInformation(hold ? "{Name} длинныйв соответствии сЭлементарные боевые навыки，cd:{Cd}" : "{Name} точкав соответствии сЭлементарные боевые навыки，cd:{Cd}", Name, cd);
                // todo ПучокcdПрисоединиться к очереди выполнения
                return;
            }
        }
    }

    /// <summary>
    /// Элементарные боевые навыкидаЭтоCDсередина
    /// Нижний правый 267x132
    /// 77x77
    /// </summary>
    public double GetSkillCurrentCd(ImageRegion imageRegion)
    {
        var eRa = imageRegion.DeriveCrop(AutoFightContext.Instance.FightAssets.ERect);
        var text = OcrFactory.Paddle.Ocr(eRa.SrcGreyMat);
        return StringUtils.TryParseDouble(text);
    }

    /// <summary>
    /// использоватьЭлементальный взрыв Q
    /// Qотпустить, подождать 2s Таймаут означает нетQНавык
    /// </summary>
    public void UseBurst()
    {
        // var isBurstReleased = false;
        for (var i = 0; i < 10; i++)
        {
            if (Cts is { IsCancellationRequested: true })
            {
                return;
            }

            AutoFightContext.Instance.Simulator.KeyPress(User32.VK.VK_Q);
            Sleep(200, Cts);

            var region = GetRectAreaFromDispatcher();
            ThrowWhenDefeated(region);
            var notActiveCount = CombatScenes.Avatars.Count(avatar => !avatar.IsActive(region));
            if (notActiveCount == 0)
            {
                // isBurstReleased = true;
                Sleep(1500, Cts);
                return;
            }
            // else
            // {
            //     if (!isBurstReleased)
            //     {
            //         var cd = GetBurstCurrentCd(content);
            //         if (cd > 0)
            //         {
            //             Logger.LogInformation("{Name} освобожденЭлементальный взрыв，cd:{Cd}", Name, cd);
            //             // todo  ПучокcdПрисоединиться к очереди выполнения
            //             return;
            //         }
            //     }
            // }
        }
    }

    // /// <summary>
    // /// Элементальный взрывдаЭтоCDсередина
    // /// Нижний правый 157x165
    // /// 110x110
    // /// </summary>
    // public double GetBurstCurrentCd(CaptureContent content)
    // {
    //     var qRa = content.CaptureRectArea.Crop(AutoFightContext.Instance.FightAssets.QRect);
    //     var text = OcrFactory.Paddle.Ocr(qRa.SrcGreyMat);
    //     return StringUtils.TryParseDouble(text);
    // }

    /// <summary>
    /// спринт
    /// </summary>
    public void Dash(int ms = 0)
    {
        if (Cts is { IsCancellationRequested: true })
        {
            return;
        }

        if (ms == 0)
        {
            ms = 200;
        }

        AutoFightContext.Instance.Simulator.RightButtonDown();
        Sleep(ms); // спринтне может бытьctsОтмена
        AutoFightContext.Instance.Simulator.RightButtonUp();
    }

    public void Walk(string key, int ms)
    {
        if (Cts is { IsCancellationRequested: true })
        {
            return;
        }

        User32.VK vk = User32.VK.VK_NONAME;
        if (key == "w")
        {
            vk = User32.VK.VK_W;
        }
        else if (key == "s")
        {
            vk = User32.VK.VK_S;
        }
        else if (key == "a")
        {
            vk = User32.VK.VK_A;
        }
        else if (key == "d")
        {
            vk = User32.VK.VK_D;
        }

        if (vk == User32.VK.VK_NONAME)
        {
            return;
        }

        AutoFightContext.Instance.Simulator.KeyDown(vk);
        Sleep(ms); // ходить нельзяctsОтмена
        AutoFightContext.Instance.Simulator.KeyUp(vk);
    }

    /// <summary>
    /// мобильная камера
    /// </summary>
    /// <param name="pixelDeltaX">отрицательное числодаСдвиг влево，Положительное числодаДвигаться вправо</param>
    /// <param name="pixelDeltaY"></param>
    public void MoveCamera(int pixelDeltaX, int pixelDeltaY)
    {
        Simulation.SendInput.Mouse.MoveMouseBy(pixelDeltaX, pixelDeltaY);
    }

    /// <summary>
    /// ждать
    /// </summary>
    /// <param name="ms"></param>
    public void Wait(int ms)
    {
        Sleep(ms); // Благодаря наличию макроопераций，ждатьне должно бытьctsОтмена
    }

    /// <summary>
    /// Прыгать
    /// </summary>
    public void Jump()
    {
        AutoFightContext.Instance.Simulator.KeyPress(User32.VK.VK_SPACE);
    }

    /// <summary>
    /// Дуть
    /// </summary>
    public void Charge(int ms = 0)
    {
        if (ms == 0)
        {
            ms = 1000;
        }

        if (Name == "Пупок")
        {
            AutoFightContext.Instance.Simulator.LeftButtonDown();
            while (ms >= 0)
            {
                if (Cts is { IsCancellationRequested: true })
                {
                    return;
                }

                Simulation.SendInput.Mouse.MoveMouseBy(1000, 0);
                ms -= 50;
                Sleep(50); // Непрерывная работа не должнаctsОтмена
            }

            AutoFightContext.Instance.Simulator.LeftButtonUp();
        }
        else
        {
            AutoFightContext.Instance.Simulator.LeftButtonDown();
            Sleep(ms); // Непрерывная работа не должнаctsОтмена
            AutoFightContext.Instance.Simulator.LeftButtonUp();
        }
    }

    public void MouseDown(string key = "left")
    {
        key = key.ToLower();
        if (key == "left")
        {
            AutoFightContext.Instance.Simulator.LeftButtonDown();
        }
        else if (key == "right")
        {
            AutoFightContext.Instance.Simulator.RightButtonDown();
        }
        else if (key == "middle")
        {
            Simulation.SendInput.Mouse.MiddleButtonDown();
        }
    }

    public void MouseUp(string key = "left")
    {
        key = key.ToLower();
        if (key == "left")
        {
            AutoFightContext.Instance.Simulator.LeftButtonUp();
        }
        else if (key == "right")
        {
            AutoFightContext.Instance.Simulator.RightButtonUp();
        }
        else if (key == "middle")
        {
            Simulation.SendInput.Mouse.MiddleButtonUp();
        }
    }

    public void Click(string key = "left")
    {
        key = key.ToLower();
        if (key == "left")
        {
            AutoFightContext.Instance.Simulator.LeftButtonClick();
        }
        else if (key == "right")
        {
            AutoFightContext.Instance.Simulator.RightButtonClick();
        }
        else if (key == "middle")
        {
            Simulation.SendInput.Mouse.MiddleButtonClick();
        }
    }

    public void MoveBy(int x, int y)
    {
        Simulation.SendInput.Mouse.MoveMouseBy(x, y);
    }

    public void KeyDown(string key)
    {
        var vk = User32Helper.ToVk(key);
        AutoFightContext.Instance.Simulator.KeyDown(vk);
    }

    public void KeyUp(string key)
    {
        var vk = User32Helper.ToVk(key);
        AutoFightContext.Instance.Simulator.KeyUp(vk);
    }

    public void KeyPress(string key)
    {
        var vk = User32Helper.ToVk(key);
        AutoFightContext.Instance.Simulator.KeyPress(vk);
    }
}
