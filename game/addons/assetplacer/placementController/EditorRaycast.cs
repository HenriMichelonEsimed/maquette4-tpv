// EditorRaycast.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

public partial class EditorRaycast
{
    private const float RaycastLengthPerspective = 500f;
    private const float RaycastLengthOrthogonal = 100000f; // needs to be this high

    private Node3D _worldNode;
    private Vector3 _from;
    private Vector3 _dir;
    private float _length;
     
    public EditorRaycast(Node3D worldNode, Vector3 from, Vector3 dir, Camera3D.ProjectionType camProjection)
    {
        _worldNode = worldNode;
        _from = from;
        _dir = dir;
        _length = camProjection == Camera3D.ProjectionType.Orthogonal ? RaycastLengthOrthogonal : RaycastLengthPerspective;
    }

    public Dictionary PerformRaycast(List<Node3D> placingNodes)
    {
        var spaceState = _worldNode.GetWorld3D().DirectSpaceState;
        
        var exclusion = new EditorRaycast3DExclusion(placingNodes);
        exclusion.Prepare();
        var rayParams = PhysicsRayQueryParameters3D.Create(_from, _from + _dir * _length, Settings.GetSetting(Settings.DefaultCategory, Settings.SurfaceCollisionMask).AsUInt32(), exclusion.excludedRids);
        var result = spaceState.IntersectRay(rayParams);
        exclusion.Restore();
        return result;
    }

    private class EditorRaycast3DExclusion
    {
        public Array<Rid> excludedRids;
        public List<CsgShape3D> excludedCSGs;
        public System.Collections.Generic.Dictionary<GridMap, uint> gridMapExclusions;

        public EditorRaycast3DExclusion(List<Node3D> excludedNodes)
        {
            List<Rid> ridList = new();
            excludedCSGs = new();
            gridMapExclusions = new();
            // exclude the hologram and assets that have just been painted from the raycast to avoid undesired results
            excludedNodes.ForEach(n=>AddExclusionsRecursive(n, ridList));
            excludedRids = new Array<Rid>(ridList);
        }
        
        private void AddExclusionsRecursive(Node current, List<Rid> ridList)
        {
            if (current is CollisionObject3D collisionObject3D)
            {
                ridList.Add(collisionObject3D.GetRid());
            } else if (current is SoftBody3D softBody)
            {
                ridList.Add(softBody.GetPhysicsRid());
            } else if (current is CsgShape3D csg && csg.UseCollision)
            {
                excludedCSGs.Add(csg);
            } else if (current is GridMap gridMap)
            {
                gridMapExclusions.Add(gridMap, gridMap.CollisionLayer);
            }

            foreach (var child in current.GetChildren())
            {
                AddExclusionsRecursive(child, ridList);
            }
        }
        
        public void Prepare()
        {
            // disable csg collisions
            excludedCSGs.ForEach(c=>c.UseCollision = false);
            foreach (var gridMap in gridMapExclusions.Keys)
            {
                gridMap.CollisionLayer = 0;
            }
        }

        public void Restore()
        {
            // re-enable csg collisions
            excludedCSGs.ForEach(c=>c.UseCollision = true);
            foreach (var gridMapExclusion in gridMapExclusions)
            {
                gridMapExclusion.Key.CollisionLayer = gridMapExclusion.Value;
            }
        }
    }
}
#endif
