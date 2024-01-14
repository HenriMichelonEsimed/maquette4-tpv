// ContextlessPlugin.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

namespace AssetPlacer;

[Tool]
/*
 * Base class for plugins, that do not edit specific node or resource types
 * Provides methods to draw over the editor and to access 3D viewports
 */
public abstract partial class ContextlessPlugin : EditorPlugin
{
	public const string PluginTerrain3D = "terrain_3d";
	protected Control drawPanel;
	protected bool initFailed = false;

	private Godot.Collections.Dictionary<SubViewport, bool> _vpMouseEntered = new();

	public sealed override void _EnterTree()
	{
		if (!Engine.IsEditorHint()) return;
		drawPanel = _CreateDrawPanel();
		GetEditorInterface().GetBaseControl().AddChild(drawPanel);

		int i = 0;
		foreach (SubViewport vp in Get3DViewports())
		{
			var node3DEditorViewport = vp.GetParent().GetParent<Control>();
			var vpControl = node3DEditorViewport.GetChild(1) as Control;

			Debug.Assert(vpControl != null, nameof(vpControl) + " != null");

			// I know, this looks very stupid, but upon recompiling lambdas with captured variables are killed.
			// Thus, we use constants.
			switch (i)
			{
				case 0:
					vpControl.MouseEntered += () => OnViewportMouseSignal(0, true);
					vpControl.MouseExited += () => OnViewportMouseSignal(0, false);
					break;
				case 1:
					vpControl.MouseEntered += () => OnViewportMouseSignal(1, true);
					vpControl.MouseExited += () => OnViewportMouseSignal(1, false);
					break;
				case 2:
					vpControl.MouseEntered += () => OnViewportMouseSignal(2, true);
					vpControl.MouseExited += () => OnViewportMouseSignal(2, false);
					break;
				case 3:
					vpControl.MouseEntered += () => OnViewportMouseSignal(3, true);
					vpControl.MouseExited += () => OnViewportMouseSignal(3, false);
					break;
			}
			i++;
		}
		_Init();
	}

	public sealed override void _ExitTree()
	{
		if (!Engine.IsEditorHint()) return;
		drawPanel?.QueueFree();
		if(!initFailed) _Cleanup();
	}

	/**
	 * Override this method, if you want to have a draw panel with custom logic
	 */
	protected virtual Control _CreateDrawPanel()
	{
		return new EditorDrawPanel();
	}
	
	public sealed override void _Input(InputEvent @event)
	{
		if (!Engine.IsEditorHint() || initFailed) return;
		_ForwardInput(@event);
		var viewport = GetFocused3DViewport();
		if (viewport != null && !GetTree().Root.IsInputHandled())
		{
			var stopInput = false;

			// offset positional events, such that 0,0 is at the beginning of the viewport
			var justifiedEvent = @event;
			if (@event.GetPropertyList().Any((d=>d["name"].AsString() == "position")))
			{
				
				justifiedEvent = (InputEvent) @event.Duplicate();
				Vector2 offsetPosition =
					justifiedEvent.Get("position").AsVector2() - viewport.GetScreenTransform().Origin;
				justifiedEvent.Set("position", offsetPosition / viewport.GetScreenTransform().Scale);
			}
			
			if (@event is InputEventMouse) 
			{
				// The mouse might be hovering over some buttons, context menus, etc., so we check the signals here
				if (IsMouseOverViewport(viewport))
				{
					stopInput = _Forward3DViewportInput(viewport, justifiedEvent);
				}
			}
			else
			{
				stopInput = _Forward3DViewportInput(viewport, justifiedEvent);
			}
			 
			if (stopInput)
			{
				GetTree().Root.SetInputAsHandled();
			}
		}
	}

	private void OnViewportMouseSignal(int index, bool entered)
	{
		var vps = Get3DViewports().ToList();
		Debug.Assert(index <= vps.Count);
		var subViewport = vps[index];
		_vpMouseEntered[subViewport] = entered;
	}

	private bool IsMouseOverViewport(SubViewport viewport)
	{
		if (!_vpMouseEntered.ContainsKey(viewport))
		{
			_vpMouseEntered.Add(viewport, false);
		}
		return _vpMouseEntered[viewport];
	}

