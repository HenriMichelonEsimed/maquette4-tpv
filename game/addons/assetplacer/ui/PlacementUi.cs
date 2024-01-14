// PlacementUi.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AssetPlacer;
[Tool]
public partial class PlacementUi : PanelContainer
{
	private const string PlacementModeSaveKey = "placement_mode";
	private const string PlacementPlaneSaveKey = "placement_plane";
	private const string AlignWithSurfaceSaveKey = "align_with_surface";
	private const string AlignmentDirectionSaveKey = "align_direction";
	private const string PlanePositionsSaveKey = "plane_positions";
	public const string Terrain3DSaveKey = "terrain3d_path";
	
	[Export] private NodePath placementModeOptionButton;
	[Export] private NodePath planeOptionButton;
	[Export] private NodePath planePositionLineEdit;
	[Export] private NodePath positionFromSelectedButton;
	[Export] private NodePath resetPositionButton;
	[Export] private NodePath alignToSurfaceNormalCheckbox;
	[Export] private NodePath planePlacementModeContainer;
	[Export] private NodePath surfacePlacementModeContainer;
	[Export] private NodePath alignmentDirectionOptionButton;
	[Export] private NodePath terrain3DSelectorButton;
	
	private Control _planePlacementModeContainer;
	private Control _surfacePlacementModeContainer;
	private OptionButton _placementModeOptionButton;
	private OptionButton _planeOptionButton;
	private LineEdit _planePositionLineEdit;
	private CheckBox _alignToSurfaceNormalCheckbox;
	public Button _positionFromSelectedButton;
	private Button _resetPositionButton;
	private OptionButton _alignmentDirectionOptionButton;
	private NodePathSelectorUi _terrain3DSelectorUi;

	public NodePathSelector _terrain3DSelector;

	[Signal]
	public delegate void PlanePositionChangedEventHandler();
	
	[Signal]
	public delegate void PlaneChangedEventHandler();
	
	[Signal]
	public delegate void PlacementModeChangedEventHandler(int mode);
	
	public enum PlacementMode
	{
		Plane, Surface, Terrain3D, Dummy
	}
	private Godot.Collections.Dictionary<string, PlacementMode> _placementModesStrings = new() {{"Plane", PlacementMode.Plane}, {"Surface", PlacementMode.Surface}, {"Terrain3D", PlacementMode.Terrain3D}, {"Dummy", PlacementMode.Dummy}};
	private PlacementMode GetPlacementMode(long index)
	{
		string text = _placementModeOptionButton.GetItemText((int) index);
		if (!_placementModesStrings.ContainsKey(text))
		{
			GD.PrintErr("Invalid Placement Mode: " + text);
			return PlacementMode.Dummy;
		}
		return _placementModesStrings[text];
	}
	private string GetPlacementModeString(PlacementMode mode)
	{
		return _placementModesStrings.Keys.FirstOrDefault(m => _placementModesStrings[m] == mode, "Dummy");
	}
	
	public float PlanePosition { get; private set; } = 0f;
	public bool AlignWithSurfaceNormal => _alignToSurfaceNormalCheckbox?.ButtonPressed ?? false;

	private Godot.Collections.Dictionary<Vector3.Axis, float> _planePositions = new() { {Vector3.Axis.X, 0f}, {Vector3.Axis.Y, 0f}, {Vector3.Axis.Z, 0f} };
	
	private Vector3.Axis TextToAxis(string text) {
		return text.ToLower().Substring(0,2) switch
		{
			"yz" => (Vector3.Axis.X),
			"xz"=> (Vector3.Axis.Y),
			"xy" => (Vector3.Axis.Z),
			_ => Vector3.Axis.Y
		};
	}

	public Vector3.Axis GetPlaneNormal()
	{
		if (_planeOptionButton == null) return Vector3.Axis.Y;
		return TextToAxis(_planeOptionButton.GetItemText(_planeOptionButton.Selected));
	}

	private void SetPlaneNormal(Vector3.Axis normal)
	{
		for (int i = 0; i < _planeOptionButton.ItemCount; i++)
		{
			if (TextToAxis(_planeOptionButton.GetItemText(i)) == normal)
			{
				_planeOptionButton.Select(i);
				return;
			}
		}
	}
	
	public Vector3 PlaneNormalVec {
		get
		{
			return GetPlaneNormal() switch
			{
				Vector3.Axis.X => Vector3.Right,
				Vector3.Axis.Y => Vector3.Up,
				Vector3.Axis.Z => Vector3.Forward,
				_ => Vector3.Up
			};
		}
	}
	
