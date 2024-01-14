// PlanePlacementController.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using System.Collections.Generic;
using Godot;

namespace AssetPlacer;

[Tool]
public partial class PlanePlacementController : AssetPlacementController
{
	private const string MovingPlaneTooltip = "Click to Confirm";

	private PackedScene _placementPlaneGizmoScene;
	private LineVertices _planeMovingGizmo;
	private DeconstructorNode _planeMovingGizmoDeconstructor;
	private PlacementPlane _placementPlaneGizmo;
	private DeconstructorNode _placementPlaneGizmoDeconstructor;
	private PlaneSnapping _planeSnappingTool;

	private Vector2 _planeMoveMouseStart;
	private float _planePosBeforeMove;
	private Vector3.Axis _configuringPlane;
	private Viewport _configuringPlaneViewport;

	private enum PlaneControllerState
	{
		PlacingAssets, ConfiguringPlane
	}

	private PlaneControllerState _state = PlaneControllerState.PlacingAssets;

	public PlanePlacementController()
	{
	}

	public PlanePlacementController(PlacementUi placementUi, Snapping snapping_, EditorInterface editorInterface) : base(
		placementUi, snapping_, editorInterface)
	{
		_placementPlaneGizmoScene = ResourceLoader.Load<PackedScene>("res://addons/assetplacer/gizmos/PlacementPlane.tscn");
		_planeSnappingTool = new PlaneSnapping();
		AddChild(_planeSnappingTool);
		_planeSnappingTool.OffsetLabelsChanged += OnOffsetLabelsChanged;
		placementUi.PlanePositionChanged += snapping.UpdateGridMesh;

		placementUi.PlaneChanged += OnPlaneChangedInUI;
		
		snappingGridSize = DefaultSnappingGridSize;
		placementUi._positionFromSelectedButton.Pressed += () => PlanePositionFromSelected(GetSelectedNode());
	}

	public void OnOffsetLabelsChanged(string label1, string label2)
	{
		if(Active) snapping.SetOffsetLabelTexts(label1, label2);
	}

	public void OnPlaneChangedInUI()
	{
		snapping.UpdateGridMesh();
		_planeSnappingTool.SetGridPlane(placementUi.GetPlaneNormal());
			
		if (_state == PlaneControllerState.ConfiguringPlane)
		{
			placementUi.SetPlanePosition(_configuringPlane, _planePosBeforeMove);
			_planeTransformed = false;
			_planePosBeforeMove = placementUi.PlanePosition;
			_configuringPlane = placementUi.GetPlaneNormal();
		}
	}
	
	protected override void OnActiveChanged()
	{ 
		// On recompiling, when all values get re-assigned, this method triggers before _snapping is set. 
		// We prevent a null pointer, such that the rest of the items can get assigned
		if (_planeSnappingTool == null) return;
		
		if(Active) _planeSnappingTool.SetGridPlane(placementUi.GetPlaneNormal());
	}
	public override void OnSelectionChanged()
	{
		var selectedNodes = editorInterface.GetSelection().GetSelectedNodes();
		snapping.SetOffsetFromSelectedButtonDisabled(selectedNodes.Count != 1);
		placementUi?.PositionFromSelectedButtonDisabled(selectedNodes.Count != 1);
	}
	
	public override void OffsetFromSelected(Node3D node)
	{
		if (node == null) return;
		var offset = node.GlobalPosition % (Vector3.One * snapping.TranslateSnapStep);
		snapping.SetTranslateOffset(_planeSnappingTool.planeOffsetFromWorldOffset(offset));
	}

	public override void RotateNode(Node3D node3D, Vector3 startRotation, float rotation)
	{
		node3D.Rotation = startRotation;
		node3D.GlobalRotate(placementUi.PlaneNormalVec, rotation);
	}

	private void PlanePositionFromSelected(Node3D node)
	{
		if (node == null) return;
		placementUi.SetPlanePosition(node.GlobalPosition.GetAxis(placementUi.GetPlaneNormal()));
	}

	protected override void OnTranslateOffsetChanged()
	{
		var offset = _planeSnappingTool.GetWorldOffset(snapping.TranslateSnapOffset) % (Vector3.One * snapping.TranslateSnapStep);
		snapping.SetTranslateOffset(_planeSnappingTool.planeOffsetFromWorldOffset(offset));
	}

