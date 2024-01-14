// DeconstructorNode.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

/**
 * This class is used to remove references before disposing an object by reacting to ExitTree.
 * This is important for any "Gizmos", that are added to the scene without an owner
 * because they need to be cleaned up before the scene is unloaded/closed.
 */
public partial class DeconstructorNode : Node
{
    
    [Signal]
    public  delegate void DeconstructEventHandler();
    public override void _ExitTree() // When changing scene this is disposed and we need to clear references
    {
        EmitSignal(SignalName.Deconstruct);
    }
}
#endif