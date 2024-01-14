// AssetLibraryData.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

public partial class AssetLibraryData : GodotObject
{
	public Array<Asset3DData> assetData = new ();
	public bool dirty = true;
	public string savePath = null;
	public Asset3DData.PreviewPerspective previewPerspective;

	public IEnumerable<string> GetAssetPaths()
	{
		return assetData.Select(a => a.path);
	}
	
	public bool ContainsAsset(string path)
	{
		return assetData.Any(a => a.path == path);
	}
	
	public void RemoveAsset(string path)
	{
		var rmList = new List<Asset3DData>();
		foreach (var asset3DData in assetData)
		{
			if(asset3DData.path == path) rmList.Add(asset3DData);
		}
		
		foreach (var asset3DData in rmList)
		{
			assetData.Remove(asset3DData);
		}
	}

	public Asset3DData GetAsset(string path)
	{
		return assetData.First(a => a.path == path);
	}
}
#endif
