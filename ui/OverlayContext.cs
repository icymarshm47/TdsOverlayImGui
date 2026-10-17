using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

namespace TdsOverlayImGui.UI
{
    public class OverlayContext
    {
        public List<MapStrategy> Strategies = new();
        public AppSettings Settings = new();

        public int SelectedMapIndex = -1;
        public int CurrentStepIndex = 0;
        public int CurrentImageIndex = 0;

        public string StrategySearchFilter = "";

        public bool IsOverlayOpen = true;
        public bool IsSeparateImageWindowOpen = true;
        public bool IsFirstFrame = true;

        public HashSet<string> CompletedTasks = new();
        public int CurrentWaveNumber = 1;
        public int PreviousWaveNumber = 1;

        public HashSet<string> TriggeredDjAlerts = new();
        public HashSet<string> TriggeredOcrSounds = new();
        public float AlertToastTimer = 0.0f;
        public float AlertToastMaxDuration = 4.0f;
        public string AlertToastBadge = "";
        public string AlertToastTitle = "";
        public string AlertToastSubtitle = "";
        public Vector4 AlertToastColor = Vector4.One;

        public DateTime LastCommanderAlertTime = DateTime.MinValue;
        public DateTime LastMedicAlertTime = DateTime.MinValue;
        public int CommanderRotationIndex = 0;
        public bool IsCommanderChainStopped = false;
        public bool IsMedicChainStopped = false;

        public bool SectionStrategyOpen = true;
        public bool SectionStopwatchOpen = true;
        public bool SectionScreenReaderOpen = true;
        public bool SectionCurrentWaveOpen = true;
        public bool SectionGeneralInfoOpen = true;
        public bool SectionWaveInstructionsOpen = true;

        public System.Diagnostics.Stopwatch Stopwatch = new();
        public List<TimeSpan> StopwatchLaps = new();

        public float MainWindowWidth = 430.0f;
        public float MainWindowHeight = 800.0f;
        public float TextScale = 1.0f;

        public float DjToastTimer
        {
            get => AlertToastTimer;
            set => AlertToastTimer = value;
        }
        public string DjToastMessage
        {
            get => AlertToastTitle;
            set => AlertToastTitle = value;
        }
        public Vector4 DjToastColor
        {
            get => AlertToastColor;
            set => AlertToastColor = value;
        }

        public float ToastTimer = 0.0f;
        public string DropStatusMessage = "";
        public float DropStatusTimer = 0.0f;

        public ConcurrentQueue<string> DroppedFilesQueue = new();

        public float ImageScale = 1.0f;
        public Vector2 ImageOffset = Vector2.Zero;

        public int? DetectedWaveNumber = null;
        public string LastOcrRawText = "";
        public string LastOcrStatus = "";
        public DateTime LastOcrSuccessTime = DateTime.MinValue;
        public bool IsSelectingOcrRegion = false;
        public int OcrSelectionState = 0;
        public Vector2 OcrDragStart = Vector2.Zero;
        public DateTime LastOcrScanTime = DateTime.MinValue;
        public bool TriggerOneShotOcr = false;

        public bool IsEditing = false;
        public string EditMapName = "";
        public string EditDifficulty = "";
        public string EditStrategyName = "";
        public string EditGeneralInfo = "";
        public List<string> EditImagePaths = new();
        public List<StrategyStep> EditSteps = new();
        public List<PlayerStrategy> EditPlayers = new();
        public int EditSelectedPlayerIndex = 0;
        public string EditorToastMessage = "";
        public float EditorToastTimer = 0.0f;

        public bool ShowAddMapModal = false;
        public string NewMapName = "";
        public string NewMapDiff = "Fallen";
        public string NewMapStrat = "Solo";
        public List<string> NewMapPlayerNames = new() { "Player 1" };
        public int NewMapSelectedPlayerIndex = 0;

        public bool ShowImportExportModal = false;
        public string ImportStatusMessage = "";
        public string ExportStatusMessage = "";

        public bool ShowSettingsModal = false;
        public bool ShowAboutModal = false;
        public bool ShowHelpModal = false;
        public bool ShowDeleteConfirmModal = false;

        public bool ShowUpdateModal = false;
        public string LatestVersionTag = "";
        public string ReleaseUrl = "";
        public string ManualCheckMessage = "";
        public float ManualCheckMessageTimer = 0.0f;

        public MapStrategy? CurrentMap => (SelectedMapIndex >= 0 && SelectedMapIndex < Strategies.Count)
            ? Strategies[SelectedMapIndex]
            : null;

        public void BeginEditing(MapStrategy map)
        {
            map.EnsureDefaultPlayer();
            EditMapName = map.MapName;
            EditDifficulty = map.Difficulty;
            EditStrategyName = map.StrategyName;

            EditPlayers = new List<PlayerStrategy>();
            foreach (var p in map.Players)
            {
                var newP = new PlayerStrategy
                {
                    PlayerName = p.PlayerName,
                    GeneralInfo = p.GeneralInfo,
                    ImagePaths = new List<string>(p.ImagePaths),
                    Steps = new List<StrategyStep>()
                };
                foreach (var s in p.Steps)
                {
                    newP.Steps.Add(new StrategyStep
                    {
                        StartWave = s.StartWave,
                        EndWave = s.EndWave,
                        Instruction = s.Instruction,
                        ImagePaths = new List<string>(s.ImagePaths)
                    });
                }
                EditPlayers.Add(newP);
            }
            EditSelectedPlayerIndex = Math.Clamp(map.SelectedPlayerIndex, 0, EditPlayers.Count - 1);
            SyncCurrentEditPlayerToContext();
            IsEditing = true;
        }

        public void SyncCurrentEditPlayerToContext()
        {
            if (EditPlayers.Count == 0)
            {
                EditPlayers.Add(new PlayerStrategy { PlayerName = "Player 1" });
            }
            EditSelectedPlayerIndex = Math.Clamp(EditSelectedPlayerIndex, 0, EditPlayers.Count - 1);
            var p = EditPlayers[EditSelectedPlayerIndex];
            EditGeneralInfo = p.GeneralInfo;
            EditImagePaths = p.ImagePaths;
            EditSteps = p.Steps;
        }

        public void SaveCurrentEditPlayerFromContext()
        {
            if (EditPlayers.Count == 0) return;
            EditSelectedPlayerIndex = Math.Clamp(EditSelectedPlayerIndex, 0, EditPlayers.Count - 1);
            var p = EditPlayers[EditSelectedPlayerIndex];
            p.GeneralInfo = EditGeneralInfo;
            p.ImagePaths = EditImagePaths;
            p.Steps = EditSteps;
        }

        public void ResetImageTransform()
        {
            ImageScale = 1.0f;
            ImageOffset = Vector2.Zero;
        }

        public void UpdateStepByWaveNumber(MapStrategy map, int waveNumber)
        {
            var steps = map.CurrentPlayer.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                if (waveNumber >= s.StartWave && waveNumber <= s.EndWave)
                {
                    if (CurrentStepIndex != i)
                    {
                        CurrentStepIndex = i;
                        ResetImageTransform();
                    }
                    break;
                }
            }
        }
    }
}
