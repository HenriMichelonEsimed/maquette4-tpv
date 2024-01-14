// VectorUtils.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

using Godot;

namespace AssetPlacer;

public static class VectorUtils
{
    public const float PositionPrecision = 0.0003f;
    
    public static bool AreApproximatelyEqualPositions(Vector3 v1, Vector3 v2)
    {
        var diff = v1 - v2;
        return Mathf.Abs(diff.X) < PositionPrecision && Mathf.Abs(diff.Y) < PositionPrecision &&
               Mathf.Abs(diff.Z) < PositionPrecision;
    }

    public static bool AreTransformsEqualApprox(Transform3D t1, Transform3D t2)
    {
        return t1.Basis.IsEqualApprox(t2.Basis) && AreApproximatelyEqualPositions(t1.Origin, t2.Origin);
    }
}