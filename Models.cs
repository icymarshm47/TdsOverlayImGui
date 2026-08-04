using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TdsOverlayImGui
{
    public enum AppLanguage
    {
        Russian,
        English
    }

    public static class Loc
    {
        public static AppLanguage CurrentLanguage = AppLanguage.Russian;

        private static readonly Dictionary<string, (string Ru, string En)> Dict = new()
        {
            { "File", ("Файл", "File") },
            { "Other", ("Прочее", "Other") },
            { "Settings", ("Настройки", "Settings") },
            { "ImportExport", ("Импорт / Экспорт", "Import / Export") },
            { "About", ("О программе", "About") },
            { "AddStrategy", ("+ Стратегия", "+ Strategy") },
            { "EditStrategy", ("Редактировать", "Edit") },
            { "ViewMode", ("Просмотр", "View") },
            { "DeleteStrategy", ("Удалить", "Delete") },
            { "NoStrategySelected", ("Стратегия не выбрана!", "No strategy selected!") },
            { "SelectStrategyPrompt", ("Выберите стратегию из списка выше или нажмите '+ Стратегия', чтобы создать новую.", "Select a strategy from the list above or click '+ Strategy' to create a new one.") },
            { "SearchPlaceholder", ("🔍 Поиск стратегии...", "🔍 Search strategy...") },
            { "SelectStrategyHeader", ("🎮 ВЫБОР СТРАТЕГИИ:", "🎮 STRATEGY SELECTION:") },
            { "GeneralInfo", ("📌 ОБЩАЯ ИНФОРМАЦИЯ И ЭКИПИРОВКА:", "📌 GENERAL INFO & LOADOUT:") },
            { "Step", ("Шаг", "Step") },
            { "Of", ("из", "of") },
            { "Waves", ("ВОЛНЫ", "WAVES") },
            { "PrevStep", ("< Назад", "< Prev") },
            { "NextStep", ("Вперед >", "Next >") },
            { "InstructionHeader", ("📜 ИНСТРУКЦИЯ К ТЕКУЩЕМУ ДИАПАЗОНУ:", "📜 INSTRUCTION FOR CURRENT RANGE:") },
            { "CopyInstruction", ("[Копировать текст]", "[Copy Text]") },
            { "ClearChecks", ("[Сбросить галочки]", "[Clear Checks]") },
            { "CopiedToast", ("[Скопировано в буфер!]", "[Copied to clipboard!]") },
            { "SeparateImageNotice", ("Картинки открыты в отдельном окне рядом ->", "Images opened in separate window ->") },
            { "PlacementImage", ("Картинка расстановки:", "Placement Image:") },
            { "ZoomNotice", ("Зум: {0}% | ЛКМ: Двигать | Колесико: Зум | СКМ: Сброс", "Zoom: {0}% | LMB: Drag | Wheel: Zoom | MMB: Reset") },
            { "PrevPhoto", ("< Пред. фото", "< Prev Photo") },
            { "NextPhoto", ("След. фото >", "Next Photo >") },
            { "PhotoCount", ("Фото {0} из {1}", "Photo {0} of {1}") },
            { "LanguageSetting", ("ЯЗЫК ИНТЕРФЕЙСА / LANGUAGE:", "UI LANGUAGE / LANGUAGE:") },
            { "ImageModeSetting", ("ОТОБРАЖЕНИЕ КАРТИНКИ РАССТАНОВКИ:", "PLACEMENT IMAGE DISPLAY:") },
            { "SeparateWindowMode", ("Показывать в отдельном перетаскиваемом окне", "Show in separate movable window") },
            { "EmbeddedMode", ("Показывать внутри главного окна", "Show inside main window") },
            { "Close", ("Закрыть", "Close") },
            { "Cancel", ("Отмена", "Cancel") },
            { "Save", ("Сохранить изменения", "Save changes") },
            { "Create", ("Создать", "Create") },
            { "DeleteConfirmTitle", ("Удалить стратегию?", "Delete strategy?") },
            { "DeleteConfirmText", ("Вы действительно хотите удалить стратегию:\n«{0}»?", "Are you sure you want to delete strategy:\n\"{0}\"?") },
            { "YesDelete", ("Да, удалить", "Yes, delete") },
            { "MapName", ("Название карты", "Map Name") },
            { "Difficulty", ("Сложность", "Difficulty") },
            { "StrategyVariant", ("Вариант стратегии", "Strategy Variant") },
            { "ExportZip", ("Экспортировать в ZIP-архив", "Export to ZIP archive") },
            { "ImportFile", ("Импортировать стратегию", "Import strategy") },
            { "FilePath", ("Путь к файлу", "File path") },
            { "ExportSection", ("ЭКСПОРТ ТЕКУЩЕЙ СТРАТЕГИИ:", "EXPORT CURRENT STRATEGY:") },
            { "ImportSection", ("ИМПОРТ СТРАТЕГИИ (.zip или .json):", "IMPORT STRATEGY (.zip or .json):") },
            { "AutoOcrHeader", ("🎯 Авто-сканер волны (OCR)", "🎯 Auto Wave Scanner (OCR)") },
            { "SelectOcrRegionBtn", ("📐 Выделить область на экране", "📐 Select Screen Region") },
            { "InGameWave", ("⚡ ВОЛНА В ИГРЕ:", "⚡ IN-GAME WAVE:") },
            { "AutoOcrTag", ("(Авто-OCR)", "(Auto-OCR)") },
            { "CurrentWaveHeader", ("🌊 ТЕКУЩАЯ ВОЛНА:", "🌊 CURRENT WAVE:") },
            { "ActiveInGame", ("(АКТИВНО В ИГРЕ)", "(ACTIVE IN-GAME)") },
            { "ResizeHandleText", ("[=== Зажмите ЛКМ и тяните для изменения высоты ===]", "[=== Hold LMB and drag to resize height ===]") },
            { "EditingTitle", ("РЕДАКТИРОВАНИЕ СТРАТЕГИИ", "EDITING STRATEGY") },
            { "GeneralInfoLabel", ("Общая информация / Инфо (для всей стратегии):", "General Info / Notes (for entire strategy):") },
            { "ImagesHeader", ("КАРТИНКИ РАССТАНОВКИ (ЕДИНЫ ДЛЯ ВСЕЙ СТРАТЕГИИ):", "PLACEMENT IMAGES (COMMON FOR ENTIRE STRATEGY):") },
            { "PhotoNum", ("Фото #{0}:", "Photo #{0}:") },
            { "DeletePhotoBtn", ("Удалить фото", "Delete Photo") },
            { "AddPhotoBtn", ("+ Добавить фото стратегии", "+ Add Strategy Photo") },
            { "StepsHeader", ("Шаги и диапазоны волн:", "Steps & Wave Ranges:") },
            { "MarkdownHint", ("(Используйте - [ ] в тексте для галочек)", "(Use - [ ] in text for checkboxes)") },
            { "FromWave", ("С волны", "From wave") },
            { "ToWave", ("По волну", "To wave") },
            { "StepInstructionLabel", ("Инструкция к шагу:", "Step instruction:") },
            { "DeleteStepBtn", ("Удалить этот шаг", "Delete this step") },
            { "AddStepBtn", ("+ Добавить ещё шаг", "+ Add another step") },
            { "DeleteEntireStrategyBtn", ("Удалить всю стратегию", "Delete Entire Strategy") },
            { "NoStepsNotice", ("В этой стратегии нет шагов.", "No steps in this strategy.") },
            { "AddDefaultStepBtn", ("+ Добавить шаг (1-30)", "+ Add step (1-30)") },
            { "OcrOverlayInstruction", ("🎯 КЛИКНИТЕ И ЗАЖМИТЕ ЛКМ НА ЭКРАНЕ, ЧТОБЫ ВЫДЕЛИТЬ РАМКОЙ ЦИФРУ ВОЛНЫ В ROBLOX (ESC - Отмена)", "🎯 CLICK AND HOLD LMB ON SCREEN TO SELECT WAVE NUMBER IN ROBLOX (ESC - Cancel)") },
            { "SeparateImageTitle", ("Картинка расстановки", "Placement Image") },
            { "AboutTitle", ("О программе", "About") },
            { "AboutVersion", ("Версия: 0.2 Alpha", "Version: 0.2 Alpha") },
            { "AboutAuthor", ("Автор: icymarsh", "Author: icymarsh") },
            { "AboutDesc", ("Оверлей для Tower Defense Simulator в Roblox", "Overlay for Tower Defense Simulator in Roblox") }
        };

        public static string Tr(string key)
        {
            if (Dict.TryGetValue(key, out var val))
            {
                return CurrentLanguage == AppLanguage.English ? val.En : val.Ru;
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
        public string StrategyName { get; set; } = "Основная";
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