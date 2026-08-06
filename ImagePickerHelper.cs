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
        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = Marshal.SizeOf(typeof(OpenFileName));
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)\0*.png;*.jpg;*.jpeg;*.webp;*.bmp\0All Files (*.*)\0*.*\0\0";
            public string customFilter = null!;
            public int maxCustomFilter = 0;
            public int filterIndex = 0;
            public string file = new string(new char[2560]);
            public int maxFile = 2560;
            public string fileTitle = new string(new char[640]);
            public int maxFileTitle = 640;
            public string initialDir = null!;
            public string title = "Select Image File";
            public int flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = "png";
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null!;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        public static string? OpenImageFileDialog()
        {
            TdsImGuiOverlay.SetAlwaysOnTop(false);
            string? selectedPath = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var ofn = new OpenFileName();
                    if (GetOpenFileName(ofn))
                    {
                        selectedPath = ofn.file.Replace("\0", "").Trim();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening image dialog: {ex.Message}");
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            TdsImGuiOverlay.SetAlwaysOnTop(true);
            return selectedPath;
        }

        public static string? OpenFileDialog(string filter, string defaultExt)
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
            TdsImGuiOverlay.SetAlwaysOnTop(true);
            return selectedPath;
        }

        public static string? SaveFileDialog(string defaultFileName, string filter, string defaultExt)
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
            TdsImGuiOverlay.SetAlwaysOnTop(true);
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