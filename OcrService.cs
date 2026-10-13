using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
<<<<<<< HEAD
using System.Runtime.InteropServices;
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
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
<<<<<<< HEAD
        private static readonly Regex WaveDigitsRegex = new Regex(@"\d+", RegexOptions.Compiled);

        public static async Task<int?> RecognizeWaveFromScreenAsync(int x, int y, int width, int height, bool preprocess = true)
        {
=======

        public static async Task<int?> RecognizeWaveFromScreenAsync(int x, int y, int width, int height)
        {
            // СНИЗИЛИ ОГРАНИЧЕНИЕ: теперь обрабатывает даже микро-зоны от 2х2 пикселей
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
            if (width <= 2 || height <= 2) return null;

            try
            {
                using var rawBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(rawBitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

<<<<<<< HEAD
                // Попытка 1: с повышением контраста и полями (лучше всего для шрифта Roblox)
                int? result = await RunOcrOnBitmapAsync(rawBitmap, preprocess);
                if (result.HasValue)
                    return result;

                // Попытка 2 (фолбэк): если бинаризация съела цветной текст, пробуем чистый апскейл
                if (preprocess)
                {
                    return await RunOcrOnBitmapAsync(rawBitmap, false);
=======
                int scale = 3;
                using var processedBitmap = new Bitmap(width * scale, height * scale, PixelFormat.Format32bppArgb);
                using (var gProc = Graphics.FromImage(processedBitmap))
                {
                    gProc.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gProc.SmoothingMode = SmoothingMode.HighQuality;
                    gProc.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    gProc.DrawImage(rawBitmap, 0, 0, width * scale, height * scale);
                }

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

                var match = Regex.Match(text, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int waveNum))
                {
                    return waveNum;
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows OCR Error: {ex.Message}");
            }

            return null;
        }
<<<<<<< HEAD

        private static async Task<int?> RunOcrOnBitmapAsync(Bitmap rawBitmap, bool applyThreshold)
        {
            int scale = 4;
            int pad = 16;
            int scaledW = rawBitmap.Width * scale + pad * 2;
            int scaledH = rawBitmap.Height * scale + pad * 2;

            using var processedBitmap = new Bitmap(scaledW, scaledH, PixelFormat.Format32bppArgb);
            using (var gProc = Graphics.FromImage(processedBitmap))
            {
                gProc.Clear(Color.Black);
                gProc.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gProc.SmoothingMode = SmoothingMode.HighQuality;
                gProc.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gProc.DrawImage(rawBitmap, pad, pad, rawBitmap.Width * scale, rawBitmap.Height * scale);
            }

            if (applyThreshold)
            {
                ApplyHighContrastPreprocess(processedBitmap);
            }

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

            _ocrEngine ??= OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"))
                        ?? OcrEngine.TryCreateFromUserProfileLanguages();

            if (_ocrEngine == null) return null;

            var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
            return ExtractWaveNumber(ocrResult.Text);
        }

        /// <summary>
        /// Делает светлый текст волны черным на белом фоне (идеально для Windows OCR)
        /// </summary>
        private static void ApplyHighContrastPreprocess(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int byteCount = Math.Abs(data.Stride) * bmp.Height;
                byte[] pixels = new byte[byteCount];
                Marshal.Copy(data.Scan0, pixels, 0, byteCount);

                // Находим среднюю и максимальную яркость для адаптивного порога
                int maxLum = 0;
                long sumLum = 0;
                int totalPixels = bmp.Width * bmp.Height;

                for (int i = 0; i < byteCount; i += 4)
                {
                    int b = pixels[i];
                    int g = pixels[i + 1];
                    int r = pixels[i + 2];
                    int lum = (r * 299 + g * 587 + b * 114) / 1000;
                    sumLum += lum;
                    if (lum > maxLum) maxLum = lum;
                }

                int avgLum = (int)(sumLum / Math.Max(1, totalPixels));
                int threshold = Math.Clamp((avgLum + maxLum) / 2, 115, 200);

                for (int i = 0; i < byteCount; i += 4)
                {
                    int b = pixels[i];
                    int g = pixels[i + 1];
                    int r = pixels[i + 2];
                    int lum = (r * 299 + g * 587 + b * 114) / 1000;

                    // Инвертируем: яркий текст делаем черным (0), а темный фон — белым (255)
                    byte val = lum >= threshold ? (byte)0 : (byte)255;
                    pixels[i] = val;
                    pixels[i + 1] = val;
                    pixels[i + 2] = val;
                    pixels[i + 3] = 255;
                }

                Marshal.Copy(pixels, 0, data.Scan0, byteCount);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static int? ExtractWaveNumber(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return null;

            // Исправляем типичные промахи OCR на игровых шрифтах
            string cleaned = rawText
                .Replace('O', '0').Replace('o', '0')
                .Replace('I', '1').Replace('l', '1').Replace('|', '1')
                .Replace('S', '5').Replace('$', '5')
                .Replace('B', '8')
                .Replace('Z', '2');

            // Если в строке формат вроде "14/40" или "Wave 14", берем первое адекватное число
            var matches = WaveDigitsRegex.Matches(cleaned);
            foreach (Match m in matches)
            {
                if (int.TryParse(m.Value, out int waveNum) && waveNum >= 1 && waveNum <= 200)
                {
                    return waveNum;
                }
            }

            return null;
        }
    }
}
=======
    }
}
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