	public Func<Transform3D, Vector3> AlignmentDirection
	{
		get
		{
			if(_alignmentDirectionOptionButton == null) return t=>t.Up();
			return _alignmentDirectionOptionButton.GetItemText(_alignmentDirectionOptionButton.Selected).ToLower().Substring(0,2) switch
			{
				"+x" => t=>t.Right(),
				"-x" => t=>t.Left(),
				"+y" => t=>t.Up(),
				"-y"=> t=>t.Down(),
				"+z" => t=>t.Back(),
				"-z" =>t=>t.Forward(),
				_ => t=>t.Up()
			};
		}
	}

	private Vector3.Axis _currentNormal = Vector3.Axis.Y;
	
	public void Init()
	{
		// initialize UI nodes
		_placementModeOptionButton = GetNode<OptionButton>(placementModeOptionButton);
		_planeOptionButton = GetNode<OptionButton>(planeOptionButton);
		_planePositionLineEdit = GetNode<LineEdit>(planePositionLineEdit);
		_positionFromSelectedButton = GetNode<Button>(positionFromSelectedButton);
		_resetPositionButton = GetNode<Button>(resetPositionButton);
		_alignToSurfaceNormalCheckbox = GetNode<CheckBox>(alignToSurfaceNormalCheckbox);
		_planePlacementModeContainer = GetNode<Control>(planePlacementModeContainer);
		_surfacePlacementModeContainer = GetNode<Control>(surfacePlacementModeContainer);
		_alignmentDirectionOptionButton = GetNode<OptionButton>(alignmentDirectionOptionButton);
		_terrain3DSelectorUi = GetNode<NodePathSelectorUi>(terrain3DSelectorButton);
		_terrain3DSelectorUi.Init();
		_terrain3DSelector = new NodePathSelector();
		AddChild(_terrain3DSelector);
		_terrain3DSelector.SetUi(_terrain3DSelectorUi);

		_planePositionLineEdit.TextSubmitted += OnPlanePositionTextSubmitted;
		_planeOptionButton.ItemSelected += OnPlaneChanged;
		_placementModeOptionButton.ItemSelected += OnPlacementModeSelected;
		if(AssetPlacerPlugin.Development && !PlacementOptionButtonHasItem(PlacementMode.Dummy)) _placementModeOptionButton.AddItem(GetPlacementModeString(PlacementMode.Dummy));
		_positionFromSelectedButton.Disabled = true;
		_positionFromSelectedButton.TooltipText = "Set position such that the selected object is on the plane";
		_resetPositionButton.Pressed += () => SetPlanePosition(0f);
		_alignToSurfaceNormalCheckbox.Toggled += OnAlignToSurfaceNormalChecked;
		_alignmentDirectionOptionButton.ItemSelected += OnAlignmentDirectionSelected;
		
		UpdatePlacementMode(_placementModeOptionButton.Selected);
	}

	public void AddTerrain3DOption()
	{
		if(!PlacementOptionButtonHasItem(PlacementMode.Terrain3D)) _placementModeOptionButton.AddItem(GetPlacementModeString(PlacementMode.Terrain3D));
	}
	private bool PlacementOptionButtonHasItem(PlacementMode mode)
	{
		for (int i = 0; i < _placementModeOptionButton.ItemCount; i++)
		{
			if (GetPlacementModeString(mode) == _placementModeOptionButton.GetItemText(i)) return true;
		}

		return false;
	}

	public void ApplyTheme(Control baseControl)
	{
		var selectIcon = baseControl.GetThemeIcon("EditorPositionUnselected", "EditorIcons");
		_positionFromSelectedButton.Text = "";
		_positionFromSelectedButton.Icon = selectIcon;
		var resetIcon = baseControl.GetThemeIcon("Reload", "EditorIcons");
		_resetPositionButton.Text = "";
		_resetPositionButton.Icon = resetIcon;
		_terrain3DSelectorUi.ApplyTheme(baseControl);
	}

	private void OnPlaneChanged(long index)
	{
		if(index != -1) AssetPlacerPersistence.StoreSceneData(PlacementPlaneSaveKey, index);
		UpdatePlane();
	}

	private void UpdatePlane()
	{
		//if (_currentNormal == PlaneNormal) return; // repeated press of shortcut keeps showing gizmos this way
		_currentNormal = GetPlaneNormal();
		
		// do this before calling plane changed, so the correct values can be accessed straight away.
		PlanePosition = _planePositions[GetPlaneNormal()];
		
		EmitSignal(SignalName.PlaneChanged);
		// now update also the text, and notify
		SetPlanePosition(_planePositions[GetPlaneNormal()]);
	}