	public sealed override void _UnhandledInput(InputEvent @event)
	{
		if (!Engine.IsEditorHint() || initFailed) return;
		var viewport = GetFocused3DViewport();
		if (viewport != null)
		{
			var stopInput = _Forward3DViewportUnhandledInput(viewport, @event);
			if (stopInput)
			{
				GetTree().Root.SetInputAsHandled();
			}
		}
	}

	public sealed override void _Process(double delta)
	{
		if (!Engine.IsEditorHint() || initFailed) return;
		_ProcessUpdate(delta);
		drawPanel.QueueRedraw();
	}

	protected virtual void _Init() {}
	protected virtual void _Cleanup() {}
	protected virtual void _ForwardInput(InputEvent @event) {}
	protected virtual bool _Forward3DViewportInput(Viewport vp, InputEvent e)
	{
		return false;
	}	
	protected virtual bool _Forward3DViewportUnhandledInput(Viewport vp, InputEvent e)
	{
		return false;
	}
	
	protected virtual void _ProcessUpdate(double delta) { }

	protected bool IsMouseOverValidFocused3dViewport()
	{
		var vp = Get3DViewportUnderMouse();
		var focusedVp = GetFocused3DViewport();
		if (vp != null && focusedVp != null) return IsMouseOverViewport(vp) && vp==focusedVp && !IsEditorViewportPreviewingCamera(vp);
		return false;
	}
	protected SubViewport Get3DViewportUnderMouse()
	{
		// MainScreen -> Node3DEditor
		var mainScreen3DVisible = GetEditorInterface().GetEditorMainScreen().GetChild<Control>(1).Visible;
		if (!mainScreen3DVisible) return null;

		var viewports = Get3DViewports();
		foreach (SubViewport viewport in viewports)
		{
			if (!viewport.GetParent().GetParent<Control>().Visible) continue;
			var viewportPos = viewport.GetScreenTransform().Origin;
			var viewportSize = viewport.GetVisibleRect().Size * viewport.GetScreenTransform().Scale;
			if (new Rect2(viewportPos, viewportSize).HasPoint(GetViewport().GetMousePosition()))
				return viewport;
		}

		return null;
	}
	
	protected SubViewport GetFocused3DViewport()
	{
		var viewports = Get3DViewports();
		
		foreach(SubViewport vp in viewports)
		{
			if (IsEditorViewportFocused(vp)) return vp;
		}
		
		return null;
	}

	protected SubViewport GetFirstViewport()
	{
		return Get3DViewports().First();
	}

	protected bool IsPluginEnabled(string pluginName)
	{
		return GetEditorInterface().IsPluginEnabled(pluginName);
	}

	private IEnumerable<SubViewport> Get3DViewports()
	{
		var mainScreen = GetEditorInterface().GetEditorMainScreen();
		// MainScreen -> Node3DEditor -> HSplitContainer -> HSplitContainer -> VSplitContainer -> Node3DEditorViewportContainer
		var hsplitContainer1 = mainScreen.GetChild(1).GetChild(1);
		var hsplitContainer2 = hsplitContainer1.GetChild(hsplitContainer1.GetChildCount() - 1); // last child
		var viewportContainer = hsplitContainer2.GetChild(0).GetChild(0);
		
		var node3DEditorViewports = viewportContainer.GetChildren();
		// Node3DEditorViewport -> SubViewportContainer -> SubViewport
		var viewports = node3DEditorViewports.Select((vp) => vp.GetChild(0).GetChild(0) as SubViewport);
		return viewports;
	}

	public static bool IsEditorViewportPreviewingCamera(Viewport viewport)
	{
		var viewportContainer = viewport.GetParent().GetParent(); // Node3DEditorViewportContainer
		var previewCheckbox = viewportContainer.GetChild(1).GetChild(0).GetChild<CheckBox>(1); // Node3DEditorViewportContainer -> Control -> VBoxContainer -> Checkbox (preview)
		return previewCheckbox.ButtonPressed;
	}

	public static bool IsEditorViewportFocused(Viewport viewport)
	{
		var editorVp = viewport.GetParent().GetParent<Control>();
		var vpControl = editorVp.GetChild(1) as Control;
		return editorVp.Visible && (vpControl?.HasFocus() is true);
	}

	public static void FocusEditorViewport(Viewport viewport)
	{
		var editorVp = viewport.GetParent().GetParent<Control>();
		var vpControl = editorVp.GetChild(1) as Control;
		vpControl?.GrabFocus();
	}
}
#endif
