using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TdsOverlayImGui.Helpers
{
    public static class ImagePickerHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string? lpstrFilter;
            public string? lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile;
            public int nMaxFile;
            public IntPtr lpstrFileTitle;
            public int nMaxFileTitle;
            public string? lpstrInitialDir;
            public string? lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string? lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string? lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
        private static extern bool GetOpenFileName([In, Out] ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetSaveFileNameW")]
        private static extern bool GetSaveFileName([In, Out] ref OPENFILENAME ofn);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const uint CF_BITMAP = 2;
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;
        private const int OFN_EXPLORER = 0x00080000;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;
        private const int OFN_OVERWRITEPROMPT = 0x00000002;

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
                    var ofn = new OPENFILENAME();
                    ofn.lStructSize = Marshal.SizeOf(ofn);
                    ofn.lpstrFilter = filter.Replace('|', '\0') + "\0\0";
                    ofn.lpstrDefExt = defaultExt;
                    int maxChars = 2048;
                    IntPtr buffer = Marshal.AllocHGlobal(maxChars * sizeof(char));
                    Marshal.Copy(new char[maxChars], 0, buffer, maxChars);
                    ofn.lpstrFile = buffer;
                    ofn.nMaxFile = maxChars;
                    ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

                    if (GetOpenFileName(ref ofn))
                    {
                        selectedPath = Marshal.PtrToStringUni(buffer);
                    }
                    Marshal.FreeHGlobal(buffer);
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
                    var ofn = new OPENFILENAME();
                    ofn.lStructSize = Marshal.SizeOf(ofn);
                    ofn.lpstrFilter = filter.Replace('|', '\0') + "\0\0";
                    ofn.lpstrDefExt = defaultExt;
                    int maxChars = 2048;
                    IntPtr buffer = Marshal.AllocHGlobal(maxChars * sizeof(char));
                    char[] initialChars = new char[maxChars];
                    if (!string.IsNullOrEmpty(defaultFileName))
                    {
                        defaultFileName.CopyTo(0, initialChars, 0, Math.Min(defaultFileName.Length, maxChars - 1));
                    }
                    Marshal.Copy(initialChars, 0, buffer, maxChars);
                    ofn.lpstrFile = buffer;
                    ofn.nMaxFile = maxChars;
                    ofn.Flags = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR | OFN_OVERWRITEPROMPT;

                    if (GetSaveFileName(ref ofn))
                    {
                        selectedPath = Marshal.PtrToStringUni(buffer);
                    }
                    Marshal.FreeHGlobal(buffer);
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
            if (string.IsNullOrEmpty(text)) return;
            var thread = new Thread(() =>
            {
                try
                {
                    if (!OpenClipboard(IntPtr.Zero)) return;
                    try
                    {
                        EmptyClipboard();
                        int bytesCount = (text.Length + 1) * sizeof(char);
                        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytesCount);
                        if (hGlobal != IntPtr.Zero)
                        {
                            IntPtr target = GlobalLock(hGlobal);
                            if (target != IntPtr.Zero)
                            {
                                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                                Marshal.WriteInt16(target + text.Length * sizeof(char), 0);
                                GlobalUnlock(hGlobal);
                                SetClipboardData(CF_UNICODETEXT, hGlobal);
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
                    }
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
                    if (!IsClipboardFormatAvailable(CF_UNICODETEXT)) return;
                    if (!OpenClipboard(IntPtr.Zero)) return;
                    try
                    {
                        IntPtr hGlobal = GetClipboardData(CF_UNICODETEXT);
                        if (hGlobal != IntPtr.Zero)
                        {
                            IntPtr target = GlobalLock(hGlobal);
                            if (target != IntPtr.Zero)
                            {
                                text = Marshal.PtrToStringUni(target);
                                GlobalUnlock(hGlobal);
                            }
                        }
                    }
                    finally
                    {
                        CloseClipboard();
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
                        if (!IsClipboardFormatAvailable(CF_BITMAP)) return;
                        if (!OpenClipboard(IntPtr.Zero)) return;
                        try
                        {
                            IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                            if (hBitmap != IntPtr.Zero)
                            {
                                using var img = Image.FromHbitmap(hBitmap);
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
                        finally
                        {
                            CloseClipboard();
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