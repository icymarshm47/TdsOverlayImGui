using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui.Models
{
    public class StrategyStep
    {
        public int StartWave { get; set; } = 1;
        public int EndWave { get; set; } = 30;
        public string Instruction { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();

        [JsonPropertyName("ImagePath")]
        public string? LegacyImagePath
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && ImagePaths.Count == 0)
                    ImagePaths.Add(value);
            }
        }
    }
}
