// SurfacePlacementController.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;


namespace AssetPlacer;

[Tool]
public partial class SurfacePlacementController : AssetPlacementController
{
    private Node3D _worldNode;
    private Node _root;

    public SurfacePlacementController()
    {
    }

    public SurfacePlacementController(PlacementUi placementUi, Snapping snapping, EditorInterface editorInterface, Node root) : base(placementUi, snapping, editorInterface)
    {
        SetSceneRoot(root);
        snappingGridSize = SurfaceModeSnappingGridSide;
    }

    protected SurfacePlacementPositionInfo _lastValidPlacementInfo;
    public override PlacementInfo GetPlacementPosition(Camera3D viewportCam, Vector2 viewportMousePosition, List<Node3D> placingNodes)
    {
        if(_worldNode == null || viewportCam == null) return new PlacementInfo(PlacementPositionInfo.invalidInfo);
        var from = viewportCam.ProjectRayOrigin(viewportMousePosition * viewportCam.GetViewport().GetVisibleRect().Size);
        var dir = viewportCam.ProjectRayNormal(viewportMousePosition * viewportCam.GetViewport().GetVisibleRect().Size);
        EditorRaycast raycast = new EditorRaycast(_worldNode, from, dir, viewportCam.Projection);

        var result = raycast.PerformRaycast(placingNodes);
        
        if (result.Count <= 0)
        {
            return new PlacementInfo(PlacementPositionInfo.invalidInfo, "Hover over a physics surface to place");
        }
        
        var pos = result["position"].AsVector3();
        var normal = result["normal"].AsVector3();
        var placementInfo = new SurfacePlacementPositionInfo(pos, true, placementUi.AlignWithSurfaceNormal, normal, placementUi.AlignmentDirection);
        var (gridPos, gridRot) = GetGridTransform(placementInfo);
        snapping.UpdateGridTransform(gridPos, viewportCam.Projection == Camera3D.ProjectionType.Perspective, gridRot);
        _lastValidPlacementInfo = placementInfo;
        if(placementInfo.posValid) snapping.HideGrid(false);
        return new PlacementInfo(placementInfo);
    }

    protected override void OnTranslateOffsetChanged()
    {
        var offset = snapping.TranslateSnapOffset % (Vector2.One * snapping.TranslateSnapStep);
        snapping.SetTranslateOffset(offset);
    }

    public override void OnSelectionChanged()
    {
        var selectedNodes = editorInterface.GetSelection().GetSelectedNodes();
        snapping.SetOffsetFromSelectedButtonDisabled(_lastValidPlacementInfo == null || selectedNodes.Count != 1);
    }

    public override void OffsetFromSelected(Node3D node)
    {
        // project selected node position onto the surface of last placementInfo
        if(_lastValidPlacementInfo == null) return; // should not happen
        var translateOffsetFromPosition = SurfaceSnapping.GetTranslateOffsetFromPosition(node.GlobalPosition, _lastValidPlacementInfo.pos, _lastValidPlacementInfo.surfaceNormal, snapping.TranslateSnapStep);

        snapping.SetTranslateOffset(translateOffsetFromPosition);
    }

    public override void RotateNode(Node3D node3D, Vector3 startRotation, float rotation)
    {
        if(_lastValidPlacementInfo == null) return; // should not happen
        node3D.Rotation = startRotation;
        node3D.GlobalRotate(_lastValidPlacementInfo.surfaceNormal, rotation);
    }

    private bool CheckForTerrain3DWithoutCollisionsRec(Node cur)
    {
        if (cur.IsClass("Terrain3D"))
        {
            var debugCollisions = cur.Call("get_show_debug_collision").AsInt32();
            return debugCollisions == 0; // debugCollisions disabled
        }
        foreach (var child in cur.GetChildren())
        {
            var isTerrain3D = CheckForTerrain3DWithoutCollisionsRec(child);
            if (isTerrain3D) return true;
        }

        return false;
    }

    protected override void OnActiveChanged()
    {
        // If Terrain3D is enabled, throw a warning about debug collisions having to be enabled
        if (Active && editorInterface.IsPluginEnabled(ContextlessPlugin.PluginTerrain3D) && _root != null)
        {
            if(CheckForTerrain3DWithoutCollisionsRec(_root)) GD.PushWarning("To place on Terrain3D, debug collisions need to be enabled");
        }
        
        // On recompiling, when all values get re-assigned, this method triggers before snapping is set. 
        // We prevent a null pointer, such that the rest of the items can get assigned
        if (snapping == null) return; 
        
        if (Active)
        {
            snapping.SetOffsetLabelTexts("a:", "b:");
            _lastValidPlacementInfo = null;
            snapping.ResetGridPos();
            snapping.HideGrid(true);
        }
        else
        {
            snapping.HideGrid(false);
        }
    }

    public override string Process(Node sceneRoot, Camera3D viewportCamera, bool _)
    {
        if (_worldNode == null) return null;

        if (_lastValidPlacementInfo != null)
        {
            var (gridPos, gridRot) = GetGridTransform(_lastValidPlacementInfo);
            snapping.UpdateGridTransform(gridPos, viewportCamera.Projection == Camera3D.ProjectionType.Perspective, gridRot);
        }

        return null;
    }

    public void SetSceneRoot(Node root)
    {
        _root = root;
        try
        {
            _worldNode?.QueueFree();
        }
        catch (ObjectDisposedException)
        { }

        if (root != null)
        {
            _worldNode = new Node3D();
            root.AddChild(_worldNode);
        }
        else
        {
            _worldNode = null;
        }
    }
    
    public override bool ProcessInput(AssetPlacerPlugin.InputEventType inputEventType, Vector2 viewportMousePos)
    {
        return false;
    }

    public override Vector3 SnapToPosition(PlacementPositionInfo info, float snapStep)
    {
        Debug.Assert(info is SurfacePlacementPositionInfo);
        var offset = new Vector3(snapping.TranslateSnapOffset.X, 0, snapping.TranslateSnapOffset.Y);
        return SurfaceSnapping.SnapToPositionWithOffset((SurfacePlacementPositionInfo)info, snapStep, offset);
    }
    
    protected (Vector3 pos, Vector3 rot) GetGridTransform(SurfacePlacementPositionInfo info)
    {
        var q = Vector3.Up.GetRotationToVector(info.surfaceNormal);
        
        return (SnapToPosition(info, snapping.TranslateSnapStep), q.GetEuler());
    }
}
#endif