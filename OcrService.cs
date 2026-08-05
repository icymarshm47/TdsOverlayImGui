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
                using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                }

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