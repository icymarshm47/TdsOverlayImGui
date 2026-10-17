using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui.Models
{
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(HashSet<string>))]
    [JsonSerializable(typeof(MapStrategy))]
    [JsonSerializable(typeof(List<MapStrategy>))]
    [JsonSerializable(typeof(PlayerStrategy))]
    [JsonSerializable(typeof(StrategyStep))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}
