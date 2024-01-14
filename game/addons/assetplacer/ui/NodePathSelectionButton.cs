// NodePathSelectionButton.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class NodePathSelectionButton : Button
{
	[Signal]
	public  delegate void NodeDroppedEventHandler(Node node);

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (data.Obj is Dictionary dict)
		{
			if (dict["type"].AsString() == "nodes")
			{
				var paths = dict["nodes"].AsStringArray();
				return paths.Length == 1;
			}
		}

		return false;
	}
	
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var nodePath = ((Dictionary) data.Obj)?["nodes"].AsStringArray()[0];
		var node = GetTree().Root.GetNodeOrNull(nodePath);
		if (node == null)
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Node could not be found from path {nodePath}, try using selection button instead");
			return;
		}
		EmitSignal(SignalName.NodeDropped, node);
	}
	
	public void SetNode(Node node, Texture2D icon)
	{
		if (node != null)
		{
			Text = node.Name;
			RemoveThemeColorOverride("font_color");
		}
		else
		{
			Text = "<null>";
			AddThemeColorOverride("font_color", Colors.Red);
		}
		Icon = icon;
	}
}
#endif
