// AboutDialog.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

[Tool]
public partial class AboutDialog : AcceptDialog
{
	public override void _Ready()
	{
		if (!Engine.IsEditorHint()) return;
		var file = FileAccess.Open("res://addons/assetplacer/EULA.txt", FileAccess.ModeFlags.Read);
		GetNode<TextEdit>("%License").Text = file.GetAsText();
		file.Close();
	}
}
#endif
