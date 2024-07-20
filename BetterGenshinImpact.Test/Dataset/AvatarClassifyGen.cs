using System.Diagnostics;
using System.IO;
using OpenCvSharp;

namespace BetterGenshinImpact.Test.Dataset;

public class AvatarClassifyGen
{
    // папка с базовым изображением
    private const string BaseDir = @"E:\HuiTask\Улучшенный Genshin Impact\Автоматическое секретное царство\Авто бой\Идентификация команды\Классификатор\";

    // папка с фоновым изображением
    private static readonly string BackgroundDir = Path.Combine(BaseDir, "background");

    private static readonly Random Rd = new Random();

    public static void GenAll()
    {
        // Цель
        // List<string> sideImageFiles = Directory.GetFiles(Path.Combine(BaseDir, "side_src"), "*.png", SearchOption.TopDirectoryOnly).ToList();
        // Используйте только одно изображение
        List<string> sideImageFiles = Directory.GetFiles(Path.Combine(BaseDir, "side_src"), "UI_AvatarIcon_Side_Sethos.png", SearchOption.TopDirectoryOnly).ToList();
        // List<string> sideImageFiles2 = Directory.GetFiles(Path.Combine(BaseDir, "side_src"), "UI_AvatarIcon_Side_Clorinde.png", SearchOption.TopDirectoryOnly).ToList();
        // sideImageFiles.AddRange(sideImageFiles2);

        // Создать обучающий набор
        GenTo(sideImageFiles, Path.Combine(BaseDir, @"dateset\train"), 200);
        // Создать набор тестов
        GenTo(sideImageFiles, Path.Combine(BaseDir, @"dateset\test"), 40);
        // GenTo(new List<string> { sideImageFiles[1] }, Path.Combine(BaseDir, @"dateset\test"), 1);
    }

