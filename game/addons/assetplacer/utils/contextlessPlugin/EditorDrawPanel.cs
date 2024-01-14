// EditorDrawPanel.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

/**
 * Class used by Contextless Plugin, to allow drawing over the viewport.
 * Override _Draw to use it.
 */
public partial class EditorDrawPanel : Control
{
	public override void _EnterTree()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return;
		if (GetParent() is Control control)
		{
			Size = control.Size;
		}
	}
}
#endif
