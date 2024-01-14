// LinkButton.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AssetPlacer;

[Tool]
public partial class LinkButton : Button
{
    [Export] public string themeIcon = "ExternalLink";
    [Export] public string url;
    
    public override void _Ready()
    {
        var root = new EditorScript().GetEditorInterface().GetEditedSceneRoot();
        if (root == null || !root.IsAncestorOf(this))
        {
            ApplyIcon();
            Pressed += OpenLink;
        }
    }

    private void OpenLink()
    {
        OpenUrl(url);
    }
    
    private void ApplyIcon()
    {
        var baseControl = new EditorScript().GetEditorInterface().GetBaseControl();
        Icon = baseControl.GetThemeIcon(themeIcon, "EditorIcons");
    }
    
    // thanks to https://stackoverflow.com/questions/4580263/how-to-open-in-default-browser-in-c-sharp
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}

#endif