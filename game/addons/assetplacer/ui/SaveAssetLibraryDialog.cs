// SaveAssetLibraryDialog.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

[Tool]
public partial class SaveAssetLibraryDialog : FileDialog
{
    public string AssetLibraryName { get; set; }
    public bool ChangeName { get; set; }
}
#endif