#if TOOLS
#nullable disable
using System;
using Godot;
using static AssetPlacer.AssetPlacementController;

namespace AssetPlacer;

[Tool]
public class SurfaceSnapping
{
	
	public static Vector3 SnapToPositionWithOffset(SurfacePlacementPositionInfo info, float snapStep, Vector3 offset = new())
	{
		// Get quaternion q that rotates info.normal to Vector3.Up
		var q = info.surfaceNormal.GetRotationToVector(Vector3.Up);
		
		// Rotate the placement position by q
		var p_rot = q * info.pos;
		
		// Get point s_rot that is on same plane as p_rot but with snapped x and z
		var p_rot_offset = (p_rot-offset);
		var p_snap = p_rot_offset.Snapped(Vector3.One * snapStep) + offset;
		var s_rot = new Vector3(p_snap.X, p_rot.Y, p_snap.Z);

		// Rotate the snapped position back to the original surface plane
		var s = q.Inverse() * s_rot;
		return s;
	}
	
	public static Vector3 IntersectPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 rayDirection, Vector3 rayPoint)
	{
		float ndotu = planeNormal.Dot(rayDirection);
		if (Mathf.Abs(ndotu) < 0.000001f)
		{
			throw new Exception("no intersection or line is within plane");
		}
		float w = planeNormal.Dot(planePoint - rayPoint);
		float si = -w / ndotu;
		return rayPoint + si * rayDirection;
	}

	public static Vector2 GetTranslateOffsetFromPosition(Vector3 pos, Vector3 planePos, Vector3 surfaceNormal, float translateSnapStep)
	{
		Vector3 projectedPos;
		var normal = surfaceNormal;
		projectedPos = IntersectPlane(normal, planePos, normal, pos);
		var snapped = SnapToPositionWithOffset(new SurfacePlacementPositionInfo(projectedPos, true, true, normal), translateSnapStep);
		
		// Get quaternion q that rotates info.normal to Vector3.Up
		var q = normal.GetRotationToVector(Vector3.Up);
		
		// Rotate the placement position by q
		var p_rot = q * projectedPos;
		var s_rot = q * snapped;

		var projToSnapped = p_rot - s_rot;
		return new Vector2(projToSnapped.X, projToSnapped.Z);
	}
}

#endif
