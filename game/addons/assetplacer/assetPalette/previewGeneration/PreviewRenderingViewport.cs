// PreviewRenderingViewport.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using Godot;
using static AssetPlacer.AssetPreviewGenerator;

namespace AssetPlacer;

[Tool]
public partial class PreviewRenderingViewport : SubViewport
{
	[Export] private Environment _environment;
	[Export] private NodePath _cameraPath;
	[Export] private NodePath _lightPath;
	[Export] private NodePath _lightPath2;
	private PreviewCamera3D _camera;
	private Node3D _light;
	private Node3D _light2;
	private Quaternion _lightRotation;

	private Node _previewNode;

	private bool _previewReady = false;
	public bool PreviewReady => _previewReady;

	private Perspective _perspective = Perspective.Front;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!Engine.IsEditorHint()) return;
		RenderTargetClearMode = ClearMode.Always;
		_camera = GetNode<PreviewCamera3D>(_cameraPath);
		_light = GetNode<Node3D>(_lightPath);
		_light2 = GetNode<Node3D>(_lightPath2);
		_lightRotation = Quaternion.FromEuler(_light.Rotation);
		_camera.Updated += OnCameraUpdated;
	}

	public void SetPreviewNode(Resource assetResource, Perspective previewPerspective)
	{
		Node previewNode;
		if (assetResource is PackedScene scene)
		{
			previewNode = scene.Instantiate<Node>();
		}
		else if (assetResource is Mesh mesh)
		{
			MeshInstance3D meshInstance = new MeshInstance3D();
			meshInstance.Mesh = mesh;
			previewNode = meshInstance;
		}
		else
		{
			GD.PrintErr($"{assetResource.ResourceName} can't be displayed.");
			previewNode = new Node3D();
		}
		_previewNode?.QueueFree();
		PreviewNodeSetup(previewNode, previewPerspective);
	}

	private void PreviewNodeSetup(Node node, Perspective previewPerspective)
	{
		// we parent to a new Node3D and append this to our viewport. This makes sure that even for scenes with a
		// CSG node as a root, the preview works properly
		_previewNode = new Node3D();
		_previewNode.AddChild(node);
		_previewReady = false;
		_perspective = previewPerspective;
		_previewNode.Ready += RepositionCamera;
		CallDeferred("add_child", _previewNode); // workaround, so _Ready() is not called instantly
	}
	
	private void RepositionCamera()
	{
		var aabb = GetGlobalAabb(_previewNode);
		var transform = CalculateCameraTransform(aabb, _perspective);
		_camera.SetTransform(transform);
		_light.Quaternion = _lightRotation * _camera.Quaternion;
		_light2.Quaternion = _camera.Quaternion * Quaternion.FromEuler(new Vector3(-0.4f, 0,0));
	}

	private void OnCameraUpdated()
	{
		_previewReady = true;
	}

	// Calculates how the camera should be transformed, such that it frames the object in a useful way.
	private Transform3D CalculateCameraTransform(Aabb aabb, Perspective perspective)
	{
		var enclosingSquare = GetAabbEnclosingSquareSize(aabb, perspective);
		
		// The smaller the object, the farther away the camera (relatively) to give a sense of scale.
		const float sizeRlerpMin = 0.1f;
		const float sizeRlerpMax = 100f;
		var rangeVal = Mathf.Clamp(enclosingSquare, sizeRlerpMin, sizeRlerpMax);
		var logRangeVal = LogLerp(sizeRlerpMin, sizeRlerpMax, rangeVal); // BETWEEN 0 AND 1
		var objectSizeFactor = Mathf.Lerp(0.5f, 2.5f, 1f-logRangeVal); // 0.25 - 2 logarithmically lerped
		
		// very big objects are viewed from 0.25 times the aabb size outside of the aabb
		// whereas small objects are viewed from 1.5 times the aabb size outside of the aabb
		var distance = (GetAabbLength(aabb, perspective) * 0.5f) + (enclosingSquare / 2f) * objectSizeFactor;
		
		// Position the camera, such that it is slightly looking down onto the object
		const float sideFactor = 0.2f;
		var position = aabb.GetCenter() + GetViewOffset(aabb, sideFactor, distance, perspective);
		
		var rotationEuler = GetRotationEuler(aabb, distance, sideFactor, perspective);
		var rotationQuat = Quaternion.FromEuler(rotationEuler);
		
		return new Transform3D(new Basis(rotationQuat), position);
	}

	private static Vector3 GetRotationEuler(Aabb aabb, float distance, float sideFactor, Perspective perspective)
	{
		var perspectiveOffset = 0f;
		var sign = 1f;
		switch (perspective)
		{	
			case Perspective.Bottom: return new Vector3(Mathf.Pi / 2f, 0, 0);
			case Perspective.Top: return new Vector3(-Mathf.Pi / 2f, 0, 0);
			case Perspective.Front: perspectiveOffset = 0f; break;
			case Perspective.Back: perspectiveOffset = Mathf.Pi; break;
			case Perspective.Left: 
				perspectiveOffset = -Mathf.Pi/2;
				sign = -1f; 
				break;
			case Perspective.Right: 
				perspectiveOffset = Mathf.Pi/2;
				sign = -1f; 
				break;
		}
		
		var hypot = Mathf.Sqrt(Mathf.Pow(distance, 2) + Mathf.Pow(distance * sideFactor, 2));
		return new Vector3(-Mathf.Atan((aabb.Size.Y / 4f) / hypot), perspectiveOffset + sign * Mathf.Atan(sideFactor), 0);
	}

	private static Vector3 GetViewOffset(Aabb aabb, float sideFactor, float distance, Perspective perspective)
	{
		float y = aabb.Size.Y / 4f;

		var sideOffset = sideFactor*distance;
		switch (perspective)
		{
			case Perspective.Front: return new Vector3(sideOffset, y, distance);
			case Perspective.Back: return new Vector3(-sideOffset, y, -distance);
			case Perspective.Left: return new Vector3(-distance, y, -sideOffset);
			case Perspective.Right: return new Vector3(distance, y, sideOffset);
			case Perspective.Top: return new Vector3(0, distance, 0);
			case Perspective.Bottom: return new Vector3(0, -distance, 0);
			default: return Vector3.Zero;
		}
	}

	private static float GetAabbLength(Aabb aabb, Perspective perspective)
	{
		return perspective switch
		{
			Perspective.Front => aabb.Size.Z,
			Perspective.Back => aabb.Size.Z,
			Perspective.Top => aabb.Size.Y,
			Perspective.Bottom => aabb.Size.Y,
			Perspective.Left => aabb.Size.X,
			Perspective.Right => aabb.Size.X,
			_ => 0f
		};
	}

	private static float GetAabbEnclosingSquareSize(Aabb aabb, Perspective perspective)
	{
		// depending on the side that we are looking at the object from, the enclosing square is at least as large as
		// the larger of the two other dimensions
		// e.g. if we are looking from the Z direction, we are not interested in how long the object is (Z-size)

		return perspective switch
		{
			Perspective.Front => Mathf.Max(aabb.Size.X, aabb.Size.Y),
			Perspective.Back => Mathf.Max(aabb.Size.X, aabb.Size.Y),
			Perspective.Top => Mathf.Max(aabb.Size.X, aabb.Size.Z),
			Perspective.Bottom => Mathf.Max(aabb.Size.X, aabb.Size.Z),
			Perspective.Left => Mathf.Max(aabb.Size.Y, aabb.Size.Z),
			Perspective.Right => Mathf.Max(aabb.Size.Y, aabb.Size.Z),
			_ => 0f
		};
	}

	// Logarithmic interpolation between minVal and maxVal, where val=minVal returns 0 and val=maxVal returns 1.
	private static float LogLerp(float minVal, float maxVal, float value)
	{
		var p = Mathf.Pow(10, -Mathf.Log(minVal));
		return Mathf.Log(value * p) / Mathf.Log(maxVal * p);
	}

	public static Aabb GetGlobalAabb(Node root)
	{
		var endpoints = new List<Vector3>();
		GetAabbEndpointsRecursive(root, endpoints);
		Vector3 start = Vector3.Zero;
		Vector3 end = Vector3.Zero;
		
		if (endpoints.Count > 0)
		{
			start = endpoints[0];
			end = endpoints[0];
		}
		
		foreach (var endpoint in endpoints)
		{
			if (endpoint.X < start.X) start.X = endpoint.X;
			if (endpoint.Y < start.Y) start.Y = endpoint.Y;
			if (endpoint.Z < start.Z) start.Z = endpoint.Z;

			if (endpoint.X > end.X) end.X = endpoint.X;
			if (endpoint.Y > end.Y) end.Y = endpoint.Y;
			if (endpoint.Z > end.Z) end.Z = endpoint.Z;
		}

		return new Aabb(start, end - start);
	}
	
	public static Vector3[] GetGlobalAabbEndpoints(VisualInstance3D visualInstance3D)
	{
		var globalEndpoints = new Vector3[8];
		var localAabb = visualInstance3D.GetAabb();
		for (int i = 0; i < 8; i++)
		{
			var local_endpoint = localAabb.GetEndpoint(i);
			var global_endpoint = visualInstance3D.ToGlobal(local_endpoint);
			globalEndpoints[i] = global_endpoint;
		}

		return globalEndpoints;
	}

	public static void GetAabbEndpointsRecursive(Node currentNode, List<Vector3> endpoints)
	{
		if (currentNode is VisualInstance3D instance3D)
		{
			var globalAabbEndpoints = GetGlobalAabbEndpoints(instance3D);
			endpoints.AddRange(globalAabbEndpoints);
		}

		foreach (var child in currentNode.GetChildren())
		{
			GetAabbEndpointsRecursive(child, endpoints);
		}
	}
}
#endif