	private const float VpWarpZone = 0.01f;
	public void ConfigurePlane(Vector2 currentMousePos, Viewport viewport)
	{
		_planePosBeforeMove = placementUi.PlanePosition;

		var vpSize = viewport.GetVisibleRect().Size;
		var vpRect = new Rect2(vpSize * VpWarpZone, vpSize*(1-2*VpWarpZone));
		if (!vpRect.HasPoint(viewport.GetMousePosition()))
		{
			viewport.WarpMouse(0.5f * vpSize);
			_planeMoveMouseStart = viewport.GetMousePosition() / vpSize;
		}
		else
		{
			_planeMoveMouseStart = currentMousePos;
		}
		
		_configuringPlane = placementUi.GetPlaneNormal();
		_state = PlaneControllerState.ConfiguringPlane;
		_configuringPlaneViewport = viewport;
	}
	
	public void OnPlaneChanged(Camera3D viewportCamera)
	{
		if (!Active || viewportCamera == null) return;
		if (_state != PlaneControllerState.ConfiguringPlane)
		{
			SetPlacementPlaneTransform(viewportCamera);
		}
		_placementPlaneGizmo?.ShowTemporarily();
	}
	
	public void OnSceneRootChanged()
	{
		if (_state == PlaneControllerState.ConfiguringPlane)
		{
			HidePlaneMovingGizmo();
			placementUi.SetPlanePosition(_planePosBeforeMove);
		}
		_state = PlaneControllerState.PlacingAssets;
		_placementPlaneGizmo?.QueueFree();
		_placementPlaneGizmo = null;
		_planeMovingGizmo?.QueueFree();
		_planeMovingGizmo = null;
	}
	
	public override string Process(Node sceneRoot, Camera3D viewportCamera, bool rmbPressed)
	{
		UpdateGrid(viewportCamera);

		if (_placementPlaneGizmo == null || !_placementPlaneGizmo.IsInsideTree())
		{
			///////// GIZMOS
			if(_placementPlaneGizmoDeconstructor != null) _placementPlaneGizmoDeconstructor.Deconstruct -= PlaneGizmoDeconstructor;
			_placementPlaneGizmo?.QueueFree();
			_placementPlaneGizmo = _placementPlaneGizmoScene.Instantiate<PlacementPlane>();
			_placementPlaneGizmoDeconstructor = new DeconstructorNode();
			_placementPlaneGizmo.AddChild(_placementPlaneGizmoDeconstructor);
			_placementPlaneGizmoDeconstructor.Deconstruct += PlaneGizmoDeconstructor;
			////////////
			
			sceneRoot.AddChild(_placementPlaneGizmo);
		}

		var globalPosition = _placementPlaneGizmo.GlobalPosition;
		globalPosition.SetAxis(placementUi.GetPlaneNormal(), placementUi.PlanePosition);
		_placementPlaneGizmo.GlobalPosition = globalPosition;

		if (_state == PlaneControllerState.ConfiguringPlane)
		{
			return ProcessConfiguringPlane(sceneRoot, _configuringPlaneViewport, rmbPressed);
		}
		_planeTransformed = false;
		return null;
	}

	private void PlaneGizmoDeconstructor()
	{
		_placementPlaneGizmo?.QueueFree();
		_placementPlaneGizmo = null;
		_placementPlaneGizmoDeconstructor = null;
	}
	private void PlaneMovingGizmoDeconstructor()
	{
		_planeMovingGizmo?.QueueFree();
		_planeMovingGizmo = null;
		_planeMovingGizmoDeconstructor = null;
	}

