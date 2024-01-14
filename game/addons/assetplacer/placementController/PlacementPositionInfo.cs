// PlacementPositionInfo.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

public class PlacementPositionInfo
{
    public static PlacementPositionInfo invalidInfo = new (Vector3.Zero, false);
    public Vector3 pos = Vector3.Zero;
    public bool posValid;
        
    public PlacementPositionInfo(Vector3 pos, bool posValid)
    {
        this.pos = pos;
        this.posValid = posValid;
    }
}
#endif