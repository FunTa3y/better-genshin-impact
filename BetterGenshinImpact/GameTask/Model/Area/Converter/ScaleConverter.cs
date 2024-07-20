namespace BetterGenshinImpact.GameTask.Model.Area.Converter;

/// <summary>
/// масштабная трансформация
/// </summary>
/// <param name="scale">Какое увеличение требуется?</param>
public class ScaleConverter(double scale) : INodeConverter
{
    public (int x, int y, int w, int h) ToPrev(int x, int y, int w, int h)
    {
        return ((int)(x * scale), (int)(y * scale), (int)(w * scale), (int)(h * scale));
        // return (x, y, (int)(w * scale), (int)(h * scale));
    }
}
