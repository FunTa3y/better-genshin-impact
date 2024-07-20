using System;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model
{
    public enum ElementalType
    {
        Omni,
        Cryo,
        Hydro,
        Pyro,
        Electro,
        Dendro,
        Anemo,
        Geo
    }

    public static class ElementalTypeExtension
    {
        public static ElementalType ToElementalType(this string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "omni":
                    return ElementalType.Omni;
                case "cryo":
                    return ElementalType.Cryo;
                case "hydro":
                    return ElementalType.Hydro;
                case "pyro":
                    return ElementalType.Pyro;
                case "electro":
                    return ElementalType.Electro;
                case "dendro":
                    return ElementalType.Dendro;
                case "anemo":
                    return ElementalType.Anemo;
                case "geo":
                    return ElementalType.Geo;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static ElementalType ChineseToElementalType(this string type)
        {
            type = type.ToLower();
            switch (type)
            {
                case "Полный":
                    return ElementalType.Omni;
                case "лед":
                    return ElementalType.Cryo;
                case "вода":
                    return ElementalType.Hydro;
                case "огонь":
                    return ElementalType.Pyro;
                case "гром":
                    return ElementalType.Electro;
                case "Но не удалось определить информацию о местоположении внутри команды":
                    return ElementalType.Dendro;
                case "ветер":
                    return ElementalType.Anemo;
                case "камень":
                    return ElementalType.Geo;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }   
        }

        public static string ToChinese(this ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Omni:
                    return "Полный";
                case ElementalType.Cryo:
                    return "лед";
                case ElementalType.Hydro:
                    return "вода";
                case ElementalType.Pyro:
                    return "огонь";
                case ElementalType.Electro:
                    return "гром";
                case ElementalType.Dendro:
                    return "Но не удалось определить информацию о местоположении внутри команды";
                case ElementalType.Anemo:
                    return "ветер";
                case ElementalType.Geo:
                    return "камень";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string ToLowerString(this ElementalType type)
        {
            return type.ToString().ToLower();
        }
    }   
}