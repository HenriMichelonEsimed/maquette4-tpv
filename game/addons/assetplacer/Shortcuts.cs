// Shortcuts.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetPlacer;
	
[Tool]
public class Shortcuts
{
    private Dictionary<string, Func<bool>> _shortcuts3dGui = new();

    public const string ShortcutsCategory = "Shortcuts";
    public const string PlacementPlanePosition = "Change_Placement_Plane_Position";
    public const string TransformAsset = "Transform_Asset_Blueprint";
    public const string SelectPreviousAsset = "Select_Previous_Asset";
    public const string DoubleSnapStep = "Double_Snap_Step";
    public const string HalveSnapStep = "Halve_Snap_Step";
    public const string RotateY = "Rotate_Asset_90_Degrees_Around_Y";
    public const string RotateX = "Rotate_Asset_90_Degrees_Around_X";
    public const string RotateZ = "Rotate_Asset_90_Degrees_Around_Z";
    public const string ShiftRotateY = "Rotate_Asset_Secondary_Step_Degrees_Around_Y";
    public const string ShiftRotateX = "Rotate_Asset_Secondary_Step_Degrees_Around_X";
    public const string ShiftRotateZ = "Rotate_Asset_Secondary_Step_Degrees_Around_Z";
    public const string FlipY = "Flip_Asset_On_Y_Axis";
    public const string FlipX = "Flip_Asset_On_X_Axis";
    public const string FlipZ = "Flip_Asset_On_Z_Axis";
    public const string ResetTransform = "Reset_Transform";
    public const string SelectXZPlane = "Select_X_Z_Plane";
    public const string SelectYZPlane = "Select_Y_Z_Plane";
    public const string SelectXYPlane = "Select_X_Y_Plane";

    private void RegisterShortcut(string settingName, List<InputEventKey> eventKeys)
    {
        // Register a new ProjectSetting, that contains the shortcut for the Placement Plane Position
        var shortcut = new Shortcut();
        eventKeys.ForEach(key => shortcut.Events.Add(key));
        Settings.RegisterSetting(ShortcutsCategory, settingName, shortcut, Variant.Type.Object, PropertyHint.ResourceType, "Shortcut");
    }

    public void Add3DGuiShortcut(Func<bool> action, string name, List<InputEventKey> eventKeys)
    {
        RegisterShortcut(name, eventKeys);
        _shortcuts3dGui.Add(name, action);
    }

    public void AddSimpleKeys3DGuiShortcut(Func<bool> action, string name, Key first, params Key[] keys)
    {
        AddKeys3DGuiShortcut(action, name, false, false, first, keys);
    }
    
    public void AddKeys3DGuiShortcut(Func<bool> action, string name, bool shift, bool ctrl, Key first, params Key[] keys)
    {
        List<InputEventKey> events = new();
        var allKeys = keys.Prepend(first);
        foreach (var key in allKeys)
        {
            var inputEvent = new InputEventKey();
            inputEvent.Keycode = key;
            inputEvent.Pressed = true;
            inputEvent.Echo = false;
            inputEvent.ShiftPressed = shift;
            inputEvent.CtrlPressed = ctrl;
            events.Add(inputEvent);
        }
        Add3DGuiShortcut(action, name, events);
    }

    public bool Input3DGui(InputEvent inputEvent)
    {
        return Input(inputEvent, _shortcuts3dGui);
    }

    public static string GetShortcutString(string shortcutName)
    {
        var shortcut = Settings.GetSetting(ShortcutsCategory, shortcutName).Obj as Shortcut;
        var transformStr = "";
        shortcut?.Events.ToList().ForEach(e =>
        {
            if (e.Obj is InputEventKey key)
            {
                string keyStr = key.Keycode.ToString();
                if (keyStr.Length == 4 && keyStr.StartsWith("Key"))
                {
                    keyStr = keyStr.Replace("Key", "");
                }
                string combineStr = (key.CtrlPressed ? "CTRL+" : "")+(key.ShiftPressed ? "SHIFT+" : "")+(key.AltPressed ? "ALT+" : "") + keyStr;
                transformStr += (transformStr == "" ? "" : "/") + combineStr;
            }
        });
        return transformStr;
    }
    
    private bool Input(InputEvent inputEvent, Dictionary<string, Func<bool>> shortcutList)
    {
        if (inputEvent.IsEcho() || !inputEvent.IsPressed()) return false;
        foreach(KeyValuePair<string, Func<bool>> pair in shortcutList)
        {
            var setting = Settings.GetSetting(ShortcutsCategory, pair.Key);
            if (setting.Obj is Shortcut shortcut && shortcut.MatchesEvent(inputEvent))
            {
                return pair.Value.Invoke();
            }
        }

        return false;
    }

    public Dictionary<string, string> GetShortcutStringDictionary()
    {
        Dictionary<string, string> shortcutsStringDict = new();
        foreach (var shortcut in _shortcuts3dGui.Keys.ToList())
        {
            shortcutsStringDict.Add(shortcut.Replace('_', ' '), GetShortcutString(shortcut));
        }

        return shortcutsStringDict;
    }
}

#endif