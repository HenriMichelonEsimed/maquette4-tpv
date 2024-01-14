// Settings.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable

using Godot;
using Godot.Collections;

namespace AssetPlacer;

public static class Settings
{
    public const string SettingsPrefix = "Asset_Placer";
    public const string DefaultCategory = "Settings";

    public const string ShowTooltips = "Show_Tooltips";
    public const string UseShiftSetting = "Use_Shift_instead_of_Alt";
    public const string SurfaceCollisionMask = "Surface_Placement_Collision_Mask";
    public const string LibrarySaveLocation = "Library_Save_File_Location";
    public static void InitSettings()
    {
        RegisterSetting(DefaultCategory, UseShiftSetting, false, Variant.Type.Bool);
        RegisterSetting(DefaultCategory, SurfaceCollisionMask, uint.MaxValue, Variant.Type.Int, PropertyHint.Layers3DPhysics);
        RegisterSetting(DefaultCategory, ShowTooltips, true, Variant.Type.Bool);
    }
    
    public static void RegisterSetting(string category, string settingName, Variant defaultValue, Variant.Type type, PropertyHint hint = PropertyHint.None, string hintString = null)
    {
        // Register a new ProjectSetting, that contains the shortcut for the Placement Plane Position
        var path = $"{SettingsPrefix}/{category}/{settingName}";
        if(!ProjectSettings.HasSetting(path))
        {
            ProjectSettings.SetSetting(path, defaultValue);
            ProjectSettings.SetInitialValue(path, defaultValue);
        }
        var propertyInfo = new Dictionary() {{"name", path}, {"type", (int) type}, {"hint", (int) hint}, {"hint_string", hintString}};
        ProjectSettings.AddPropertyInfo(propertyInfo);
    }

    public static Variant GetSetting(string category, string settingName)
    {
        return ProjectSettings.GetSetting($"{SettingsPrefix}/{category}/{settingName}");
    }
}

#endif