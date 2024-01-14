// RightClickPopup.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class RightClickPopup : PopupMenu
{
    public string itemPath;
    public Godot.Collections.Dictionary<long, Callable> actions = new();
    public Godot.Collections.Dictionary<long, Callable> conditions = new();
    public Godot.Collections.Dictionary<long, StringName> actionSignals = new();
    public Godot.Collections.Dictionary<long, Callable> enumActions = new();
    
    public override void _Ready()
    {
        IdPressed += OnEntryPressed;
    }

    private void OnEntryPressed(long id)
    {
        if (actionSignals.ContainsKey(id))
        {
            actions[id].Call(actionSignals[id], itemPath);
        }
        else if(actions.ContainsKey(id))
        {
            actions[id].Call(itemPath);
        }
    }

    public void UpdateConditions()
    {
        foreach (var id in conditions.Keys)
        {
            SetItemDisabled((int) id, !conditions[id].Call(itemPath).AsBool());
        }
    }

    public void AddEntry(string label, Texture2D icon, Callable onPressed)
    {
        var itemCount = ItemCount;
        AddIconItem(icon, label, itemCount);
        actions[itemCount] = onPressed;
    }

    public void AddEntry(string label, Texture2D icon, Callable onPressed, StringName signalName)
    {
        var itemCount = ItemCount;
        AddEntry(label, icon, onPressed);
        actionSignals[itemCount] = signalName;
    }
    
    public void AddEntry(string label, Texture2D icon, Callable onPressed, Callable condition)
    {
        var itemCount = ItemCount;
        AddEntry(label, icon, onPressed);
        conditions[itemCount] = condition;
    }

    public void AddEnumEntry(string label, Callable onPressed, string[] subEntries)
    {
        var itemCount = ItemCount;
        var subMenuPopup = new RightClickPopupSubmenu(itemCount);
        subMenuPopup.Name = label;
        AddSubmenuItem(label, label, itemCount);
        AddChild(subMenuPopup);
        enumActions[itemCount] = onPressed;

        int sub = 0;
        foreach (var entry in subEntries)
        {
            subMenuPopup.AddRadioCheckItem(entry, sub);
            sub++;
        }
        actions[itemCount] = onPressed;
        subMenuPopup.SubmenuSelected += (id, subId) => { enumActions[id].Call(itemPath, subId);};
    }

    public void SetEnumEntryChecked(string label, int idx)
    {
        var submenu = GetNode<RightClickPopupSubmenu>(label);
        for (int i = 0; i < submenu.ItemCount; i++)
        {
            submenu.SetItemChecked(i, i==idx);
        }
    }
}
#endif