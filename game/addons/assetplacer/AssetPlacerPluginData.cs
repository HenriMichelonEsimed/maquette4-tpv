// AssetPlacerPluginData.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class AssetPlacerPluginData : Resource
{
    [Export] public Godot.Collections.Dictionary<string, SceneData> sceneData = new ();
    [Export] public Godot.Collections.Dictionary<string, Variant> globalData = new ();
    [Export] public bool showLicenseOnStart = true;
}
#endif