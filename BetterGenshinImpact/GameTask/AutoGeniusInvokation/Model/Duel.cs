using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Assets;
using BetterGenshinImpact.GameTask.AutoGeniusInvokation.Exception;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.Service.Notification;
using BetterGenshinImpact.View.Drawable;
using BetterGenshinImpact.ViewModel.Pages;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model;

/// <summary>
/// игра
/// </summary>
public class Duel
{
    private readonly ILogger<Duel> _logger = App.GetLogger<Duel>();

    public Character CurrentCharacter { get; set; }
    public Character[] Characters { get; set; } = new Character[4];

    /// <summary>
    /// Вырезать зону команды
    /// </summary>
    public List<ActionCommand> ActionCommandQueue { get; set; } = new List<ActionCommand>();

    /// <summary>
    /// Текущее число раундов
    /// </summary>
    public int RoundNum { get; set; } = 1;

    /// <summary>
    /// Положение карты персонажа
    /// </summary>
    public List<Rect> CharacterCardRects { get; set; }

    /// <summary>
    /// Количество карт на руке
    /// </summary>
    public int CurrentCardCount { get; set; } = 0;

    /// <summary>
    /// Количество кубиков
    /// </summary>
    public int CurrentDiceCount { get; set; } = 0;

    public CancellationTokenSource Cts { get; set; }

    private int _keqingECount = 0;

    public async Task RunAsync(GeniusInvokationTaskParam taskParam)
    {
        await Task.Run(() => { Run(taskParam); });
    }

