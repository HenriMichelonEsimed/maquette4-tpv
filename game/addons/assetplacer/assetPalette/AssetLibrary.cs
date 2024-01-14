// AssetLibrary.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Linq;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

[Tool]
public partial class AssetLibrary : Resource
{
	[Export] public Array<AssetPersistentData> assetData = new Array<AssetPersistentData>();
	[Export] public Asset3DData.PreviewPerspective previewPerspective = Asset3DData.PreviewPerspective.Default;

	public static AssetLibrary BuildAssetLibary(AssetLibraryData libraryData)
	{
		var data = libraryData.assetData.Select(d => new AssetPersistentData(d.path, d.previewPerspective, d.isMesh));
		var library = new AssetLibrary();
		library.assetData = new Array<AssetPersistentData>(data);
		library.previewPerspective = libraryData.previewPerspective;
		return library;
	}
}
#endif
