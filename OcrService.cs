using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                // 1. Снимаем скриншот выделенной области
                using var rawBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(rawBitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

                // 2. Увеличиваем кадр в 3 раза с высоким качеством сглаживания шрифта
                int scale = 3;
                using var processedBitmap = new Bitmap(width * scale, height * scale, PixelFormat.Format32bppArgb);
                using (var gProc = Graphics.FromImage(processedBitmap))
                {
                    gProc.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gProc.SmoothingMode = SmoothingMode.HighQuality;
                    gProc.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gProc.DrawImage(rawBitmap, 0, 0, width * scale, height * scale);
                }

                // 3. СОХРАНЯЕМ ТЕСТОВЫЙ СНИМОК на диск для отладки
                try 
                { 
                    processedBitmap.Save("ocr_debug.png", ImageFormat.Png); 
                } 
                catch { }

                // 4. Передаём картинку в Windows OCR
                using var stream = new MemoryStream();
                processedBitmap.Save(stream, ImageFormat.Png);
                byte[] bytes = stream.ToArray();

                using var randomAccessStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                }

                var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
                using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                if (_ocrEngine == null)
                {
                    _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages() 
                              ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
                }

                if (_ocrEngine == null) return null;

                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
                string text = ocrResult.Text;

                // Печатаем в консоль всё, что запеленговал сканер
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine($"[OCR Scan] Распознанный текст: '{text.Trim()}'");
                }

                var match = Regex.Match(text, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int waveNum))
                {
                    return waveNum;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows OCR Error: {ex.Message}");
            }

            return null;
        }
    }
}