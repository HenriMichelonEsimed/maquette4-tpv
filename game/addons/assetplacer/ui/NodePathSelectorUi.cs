// NodePathSelectorUi.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

[Tool]
public partial class NodePathSelectorUi : Control
{
	[Export] public NodePath selectNodeButton;
	[Export] public NodePath setSelectedButton;
	[Export] public bool defaultAssignRoot;
	[Export] public string classType;

	public NodePathSelectionButton _selectNodeButton;
	public Button _setSelectedButton;

	public void Init()
	{
		_selectNodeButton = GetNode<NodePathSelectionButton>(selectNodeButton);
		_setSelectedButton = GetNode<Button>(setSelectedButton);
		_setSelectedButton.Disabled = true;
		_setSelectedButton.Text = "";
	}

	public void ApplyTheme(Control baseControl)
	{
		// apply special themes for hard edge buttons with no coloring
		var selectIcon = baseControl.GetThemeIcon("ListSelect", "EditorIcons");
		_setSelectedButton.Icon = selectIcon;
		var themeStyleboxNormal = _selectNodeButton.GetThemeStylebox("normal") as StyleBoxFlat;
		themeStyleboxNormal.BgColor = ((StyleBoxFlat) baseControl.GetThemeStylebox("normal", "Button")).BgColor;
		_selectNodeButton.AddThemeStyleboxOverride("normal", themeStyleboxNormal);
		_setSelectedButton.AddThemeStyleboxOverride("normal", themeStyleboxNormal);
		Color font = baseControl.GetThemeColor("font_color", "Label");
		Color highlightedFont = (font.R + font.G + font.B) / 3f > 0.5f ? Colors.White : new Color(0.05f, 0.05f, 0.05f); 
		_selectNodeButton.AddThemeColorOverride("font_hover_color", highlightedFont);
	}

	public void SetSelectedButtonDisabled(bool value)
	{
		_setSelectedButton.Disabled = value;
	}
	
	public void SetNode(Node node, Texture2D icon)
	{
		_selectNodeButton.SetNode(node, icon);
	}
}
#endif
