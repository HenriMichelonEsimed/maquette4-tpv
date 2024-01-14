// SurfacePlacementPositionInfo.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using Godot;

namespace AssetPlacer;

public class SurfacePlacementPositionInfo : PlacementPositionInfo
{
    public Func<Transform3D, Vector3> alignmentDir;
    public bool align;
    public Vector3 surfaceNormal;

    public SurfacePlacementPositionInfo(Vector3 pos, bool posValid, bool align = false, Vector3 surfaceNormal = new (), Func<Transform3D, Vector3> alignmentDir = null) : base(pos, posValid)
    {
        this.surfaceNormal = surfaceNormal;
        this.alignmentDir = alignmentDir ?? (transform3D =>transform3D.Up());
        this.align = align;
    }
}
#endif