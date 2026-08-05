using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui
{
    public enum AppLanguage
    {
        English,
        Russian
    }

    public static class Loc
    {
        public static AppLanguage CurrentLanguage = AppLanguage.English;

        private static readonly Dictionary<string, string> DictEn = new()
        {
            { "File", "File" },
            { "Other", "Other" },
            { "Settings", "Settings" },
            { "ImportExport", "Import / Export" },
            { "About", "About" },
            { "ExitApp", "Exit Application" },
            { "AddStrategy", "+ Strategy" },
            { "EditStrategy", "Edit" },
            { "ViewMode", "View Mode" },
            { "DeleteStrategy", "Delete" },
            { "NoStrategySelected", "No strategy selected!" },
            { "SelectStrategyPrompt", "Select a strategy from the list above or click '+ Strategy' to create a new one." },
            { "SearchPlaceholder", "Search strategy..." },
            { "SelectStrategyHeader", "STRATEGY SELECTION:" },
            { "SelectStrategyCombo", "- Select Strategy -" },
            { "GeneralInfo", "GENERAL INFO & LOADOUT:" },
            { "Step", "Step" },
            { "Of", "of" },
            { "Waves", "WAVES" },
            { "PrevStep", "< Prev" },
            { "NextStep", "Next >" },
            { "InstructionHeader", "INSTRUCTION FOR CURRENT RANGE:" },
            { "CopyInstruction", "[Copy Text]" },
            { "ClearChecks", "[Clear Checks]" },
            { "CopiedToast", "[Copied to clipboard!]" },
            { "SeparateImageNotice", "Images opened in separate window ->" },
            { "PlacementImage", "Placement Image:" },
            { "ZoomNotice", "Zoom: {0}% | LMB: Drag | Wheel: Zoom | MMB: Reset" },
            { "PrevPhoto", "< Prev Photo" },
            { "NextPhoto", "Next Photo >" },
            { "PhotoCount", "Photo {0} of {1}" },
            { "LanguageSetting", "UI LANGUAGE:" },
            { "ImageModeSetting", "PLACEMENT IMAGE DISPLAY:" },
            { "SeparateWindowMode", "Show in separate movable window" },
            { "EmbeddedMode", "Show inside main window" },
            { "Close", "Close" },
            { "Cancel", "Cancel" },
            { "Save", "Save changes" },
            { "Create", "Create" },
            { "DeleteConfirmTitle", "Delete strategy?" },
            { "DeleteConfirmText", "Are you sure you want to delete strategy:\n\"{0}\"?" },
            { "YesDelete", "Yes, delete" },
            { "MapName", "Map Name" },
            { "Difficulty", "Difficulty" },
            { "StrategyVariant", "Strategy Name" },
            { "ExportZip", "Export to ZIP archive" },
            { "ImportFile", "Import strategy" },
            { "FilePath", "File path" },
            { "ExportSection", "EXPORT CURRENT STRATEGY:" },
            { "ImportSection", "IMPORT STRATEGY (.zip or .json):" },
            { "AutoOcrHeader", "Auto Wave Scanner (OCR)" },
            { "SelectOcrRegionBtn", "Select Screen Region" },
            { "InGameWave", "IN-GAME WAVE:" },
            { "AutoOcrTag", "(Auto-OCR)" },
            { "CurrentWaveHeader", "CURRENT WAVE:" },
            { "ActiveInGame", "(ACTIVE IN-GAME)" },
            { "ResizeHandleText", "[=== Hold LMB and drag to resize height ===]" },
            { "EditingTitle", "EDITING STRATEGY" },
            { "GeneralInfoLabel", "General Info / Notes (for entire strategy):" },
            { "ImagesHeader", "PLACEMENT IMAGES (COMMON FOR ENTIRE STRATEGY):" },
            { "PhotoNum", "Photo #{0}:" },
            { "DeletePhotoBtn", "Delete Photo" },
            { "AddPhotoBtn", "+ Add Strategy Photo" },
            { "SelectFileBtn", "Browse File" },
            { "PasteClipboardBtn", "Paste from clipboard" },
            { "NoImageSelected", "[No image selected]" },
            { "ClipboardNoImageToast", "[No image in clipboard!]" },
            { "StepsHeader", "Steps & Wave Ranges:" },
            { "MarkdownHint", "(Use - [ ] in text for checkboxes)" },
            { "FromWave", "From wave" },
            { "ToWave", "To wave" },
            { "StepInstructionLabel", "Step instruction:" },
            { "DeleteStepBtn", "Delete this step" },
            { "AddStepBtn", "+ Add another step" },
            { "DeleteEntireStrategyBtn", "Delete Entire Strategy" },
            { "NoStepsNotice", "No steps in this strategy." },
            { "AddDefaultStepBtn", "+ Add step (1-30)" },
            { "OcrOverlayInstruction", "CLICK AND HOLD LMB ON SCREEN TO SELECT WAVE NUMBER IN ROBLOX (ESC - Cancel)" },
            { "SeparateImageTitle", "Placement Image" },
            { "AboutTitle", "About" },
            { "AboutVersion", "Version: 0.4" },
            { "AboutAuthor", "Authors: icymarsh, Google Gemini (code)" },
            { "AboutDesc", "Overlay for Tower Defense Simulator in Roblox" },

            // Update Translations
            { "CheckUpdates", "Check for Updates" },
            { "UpdateModalTitle", "New Update Available!" },
            { "UpdateNotice", "A new version of TDS Strategy Overlay is available on GitHub." },
            { "CurrentVersionLabel", "Current Version:" },
            { "LatestVersionLabel", "Latest Version:" },
            { "DownloadUpdateBtn", "Open Download Page" },
            { "NoUpdatesNotice", "You are using the latest version!" }
        };

        private static readonly Dictionary<string, string> DictRu = new()
        {
            { "File", "Файл" },
            { "Other", "Другое" },
            { "Settings", "Настройки" },
            { "ImportExport", "Импорт / Экспорт" },
            { "About", "О программе" },
            { "ExitApp", "Закрыть приложение" },
            { "AddStrategy", "+ Стратегия" },
            { "EditStrategy", "Редактировать" },
            { "ViewMode", "Режим просмотра" },
            { "DeleteStrategy", "Удалить" },
            { "NoStrategySelected", "Стратегия не выбрана!" },
            { "SelectStrategyPrompt", "Выберите стратегию из списка выше или нажмите '+ Стратегия', чтобы создать новую." },
            { "SearchPlaceholder", "Поиск стратегии..." },
            { "SelectStrategyHeader", "ВЫБОР СТРАТЕГИИ:" },
            { "SelectStrategyCombo", "- Выберите стратегию -" },
            { "GeneralInfo", "ОБЩАЯ ИНФОРМАЦИЯ И ЛОАДАУТ:" },
            { "Step", "Шаг" },
            { "Of", "из" },
            { "Waves", "ВОЛНЫ" },
            { "PrevStep", "< Назад" },
            { "NextStep", "Вперед >" },
            { "InstructionHeader", "ИНСТРУКЦИЯ ДЛЯ ТЕКУЩЕГО ДИАПАЗОНА:" },
            { "CopyInstruction", "[Копировать текст]" },
            { "ClearChecks", "[Сбросить галочки]" },
            { "CopiedToast", "[Скопировано в буфер обмена!]" },
            { "SeparateImageNotice", "Изображения открыты в отдельном окне ->" },
            { "PlacementImage", "Изображение расстановки:" },
            { "ZoomNotice", "Масштаб: {0}% | ЛКМ: Перемещение | Колесо: Масштаб | СКМ: Сброс" },
            { "PrevPhoto", "< Пред. фото" },
            { "NextPhoto", "След. фото >" },
            { "PhotoCount", "Фото {0} из {1}" },
            { "LanguageSetting", "ЯЗЫК ИНТЕРФЕЙСА:" },
            { "ImageModeSetting", "ОТОБРАЖЕНИЕ ИЗОБРАЖЕНИЙ:" },
            { "SeparateWindowMode", "Показывать в отдельном перемещаемом окне" },
            { "EmbeddedMode", "Показывать внутри главного окна" },
            { "Close", "Закрыть" },
            { "Cancel", "Отмена" },
            { "Save", "Сохранить изменения" },
            { "Create", "Создать" },
            { "DeleteConfirmTitle", "Удалить стратегию?" },
            { "DeleteConfirmText", "Вы уверены, что хотите удалить стратегию:\n\"{0}\"?" },
            { "YesDelete", "Да, удалить" },
            { "MapName", "Название карты" },
            { "Difficulty", "Сложность" },
            { "StrategyVariant", "Название стратегии" },
            { "ExportZip", "Экспортировать в ZIP-архив" },
            { "ImportFile", "Импортировать стратегию" },
            { "FilePath", "Путь к файлу" },
            { "ExportSection", "ЭКСПОРТ ТЕКУЩЕЙ СТРАТЕГИИ:" },
            { "ImportSection", "ИМПОРТ СТРАТЕГИИ (.zip или .json):" },
            { "AutoOcrHeader", "Сканер волн (OCR)" },
            { "SelectOcrRegionBtn", "Выбрать область экрана" },
            { "InGameWave", "ВОЛНА В ИГРЕ:" },
            { "AutoOcrTag", "(Авто-OCR)" },
            { "CurrentWaveHeader", "ТЕКУЩАЯ ВОЛНА:" },
            { "ActiveInGame", "(АКТИВНО В ИГРЕ)" },
            { "ResizeHandleText", "[=== Зажмите ЛКМ и тяните для изменения высоты ===]" },
            { "EditingTitle", "РЕДАКТИРОВАНИЕ СТРАТЕГИИ" },
            { "GeneralInfoLabel", "Общая информация / Заметки (для всей стратегии):" },
            { "ImagesHeader", "ИЗОБРАЖЕНИЯ РАССТАНОВКИ (ОБЩИЕ ДЛЯ ВСЕЙ СТРАТЕГИИ):" },
            { "PhotoNum", "Фото №{0}:" },
            { "DeletePhotoBtn", "Удалить фото" },
            { "AddPhotoBtn", "+ Добавить фото стратегии" },
            { "SelectFileBtn", "Выбрать файл" },
            { "PasteClipboardBtn", "Вставить из буфера" },
            { "NoImageSelected", "[Файл не выбран]" },
            { "ClipboardNoImageToast", "[В буфере нет картинки!]" },
            { "StepsHeader", "Шаги и диапазоны волн:" },
            { "MarkdownHint", "(Используйте - [ ] в тексте для чекбоксов)" },
            { "FromWave", "С волны" },
            { "ToWave", "По волну" },
            { "StepInstructionLabel", "Инструкция шага:" },
            { "DeleteStepBtn", "Удалить этот шаг" },
            { "AddStepBtn", "+ Добавить еще один шаг" },
            { "DeleteEntireStrategyBtn", "Удалить всю стратегию" },
            { "NoStepsNotice", "В этой стратегии нет шагов." },
            { "AddDefaultStepBtn", "+ Добавить шаг (1-30)" },
            { "OcrOverlayInstruction", "ЗАЖМИТЕ ЛКМ НА ЭКРАНЕ, ЧТОБЫ ВЫДЕЛИТЬ НОМЕР ВОЛНЫ В ROBLOX (ESC - Отмена)" },
            { "SeparateImageTitle", "Изображение расстановки" },
            { "AboutTitle", "О программе" },
            { "AboutVersion", "Версия: 0.4" },
            { "AboutAuthor", "Авторы: icymarsh, Google Gemini (code)" },
            { "AboutDesc", "Оверлей для Tower Defense Simulator в Roblox" },

            // Update Translations
            { "CheckUpdates", "Проверить обновления" },
            { "UpdateModalTitle", "Доступно новое обновление!" },
            { "UpdateNotice", "На GitHub вышло новое обновление оверлея." },
            { "CurrentVersionLabel", "Текущая версия:" },
            { "LatestVersionLabel", "Новая версия:" },
            { "DownloadUpdateBtn", "Скачать обновление" },
            { "NoUpdatesNotice", "У вас установлена последняя версия!" }
        };

        public static string Tr(string key)
        {
            if (CurrentLanguage == AppLanguage.Russian && DictRu.TryGetValue(key, out var valRu))
            {
                return valRu;
            }

            if (DictEn.TryGetValue(key, out var valEn))
            {
                return valEn;
            }

            return key;
        }
    }

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
    }
}