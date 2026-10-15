using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui.Models
{
    public class PlayerStrategy
    {
        public string PlayerName { get; set; } = "Player 1";
        public string GeneralInfo { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();
        public List<StrategyStep> Steps { get; set; } = new();
    }

    public class MapStrategy
    {
        public string MapName { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string StrategyName { get; set; } = "Main";
        public string GeneralInfo { get; set; } = "";
        public List<string> ImagePaths { get; set; } = new();

        public List<StrategyStep> Steps { get; set; } = new();

        public List<PlayerStrategy> Players { get; set; } = new();

        [JsonIgnore]
        public int SelectedPlayerIndex { get; set; } = 0;

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

        public void EnsureDefaultPlayer()
        {
            if (Players == null || Players.Count == 0)
            {
                Players = new List<PlayerStrategy>
                {
                    new PlayerStrategy
                    {
                        PlayerName = "Player 1",
                        GeneralInfo = GeneralInfo ?? "",
                        ImagePaths = ImagePaths != null ? new List<string>(ImagePaths) : new(),
                        Steps = Steps != null ? new List<StrategyStep>(Steps) : new()
                    }
                };
            }
            else
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(Players[i].PlayerName))
                    {
                        Players[i].PlayerName = $"Player {i + 1}";
                    }
                    Players[i].Steps ??= new();
                    Players[i].ImagePaths ??= new();
                    Players[i].GeneralInfo ??= "";
                }
            }

            if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= Players.Count)
            {
                SelectedPlayerIndex = 0;
            }
        }

        [JsonIgnore]
        public PlayerStrategy CurrentPlayer
        {
            get
            {
                EnsureDefaultPlayer();
                return Players[SelectedPlayerIndex];
            }
        }
    }
}