	private bool _planeTransformed;
	private string ProcessConfiguringPlane(Node sceneRoot, Viewport viewport, bool rmbPressed)
	{
		if (_configuringPlaneViewport == null || !_configuringPlaneViewport.IsInsideTree()
			|| !_configuringPlaneViewport.GetParent().GetParent<Control>().Visible)
		{
			CancelConfiguringPlane();
			return null;
		}
		
		if(!ContextlessPlugin.IsEditorViewportFocused(_configuringPlaneViewport)) 
			ContextlessPlugin.FocusEditorViewport(_configuringPlaneViewport);	     

		var viewportCamera = viewport.GetCamera3D();
		// remove invalid gizmos
		if (sceneRoot == null || _planeMovingGizmo?.IsInsideTree() == false)
		{
			_planeMovingGizmo.QueueFree();
			_planeMovingGizmo = null;
		}
		
		if (sceneRoot == null)
		{
			_state = PlaneControllerState.PlacingAssets;
			return null;
		}
		if (!_planeTransformed)
		{
			SetPlacementPlaneTransform(viewportCamera);
			_planeTransformed = true;
		}
		_placementPlaneGizmo.SetVisible(true);
		
		// Initialize Gizmos
		if (_planeMovingGizmo == null) 
		{
			var planeMovingGizmo = new LineVertices();
			planeMovingGizmo.CreateMesh();
			_planeMovingGizmo = planeMovingGizmo;
			sceneRoot.AddChild(_planeMovingGizmo);
			if(_planeMovingGizmoDeconstructor != null) _planeMovingGizmoDeconstructor.Deconstruct -= PlaneMovingGizmoDeconstructor;
			_planeMovingGizmoDeconstructor = new DeconstructorNode();
			_planeMovingGizmo.AddChild(_planeMovingGizmoDeconstructor);
			_planeMovingGizmoDeconstructor.Deconstruct += PlaneMovingGizmoDeconstructor;
		}

		// Confine the mouse cursor to the viewport and warp it to its other side when it leaves the viewport.
		var vpSize = viewport.GetVisibleRect().Size;
		var unitRect = new Rect2(Vector2.Zero, Vector2.One);
		var warpRect = new Rect2(vpSize * VpWarpZone, vpSize*(1-2*VpWarpZone));
		// we calculate the mouse position anew, because the one we could get from the plugin
		// (coming from the built-in _Input method) is not updated, when the mouse is warped
		var vpUnitMousePos = GetVpUnitMouseMos(viewport);
		var isMouseOverViewport = unitRect.HasPoint(vpUnitMousePos);
		if (!isMouseOverViewport)
		{
			var clampPos = vpUnitMousePos.Clamp(Vector2.Zero, Vector2.One);
			var warpPos = warpRect.Position + clampPos * warpRect.Size;
			var warpDistance = 1-VpWarpZone;
			if (clampPos.X >= 1)
			{
				warpPos.X = warpRect.Position.X;
				_planeMoveMouseStart.X -= warpDistance;
			} else if (clampPos.X <= 0)
			{
				warpPos.X = warpRect.Position.X + warpRect.Size.X;
				_planeMoveMouseStart.X += warpDistance;
			}
			if (clampPos.Y >= 1)
			{
				warpPos.Y = warpRect.Position.Y;
				_planeMoveMouseStart.Y -= warpDistance;
			} else if (clampPos.Y <= 0)
			{
				warpPos.Y = warpRect.Position.Y + warpRect.Size.Y;
				_planeMoveMouseStart.Y += warpDistance;
			}
			viewport.WarpMouse(warpPos);
			vpUnitMousePos = GetVpUnitMouseMos(viewport); // update after warp
		}

		// Update GlobalPosition of _planeMovingGizmo such that it is always twice the zNear in front of the _viewportCamera
		var cameraPos = viewportCamera.GlobalPosition;
		var cameraForward = viewportCamera.GlobalTransform.Forward();
		var camRot = viewportCamera.Transform.Basis.GetEuler();
		float camToPlaneAngle;

		switch (placementUi.GetPlaneNormal())
		{
			case Vector3.Axis.Y:
				_planeMovingGizmo.Rotation = new Vector3(0, 0, 0);
				camToPlaneAngle = camRot.X;
				break;
			case Vector3.Axis.X:
				_planeMovingGizmo.Rotation = new Vector3(Mathf.DegToRad(90), 0, Mathf.DegToRad(90));
				camToPlaneAngle = camRot.Y;
				break;
			case Vector3.Axis.Z:
				_planeMovingGizmo.Rotation = new Vector3(Mathf.DegToRad(90), 0, 0);
				camToPlaneAngle = camRot.Y;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(placementUi.GetPlaneNormal));
		}

		var scaleDueToAngle = Mathf.Pow(Mathf.Clamp( Mathf.Abs(camToPlaneAngle) / (Mathf.Pi/2f), 0f, 1f),2) * 10f;
		_planeMovingGizmo.Scale = Vector3.One * 4f * scaleDueToAngle;
		_planeMovingGizmo.GlobalPosition = cameraPos + cameraForward * (1f+viewportCamera.Near * 2f);

