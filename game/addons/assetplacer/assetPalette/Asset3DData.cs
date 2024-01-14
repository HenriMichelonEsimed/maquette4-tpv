// Asset3DData.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

public partial class Asset3DData : GodotObject
{
    public Transform3D defaultTransform = Transform3D.Identity;
    public Transform3D lastTransform = Transform3D.Identity;
    public bool hologramInstantiated = false; // true when the asset has been instantiated at least once
    public bool isMesh;
    public string path;
    public PreviewPerspective previewPerspective = PreviewPerspective.Default;

    public enum PreviewPerspective
    {
        Default, Front, Back, Top, Bottom, Left, Right
    }
    public Asset3DData()
    {
    }

    public Asset3DData(string path, PreviewPerspective perspective, bool isMesh = false)
    {
        this.path = path;
        this.previewPerspective = perspective;
        this.isMesh = isMesh;
    }
}
#endif