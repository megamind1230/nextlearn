using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace NextLearn.Desktop.Services;

public static class ThemeHelper
{
    private static readonly Dictionary<string, string> DarkColors = new()
    {
        ["PageBgBrush"] = "#0F172A",
        ["PanelBgBrush"] = "#1E293B",
        ["SurfaceBgBrush"] = "#334155",
        ["CardBgBrush"] = "#1E293B",
        ["SearchBoxBgBrush"] = "#1E293B",
        ["PathFieldBgBrush"] = "#1E293B",
        ["SelectorBgBrush"] = "#1E293B",
        ["CheckboxBgBrush"] = "#1E293B",
        ["CheckmarkFgBrush"] = "#3B82F6",
        ["CardSelectedBgBrush"] = "#1E3A5F",
        ["CodeBlockBgBrush"] = "#282C34",
        ["McqWrongBgBrush"] = "#450A0A",
        ["TextPrimaryBrush"] = "#E2E8F0",
        ["TextSecondaryBrush"] = "#94A3B8",
        ["TextMutedBrush"] = "#64748B",
        ["TextAccentBrush"] = "#FBBF24",
        ["TextHoverBrush"] = "#CBD5E1",
        ["TextPressedBrush"] = "#475569",
        ["TextOnAccentBrush"] = "White",
        ["TextOnAccentMutedBrush"] = "#BFDBFE",
        ["ButtonDefaultBgBrush"] = "#E2E8F0",
        ["ButtonDefaultFgBrush"] = "#1E293B",
        ["ButtonHoverBgBrush"] = "#CBD5E1",
        ["ButtonPressedBgBrush"] = "#94A3B8",
        ["ButtonPressedFgBrush"] = "#475569",
        ["ButtonFadedBgBrush"] = "#F1F5F9",
        ["ButtonFadedFgBrush"] = "#94A3B8",
        ["ButtonFadedHoverBgBrush"] = "#E2E8F0",
        ["ButtonFadedHoverFgBrush"] = "#64748B",
        ["ButtonFadedPressedBgBrush"] = "#CBD5E1",
        ["ButtonFadedPressedFgBrush"] = "#475569",
        ["CommandPaletteHoverBgBrush"] = "#2D3748",
        ["ButtonSecondaryBgBrush"] = "#334155",
        ["ButtonPrimaryBgBrush"] = "#7C0AED",
        ["ButtonSuccessBgBrush"] = "#7C0AED",
        ["ButtonDangerBgBrush"] = "#EF4444",
        ["BorderDefaultBrush"] = "#334155",
        ["BorderMutedBrush"] = "#475569",
        ["ModalBackdropBrush"] = "#80000000",
        ["StatMinutesBrush"] = "#FDBA74",
        ["StatPagesBrush"] = "#60A5FA",
        ["StatDecksBrush"] = "#34D399",
        ["StatStreakBrush"] = "#F472B6",
        ["HeatmapLevel0"] = "#1E293B",
        ["HeatmapLevel1"] = "#FED7AA",
        ["HeatmapLevel2"] = "#FDBA74",
        ["HeatmapLevel3"] = "#FB923C",
        ["HeatmapLevel4"] = "#EA580C",
        ["HeatmapLevel5"] = "#C2410C",
    };

    private static readonly Dictionary<string, string> LightColors = new()
    {
        ["PageBgBrush"] = "#F8FAFC",
        ["PanelBgBrush"] = "#AAAAAA",
        ["SurfaceBgBrush"] = "#F1F5F9",
        ["CardBgBrush"] = "#F8FAFC",
        ["SearchBoxBgBrush"] = "#F8FAFC",
        ["PathFieldBgBrush"] = "#F8FAFC",
        ["SelectorBgBrush"] = "#F8FAFC",
        ["CheckboxBgBrush"] = "#F8FAFC",
        ["CheckmarkFgBrush"] = "#2563EB",
        ["CardSelectedBgBrush"] = "#EFF6FF",
        ["CodeBlockBgBrush"] = "#F1F5F9",
        ["McqWrongBgBrush"] = "#FEE2E2",
        ["TextPrimaryBrush"] = "#1E293B",
        ["TextSecondaryBrush"] = "#64748B",
        ["TextMutedBrush"] = "#6B7C8E",
        ["TextAccentBrush"] = "#FBBF24",
        ["TextHoverBrush"] = "#64748B",
        ["TextPressedBrush"] = "#CBD5E1",
        ["TextOnAccentBrush"] = "White",
        ["TextOnAccentMutedBrush"] = "#BFDBFE",
        ["ButtonDefaultBgBrush"] = "#F1F5F9",
        ["ButtonDefaultFgBrush"] = "#1E293B",
        ["ButtonHoverBgBrush"] = "#E2E8F0",
        ["ButtonPressedBgBrush"] = "#CBD5E1",
        ["ButtonPressedFgBrush"] = "#64748B",
        ["ButtonFadedBgBrush"] = "#E2E8F0",
        ["ButtonFadedFgBrush"] = "#64748B",
        ["ButtonFadedHoverBgBrush"] = "#CBD5E1",
        ["ButtonFadedHoverFgBrush"] = "#475569",
        ["ButtonFadedPressedBgBrush"] = "#94A3B8",
        ["ButtonFadedPressedFgBrush"] = "#334155",
        ["CommandPaletteHoverBgBrush"] = "#E2E8F0",
        ["ButtonSecondaryBgBrush"] = "#E2E8F0",
        ["ButtonPrimaryBgBrush"] = "#7C0AED",
        ["ButtonSuccessBgBrush"] = "#7C0AED",
        ["ButtonDangerBgBrush"] = "#EF4444",
        ["BorderDefaultBrush"] = "#E2E8F0",
        ["BorderMutedBrush"] = "#CBD5E1",
        ["ModalBackdropBrush"] = "#80000000",
        ["StatMinutesBrush"] = "#FDBA74",
        ["StatPagesBrush"] = "#60A5FA",
        ["StatDecksBrush"] = "#34D399",
        ["StatStreakBrush"] = "#F472B6",
        ["HeatmapLevel0"] = "#AAAAAA",
        ["HeatmapLevel1"] = "#FED7AA",
        ["HeatmapLevel2"] = "#FDBA74",
        ["HeatmapLevel3"] = "#FB923C",
        ["HeatmapLevel4"] = "#EA580C",
        ["HeatmapLevel5"] = "#C2410C",
    };

    public static void ApplyTheme(string? theme)
    {
        var isLight = theme == "Light";
        var colors = isLight ? LightColors : DarkColors;
        var resources = Application.Current!.Resources;

        foreach (var (key, color) in colors)
        {
            resources[key] = new SolidColorBrush(Color.Parse(color));
        }

        Application.Current.RequestedThemeVariant =
            isLight ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