		var mouseDiffVec = (vpUnitMousePos - _planeMoveMouseStart);
		var camToPlaneDistance = viewportCamera.GlobalPosition.GetAxis(placementUi.GetPlaneNormal()) - _planePosBeforeMove;
		var mouseDiff = placementUi.GetPlaneNormal() switch
		{
			Vector3.Axis.Y => -mouseDiffVec.Y,
			Vector3.Axis.X => mouseDiffVec.X * Mathf.Cos(viewportCamera.Rotation.Y) + mouseDiffVec.Y * Mathf.Sin(viewportCamera.Rotation.Y) * Mathf.Sign(-viewportCamera.Rotation.X),
			Vector3.Axis.Z => mouseDiffVec.X * -Mathf.Sin(viewportCamera.Rotation.Y) + mouseDiffVec.Y * (Mathf.Cos(viewportCamera.Rotation.Y)) * Mathf.Sign(-viewportCamera.Rotation.X),
			_ => throw new ArgumentOutOfRangeException()
		};
		if (rmbPressed) return null;
		var snap = AssetPlacerPlugin.ctrlPressed != snapping.TranslateSnappingActive();
		const float planeMovingConst = 3;
		float distanceFactor;
		if (viewportCamera.Projection == Camera3D.ProjectionType.Perspective)
		{
			distanceFactor = planeMovingConst*Mathf.Abs(camToPlaneDistance);
			distanceFactor = Mathf.Max(distanceFactor, 0.1f); // mimimum of 0.1
		}
		else
		{
			distanceFactor = viewportCamera.Size;
		}

		var step = 0f;
		if (snap)
		{
			step = AssetPlacerPlugin.shiftPressed ? snapping.TranslateShiftSnapStep: snapping.TranslateSnapStep;
			distanceFactor = Mathf.Max(distanceFactor, step*planeMovingConst);
		}
		
