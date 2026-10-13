using System;
<<<<<<< HEAD
using System.Collections.Concurrent;
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
using System.Collections.Generic;
using System.Numerics;

namespace TdsOverlayImGui
{
    public class OverlayContext
    {
        public List<MapStrategy> Strategies = new();
        public AppSettings Settings = new();

        public int SelectedMapIndex = -1;
        public int CurrentStepIndex = 0;
        public int CurrentImageIndex = 0;

<<<<<<< HEAD
        public string StrategySearchFilter = "";

=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
        public bool IsOverlayOpen = true;
        public bool IsFirstFrame = true;

        public HashSet<string> CompletedTasks = new();
        public int CurrentWaveNumber = 1;
        public int PreviousWaveNumber = 1;

        // DJ Toast & Audio
        public HashSet<string> TriggeredDjAlerts = new();
        public HashSet<string> TriggeredOcrSounds = new();
        public float DjToastTimer = 0.0f;
        public string DjToastMessage = "";
        public Vector4 DjToastColor = Vector4.One;

        public float ToastTimer = 0.0f;
<<<<<<< HEAD
        public string DropStatusMessage = "";
        public float DropStatusTimer = 0.0f;

        // Очередь файлов, сброшенных через Drag-and-Drop
        public ConcurrentQueue<string> DroppedFilesQueue = new();
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5

        // Image Canvas Transform
        public float ImageScale = 1.0f;
        public Vector2 ImageOffset = Vector2.Zero;

        // OCR State
        public int? DetectedWaveNumber = null;
        public bool IsSelectingOcrRegion = false;
        public int OcrSelectionState = 0;
        public Vector2 OcrDragStart = Vector2.Zero;
        public DateTime LastOcrScanTime = DateTime.MinValue;

        // Editing State
        public bool IsEditing = false;
        public string EditMapName = "";
        public string EditDifficulty = "";
        public string EditStrategyName = "";
        public string EditGeneralInfo = "";
<<<<<<< HEAD
        public string EditorToastMessage = "";
        public float EditorToastTimer = 0.0f;
=======
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5

        // Modals
        public bool ShowAddMapModal = false;
        public string NewMapName = "";
        public string NewMapDiff = "Fallen";
        public string NewMapStrat = "Solo";

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

<<<<<<< HEAD
        public MapStrategy? CurrentMap => (SelectedMapIndex >= 0 && SelectedMapIndex < Strategies.Count)
            ? Strategies[SelectedMapIndex]
=======
        public MapStrategy? CurrentMap => (SelectedMapIndex >= 0 && SelectedMapIndex < Strategies.Count) 
            ? Strategies[SelectedMapIndex] 
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
            : null;

        public void ResetImageTransform()
        {
            ImageScale = 1.0f;
            ImageOffset = Vector2.Zero;
        }

        public void UpdateStepByWaveNumber(MapStrategy map, int waveNumber)
        {
            for (int i = 0; i < map.Steps.Count; i++)
            {
                var s = map.Steps[i];
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
<<<<<<< HEAD
}
=======
}
>>>>>>> b4a6a7b745000810b3bcafdfceec4f9a854bace5
