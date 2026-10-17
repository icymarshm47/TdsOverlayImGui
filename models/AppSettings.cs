namespace TdsOverlayImGui.Models
{
    public class AppSettings
    {
        public bool AlwaysOnTop { get; set; } = true;
        public bool HideFromCapture { get; set; } = false;
        public bool CheckUpdatesOnStartup { get; set; } = true;
        public bool SeparateImageWindow { get; set; } = true;
        public float GeneralInfoBoxHeight { get; set; } = 60.0f;
        public float InstructionBoxHeight { get; set; } = 160.0f;
        public float EmbeddedImageBoxHeight { get; set; } = 220.0f;
        public float WindowOpacity { get; set; } = 0.96f;
        public bool EnableAdaptiveTransparency { get; set; } = false;
        public string Language { get; set; } = "en";

        public bool EnableOcr { get; set; } = false;
        public bool EnableOcrSound { get; set; } = true;
        public bool OcrPreprocess { get; set; } = true;

        public int OcrX { get; set; } = 100;
        public int OcrY { get; set; } = 100;
        public int OcrW { get; set; } = 150;
        public int OcrH { get; set; } = 50;
    }
}
