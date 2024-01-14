// PlacementInfo.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

using Godot;

#if TOOLS
#nullable disable
namespace AssetPlacer;

public abstract partial class AssetPlacementController
{
    public class PlacementInfo
    {
        public PlacementPositionInfo positionInfo;
        public string placementTooltip;
        public Color? placementTooltipColor;

        public PlacementInfo(PlacementPositionInfo positionInfo, string placementTooltip = null, Color? placementTooltipColor = null)
        {
            this.positionInfo = positionInfo;
            this.placementTooltip = placementTooltip;
            this.placementTooltipColor = placementTooltipColor;
        }
    }
}
#endif