// SceneData.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class SceneData : Resource
{
    [Export] public Godot.Collections.Dictionary<string, Variant> data = new();
}
#endif