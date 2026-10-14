using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TdsOverlayImGui
{
    public static class ImagePickerHelper
    {
        public static string? OpenImageFileDialog(bool restoreAlwaysOnTop = true)
        {
            return OpenFileDialog("Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*", "png", restoreAlwaysOnTop);
        }

        public static string? OpenFileDialog(string filter, string defaultExt, bool restoreAlwaysOnTop = true)
        {
            TdsImGuiOverlay.SetAlwaysOnTop(false);
            string? selectedPath = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var dlg = new OpenFileDialog
                    {
                        Filter = filter,
                        DefaultExt = defaultExt,
                        CheckFileExists = true
                    };
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        selectedPath = dlg.FileName;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening file dialog: {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (restoreAlwaysOnTop)
            {
                TdsImGuiOverlay.SetAlwaysOnTop(true);
            }
            return selectedPath;
        }

        public static string? SaveFileDialog(string defaultFileName, string filter, string defaultExt, bool restoreAlwaysOnTop = true)
        {
            TdsImGuiOverlay.SetAlwaysOnTop(false);
            string? selectedPath = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var dlg = new SaveFileDialog
                    {
                        FileName = defaultFileName,
                        Filter = filter,
                        DefaultExt = defaultExt
                    };
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        selectedPath = dlg.FileName;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving file dialog: {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (restoreAlwaysOnTop)
            {
                TdsImGuiOverlay.SetAlwaysOnTop(true);
            }
            return selectedPath;
        }

        public static void SetClipboardText(string text)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Clipboard set error: {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public static string? GetClipboardText()
        {
            string? text = null;
            var thread = new Thread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        text = Clipboard.GetText();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Clipboard get error: {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return text;
        }

        public static string? SaveImageFromClipboard(string mapName, int imgNum)
        {
            try
            {
                string? savedPath = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (Clipboard.ContainsImage())
                        {
                            using var img = Clipboard.GetImage();
                            if (img != null)
                            {
                                if (!Directory.Exists("images"))
                                    Directory.CreateDirectory("images");

                                string cleanMap = Sanitize(mapName);
                                string fileName = $"images/{cleanMap}_clip_img{imgNum}_{Guid.NewGuid().ToString("N")[..6]}.png";
                                img.Save(fileName, ImageFormat.Png);
                                savedPath = fileName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Clipboard error: {ex.Message}");
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                return savedPath;
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "map";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}