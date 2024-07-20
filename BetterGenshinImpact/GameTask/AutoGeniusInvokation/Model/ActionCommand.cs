using System;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model
{
    public class ActionCommand
    {
        /// <summary>
        ///  Роль
        /// </summary>
        public Character Character { get; set; }

        public ActionEnum Action { get; set; }

        /// <summary>
        /// целевое число（Номер навыка，справа налево）
        /// </summary>
        public int TargetIndex { get; set; }

        public override string ToString()
        {
            if (Action == ActionEnum.UseSkill)
            {
                if (string.IsNullOrEmpty(Character.Skills[TargetIndex].Name))
                {
                    return $"【{Character.Name}】использовать【Навык{TargetIndex}】";
                }
                else
                {
                    return $"【{Character.Name}】использовать【{Character.Skills[TargetIndex].Name}】";
                }
            }
            else if (Action == ActionEnum.SwitchLater)
            {
                return $"【{Character.Name}】переключить на【Роль{TargetIndex}】";
            }
            else
            {
                return base.ToString();
            }
        }


        public int GetSpecificElementDiceUseCount()
        {
            if (Action == ActionEnum.UseSkill)
            {
                return Character.Skills[TargetIndex].SpecificElementCost;
            }
            else
            {
                throw new ArgumentException("неизвестное действие");
            }
        }

        public int GetAllDiceUseCount()
        {
            if (Action == ActionEnum.UseSkill)
            {
                return Character.Skills[TargetIndex].AllCost;
            }
            else
            {
                throw new ArgumentException("неизвестное действие");
            }
        }

        public ElementalType GetDiceUseElementType()
        {
            if (Action == ActionEnum.UseSkill)
            {
                return Character.Element;
            }
            else
            {
                throw new ArgumentException("неизвестное действие");
            }
        }

        public bool SwitchLater()
        {
            return Character.SwitchLater();
        }

        public bool UseSkill(Duel duel)
        {
            return Character.UseSkill(TargetIndex, duel);
        }
    }
}