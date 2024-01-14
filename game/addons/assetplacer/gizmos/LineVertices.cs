// LineVertices.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

[Tool]
public partial class LineVertices : MeshInstance3D
{
	public void CreateMesh()
	{
		var mesh = new ArrayMesh();
		var surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Lines);
		surfaceTool.AddVertex(new Vector3(0, -0.5f, 0));
		surfaceTool.AddVertex(new Vector3(0, 0.5f, 0));
		var mat = new StandardMaterial3D();
		mat.AlbedoColor = new Color(0, 1, 1);
		mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		//mat.NoDepthTest = true;
		surfaceTool.SetMaterial(mat);
		
		var arrayMesh = surfaceTool.Commit(mesh);
		Mesh = arrayMesh;
		
	}
}
#endif