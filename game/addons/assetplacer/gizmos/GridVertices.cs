// GridVertices.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable

using Godot;
using System;

namespace AssetPlacer;

[Tool]
public partial class GridVertices : MeshInstance3D
{
	[Export] public StandardMaterial3D mat;
	[Export] public Color gridColor = Colors.White;

	public int lineCnt;
	public float lineSpacing;
	
	// Called when the node enters the scene tree for the first time.
	public void CreateMesh()
	{
		var mesh = new ArrayMesh();
		var surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Lines);
		
		surfaceTool.SetMaterial(mat);

		var lineLen = lineCnt * lineSpacing;
		var matNextPass = mat.NextPass;
		if (matNextPass is ShaderMaterial shader)
		{
			shader.SetShaderParameter("line_len", lineLen);
			shader.SetShaderParameter("grid_color", gridColor);
		}

		// parallel to Z axis
		for (int i = 0; i < lineCnt; i++)
		{
			surfaceTool.AddVertex(new Vector3((-lineCnt/2+i)*lineSpacing,0,-lineLen/2));
			surfaceTool.AddVertex(new Vector3((-lineCnt/2+i)*lineSpacing,0,lineLen/2));
		}
        
		// parallel to X axis
		for (int i = 0; i < lineCnt; i++)
		{
			surfaceTool.AddVertex(new Vector3(-lineLen/2, 0, (-lineCnt/2+i)*lineSpacing));
			surfaceTool.AddVertex(new Vector3(lineLen/2, 0, (-lineCnt/2+i)*lineSpacing));
		}

		var arrayMesh = surfaceTool.Commit(mesh);
		Mesh = arrayMesh;
	}

	public void UpdateCam(bool isPerspective)
	{
		var matNextPass = mat.NextPass;
		if (matNextPass is ShaderMaterial shader)
		{
			shader.SetShaderParameter("is_perspective", isPerspective);
		}
	}
}
#endif