    static void GenTo(List<string> sideImageFiles, string dataFolder, int count)
    {
        // зарезервированная территория Фиксированная точка от центра нижнего края
        var reservedSize = new Size(60, 80);

        Directory.CreateDirectory(dataFolder);
        // Цикл для генерации набора данных, соответствующего каждому базовому изображению.
        foreach (string sideImageFile in sideImageFiles)
        {
            // Получить имя файла базового изображения
            string sideImageFileName = Path.GetFileNameWithoutExtension(sideImageFile);
            sideImageFileName = sideImageFileName.Replace("UI_AvatarIcon_Side_", "");
            // Создайте папку набора данных, соответствующую базовому изображению.
            string sideDataFolder = Path.Combine(dataFolder, sideImageFileName);
            Directory.CreateDirectory(sideDataFolder);

            // Цель
            Mat sideImageSrc = Cv2.ImRead(sideImageFile, ImreadModes.Unchanged);
            var channels = sideImageSrc.Split();
            var alphaChannel = channels[3]; // прозрачный канал
            for (int i = 0; i < 3; i++)
            {
                Cv2.Multiply(channels[i], alphaChannel, channels[i], 1 / 255.0);
            }

            var sideImage = new Mat();
            Cv2.Merge(channels[..3], sideImage);

            // Cv2.ImShow("avatar", sideImage);

            // Цикл для создания изображений
            for (int i = 0; i < count; i++)
            {
                // Случайный выбор фонового изображения
                string backgroundImageFile = Path.Combine(BackgroundDir, Directory.GetFiles(BackgroundDir, "*.png")[Rd.Next(Directory.GetFiles(BackgroundDir, "*.png").Length)]);

                // Выберите случайный фрагмент фонового изображения. 128x128 Область
                Mat backgroundImage = Cv2.ImRead(backgroundImageFile, ImreadModes.Color);
                Rect backgroundRect = new Rect(Rd.Next(backgroundImage.Width - 128), new Random().Next(backgroundImage.Height - 128), 128, 128);
                Mat backgroundImageRegion = backgroundImage[backgroundRect];

                // Случайный перевод、Увеличитьзарезервированная территория
                float scale = (float)(Rd.NextDouble() * (1.6 - 0.7) + 0.7);
                int w = (int)(sideImage.Width * scale);
                int h = (int)(sideImage.Height * scale);

                Debug.WriteLine($"{sideImageFileName} Генерация случайного масштабирования{scale}");

                // Пучокзарезервированная территорияНаложение на фоновое изображение
                Mat backgroundImageRegionClone = backgroundImageRegion.Clone();
                var resizedSideImage = new Mat();
                Cv2.Resize(sideImage, resizedSideImage, new Size(128 * scale, 128 * scale));
                // Cv2.ImShow("resizedSideImage", resizedSideImage);
                var resizedMaskImage = new Mat();
                // Cv2.Threshold(alphaChannel, alphaChannel, 200, 255, ThresholdTypes.Otsu);
                Cv2.Resize(255 - alphaChannel, resizedMaskImage, new Size(128 * scale, 128 * scale), 0, 0, InterpolationFlags.Cubic);
                var resizedAlphaChannel = new Mat();
                Cv2.Resize(alphaChannel, resizedAlphaChannel, new Size(128 * scale, 128 * scale), 0, 0, InterpolationFlags.Cubic);

                // Cv2.ImShow("resizedMaskImage", resizedMaskImage);
                // generatedImage[transformedRect] = resizedSideImage;
                Mat result;
                if (scale > 1)
                {
                    int xSpace1 = (int)((128 - reservedSize.Width * scale) / 2.0);
                    int ySpace1 = (int)(128 - reservedSize.Height * scale);
                    int xSpace2 = (int)((resizedSideImage.Width - 128) / 2.0);
                    int ySpace2 = resizedSideImage.Height - 128;
                    int xSpace = Math.Min(xSpace1, xSpace2);
                    int ySpace = Math.Min(ySpace1, ySpace2);
                    int offsetX = Rd.Next(-xSpace, xSpace);
                    int offsetY = Rd.Next(-ySpace, 0);
                    Debug.WriteLine($"{sideImageFileName} Увеличить{scale}больше, чем1 компенсировать ({offsetX},{offsetY})");

                    var roi = new Rect((resizedSideImage.Width - 128) / 2 + offsetX, (resizedSideImage.Height - 128) + offsetY, 128, 128);
                    // result = new Mat();
                    // Cv2.BitwiseAnd(backgroundImageRegionClone, backgroundImageRegionClone, result, resizedMaskImage[roi]);
                    result = Mul(backgroundImageRegionClone, resizedAlphaChannel[roi]);
                    Cv2.Add(result, resizedSideImage[roi], result);
                }
                else
                {
                    int xSpace = (128 - w) / 2;
                    int ySpace = 128 - h;
                    int offsetX = Rd.Next(-xSpace, xSpace);
                    int offsetY = Rd.Next(-ySpace, 0);
                    Debug.WriteLine($"{sideImageFileName} Увеличить{scale}меньше или равно1 компенсировать ({offsetX},{offsetY})");

                    var roi = new Rect((128 - resizedSideImage.Width) / 2 + offsetX, (128 - resizedSideImage.Height) + offsetY, resizedSideImage.Width, resizedSideImage.Height);
                    var res = new Mat();
                    // Cv2.BitwiseAnd(backgroundImageRegionClone[roi], backgroundImageRegionClone[roi], res, resizedMaskImage);
                    res = Mul(backgroundImageRegionClone[roi], resizedAlphaChannel);
                    Cv2.Add(res, resizedSideImage, res);
                    backgroundImageRegionClone[roi] = res;
                    result = backgroundImageRegionClone.Clone();
                }

                // Cv2.ImShow("avatarR", result);
                // Сохраните созданное изображение
                Cv2.ImWrite(Path.Combine(sideDataFolder, $"{sideImageFileName}_{i}.png"), result);
            }
        }

        static Mat Mul(Mat background, Mat alphaChannel)
        {
            var channels = background.Split();
            for (int i = 0; i < 3; i++)
            {
                Cv2.Multiply(channels[i], 255 - alphaChannel, channels[i], 1 / 255.0);
            }
            Mat result = new Mat();
            Cv2.Merge(channels[..3], result);
            return result;
        }
    }
}
