using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.Helpers.Extensions;
using OpenCvSharp;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model
{
    public class Character
    {
        private readonly ILogger _logger = App.GetLogger<Character>();

        /// <summary>
        /// 1-3 Индексы массива согласованы
        /// </summary>
        public int Index { get; set; }

        public string Name { get; set; }
        public ElementalType Element { get; set; }
        public Skill[] Skills { get; set; }


        /// <summary>
        /// стоит ли потерпеть поражение
        /// </summary>
        public bool IsDefeated { get; set; }

        /// <summary>
        /// зарядная станция
        /// </summary>
        public int Energy { get; set; }

        /// <summary>
        /// hp
        /// -2 Не опознано
        /// </summary>
        public int Hp { get; set; } = -2;


        /// <summary>
        /// зарядная станцияот распознавания изображений
        /// </summary>
        public int EnergyByRecognition { get; set; }

        /// <summary>
        /// Негативный статус персонажа
        /// </summary>
        public List<CharacterStatusEnum> StatusList { get; set; } = new List<CharacterStatusEnum>();

        /// <summary>
        /// область персонажа
        /// </summary>
        public Rect Area { get; set; }

        /// <summary>
        /// область над объемом крови，Используется, чтобы определить, стоит ли идти на войну
        /// </summary>
        public Rect HpUpperArea { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Роль{Index}，");
            if (Hp != -2)
            {
                sb.Append($"HP={Hp}，");
            }
            sb.Append($"перезарядка={EnergyByRecognition}，");
            if (StatusList?.Count > 0)
            {
                sb.Append($"состояние：{string.Join(",", StatusList)}");
            }

            return sb.ToString();
        }

        public void ChooseFirst()
        {
            ClickExtension.Move(GeniusInvokationControl.GetInstance().MakeOffset(Area.GetCenterPoint()))
                .LeftButtonClick()
                .Sleep(200)
                .LeftButtonClick();
        }

        public bool SwitchLater()
        {
            GeniusInvokationControl.GetInstance().ClickGameWindowCenter();
            GeniusInvokationControl.GetInstance().Sleep(800);
            var p = GeniusInvokationControl.GetInstance().MakeOffset(Area.GetCenterPoint());
            // выбиратьРоль
            p.Click();

            // Нажмите кнопку вырезать
            GeniusInvokationControl.GetInstance().ActionPhasePressSwitchButton();
            return true;
        }

        /// <summary>
        /// РольДвойной щелчок при пораженииРолькарта повторноидти воевать
        /// </summary>
        /// <returns></returns>
        public void SwitchWhenTakenOut()
        {
            _logger.LogInformation("иметьРольБыть поверженным,Текущий выбор{Name}идти воевать", Name);
            var p = GeniusInvokationControl.GetInstance().MakeOffset(Area.GetCenterPoint());
            // выбиратьРоль
            p.Click();
            // Дважды щелкните, чтобы вырезать людей
            GeniusInvokationControl.GetInstance().Sleep(500);
            p.Click();
            GeniusInvokationControl.GetInstance().Sleep(300);
        }

        public bool UseSkill(int skillIndex, Duel duel)
        {
            var res = GeniusInvokationControl.GetInstance().ActionPhaseAutoUseSkill(skillIndex, Skills[skillIndex].SpecificElementCost, Skills[skillIndex].Type, duel);
            if (res)
            {
                return true;
            }
            else
            {
                _logger.LogWarning("Недостаточно карточек рук или кубиков стихий для применения навыка.");
                return false;
            }
        }
    }
}