// ShortcutTable.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System.Collections.Generic;

[Tool]
public partial class ShortcutTable : GridContainer
{
	public void Init(Dictionary<string, string> shortcutStringDictionary)
	{
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		// Add all shortcuts
		foreach (var shortcut in shortcutStringDictionary)
		{
			Label nameLabel = new Label();
			AddChild(nameLabel);
			nameLabel.Text = shortcut.Key;
			
			Label shortcutLabel = new Label();
			AddChild(shortcutLabel);
			shortcutLabel.Text = shortcut.Value;
		}
	}
}
#endif
