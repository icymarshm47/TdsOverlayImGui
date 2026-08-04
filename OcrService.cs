using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace TdsOverlayImGui
{
    public static class OcrService
    {
        private static OcrEngine? _ocrEngine;

        public static async Task<int?> RecognizeWaveFromScreenAsync(int x, int y, int width, int height)
        {
            if (width <= 5 || height <= 5) return null;

            try
            {
                // 1. Делаем скриншот выделенной области экрана
                using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                // 2. Конвертируем Bitmap в WinRT SoftwareBitmap через поток InMemoryRandomAccessStream
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                byte[] bytes = stream.ToArray();

                using var randomAccessStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                }

                var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // 3. Инициализируем нативный движок Windows OCR
                if (_ocrEngine == null)
                {
                    _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages() 
                              ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
                }

                if (_ocrEngine == null) return null;

                // 4. Распознаем текст
                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
                string text = ocrResult.Text;

                // 5. Вытаскиваем число волны из текста
                var match = Regex.Match(text, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int waveNum))
                {
                    return waveNum;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка Windows OCR: {ex.Message}");
            }

            return null;
        }
    }
}