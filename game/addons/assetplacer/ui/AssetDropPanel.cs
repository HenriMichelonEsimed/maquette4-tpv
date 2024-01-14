// AssetDropPanel.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class AssetDropPanel : Control
{
	[Signal]
	public  delegate void AssetsDroppedEventHandler(string[] assetPaths);

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is Dictionary dict)
		{
			if (dict["type"].AsString() == "files")
			{
				var paths = dict["files"].AsStringArray();
				return !paths.IsEmpty();
			}
		}

		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		EmitSignal(SignalName.AssetsDropped, ((Dictionary) data.Obj)?["files"].AsStringArray());
	}
}
#endif

