using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace TdsOverlayImGui.Services
{
    public static class OcrService
    {
        private static OcrEngine? _ocrEngine;

        private static readonly Regex WaveWordRegex = new Regex(@"(?:wave|волна|w)[\s:.]*(\d{1,3})", RegexOptions.Compiled);
        private static readonly Regex WaveFractionRegex = new Regex(@"(\d{1,3})\s*[\/|\\]\s*\d{1,3}", RegexOptions.Compiled);
        private static readonly Regex WaveStandaloneRegex = new Regex(@"\b(\d{1,3})\b", RegexOptions.Compiled);

        public static async Task<(int? wave, string rawText)> RecognizeWaveFromScreenAsync(int x, int y, int width, int height, bool preprocess = true)
        {
            if (width <= 2 || height <= 2) return (null, "");

            try
            {
                var vScreen = SystemInformation.VirtualScreen;
                int clampedX = Math.Max(vScreen.Left, x);
                int clampedY = Math.Max(vScreen.Top, y);
                int clampedRight = Math.Min(vScreen.Right, x + width);
                int clampedBottom = Math.Min(vScreen.Bottom, y + height);
                int clampedW = clampedRight - clampedX;
                int clampedH = clampedBottom - clampedY;

                if (clampedW <= 2 || clampedH <= 2) return (null, "");

                using var rawBitmap = new Bitmap(clampedW, clampedH, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(rawBitmap))
                {
                    g.CopyFromScreen(clampedX, clampedY, 0, 0, new Size(clampedW, clampedH), CopyPixelOperation.SourceCopy);
                }

                var cleanResult = await RunOcrOnBitmapAsync(rawBitmap, false);
                if (cleanResult.wave.HasValue)
                {
                    return cleanResult;
                }

                if (preprocess)
                {
                    var boostedResult = await RunOcrOnBitmapAsync(rawBitmap, true);
                    if (boostedResult.wave.HasValue)
                    {
                        return boostedResult;
                    }

                    if (!string.IsNullOrWhiteSpace(boostedResult.rawText))
                    {
                        return boostedResult;
                    }
                }

                return cleanResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows OCR Error: {ex.Message}");
            }

            return (null, "");
        }

        private static async Task<(int? wave, string rawText)> RunOcrOnBitmapAsync(Bitmap rawBitmap, bool applyContrast)
        {
            int scale = 4;
            int pad = 14;
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

            if (applyContrast)
            {
                ApplyContrastBoost(processedBitmap);
            }

            var rect = new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height);
            var bmpData = processedBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] pixels;
            try
            {
                int byteCount = Math.Abs(bmpData.Stride) * processedBitmap.Height;
                pixels = new byte[byteCount];
                Marshal.Copy(bmpData.Scan0, pixels, 0, byteCount);
            }
            finally
            {
                processedBitmap.UnlockBits(bmpData);
            }

            using var writer = new DataWriter();
            writer.WriteBytes(pixels);
            var buffer = writer.DetachBuffer();

            using var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                buffer,
                BitmapPixelFormat.Bgra8,
                scaledW,
                scaledH,
                BitmapAlphaMode.Premultiplied);

            _ocrEngine ??= OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"))
                        ?? OcrEngine.TryCreateFromUserProfileLanguages();

            if (_ocrEngine == null) return (null, "");

            var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
            string rawText = (ocrResult.Text ?? "").Trim();
            int? wave = ExtractWaveNumber(rawText);
            return (wave, rawText);
        }

        private static void ApplyContrastBoost(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int byteCount = Math.Abs(data.Stride) * bmp.Height;
                byte[] pixels = new byte[byteCount];
                Marshal.Copy(data.Scan0, pixels, 0, byteCount);

                int minLum = 255;
                int maxLum = 0;

                for (int i = 0; i < byteCount; i += 4)
                {
                    int b = pixels[i];
                    int g = pixels[i + 1];
                    int r = pixels[i + 2];
                    int lum = (r * 299 + g * 587 + b * 114) / 1000;
                    if (lum < minLum) minLum = lum;
                    if (lum > maxLum) maxLum = lum;
                }

                int range = maxLum - minLum;
                if (range > 20)
                {
                    for (int i = 0; i < byteCount; i += 4)
                    {
                        int b = pixels[i];
                        int g = pixels[i + 1];
                        int r = pixels[i + 2];

                        int lum = (r * 299 + g * 587 + b * 114) / 1000;
                        int stretched = Math.Clamp((lum - minLum) * 255 / range, 0, 255);

                        pixels[i] = (byte)stretched;
                        pixels[i + 1] = (byte)stretched;
                        pixels[i + 2] = (byte)stretched;
                        pixels[i + 3] = 255;
                    }

                    Marshal.Copy(pixels, 0, data.Scan0, byteCount);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public static int? ExtractWaveNumber(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return null;

            string cleaned = rawText
                .Replace('O', '0').Replace('o', '0')
                .Replace('I', '1').Replace('l', '1').Replace('|', '1')
                .Replace('S', '5').Replace('$', '5')
                .Replace('B', '8')
                .Replace('Z', '2');

            var matchWord = WaveWordRegex.Match(cleaned);
            if (matchWord.Success && int.TryParse(matchWord.Groups[1].Value, out int waveFromWord) && waveFromWord >= 1 && waveFromWord <= 200)
            {
                return waveFromWord;
            }

            var matchFraction = WaveFractionRegex.Match(cleaned);
            if (matchFraction.Success && int.TryParse(matchFraction.Groups[1].Value, out int waveFromFraction) && waveFromFraction >= 1 && waveFromFraction <= 200)
            {
                return waveFromFraction;
            }

            var matches = WaveStandaloneRegex.Matches(cleaned);
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
