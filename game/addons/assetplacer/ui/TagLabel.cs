// TagLabel.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

[Tool]
public partial class TagLabel : Label
{
	public override void _Ready()
	{
		var root = new EditorScript().GetEditorInterface().GetEditedSceneRoot();
		if(root == null || !root.IsAncestorOf(this)) Text = AssetPlacerPlugin.ReplaceTags(Text);
	}
}
#endif
