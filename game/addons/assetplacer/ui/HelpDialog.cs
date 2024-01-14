// HelpDialog.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Godot;

namespace AssetPlacer;

[Tool]
public partial class HelpDialog : AcceptDialog
{
	public void InitShortcutTable(Dictionary<string, string> shortcutStringDictionary)
	{
		GetNode<ShortcutTable>("%ShortcutsTable").Init(shortcutStringDictionary);
	}
}
#endif
