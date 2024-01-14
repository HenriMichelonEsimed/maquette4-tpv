// AssetPlacementController.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using Godot;

namespace AssetPlacer;

[Tool]
public abstract partial class AssetPlacementController : Node
{
    public bool Active { get => _active;
        set
        {
            var before = _active;
            _active = value;
            if(before != value) OnActiveChanged();
        }
    }

    protected virtual void OnActiveChanged()
    { }

    private bool _active;
    public float snappingGridSize;
    public const float DefaultSnappingGridSize = 1.0f;
    public const float SurfaceModeSnappingGridSide = .2f;
    protected PlacementUi placementUi;
    protected Snapping snapping;
    protected EditorInterface editorInterface;

    protected AssetPlacementController()
    {
    }

    protected AssetPlacementController(PlacementUi placementUi, Snapping snapping, EditorInterface editorInterface)
    {
        this.placementUi = placementUi;
        this.snapping = snapping;
        this.editorInterface = editorInterface;
        snapping.OffsetFromSelectedButtonPressed += () =>
        {
            if (Active) OffsetFromSelected(GetSelectedNode());
        };
        snapping.TranslateOffsetChanged += ()=> {
            if(Active) OnTranslateOffsetChanged();
        };
    }

    protected abstract void OnTranslateOffsetChanged();

    public abstract void OnSelectionChanged();
    
    protected Node3D GetSelectedNode()
    {
        var selectedNodes = editorInterface.GetSelection().GetSelectedNodes();
        if (selectedNodes.Count == 1 && selectedNodes[0] is Node3D node)
        {
            return node;
        }
        return null;
    }
    
    /**
     * returns the tooltip that should be shown on the mouse
     */
    public virtual string Process(Node sceneRoot, Camera3D viewportCamera, bool rmbPressed)
    {
        return null;
    }

    public abstract PlacementInfo GetPlacementPosition(Camera3D viewportCam, Vector2 viewportMousePosition, List<Node3D> placingNodes);

    public virtual bool ProcessInput(AssetPlacerPlugin.InputEventType inputEventType, Vector2 viewportMousePos)
    {
        return false;
    }

    public abstract Vector3 SnapToPosition(PlacementPositionInfo info, float snapStep);
    public abstract void OffsetFromSelected(Node3D node);
    public abstract void RotateNode(Node3D node3D, Vector3 startRotation, float rotation);
}

#endif