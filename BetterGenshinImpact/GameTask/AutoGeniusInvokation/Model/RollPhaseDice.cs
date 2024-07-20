using System;
using System.Drawing;

namespace BetterGenshinImpact.GameTask.AutoGeniusInvokation.Model
{
    /// <summary>
    /// бросать кости во время
    /// </summary>
    [Obsolete]
    public class RollPhaseDice
    {
        /// <summary>
        /// тип элемента
        /// </summary>
        public ElementalType Type { get; set; }
        
        /// <summary>
        /// положение центральной точки
        /// </summary>
        public Point CenterPosition { get; set; }

        public RollPhaseDice(ElementalType type, Point centerPosition)
        {
            Type = type;
            CenterPosition = centerPosition;
        }

        public RollPhaseDice()
        {
        }

        public override string ToString()
        {
            return $"Type:{Type},CenterPosition:{CenterPosition}";
        }

        public void Click()
        {
            //MouseUtils.Click(CenterPosition.X, CenterPosition.Y);
        }
    }
}
