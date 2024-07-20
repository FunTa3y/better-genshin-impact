using BetterGenshinImpact.Test.Simple.MiniMap;
using OxyPlot;
using OxyPlot.Series;

namespace BetterGenshinImpact.Test.View;

internal class CameraRecViewModel
{
    public PlotModel LeftModel { get; private set; }
    public PlotModel RightModel { get; private set; }
    public PlotModel AllModel { get; private set; }

    public CameraRecViewModel()
    {
        var data = CameraOrientationTest.Test1();

        LeftModel = BuildModel(data.Item1, "Левый");
        RightModel = BuildModel(data.Item2, "верно(Левыйсдвиг90После степени)");
        AllModel = BuildModel(data.Item3, "продукт");
    }

    public PlotModel BuildModel(int[] data, string name)
    {
        // Создайте линейную диаграмму
        var series = new LineSeries();
        for (int i = 0; i < data.Length; i++)
        {
            series.Points.Add(new DataPoint(i, data[i]));
        }

        // Создайте модель чертежа и добавьте линейную диаграмму.
        var plotModel = new PlotModel();
        plotModel.Series.Add(series);

        // Установить заголовок диаграммы
        plotModel.Title = name;
        return plotModel;
    }
}
