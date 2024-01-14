// Vector3Extension.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

using Godot;

namespace AssetPlacer;

public static class Vector3Extension
{
    public static float GetAxis(this Vector3 v, Vector3.Axis axis)
    {
        return v[(int) axis];
    }
    public static void SetAxis(this ref Vector3 v, Vector3.Axis axis, float val)
    {
        v[(int) axis] = val;
    }

    public static Quaternion GetRotationToVector(this Vector3 from, Vector3 to)
    {
        from = from.Normalized();
        to = to.Normalized();
        
        if (from.Dot(to) > 0.99999f) // "from" has the same direction as "to"
        {
            return Quaternion.Identity;
        }
        if (from.Dot(to) < -0.99999f) // "from" is opposite direction to "to"
        {
            var normalAxis = Vector3.Up.Cross(from);
            if (normalAxis.LengthSquared() < 0.00001f) // "from" is parallel to "up"
            {
                normalAxis = Vector3.Right.Cross(from);
            }
            return new Quaternion(normalAxis.Normalized(), Mathf.Pi); // rotate 180 degrees by any axis normal to "from"
        }
        
        // This calculation is due to https://stackoverflow.com/questions/1171849/
        
        Vector3 axis = from.Cross(to);
        float angle = Mathf.Sqrt(from.LengthSquared() * to.LengthSquared()) + from.Dot(to);
        
        // have to construct quaternion manually because Quaternion(axis, angle) doesn't allow for
        // axis to be zero and angle to be whatever the above equation returns
        Quaternion q;
        q.X=axis.X;
        q.Y=axis.Y;
        q.Z=axis.Z;
        q.W = angle;
        
        return q.Normalized();
    }
}