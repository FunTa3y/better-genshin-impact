﻿using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using OpenCvSharp;

namespace BetterGenshinImpact.Test.Simple.AllMap;

public class MapPuzzle
{
    public static readonly int block = 2048;

    public static List<string> PicWhiteHashList = new List<string>
    {
        "25E23B0D18C2CBEA19D28E5E399D42FA",
        "3B4847FEA7D506EAF3B75D4F4541E867",
        "CCCAF02768432A46AA0001E51DC5991B",
        "F80C609208063B289A05B8A1E0351226",
        "06767779699056515930D0597072AB7D",
        "04F757BD57FBB5FA74A6C9C0634BB122",
        "04C1EDFCD89249209D1DD885DD2F2129",
        "01B8641F5F58BBDE6E10F53D56EA7288",
        "AF56BDE27BDF534317A9FB34E08165C0",
        "05F4EAB8C6BFBADD60B8A5CCDD614F83"
    };

    public static MD5 Md5Service = MD5.Create();

    public static void Put()
    {
        string folderPath = @"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\UI_Map"; // Путь к папке с изображениями
        string pattern = @"UI_MapBack_([-+]?\d+)_([-+]?\d+)(.*)";
        var images = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly); // Получить все пути к файлам изображений

        // Проанализируйте информацию о местоположении изображения и сохраните ее в словаре.
        var imageLocations = new Dictionary<(int row, int col), ImgInfo>();
        foreach (var imagePath in images)
        {
            // Получить размер файла
            var fileInfo = new FileInfo(imagePath);

            // Проанализируйте информацию о строке и столбце в имени изображения.
            var name = Path.GetFileNameWithoutExtension(imagePath);
            var match = Regex.Match(name, pattern);
            int row, col;
            if (match.Success)
            {
                // Debug.WriteLine($"Соответствует ({match.Groups[1].Value}, {match.Groups[2].Value}) {name}");
                row = int.Parse(match.Groups[1].Value);
                col = int.Parse(match.Groups[2].Value);
            }
            else
            {
                // Debug.WriteLine($"Не соответствует {name}");
                continue;
            }

            // Исключить изображения в указанных строках и столбцах
            if ((row, col) == (4, 6) || (row, col) == (5, 6) || (row, col) == (5, 5) || (row, col) == (5, 2) || (row, col) == (5, 1) || (row, col) == (4, 1))
            {
                continue;
            }

            // Прочитайте изображение и посчитайтеhashценить
            Mat img = Cv2.ImRead(imagePath);
            var hashBytes = Md5Service.ComputeHash(File.ReadAllBytes(imagePath));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

            if (img.Width < 8)
            {
                Debug.WriteLine($"Не слишком маленький ({row}, {col}) {img.Width} {img.Height}  {name}");
                continue;
            }

            // Если требуется только городское сращивание，Раскомментируйте этот абзац
            // if (block == 2048 && img.Width != 2048)
            // {
            //     Debug.WriteLine($"нет2048Не хочу ({row}, {col}) {img.Width} {img.Height}  {name}");
            //     continue;
            // }

            // Если в текущем местоположении уже есть изображение，Сохраняйте изображения большего размера
            if (imageLocations.ContainsKey((row, col)))
            {
                // Если изображение в текущем местоположении былоhashзапирание，перепрыгни
                if (imageLocations[(row, col)].Locked)
                {
                    Debug.WriteLine($"ужезапирание ({row}, {col}) {name}");
                    continue;
                }

                if (img.Width > imageLocations[(row, col)].Img.Width || fileInfo.Length > imageLocations[(row, col)].FileLength || PicWhiteHashList.Contains(hash))
                {
                    imageLocations[(row, col)] = new ImgInfo(img, name, fileInfo.Length, PicWhiteHashList.Contains(hash));
                }
                else
                {
                    Debug.WriteLine($"повторить ({row}, {col}) {img.Width} {img.Height}  {name}");
                }
            }
            else
            {
                imageLocations[(row, col)] = new ImgInfo(img, name, fileInfo.Length, PicWhiteHashList.Contains(hash));
            }
        }

        int minRow = imageLocations.Keys.Min(key => key.row);
        int minCol = imageLocations.Keys.Min(key => key.col);

        // Определить количество строк и столбцов большого графика
        int maxRow = imageLocations.Keys.Max(key => key.row);
        int maxCol = imageLocations.Keys.Max(key => key.col);

        // Рассчитать общую ширину и высоту большого изображения
        var lenCol = maxCol - minCol;
        var lenRow = maxRow - minRow;
        int totalWidth = (lenCol + 1) * block;
        int totalHeight = (lenRow + 1) * block;

        // Создайте большое пустое изображение
        Mat largeImage = new Mat(totalHeight, totalWidth, MatType.CV_8UC3, new Scalar(0, 0, 0));

        // Сшить картинки
        int[,] arr = new int[lenRow + 1, lenCol + 1];
        foreach (var location in imageLocations)
        {
            int row = location.Key.row - minRow;
            int col = location.Key.col - minCol;
            Mat img = location.Value.Img;

            arr[row, col] = 1;

            // Вычислить положение верхнего левого угла текущего изображения на большом изображении.
            int x = (lenCol - col) * block; // Порядок отменен，屮
            int y = (lenRow - row) * block; // Порядок отменен，屮

            // Вставить изображение в увеличенное изображение
            if (img.Width != block || img.Height != block)
            {
                img = img.Resize(new Size(block, block), 0, 0, InterpolationFlags.Nearest);
            }

            // Добавить идентификатор местоположения
            // img.PutText($"{location.Key.row} , {location.Key.col}", new Point(50, 50), HersheyFonts.HersheyComplex, 2, Scalar.Red, 2, LineTypes.Link8);

            img.CopyTo(new Mat(largeImage, new Rect(x, y, img.Width, img.Height)));
        }

        // Результат сращивания двумерного массива Канкан
        for (int i = 0; i < arr.GetLength(0); i++)
        {
            for (int j = 0; j < arr.GetLength(1); j++)
            {
                Debug.Write(arr[i, j] + " ");
            }

            Debug.WriteLine("");
        }

        // Сохранить большое изображение
        Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\map_46_2048.png", largeImage);
        // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\combined_image_sd4x.png", largeImage.Resize(new Size(largeImage.Width / 4, largeImage.Height / 4), 0, 0, InterpolationFlags.Cubic));
        // Cv2.ImWrite(@"E:\HuiTask\Улучшенный Genshin Impact\сопоставление карт\combined_image_small.png", largeImage.Resize(new Size(1400, 1300), 0, 0, InterpolationFlags.Cubic));

        // Освободить ресурсы
        largeImage.Dispose();
        foreach (var img in imageLocations.Values)
        {
            img.Img.Dispose();
        }
    }

    public class ImgInfo
    {
        public Mat Img { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Размер файла
        /// </summary>
        public long FileLength { get; set; }

        public bool Locked { get; set; }

        public ImgInfo(Mat mat, string name, long fileLength, bool locked = false)
        {
            Img = mat;
            Name = name;
            FileLength = fileLength;
            Locked = locked;
        }
    }
}