	public void SetPlanePosition(float pos)
	{
		SetPlanePosition(GetPlaneNormal(), pos);
	}

	public void SetPlanePosition(Vector3.Axis planeNormal, float pos)
	{
		_planePositions[planeNormal] = pos;
		if (GetPlaneNormal() == planeNormal)
		{
			PlanePosition = pos;
			_planePositionLineEdit.Text = PlanePosition.ToString(CultureInfo.InvariantCulture);
			 EmitSignal(SignalName.PlanePositionChanged);
		}
		AssetPlacerPersistence.StoreSceneData(PlanePositionsSaveKey, new []{_planePositions[Vector3.Axis.X],_planePositions[Vector3.Axis.Y],_planePositions[Vector3.Axis.Z] });
	}

	public void SetPlane(Vector3.Axis normal)
	{
		SetPlaneNormal(normal);
		OnPlaneChanged(-1);
	}
	private void OnPlacementModeSelected(long index)
	{
		AssetPlacerPersistence.StoreSceneData(PlacementModeSaveKey, index);
		UpdatePlacementMode(index);
	}

	private void UpdatePlacementMode(long index)
	{
		switch (GetPlacementMode(index))
		{
			case PlacementMode.Plane:
				_planePlacementModeContainer.Visible = true;
				_surfacePlacementModeContainer.Visible = false;
				_terrain3DSelectorUi.Visible = false;
				break;
			case PlacementMode.Surface:
				_planePlacementModeContainer.Visible = false;
				_surfacePlacementModeContainer.Visible = true;
				_terrain3DSelectorUi.Visible = false;
				break;
			case PlacementMode.Terrain3D:
				_planePlacementModeContainer.Visible = false;
				_surfacePlacementModeContainer.Visible = true;
				_terrain3DSelectorUi.Visible = true;
				break;
			case PlacementMode.Dummy:
				_planePlacementModeContainer.Visible = false;
				_surfacePlacementModeContainer.Visible = false;
				_terrain3DSelectorUi.Visible = false;
				break;
		}
		 EmitSignal(SignalName.PlacementModeChanged, index);
	}


	private void OnAlignmentDirectionSelected(long index)
	{
		AssetPlacerPersistence.StoreSceneData(AlignmentDirectionSaveKey, index);
	}

	private void OnAlignToSurfaceNormalChecked(bool toggle)
	{
		AssetPlacerPersistence.StoreSceneData(AlignWithSurfaceSaveKey, toggle);
	}

	private void OnPlanePositionTextSubmitted(string newtext)
	{
		float val;
		if (AssetPlacerUi.TryParseFloat(newtext, out val) && val != PlanePosition)
		{
			SetPlanePosition(val);
		}
		_planePositionLineEdit.Text = PlanePosition.ToString(CultureInfo.InvariantCulture);
		_planePositionLineEdit.ReleaseFocus();
	}
	
	public void PositionFromSelectedButtonDisabled(bool value)
	{
		_positionFromSelectedButton.Disabled = value;
	}

	public void OnSceneChanged()
	{
		// placement mode dropdown
		var placementMode = AssetPlacerPersistence.LoadSceneData(PlacementModeSaveKey, 0, Variant.Type.Int).AsInt32();
		_placementModeOptionButton.Select(placementMode);
		UpdatePlacementMode(placementMode);
		
		// plane dropdown and position
		var plane = AssetPlacerPersistence.LoadSceneData(PlacementPlaneSaveKey, 1, Variant.Type.Int).AsInt32();
		var planePositions = AssetPlacerPersistence.LoadSceneData(PlanePositionsSaveKey, new float[] { 0, 0, 0 },
			Variant.Type.PackedFloat32Array).AsFloat32Array();
		_planePositions = new Godot.Collections.Dictionary<Vector3.Axis, float>() { { Vector3.Axis.X, planePositions[0]},{Vector3.Axis.Y, planePositions[1]},{Vector3.Axis.Z, planePositions[2]}};
		_planeOptionButton.Select(plane);
		UpdatePlane();
		
		// align with surface checkbox
		var align = AssetPlacerPersistence.LoadSceneData(AlignWithSurfaceSaveKey, false, Variant.Type.Bool).AsBool();
		_alignToSurfaceNormalCheckbox.SetPressedNoSignal(align);
		
		// alignmentDirectionDropdown
		var alignDirection = AssetPlacerPersistence.LoadSceneData(AlignmentDirectionSaveKey, 2, Variant.Type.Int).AsInt32();
		_alignmentDirectionOptionButton.Select(alignDirection);
		
	}
}
#endif