    public void Run(GeniusInvokationTaskParam taskParam)
    {
        var hasLock = false;
        Cts = taskParam.Cts;
        try
        {
            AutoGeniusInvokationAssets.DestroyInstance();
            hasLock = TaskControl.TaskSemaphore.Wait(0);
            if (!hasLock)
            {
                _logger.LogError("Не удалось запустить функцию автоматического призыва Семи Святых.：В настоящее время выполняются независимые задачи，Пожалуйста, не повторяйте задания！");
                return;
            }

            LogScreenResolution();
            _logger.LogInformation("========================================");
            _logger.LogInformation("→ {Text}", "Полностью автоматический призыв семи святых，запускать！");
            NotificationHelper.SendTaskNotificationUsing(b => b.GeniusInvocation().Started().WithScreenshot(TaskControl.CaptureGameBitmap()).Build());
            GeniusInvokationControl.GetInstance().Init(taskParam);
            SystemControl.ActivateWindow();

            // играПодготовить Выберите начальную руку
            GeniusInvokationControl.GetInstance().CommonDuelPrepare();

            // Получить область персонажа
            try
            {
                CharacterCardRects = NewRetry.Do(() => GeniusInvokationControl.GetInstance().GetCharacterRects(), TimeSpan.FromSeconds(1.5), 3);
            }
            catch
            {
                // ignored
            }

            if (CharacterCardRects is not { Count: 3 })
            {
                CharacterCardRects = new List<Rect>();
                var defaultCharacterCardRects = TaskContext.Instance().Config.AutoGeniusInvokationConfig.DefaultCharacterCardRects;
                var assetScale = TaskContext.Instance().SystemInfo.AssetScale;
                for (var i = 0; i < defaultCharacterCardRects.Count; i++)
                {
                    CharacterCardRects.Add(defaultCharacterCardRects[i].Multiply(assetScale));
                }

                _logger.LogInformation("Получить область персонажанеудача，Использовать зону по умолчанию");
            }

            for (var i = 1; i < 4; i++)
            {
                Characters[i].Area = CharacterCardRects[i - 1];
            }

            // Играющая роль
            CurrentCharacter = ActionCommandQueue[0].Character;
            CurrentCharacter.ChooseFirst();

            // Область карты персонажа не распознается
            while (true)
            {
                _logger.LogInformation("--------------Нет.{RoundNum}круглый--------------", RoundNum);
                ClearCharacterStatus(); // четкий порядоккруглыйненормальное состояние
                if (RoundNum == 1)
                {
                    CurrentCardCount = 5;
                }
                else
                {
                    CurrentCardCount += 2;
                }

                CurrentDiceCount = 8;

                // Предварительно рассчитанная книгакруглыйвсе возможные элементы внутри // И отрегулируйте порядок идентификации материалов кубиков.
                var elementSet = PredictionDiceType();

                // 0 Бросить кости
                GeniusInvokationControl.GetInstance().ReRollDice(elementSet.ToArray());

                // жду своегокруглый // Бросить костиВремя анимации не определено  // Возможно, другая сторона сделала первый шаг
                GeniusInvokationControl.GetInstance().WaitForMyTurn(this, 1000);

                // Начните действовать
                while (true)
                {
                    // Действие заканчивается, когда кубиков больше не осталось.
                    _logger.LogInformation("Действие начинается,Текущее количество кубиков[{CurrentDiceCount}],Текущее количество карт на руке[{CurrentCardCount}]", CurrentDiceCount, CurrentCardCount);
                    if (CurrentDiceCount <= 0)
                    {
                        _logger.LogInformation("Кости были израсходованы");
                        GeniusInvokationControl.GetInstance().Sleep(2000);
                        break;
                    }

                    // Проверяйте текущего персонажа перед каждым действием
                    CurrentCharacter = GeniusInvokationControl.GetInstance().WhichCharacterActiveWithRetry(this);

                    // Прежде чем предпринимать действия, подтвердите действие.Количество кубиков
                    var diceCountFromOcr = GeniusInvokationControl.GetInstance().GetDiceCountByOcr();
                    if (diceCountFromOcr != -10)
                    {
                        var diceDiff = Math.Abs(CurrentDiceCount - diceCountFromOcr);
                        if (diceDiff is > 0 and <= 4)
                        {
                            _logger.LogInformation("Могут быть полевые карточки, влияющие на количество кубиков.[{CurrentDiceCount}] -> [{DiceCountFromOcr}]", CurrentDiceCount, diceCountFromOcr);
                            CurrentDiceCount = diceCountFromOcr;
                        }
                        else if (diceDiff > 4)
                        {
                            _logger.LogWarning(" OCRКоличество распознанных кубиков[{DiceCountFromOcr}]и рассчитанное количество кубиков[{CurrentDiceCount}]Разрыв большой，отказаться от результатов", diceCountFromOcr, CurrentDiceCount);
                        }
                    }

                    var alreadyExecutedActionIndex = new List<int>();
                    var alreadyExecutedActionCommand = new List<ActionCommand>();
                    var i = 0;
                    for (i = 0; i < ActionCommandQueue.Count; i++)
                    {
                        var actionCommand = ActionCommandQueue[i];
                        // Персонаж в команде не побеждён、Персонаж находится в ненормальном состоянии пропустить инструкцию
                        if (actionCommand.Character.IsDefeated || actionCommand.Character.StatusList?.Count > 0)
                        {
                            continue;
                        }

                        // текущийИграющая рольНе выполняйте указания этого персонажа при ненормальном состоянии организма.
                        if (CurrentCharacter.StatusList?.Count > 0 &&
                            actionCommand.Character.Index == CurrentCharacter.Index)
                        {
                            continue;
                        }

                        // 1. Судить и резать людей
                        if (CurrentCharacter.Index != actionCommand.Character.Index)
                        {
                            if (CurrentDiceCount >= 1)
                            {
                                actionCommand.SwitchLater();
                                CurrentDiceCount--;
                                alreadyExecutedActionIndex.Add(-actionCommand.Character.Index); // Отметить как выполненное
                                var switchAction = new ActionCommand
                                {
                                    Character = CurrentCharacter,
                                    Action = ActionEnum.SwitchLater,
                                    TargetIndex = actionCommand.Character.Index
                                };
                                alreadyExecutedActionCommand.Add(switchAction);
                                _logger.LogInformation("→Выполнение инструкции завершено：{Action}", switchAction);
                                break;
                            }
                            else
                            {
                                _logger.LogInformation("Недостаточно кубиков, чтобы перейти к следующему шагу：Поменяться ролями {CharacterIndex}", actionCommand.Character.Index);
                                break;
                            }
                        }

                        // 2. Определить использование навыков
                        if (actionCommand.GetAllDiceUseCount() > CurrentDiceCount)
                        {
                            _logger.LogInformation("Недостаточно кубиков, чтобы перейти к следующему шагу：{Action}", actionCommand);
                            break;
                        }
                        else
                        {
                            bool useSkillRes = actionCommand.UseSkill(this);
                            if (useSkillRes)
                            {
                                CurrentDiceCount -= actionCommand.GetAllDiceUseCount();
                                alreadyExecutedActionIndex.Add(i);
                                alreadyExecutedActionCommand.Add(actionCommand);
                                _logger.LogInformation("→Выполнение инструкции завершено：{Action}", actionCommand);
                                // прозрачныйEдобавить руку
                                if (actionCommand.Character.Name == "Цинцин" && actionCommand.TargetIndex == 2)
                                {
                                    _keqingECount++;
                                    if (_keqingECount % 2 == 0)
                                    {
                                        CurrentCardCount -= 1;
                                    }
                                    else
                                    {
                                        CurrentCardCount += 1;
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning("→Не удалось выполнить команду(Возможно, на руке недостаточно карт)：{Action}", actionCommand);
                                GeniusInvokationControl.GetInstance().Sleep(1000);
                                GeniusInvokationControl.GetInstance().ClickGameWindowCenter();
                            }

                            break;
                        }
                    }

                    if (alreadyExecutedActionIndex.Count != 0)
                    {
                        foreach (var index in alreadyExecutedActionIndex)
                        {
                            if (index >= 0)
                            {
                                ActionCommandQueue.RemoveAt(index);
                            }
                        }

                        alreadyExecutedActionIndex.Clear();
                        // Подождите, пока действие противника завершится. （Подожди еще немного, когда откроешь его.）
                        var sleepTime = ComputeWaitForMyTurnTime(alreadyExecutedActionCommand);
                        GeniusInvokationControl.GetInstance().WaitForMyTurn(this, sleepTime);
                        alreadyExecutedActionCommand.Clear();
                    }
                    else
                    {
                        // Если нет инструкций для выполнения затем выпрыгни из цикла
                        // TODO Также возможно, что персонаж умирает/Все символы заморожены, поэтому никакие инструкции не могут быть выполнены.
                        //if (i >= ActionCommandQueue.Count)
                        //{
                        //    throw new DuelEndException("Все инструкции по стратегии выполнены.，Завершить автоматическую игру в карты");
                        //}
                        GeniusInvokationControl.GetInstance().Sleep(2000);
                        break;
                    }

                    if (ActionCommandQueue.Count == 0)
                    {
                        throw new NormalEndException("Все инструкции по стратегии выполнены.，Завершить автоматическую игру в карты");
                    }
                }

                // круглыйЗаканчивать
                GeniusInvokationControl.GetInstance().Sleep(1000);
                _logger.LogInformation("Наш кликкруглыйЗаканчивать");
                GeniusInvokationControl.GetInstance().RoundEnd();

                // Подождите, пока другая сторона начнет действовать+круглыйУрегулирование
                GeniusInvokationControl.GetInstance().WaitOpponentAction(this);

                VisionContext.Instance().DrawContent.ClearAll();
                RoundNum++;
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogInformation(ex.Message);
            NotificationHelper.SendTaskNotificationUsing(b => b.GeniusInvocation().Cancelled().WithScreenshot(TaskControl.CaptureGameBitmap()).Build());
        }
        catch (NormalEndException ex)
        {
            _logger.LogInformation(ex.Message);
            _logger.LogInformation("играЗаканчивать");
            NotificationHelper.SendTaskNotificationUsing(b => b.GeniusInvocation().Success().WithScreenshot(TaskControl.CaptureGameBitmap()).Build());
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex.Message);
            _logger.LogDebug(ex.StackTrace);
            if (TaskContext.Instance().Config.DetailedErrorLogs)
            {
                _logger.LogError(ex.StackTrace);
            }
            NotificationHelper.SendTaskNotificationUsing(b => b.GeniusInvocation().Failure().WithScreenshot(TaskControl.CaptureGameBitmap()).Build());
        }
        finally
        {
            VisionContext.Instance().DrawContent.ClearAll();
            TaskSettingsPageViewModel.SetSwitchAutoGeniusInvokationButtonText(false);
            _logger.LogInformation("← {Text}", "покидатьПолностью автоматический призыв семи святых");
            TaskTriggerDispatcher.Instance().StartTimer();

            if (hasLock)
            {
                TaskControl.TaskSemaphore.Release();
            }
        }
    }

    private HashSet<ElementalType> PredictionDiceType()
    {
        var actionUseDiceSum = 0;
        var elementSet = new HashSet<ElementalType>
        {
            ElementalType.Omni
        };
        for (var i = 0; i < ActionCommandQueue.Count; i++)
        {
            var actionCommand = ActionCommandQueue[i];

            // Это можно выполнить только в том случае, если персонаж не был побежден.
            if (actionCommand.Character.IsDefeated)
            {
                continue;
            }

            // проходитьКоличество кубиковОпределить, можно ли его выполнить

            // 1. Судить и резать людей
            if (i > 0 && actionCommand.Character.Index != ActionCommandQueue[i - 1].Character.Index)
            {
                actionUseDiceSum++;
                if (actionUseDiceSum > CurrentDiceCount)
                {
                    break;
                }
                else
                {
                    // elementSet.Add(actionCommand.GetDiceUseElementType());
                    //executeActionIndex.Add(-actionCommand.Character.Index);
                }
            }

            // 2. Определить использование навыков
            actionUseDiceSum += actionCommand.GetAllDiceUseCount();
            if (actionUseDiceSum > CurrentDiceCount)
            {
                break;
            }
            else
            {
                elementSet.Add(actionCommand.GetDiceUseElementType());
                //executeActionIndex.Add(i);
            }
        }

        // Отрегулируйте порядок материалов для распознавания элементов.
        GeniusInvokationControl.GetInstance().SortActionPhaseDiceMats(elementSet);

        return elementSet;
    }

    public void ClearCharacterStatus()
    {
        foreach (var character in Characters)
        {
            character?.StatusList?.Clear();
        }
    }

    /// <summary>
    /// Рассчитать время ожидания на основе ранее выполненных команд
    /// Ожидание окончательного хода15Второй
    /// Быстрое переключение ожидания3Второй
    /// </summary>
    /// <param name="alreadyExecutedActionCommand"></param>
    /// <returns></returns>
    private int ComputeWaitForMyTurnTime(List<ActionCommand> alreadyExecutedActionCommand)
    {
        foreach (var command in alreadyExecutedActionCommand)
        {
            if (command.Action == ActionEnum.UseSkill && command.TargetIndex == 1)
            {
                return 15000;
            }

            // мона переключатель ждет3Второй
            if (command.Character.Name == "Мона" && command.Action == ActionEnum.SwitchLater)
            {
                return 3000;
            }
        }

        return 10000;
    }

    /// <summary>
    /// Получить порядок переключения ролей
    /// </summary>
    /// <returns></returns>
    public List<int> GetCharacterSwitchOrder()
    {
        List<int> orderList = new List<int>();
        for (var i = 0; i < ActionCommandQueue.Count; i++)
        {
            if (!orderList.Contains(ActionCommandQueue[i].Character.Index))
            {
                orderList.Add(ActionCommandQueue[i].Character.Index);
            }
        }

        return orderList;
    }

    ///// <summary>
    ///// Получить количество живых персонажей
    ///// </summary>
    ///// <returns></returns>
    //public int GetCharacterAliveNum()
    //{
    //    int num = 0;
    //    foreach (var character in Characters)
    //    {
    //        if (character != null && !character.IsDefeated)
    //        {
    //            num++;
    //        }
    //    }

    //    return num;
    //}

    private void LogScreenResolution()
    {
        var gameScreenSize = SystemControl.GetGameScreenRect(TaskContext.Instance().GameHandle);
        if (gameScreenSize.Width != 1920 || gameScreenSize.Height != 1080)
        {
            _logger.LogWarning("Разрешение окна игры не 1920x1080 ！Текущее разрешение {Width}x{Height} , Нет 1920x1080 Игра с разными разрешениями может не иметь возможности правильно использовать автоматический вызов Семи Святых. !", gameScreenSize.Width, gameScreenSize.Height);
        }
    }
}
