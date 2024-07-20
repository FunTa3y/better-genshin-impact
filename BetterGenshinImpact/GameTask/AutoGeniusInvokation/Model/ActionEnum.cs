using System;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model
{
    public enum ActionEnum
    {
        ChooseFirst, SwitchLater, UseSkill
    }

    public static class ActionEnumExtension
    {
        public static ActionEnum ChineseToActionEnum(this string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "идти воевать":
                    //return ActionEnum.ChooseFirst;
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                case "выключатель":
                    //return ActionEnum.SwitchLater;
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                case "использовать":
                    return ActionEnum.UseSkill;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string ToChinese(this ActionEnum type)
        {
            switch (type)
            {
                case ActionEnum.ChooseFirst:
                    return "идти воевать";
                case ActionEnum.SwitchLater:
                    return "выключатель";
                case ActionEnum.UseSkill:
                    return "использовать";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
