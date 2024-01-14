// PlaneSnapping.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using Godot;

namespace AssetPlacer;

[Tool]
public partial class PlaneSnapping : Node
{
    [Signal]
    public delegate void OffsetLabelsChangedEventHandler(string label1, string label2); 
    
    private Func<Vector2, Vector3> _worldOffsetFromPlaneOffset = (v2) => { return Vector3.Zero; };
    public Func<Vector3, Vector2> planeOffsetFromWorldOffset = (vec3) => { return Vector2.Zero; };
    public Vector3 GetWorldOffset(Vector2 planeOffset)
    {
        return _worldOffsetFromPlaneOffset(planeOffset);
    }
    
    public void SetGridPlane(Vector3.Axis planeNormal)
    {
        switch (planeNormal)
        {
            case Vector3.Axis.X:
                _worldOffsetFromPlaneOffset = (planeOffset) => new Vector3(0, planeOffset.X, planeOffset.Y);
                planeOffsetFromWorldOffset = (vec3) => new Vector2(vec3.Y, vec3.Z);
                break;
            case Vector3.Axis.Y:
                _worldOffsetFromPlaneOffset = (planeOffset) => new Vector3(planeOffset.X, 0, planeOffset.Y);
                planeOffsetFromWorldOffset = (vec3) => new Vector2(vec3.X, vec3.Z);
                break;
            case Vector3.Axis.Z:
                _worldOffsetFromPlaneOffset = (planeOffset) => new Vector3(planeOffset.X, planeOffset.Y, 0);
                planeOffsetFromWorldOffset = (vec3) => new Vector2(vec3.X, vec3.Y);
                break;
        }

        var label1 = planeNormal != Vector3.Axis.X ? "x:" : "y:";
        var label2 = planeNormal != Vector3.Axis.Z ? "z:" : "y:";
        EmitSignal(SignalName.OffsetLabelsChanged, label1, label2);
    }
    
    public static Vector3 SnapToPosition(Vector3 input, float snap, Vector3 translateSnapOffset, Vector3.Axis planeNormal)
    {
        Vector3 offsetInput = input - translateSnapOffset;
        var snapped =  offsetInput.Snapped(Vector3.One * snap) + translateSnapOffset;
        snapped.SetAxis(planeNormal, input.GetAxis(planeNormal));
        return snapped;
    }
}

#endif