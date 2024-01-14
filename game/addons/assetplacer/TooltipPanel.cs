// TooltipPanel.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Linq;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class TooltipPanel : EditorDrawPanel
{
	private const int FirstFontSize = 12;
	private const int FontSize = 10;
	private string _tooltip;
	private Font _font;
	private Vector2 _position;
	private Array<Color> _tooltipColors;

	public override void _Draw()
	{
		if (_font == null || _tooltipColors == null) return;

		if (!Settings.GetSetting(Settings.DefaultCategory, Settings.ShowTooltips).AsBool()) return;
		
		if (!string.IsNullOrEmpty(_tooltip))
		{
			var toolTips = _tooltip.Split("\n");
			
			DrawString(_font,  _position+ new Vector2(-128,48), toolTips[0], HorizontalAlignment.Center, 256, FirstFontSize, _tooltipColors[0]);
			
			for (int i = 1; i<toolTips.Length; i++)
			{
				var color = _tooltipColors[_tooltipColors.Count <= i ? _tooltipColors.Count - 1 : i];
				DrawString(_font, _position + new Vector2(-128,48+16*i), toolTips[i], HorizontalAlignment.Center, 256, FontSize, color);
			}
		}
	}

	/**
	 * lineColors: define custom colors for the lines. Each is applied to one line. Excess lines are all colored half-transparent white.
	 */
	public void SetTooltip(string tooltip, Font font, Vector2 position, Rect2 tooltipRect, params Color[] lineColors)
	{
		var lines = tooltip.Split("\n");
		var height = FirstFontSize + (lines.Length-1) * FontSize;
		var width = lines.Max((s)=>s.Length);
		var posX = Mathf.Min(tooltipRect.End.X-width*4, Mathf.Max(tooltipRect.Position.X+width*4, position.X));
		var posY = Mathf.Min(tooltipRect.End.Y-height*2.5f, Mathf.Max(tooltipRect.Position.Y, position.Y+FirstFontSize));
		
		_position = new Vector2(posX, posY);
		_tooltip = tooltip;
		_font = font;
		
		var list = lineColors.ToList();
		list.ForEach(color=> color.A = Mathf.Min(color.A, 0.8f));
		list.Add(new Color(1,1,1,0.5f));
		_tooltipColors = new Array<Color>(list);
	}
}
#endif
