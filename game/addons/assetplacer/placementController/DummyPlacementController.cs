// DummyPlacementController.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using Godot;

namespace AssetPlacer;

public partial class DummyPlacementController : AssetPlacementController
{
	protected override void OnTranslateOffsetChanged()
	{
	}

	public override void OnSelectionChanged()
	{
	}

	public override PlacementInfo GetPlacementPosition(Camera3D viewportCam, Vector2 viewportMousePosition, List<Node3D> placingNodes)
	{
		return new(PlacementPositionInfo.invalidInfo);
	}

	public override Vector3 SnapToPosition(PlacementPositionInfo info, float snapStep)
	{
		return Vector3.Zero;
	}

	public override void OffsetFromSelected(Node3D node)
	{
	}

	public override void RotateNode(Node3D node3D, Vector3 startRotation, float rotation)
	{
	}
}
#endif