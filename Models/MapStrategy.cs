using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui.Models
{
    public class MapStrategy
    {
        public string MapName { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string StrategyName { get; set; } = "Main";
        public string GeneralInfo { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();

        public List<StrategyStep> Steps { get; set; } = new();

        [JsonIgnore]
        public string? FilePath { get; set; }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(StrategyName)
            ? $"{MapName} [{Difficulty}]"
            : $"{MapName} [{Difficulty}] - {StrategyName}";

        [JsonIgnore]
        public string TaskId => !string.IsNullOrEmpty(FilePath)
            ? Path.GetFileNameWithoutExtension(FilePath)
            : $"{MapName}_{Difficulty}_{StrategyName}";
    }
}
