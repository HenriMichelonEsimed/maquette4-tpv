// Transform3DExtension.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.


using System;
using Godot;

namespace AssetPlacer;

public static class Transform3DExtension
{
    /**
     * The forward Vector, i.e. the direction the object is facing = the local negative Z-axis of the object
     * Equal to -transform.Basis[2]
     */
    public static Vector3 Forward(this Transform3D transform)
    {
        return -transform.Basis.Z;
    }
    
    
    /**
     * The forward Vector, i.e. the direction the object is facing = the local positive Z-axis of the object
     * Equal to transform.Basis[2]
     */
    public static Vector3 Back(this Transform3D transform)
    {
        return transform.Basis.Z;
    }
    
    /**
     * The forward Vector, i.e. the direction the object is facing = the local negative X-axis of the object
     * Equal to -transform.Basis[0]
     */
    public static Vector3 Left(this Transform3D transform)
    {
        return -transform.Basis.X;
    }
    
    
    /**
     * The forward Vector, i.e. the direction the object is facing = the local positive X-axis of the object
     * Equal to transform.Basis[0]
     */
    public static Vector3 Right(this Transform3D transform)
    {
        return transform.Basis.X;
    }
    
    
    /**
     * The forward Vector, i.e. the direction the object is facing = the local positive Y-axis of the object
     * Equal to transform.Basis[1]
     */
    public static Vector3 Up(this Transform3D transform)
    {
        return transform.Basis.Y;
    }
    
    /**
     * The forward Vector, i.e. the direction the object is facing = the local negative Y-axis of the object
     * Equal to -transform.Basis[1]
     */
    public static Vector3 Down(this Transform3D transform)
    {
        return -transform.Basis.Y;
    }

    /**
     * Rotates the transform, such that it's local up Vector equals newUp.
     * Mostly useful for aligning objects with terrain.
     * For aligning to any other direction vector use Align(Vector3 direction, Vector3 newVec)
     */
    public static Transform3D AlignUp(this Transform3D transform, Vector3 newUp)
    {
        return Align(transform, transform.Basis.Y, newUp);
    }

    /**
     * Rotates the transform, such that it's local down Vector equals newDown.
     */
    public static Transform3D AlignDown(this Transform3D transform, Vector3 newDown)
    {
        return Align(transform, -transform.Basis.Y, newDown);
    }
    
    /**
     * Rotates the transform, such that it's local left Vector equals newLeft.
     */
    public static Transform3D AlignLeft(this Transform3D transform, Vector3 newLeft)
    {
        return Align(transform, transform.Basis.X, newLeft);
    }
    
    /**
     * Rotates the transform, such that it's local left Vector equals newLeft.
     */
    public static Transform3D AlignRight(this Transform3D transform, Vector3 newRight)
    {
        return Align(transform, transform.Basis.X, newRight);
    }
    
    /**
     * Rotates the transform, such that a local direction vector "currentDir" is rotated towards "targetDir".
     */
    public static Transform3D Align(this Transform3D transform, Vector3 currentDir, Vector3 targetDir)
    {
        var vec = currentDir.Normalized();
        targetDir = targetDir.Normalized();
        if (targetDir.DistanceSquaredTo(vec) < 0.001f) return transform;

        var cosA = vec.Dot(targetDir);
        var alpha = Mathf.Acos(Mathf.Clamp(cosA, -1.0f, 1.0f));
        if (Math.Abs(alpha % Mathf.Pi) < 0.001f) // Vectors are parallel
        {
            return transform.Rotated(transform.Basis.X.Normalized(), alpha);
        }

        var axis = vec.Cross(targetDir);
        axis = axis.Normalized();

        return transform.Rotated(axis, alpha);
    }
}