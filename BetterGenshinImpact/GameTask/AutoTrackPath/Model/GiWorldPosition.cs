namespace BetterGenshinImpact.GameTask.AutoTrackPath.Model;

/// <summary>
/// Мировые координаты Genshin Impact
/// https://github.com/babalae/better-genshin-impact/issues/318
/// </summary>
public class GiWorldPosition
{
    /// <summary>
    /// Назовите эту точку координат
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Описание координат
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// координировать x,y,z три значения，соответственно представляют вертикаль、высокий、Горизонтальный，Используйте фактические данные Genshin ImpactкоординироватьГалстук
    /// В связи с этимкоординироватьГалстук和一般的координироватьГалстук不同，Итак, чтобы облегчить понимание，Предположим, это3значениеa,b,c
    ///     ▲
    ///     │a
    /// ◄───┼────
    ///   c │
    ///
    /// Уровень масштабирования значения и1024блокироватькоординироватьГалстук的缩放一致
    /// </summary>
    public decimal[] Position { get; set; } = new decimal[3];

    public double X => (double)Position[2]; // a
    public double Y => (double)Position[0]; // c
}