		var worldPos = _planePosBeforeMove + mouseDiff * distanceFactor;
		var planePos = Mathf.Snapped(worldPos, step);
		placementUi.SetPlanePosition(planePos);
		return FormattableString.Invariant($"{planePos:N3}\n")+MovingPlaneTooltip;
	}

	private static Vector2 GetVpUnitMouseMos(Viewport viewport)
	{
		return viewport.GetMousePosition() / viewport.GetVisibleRect().Size;
	}

	public override PlacementInfo GetPlacementPosition(Camera3D viewportCam, Vector2 viewportMousePosition, List<Node3D> _)
	{
		if(viewportCam == null) return new PlacementInfo(PlacementPositionInfo.invalidInfo);
		var from = viewportCam.ProjectRayOrigin(viewportMousePosition * viewportCam.GetViewport().GetVisibleRect().Size);
		var dir = viewportCam.ProjectRayNormal(viewportMousePosition * viewportCam.GetViewport().GetVisibleRect().Size);

		var planeNormal = placementUi.GetPlaneNormal();

		if (dir.GetAxis(planeNormal)!=0f) // check that dir is not parallel to plane (mouse ray has no intersection with plane)
		{
			var t = (placementUi.PlanePosition - from.GetAxis(planeNormal)) / dir.GetAxis(planeNormal);
			var posValid = t >= 0; // if t < 0, the intersection is behind the camera
			var pos = from + t * dir;
			if (!IsPlacementOnPlaneHorizon(dir, planeNormal, viewportCam.Position, placementUi.PlanePosition, pos))
			{
				return new PlacementInfo(new PlacementPositionInfo(pos, posValid));
			}
		}

		return new PlacementInfo(PlacementPositionInfo.invalidInfo);
	}
	
	private bool IsPlacementOnPlaneHorizon(Vector3 camLookDirection, Vector3.Axis planeNormal, Vector3 camPosition,
		float planeHeight, Vector3 placementPosition)
	{
		Vector3 flatCamPosition = camPosition;
		flatCamPosition.SetAxis(planeNormal, planeHeight);
		
		var camAngle = GetCamPlaneAngle(camLookDirection, planeNormal, planeHeight);
		var distSq = (placementPosition - flatCamPosition).LengthSquared();
		var camPlaneDistance = Mathf.Abs(camPosition.GetAxis(planeNormal) - planeHeight);
		
		const float horizonAngle = 5f;
		float horizonDistance = 10f + (camPlaneDistance*10f);
		return camAngle < Mathf.DegToRad(horizonAngle) && distSq > horizonDistance;
	}

	private static float GetCamPlaneAngle(Vector3 camLookDirection, Vector3.Axis planeNormal, float planeHeight)
	{
		Vector3 flatCamLookDir = camLookDirection;
		flatCamLookDir.SetAxis(planeNormal, planeHeight);
		return camLookDirection.AngleTo(flatCamLookDir);
	}

	private void SetPlacementPlaneTransform(Camera3D viewportCamera)
	{
		if(_placementPlaneGizmo == null) return;
		_placementPlaneGizmo.Rotation = placementUi.GetPlaneNormal() switch
		{
			Vector3.Axis.X => new Vector3(0, Mathf.DegToRad(90), 0),
			Vector3.Axis.Y => new Vector3(Mathf.DegToRad(90), 0, 0),
			Vector3.Axis.Z => new Vector3(0, 0, 0),
			_ => new Vector3(0, 0, 0)
		};
		
		_placementPlaneGizmo.GlobalPosition = GetGridPos(viewportCamera);
		
		float camScale;
		if (viewportCamera.Projection == Camera3D.ProjectionType.Perspective)
		{
			var distanceFac = Mathf.Abs(viewportCamera.GlobalPosition.GetAxis(placementUi.GetPlaneNormal()) - placementUi.PlanePosition);
			var camPlaneAngle = GetCamPlaneAngle(viewportCamera.Transform.Forward(), placementUi.GetPlaneNormal(), placementUi.PlanePosition);
			var angleFac = Mathf.Pi / 2f / Mathf.Max(camPlaneAngle, 0.1f) * 0.5f;
			var fovFac = viewportCamera.Fov / 50f;
			camScale = Mathf.Max(distanceFac, 1f) * fovFac * 3f * Mathf.Max(angleFac, 0.1f);
		}
		else
		{
			camScale = viewportCamera.Size;
		}
		_placementPlaneGizmo.Scale =  camScale * Vector3.One;

	}


	public override bool ProcessInput(AssetPlacerPlugin.InputEventType inputEventType, Vector2 viewportMousePos)
	{
		if (_state == PlaneControllerState.ConfiguringPlane)
			switch (inputEventType)
			{
				case AssetPlacerPlugin.InputEventType.Placement 
					or AssetPlacerPlugin.InputEventType.Confirm:
					HidePlaneMovingGizmo();
					_placementPlaneGizmo?.ShowTemporarily();
					_state = PlaneControllerState.PlacingAssets;
					return true;
				case AssetPlacerPlugin.InputEventType.Cancel:
					CancelConfiguringPlane();
					return true;
				default:
					return false;
			}

		return false;
	}

	private void CancelConfiguringPlane()
	{
		HidePlaneMovingGizmo();
		placementUi.SetPlanePosition(_planePosBeforeMove);
		_state = PlaneControllerState.PlacingAssets;
		_configuringPlaneViewport = null;
	}

	public override Vector3 SnapToPosition(PlacementPositionInfo info, float snapStep)
	{
		return PlaneSnapping.SnapToPosition(info.pos, snapStep, _planeSnappingTool.GetWorldOffset(snapping.TranslateSnapOffset), placementUi.GetPlaneNormal());
	}

	private void HidePlaneMovingGizmo()
	{
		_planeMovingGizmo?.QueueFree();
		_planeMovingGizmo = null;
	}

	public void Cleanup()
	{
		_planeMovingGizmo?.QueueFree();
		_placementPlaneGizmo?.QueueFree();
	}

	private void UpdateGrid(Camera3D viewportCamera)
	{
		// On recompiling, when all values get re-assigned, this method triggers before _snapping is set. 
		// We prevent a null pointer, such that the rest of the items can get assigned
		if (placementUi == null)
		{
			return;
		}
		// rotate the grid according to the plane normal
		var gridRot = placementUi.GetPlaneNormal() switch
		{
			Vector3.Axis.X => new Vector3(0, 0, Mathf.DegToRad(90)),
			Vector3.Axis.Y => new Vector3(0, 0, 0),
			Vector3.Axis.Z => new Vector3(Mathf.DegToRad(90), 0, 0),
			_ => new Vector3(0, 0, 0)
		};
		snapping.UpdateGridTransform(GetGridPos(viewportCamera),
			viewportCamera.Projection == Camera3D.ProjectionType.Perspective, gridRot);
	}
	
	private Vector3 GetGridPos(Camera3D viewportCamera)
	{
		var placement = GetPlacementPosition(viewportCamera, new Vector2(0.5f, 0.5f), null);
		var backupPos = viewportCamera.GlobalPosition;
		backupPos.SetAxis(placementUi.GetPlaneNormal(), placementUi.PlanePosition);
		var pos = placement.positionInfo.posValid ? placement.positionInfo.pos : backupPos;
		pos = PlaneSnapping.SnapToPosition(pos, snapping.TranslateSnapStep, _planeSnappingTool.GetWorldOffset(snapping.TranslateSnapOffset), placementUi.GetPlaneNormal());
		return pos;
	}

}
#endif
