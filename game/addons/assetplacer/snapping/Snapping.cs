// Snapping.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using Godot;
using static Godot.GD;

namespace AssetPlacer;

[Tool]
public partial class Snapping : Node
{
    private GridVertices _grid;
    private PackedScene _gridScene;
    private EditorInterface _editorInterface;
    private SnappingUi _snappingUi;
    private bool _forceVisible;
    private bool _forceInvisible;
    public float TranslateSnapStep => _snappingUi?.TranslateSnapStep ?? 1f;
    public float TranslateShiftSnapStep => _snappingUi?.TranslateShiftSnapStep ?? 0.1f;

    private PlacementUi _placementUi;
    private DeconstructorNode _gridDeconstructor;
    public int LineCnt { get; set; } = DefaultLineCnt;
    public const int DefaultLineCnt = 100;
    
    [Signal]
    public  delegate void OffsetFromSelectedButtonPressedEventHandler();
    
    [Signal]
    public  delegate void TranslateOffsetChangedEventHandler();
    
    public Vector2 TranslateSnapOffset => _snappingUi?.TranslateSnapOffset ?? Vector2.Zero;

    public void Init(EditorInterface editorInterface)
    {
        _editorInterface = editorInterface;
        _gridScene = ResourceLoader.Load<PackedScene>("res://addons/assetplacer/gizmos/Grid4.2.tscn");
    }

    public void SetUi(SnappingUi snappingUi)
    {
        _snappingUi = snappingUi;
        _snappingUi.TranslateSnapStepChanged += OnTranslateSnapStepChanged;
        _snappingUi.TranslateOffsetChanged += () => EmitSignal(SignalName.TranslateOffsetChanged);
        _snappingUi._offsetFromSelectedButton.Pressed += () => EmitSignal(SignalName.OffsetFromSelectedButtonPressed);
    }

    public override void _Process(double delta)
    {
        if (!Engine.IsEditorHint()) return;
        if (!IsInsideTree() || _editorInterface == null || _snappingUi == null || _editorInterface.GetEditedSceneRoot() == null) return;
        if (_grid == null || !_grid.IsInsideTree())
        {
            _grid?.QueueFree();
            _grid = _gridScene.Instantiate<GridVertices>();
            if(_gridDeconstructor != null) _gridDeconstructor.Deconstruct -= GridGizmoDeconstructor;
            _gridDeconstructor = new DeconstructorNode();
            _gridDeconstructor.Deconstruct += GridGizmoDeconstructor;
            _grid.AddChild(_gridDeconstructor);
            _editorInterface.GetEditedSceneRoot().AddChild(_grid);
            UpdateGridMesh();
            ShowGrid(false);
        }

        _grid.Visible = (_forceVisible || TranslateSnappingActive()) && !_forceInvisible;
    }

    public void ClearGrid()
    {
        if (_grid != null && _grid.IsInsideTree())
        {
            _grid.QueueFree();
            _gridDeconstructor.Deconstruct -= ClearGrid;
        }
        _grid = null;
    }
    private void GridGizmoDeconstructor()
    {
        _grid?.QueueFree();
        _grid = null;
        _gridDeconstructor = null;
    }
    
    public void UpdateGridMesh()
    {
        if (_grid == null) return;
        _grid.lineSpacing = _snappingUi.TranslateSnapStep;
        _grid.lineCnt = LineCnt;
        _grid.CreateMesh();
    }

    public void UpdateGridTransform(Vector3 gridPos, bool isPerspective, Vector3 gridRot)
    {
        if (_grid == null) return;
        _grid.GlobalPosition = gridPos;
        _grid.GlobalRotation = gridRot;
        _grid.UpdateCam(isPerspective);
    }

    public void ResetGridPos()
    {
        if (_grid == null) return;
        _grid.GlobalPosition = Vector3.Zero;
        _grid.GlobalRotation = Vector3.Zero;
    }
    
    public void ShowGrid(bool show)
    {
        _forceVisible = show;
    }
    
    public void HideGrid(bool hide)
    {
        _forceInvisible = hide;
    }

    public bool TranslateSnappingActive()
    {
        return _snappingUi.TranslateSnappingActive();
    }
    
    public void SetOffsetLabelTexts(string text1, string text2)
    {
        _snappingUi.SetOffsetLabelTexts(text1, text2);
    }

    public void SetOffsetFromSelectedButtonDisabled(bool disabled)
    {
        _snappingUi?.OffsetFromSelectedButtonDisabled(disabled);
    }

    public void SetTranslateOffset(Vector2 offset)
    {
        _snappingUi.SetTranslateOffset(offset);
    }

    public void OnTranslateSnapStepChanged()
    {
        EmitSignal(SignalName.TranslateOffsetChanged); // to make offset smaller than step
        UpdateGridMesh();
    }

    public void DoubleSnapStep()
    {
        _snappingUi.MultiplySnapSteps(2);
    }
    public void HalveSnapStep()
    {
        _snappingUi.MultiplySnapSteps(0.5f);
    }
}

#endif