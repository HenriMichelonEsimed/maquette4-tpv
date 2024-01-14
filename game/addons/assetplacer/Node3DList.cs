// Node3DList.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using Godot;

namespace AssetPlacer;

[Tool]
public partial class Node3DList : GodotObject
{
	public Dictionary<Node3D, Transform3D> NodeTransforms { get; set; }

	public Node3DList(List<Node3D> nodes)
	{
		NodeTransforms = new Dictionary<Node3D, Transform3D>();
		foreach (var node3D in nodes)
		{
			NodeTransforms.Add(node3D, node3D.Transform);
		}
	}

	public Node3DList()
	{
		NodeTransforms = new Dictionary<Node3D, Transform3D>();
	}
}
#endif
