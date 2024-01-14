// AssetPalette.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot.Collections;

namespace AssetPlacer;

using Array = System.Array;


[Tool]
public partial class AssetPalette : Node
{
	private const string OpenedLibrariesSaveKey = "opened_libraries";
	private const string LastSelectedLibrarySaveKey = "selected_library";
	
	private EditorInterface _editorInterface;
	private AssetPlacerUi _assetPlacerUi;
	public Node3D Hologram { get; private set; }
	public Node3D LastPlacedAsset { get; private set; }
	private Asset3DData _selectedAsset;
	private string _lastSelectedAssetPath;
	public string SelectedAssetName { get; private set; }
	private Godot.Collections.Dictionary<string, AssetLibraryData> _libraryDataDict = new();
	private string _currentLibrary = null;
	private const string NewLibraryName = "Unnamed Library";
	private AssetPreviewGenerator _previewGenerator;
	private AssetPreviewGenerator PreviewGenerator
	{
		get
		{
			if (_previewGenerator == null)
			{
				_previewRenderingViewports = new();
				_previewGenerator = new AssetPreviewGenerator();
				const int previewVpCount = 3;
				for (int i = 0; i < previewVpCount; i++)
				{
					var previewVpScene = ResourceLoader.Load<PackedScene>("res://addons/assetplacer/assetPalette/previewGeneration/PreviewRenderingViewport.tscn");
					var previewRenderingViewport = previewVpScene.Instantiate<PreviewRenderingViewport>();
					_previewGenerator.AddChild(previewRenderingViewport);
					_previewRenderingViewports.Add(previewRenderingViewport);
				}
				_previewGenerator.Init(_previewRenderingViewports);
				AddChild(_previewGenerator);
			}
			return _previewGenerator;
		}
	}

	private Array<PreviewRenderingViewport> _previewRenderingViewports = new();
	private TextureRect _viewportTextureRect;

	public void Init(EditorInterface editorInterface)
	{
		var _ = PreviewGenerator; // Force Initialization

		_editorInterface = editorInterface;
	}

	public void PostInit()
	{
		var openedLibraries = AssetPlacerPersistence.LoadGlobalData(OpenedLibrariesSaveKey,Array.Empty<string>(), Variant.Type.PackedStringArray);
		var lastSelectedLibrary =
			AssetPlacerPersistence.LoadGlobalData(LastSelectedLibrarySaveKey, "", Variant.Type.String);
		InitLibraries(openedLibraries.AsStringArray(), lastSelectedLibrary.AsString());
		OnSelectionChanged();
	}
	
	public void Cleanup()
	{
		foreach (var vp in _previewRenderingViewports)
		{
			vp.QueueFree();
		}
	}

	public void OnSelectionChanged()
	{
		_assetPlacerUi.MatchSelectedButtonDisabled(!CanMatchSelection());
	}

	private bool CanMatchSelection()
	{
		var nodes = _editorInterface.GetSelection().GetSelectedNodes();
		return nodes.Count == 1 && nodes[0].Owner == _editorInterface.GetEditedSceneRoot();
	}

	public void OnMatchSelectedPressed()
	{
		Debug.Assert(CanMatchSelection());
		var selected = _editorInterface.GetSelection().GetSelectedNodes()[0];
		_editorInterface.GetSelection().GetSelectedNodes();
		if (!string.IsNullOrEmpty(selected.SceneFilePath))
		{
			if (_currentLibrary != null && CurrentLibraryData.ContainsAsset(selected.SceneFilePath))
			{
				_assetPlacerUi.SelectAssetButtonFromPath(selected.SceneFilePath);
			}
			else
			{
				OnAddNewAsset(new []{selected.SceneFilePath});
				_assetPlacerUi.SelectAssetButtonFromPath(selected.SceneFilePath);
			}
		}
		else if (selected is MeshInstance3D meshInstance)
		{
			var resourcePath = meshInstance.Mesh.ResourcePath;
			if (resourcePath.StartsWith("res://") &&
			    (new[] { ".res", ".tres", ".obj" }).Any(s => resourcePath.EndsWith(s)))
			{
				if (_currentLibrary != null && CurrentLibraryData.ContainsAsset(resourcePath))
				{
					_assetPlacerUi.SelectAssetButtonFromPath(resourcePath);
				}
				else
				{
					OnAddNewAsset(new []{resourcePath});
					_assetPlacerUi.SelectAssetButtonFromPath(resourcePath);
				}
			}
			else
			{
				GD.PrintErr($"To add {selected.Name} as an asset, either save as a scene, or save its Mesh as a Resource");
			}
		}
		else
		{
			GD.PrintErr($"To add {selected.Name} as an asset, save it as a scene first (needs to be an instanced scene).");
		}
	}

	public const string AssetLibrarySaveFolder = ".assetPlacerLibraries";
	public void SetUi(AssetPlacerUi assetPlacerUi)
	{
		_assetPlacerUi = assetPlacerUi;
		_assetPlacerUi.AssetsAdded += OnAddNewAsset;
		_assetPlacerUi.AssetSelected += OnSelectAsset;
		_assetPlacerUi.AssetsRemoved += OnRemoveAsset;
		_assetPlacerUi.AssetTransformReset += OnResetAssetTransform;
		_assetPlacerUi.AssetsOpened += OnOpenAsset;
		_assetPlacerUi.AssetShownInFileSystem += OnShowAssetInFileSystem;
		_assetPlacerUi.AssetLibrarySelected += path => OnLibraryLoad(path, true);
		_assetPlacerUi.AssetTabSelected += OnAssetLibrarySelect;
		_assetPlacerUi.TabsRearranged += StoreOpenedLibraries;
		_assetPlacerUi.NewTabPressed += () => OnNewAssetLibrary();
		_assetPlacerUi.SaveButtonPressed += OnSaveCurrentAssetLibrary;
		_assetPlacerUi.AssetLibrarySaved += SaveLibraryAt;
		_assetPlacerUi.AssetLibraryRemoved += OnRemoveAssetLibrary;
		_assetPlacerUi.ReloadLibraryPreviews += OnReloadLibraryPreviews;
		_assetPlacerUi.DefaultLibraryPreviews += OnDefaultLibraryPreviews;
		_assetPlacerUi.ReloadAssetPreview += ReloadAssetPreview;
		_assetPlacerUi.AssetPreviewPerspectiveChanged += OnAssetPreviewPerspectiveChanged;
		_assetPlacerUi.MatchSelectedPressed += OnMatchSelectedPressed;

		_assetPlacerUi.AssetLibraryShownInFileManager += OnShowAssetLibraryInFileManager;
		_assetPlacerUi.LibraryPreviewPerspectiveChanged += OnLibraryPreviewPerspectiveChanged;
		_assetPlacerUi.AssetButtonRightClicked += OnAssetButtonRightClicked;
		_assetPlacerUi.TabRightClicked += OnAssetTabRightClicked;
		_assetPlacerUi.SetAssetLibrarySaveDisabled(true);
	}

	private void OnAssetButtonRightClicked(string assetPath, Vector2 pos)
	{
		Debug.Assert(CurrentLibraryData.assetData.Any(a=>a.path == assetPath), $"AssetPath {assetPath} does not exist in current library");
		int prevPerspective = (int) CurrentLibraryData.assetData.First(a => a.path == assetPath).previewPerspective;
		_assetPlacerUi.DisplayAssetRightClickPopup(assetPath, prevPerspective, pos);
	}
	private void OnAssetTabRightClicked(string library, Vector2 pos)
	{
		Debug.Assert(_libraryDataDict.ContainsKey(library), $"Data of library {library} not found");
		int prevPerspective = (int) _libraryDataDict[library].previewPerspective;
		_assetPlacerUi.DisplayTabRightClickPopup(library, prevPerspective, pos);
	}

	private void OnDefaultLibraryPreviews(string library)
	{
		Debug.Assert(_libraryDataDict.ContainsKey(library), $"Data of library {library} not found");
		var lib = _libraryDataDict[library];
		foreach (var asset3DData in lib.assetData)
		{
			asset3DData.previewPerspective = Asset3DData.PreviewPerspective.Default;
			
		}
		OnReloadLibraryPreviews(library);
		lib.dirty = true;
		UpdateSaveDisabled();
	}
	
	private void OnReloadLibraryPreviews(string library)
	{
		Debug.Assert(_libraryDataDict.ContainsKey(library), $"Data of library {library} not found");
		_assetPlacerUi.SelectAssetTab(library);
		GeneratePreviews(_libraryDataDict[library].assetData, true, _libraryDataDict[library].previewPerspective);
	}

	private void ReloadAssetPreview(string path)
	{
		Debug.Assert(CurrentLibraryData.GetAssetPaths().Contains(path), $"Asset {path} is not part of current library");
		var asset = CurrentLibraryData.assetData.Where(a => a.path == path);
		GeneratePreviews(asset, true, CurrentLibraryData.previewPerspective);
	}

	private void OnAssetPreviewPerspectiveChanged(string path, Asset3DData.PreviewPerspective perspective)
	{
		Debug.Assert(CurrentLibraryData.GetAssetPaths().Contains(path), $"Asset {path} is not part of current library");
		var asset = CurrentLibraryData.assetData.Where(a => a.path == path).ToList();
		CurrentLibraryData.dirty = true;
		UpdateSaveDisabled();
		asset.ForEach(a=>a.previewPerspective = perspective); // change perspective
		GeneratePreviews(asset, true, CurrentLibraryData.previewPerspective);
	}
	private void OnLibraryPreviewPerspectiveChanged(string library, Asset3DData.PreviewPerspective perspective)
	{
		Debug.Assert(_libraryDataDict.ContainsKey(library), $"Data of library {library} not found");
		_libraryDataDict[library].previewPerspective = perspective;
		_libraryDataDict[library].dirty = true;
		UpdateSaveDisabled();
		GeneratePreviews(_libraryDataDict[library].assetData, true, perspective);
	}

	private void OnRemoveAssetLibrary(string libraryName)
	{
		if (_libraryDataDict.ContainsKey(libraryName))
		{
			_libraryDataDict.Remove(libraryName);
			if(libraryName == _currentLibrary) _currentLibrary = null;
			
			StoreOpenedLibraries();
			_assetPlacerUi.CallDeferred(nameof(_assetPlacerUi.RemoveAssetLibrary), libraryName); // deferred to avoid out of bounds error
		}
	}

	private void StoreOpenedLibraries()
	{
		// Store libraries in the order they are opened in the tab bar, excluding any that have not been saved yet
		AssetPlacerPersistence.StoreGlobalData(OpenedLibrariesSaveKey, 
			_libraryDataDict.Values.Select(l=>l.savePath).Where(p=>!string.IsNullOrEmpty(p)).OrderBy((p)=>_assetPlacerUi.GetTabIdx(GetLibraryNameFromPath(p))).ToArray());
	}

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return;
		PreviewGenerator.Process();
	}

	private void OnShowAssetLibraryInFileManager(string libraryName)
	{
		if (!string.IsNullOrEmpty(_libraryDataDict[libraryName].savePath))
		{
			OS.ShellOpen($"{ProjectSettings.GlobalizePath(GetFolderPathFromFilePath(_libraryDataDict[libraryName].savePath))}");
		}
		else GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Can't open library in file manager. Library is not saved.");
	}

	public static string GetAssetLibraryDirPath(FileDialog.AccessEnum access)
	{
		return $"{(access == FileDialog.AccessEnum.Userdata ? "user://" : "res://")}{AssetLibrarySaveFolder}";
	}

	private void OnSaveCurrentAssetLibrary()
	{
		if(_currentLibrary != null && CurrentLibraryData.savePath != null) {
			SaveLibraryAt(_currentLibrary, CurrentLibraryData.savePath, false);
		}
		else
		{
			_assetPlacerUi.ShowSaveDialog(_currentLibrary, true);
		}
	}

	private AssetLibraryData CurrentLibraryData => _libraryDataDict[_currentLibrary];
	
	private string OnNewAssetLibrary(string name = NewLibraryName, bool selectLibrary = true)
	{
		var libraryName = GetAvailableLibraryName(name);
		_libraryDataDict.Add(libraryName, new AssetLibraryData());
		
		if(selectLibrary) _assetPlacerUi.AddAndSelectAssetTab(libraryName);
		else _assetPlacerUi.AddAssetTab(libraryName);
		
		return libraryName;
	}

	private void InitLibraries(string[] openedLibraries, string selectLibraryPath)
	{
		if (openedLibraries.Length == 0) return;
		foreach (var libraryPath in openedLibraries.Where(s=>!string.IsNullOrEmpty(s)))
		{
			OnLibraryLoad(libraryPath, false);
		}

		var library = GetLibraryNameFromPath(selectLibraryPath) ?? GetLibraryNameFromPath(openedLibraries[0]);
		_assetPlacerUi.SelectAssetTab(library);
	}

	// Returns the name with which the library can be selected in the UI (tab title).
	// Not to be confused with the library file name.
	private string GetLibraryNameFromPath(string selectLibraryPath)
	{
		return _libraryDataDict.Keys.FirstOrDefault(a => _libraryDataDict[a].savePath == selectLibraryPath);
	}

	private void OnAssetLibrarySelect(string tabTitle, int scrollPosition)
	{
		if (tabTitle == _currentLibrary) return;
		_currentLibrary = tabTitle;
		if (_currentLibrary != null)
		{
			var perspective = _libraryDataDict.ContainsKey(_currentLibrary)
				? _libraryDataDict[_currentLibrary].previewPerspective
				: Asset3DData.PreviewPerspective.Default;
			OnLibraryChange(perspective, scrollPosition);
		}
		UpdateSaveDisabled();
	}

	private void OnLibraryLoad(string path, bool selectLibrary)
	{
		var assetLibraryResource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Ignore);
		if (assetLibraryResource is AssetLibrary assetLibrary) // path must be new
		{
			var existingLibrary = GetLibraryNameFromPath(path);
			if (existingLibrary == null)
			{
				string libraryName = OnNewAssetLibrary(GetFileNameFromFilePath(path), selectLibrary);
				
				// load library settings
				var libraryData = _libraryDataDict[libraryName];
				libraryData.previewPerspective = assetLibrary.previewPerspective;
				var asset3DData = assetLibrary.assetData.Select(AssetPersistentData.GetAsset3DData);
				asset3DData.ToList().ForEach(asset=>libraryData.assetData.Add(asset));
				if (selectLibrary)
				{
					_currentLibrary = libraryName;
					OnLibraryChange(libraryData.previewPerspective);
				}
				libraryData.dirty = false;
				libraryData.savePath = path;
				UpdateSaveDisabled();
				StoreOpenedLibraries();
			}
			else
			{
				GD.Print($"{nameof(AssetPlacerPlugin)}: {path} is already loaded");
				if(selectLibrary) _assetPlacerUi.SelectAssetTab(existingLibrary);
			}
		}
		else
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Resource found at {path} is not a scene library");
		}
		
	}

	private void SaveLibraryAt(string libraryKey, string path, bool changeName)
	{
		if (!_libraryDataDict.ContainsKey(libraryKey))
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Error saving asset library '{libraryKey}': Library not found in loaded libraries.");
			return;
		}

		if (_libraryDataDict.Keys.Any(lib=>lib != libraryKey && _libraryDataDict[lib].savePath == path)) // different library with same file location loaded
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Error saving asset library at {path}: A library saved to {path} is currently open.");
			return;
		}

		// create a SceneLibrary resource and copy the assetPaths into it
		var assetLibraryData = _libraryDataDict[libraryKey];
		var assetLibrary = AssetLibrary.BuildAssetLibary(assetLibraryData);
		var folder = GetFolderPathFromFilePath(path);
		if (!string.IsNullOrEmpty(folder) && !DirAccess.DirExistsAbsolute(folder))
		{
			DirAccess.MakeDirRecursiveAbsolute(folder);
		}
		
		var error = ResourceSaver.Save(assetLibrary, path);
		if (error == Error.Ok)
		{
			GD.PrintRich($"[b]Asset selection saved to: {path}[/b]");
			_libraryDataDict[libraryKey].dirty = false;
			_libraryDataDict[libraryKey].savePath = path;
			UpdateSaveDisabled();
			if(changeName) ChangeLibraryName(libraryKey, GetFileNameFromFilePath(path));
			StoreOpenedLibraries();
		}
		else
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Error saving asset library: {error}");
		}
	}

	private void UpdateSaveDisabled()
	{
		if (!_libraryDataDict.ContainsKey(_currentLibrary))
		{
			_assetPlacerUi.SetAssetLibrarySaveDisabled(true);
		}
		else
		{
			var hasChanges = CurrentLibraryData.savePath == null || CurrentLibraryData.dirty;
			_assetPlacerUi.SetAssetLibrarySaveDisabled(!hasChanges);
		}
	}
	
	private void ChangeLibraryName(string oldName, string newName)
	{
		if (oldName == newName) return;
		var data = _libraryDataDict[oldName];
		_libraryDataDict.Remove(oldName);
		var availableNewName = GetAvailableLibraryName(newName);
		_assetPlacerUi.ChangeTabTitle(oldName, availableNewName);
		_libraryDataDict.Add(availableNewName, data);
		
		if(oldName == _currentLibrary) _currentLibrary = availableNewName;
	}

	private void OnShowAssetInFileSystem(string assetPath)
	{
		_editorInterface.GetFileSystemDock().NavigateToPath(assetPath);
	}

	private void OnOpenAsset(string assetPath)
	{
		_editorInterface.OpenSceneFromPath(assetPath);
	}

	private void OnSelectAsset(string path, string name)
	{
		if (path == _selectedAsset?.path) return;
		var pathNull = string.IsNullOrEmpty(path);
		_selectedAsset = pathNull ? null : CurrentLibraryData.GetAsset(path);
		SelectedAssetName = name;
		ClearHologram();
		
		if (!pathNull)
		{
			Hologram = CreateHologram();
			if (Hologram == null)
			{
				DeselectAsset();
			}
			else
			{
				_lastSelectedAssetPath = _selectedAsset.path;
			}
		}
	}

	private void OnResetAssetTransform(string path)
	{
		var data = _libraryDataDict[_currentLibrary].GetAsset(path);
		data.lastTransform = data.defaultTransform;
		UpdateResetTransformButton(data);
	}
	
	public const string resFileEnding = ".res";
	public const string tresFileEnding = ".tres";

	private bool IsValidFile(string filePath)
	{
		const string sceneFileEnding = ".tscn";
		const string compressedSceneFileEnding = ".scn";
		const string objFileEnding = ".obj";
		const string gltfFileEnding = ".gltf";
		const string glbFileEnding = ".glb";
		const string fbxFileEnding = ".fbx";
		const string colladaFileEnding = ".dae";
		const string blendFileEnding = ".blend";

		string[] validEndings =
		{
			sceneFileEnding, compressedSceneFileEnding, objFileEnding, gltfFileEnding, glbFileEnding, fbxFileEnding, colladaFileEnding, blendFileEnding, resFileEnding, tresFileEnding
		};

		// Check File Ending, and if file exists and is a Scene or Mesh
		if (validEndings.Any(filePath.EndsWith))
		{
			if(ResourceLoader.Exists(filePath)) return true;
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: {filePath} not found. It might have been moved or deleted");
		}
		GD.PrintErr($"{nameof(AssetPlacerPlugin)}: {filePath} has an unsupported file ending. Are you sure it is a 3D file?");
		return false;
	}

	private void OnAddNewAsset(string[] assetPaths)
	{
		List<Asset3DData> assets = new();
		foreach (var assetPath in assetPaths)
		{
			assets.Add(new Asset3DData(assetPath, Asset3DData.PreviewPerspective.Default));
		}
		OnAddAssetData(assets);
	}
	
	private void OnAddAssetData(IEnumerable<Asset3DData> assets)
	{
		List<Asset3DData> validAssets = new();
		foreach (var asset in assets)
		{
			// if the _currentLibrary is "[Empty]", we create a new library 
			if (_currentLibrary == null || !_libraryDataDict.ContainsKey(_currentLibrary)) OnNewAssetLibrary();
			
			Debug.Assert(_currentLibrary != null, nameof(_currentLibrary) + " != null");
			if (CurrentLibraryData.ContainsAsset(asset.path)) continue;
			if (!IsValidFile(asset.path)) continue;
			
			var res = ResourceLoader.Load(asset.path); // Expensive operation
			asset.isMesh = res is Mesh;
			if (res is Mesh or PackedScene)
			{
				CurrentLibraryData.assetData.Add(asset);
				CurrentLibraryData.dirty = true;
				validAssets.Add(asset);
				UpdateSaveDisabled();
			}
			else
			{
				GD.PrintErr($"{nameof(AssetPlacerPlugin)}: {asset.path} is not a scene or mesh. Other Resources are not supported.");
			}
		}
		if (_currentLibrary == null) return;
		_assetPlacerUi.AddAssets(validAssets);
		GeneratePreviews(validAssets, false, CurrentLibraryData.previewPerspective);
	}

	private void OnRemoveAsset(string[] paths)
	{
		foreach (var path in paths)
		{
			if (CurrentLibraryData.ContainsAsset(path))
			{
				CurrentLibraryData.RemoveAsset(path);
				CurrentLibraryData.dirty = true;
				UpdateSaveDisabled();
			}
		}
		_assetPlacerUi.RemoveAssets(paths);
	}

	private void OnLibraryChange(Asset3DData.PreviewPerspective perspective, int scrollPosition = -1)
	{
		DeselectAsset();
		// _currentLibrary is the title of the currently selected tab bar.
		// if the last library was removed, and _currentLibrary is "[Empty]",
		// we clear all the assets
		if (!_libraryDataDict.ContainsKey(_currentLibrary))
		{
			_assetPlacerUi.UpdateAllAssets(new List<Asset3DData>());
		}
		else
		{
			_assetPlacerUi.UpdateAllAssets(CurrentLibraryData.assetData, scrollPosition);
			if(!string.IsNullOrEmpty(CurrentLibraryData.savePath)) AssetPlacerPersistence.StoreGlobalData(LastSelectedLibrarySaveKey, CurrentLibraryData.savePath);
			GeneratePreviews(CurrentLibraryData.assetData, false, perspective);
		}
	}

	private void GeneratePreviews(IEnumerable<Asset3DData> libraryData, bool forceReload, Asset3DData.PreviewPerspective libraryPerspective = Asset3DData.PreviewPerspective.Default)
	{
		var thumbnailSize = Vector2I.One * _editorInterface.GetEditorSettings()
			.GetSetting("filesystem/file_dialog/thumbnail_size").AsInt32();
		
		foreach (Asset3DData asset in libraryData)
		{
			var perspective = asset.previewPerspective != Asset3DData.PreviewPerspective.Default ? asset.previewPerspective : libraryPerspective;
			try
			{
				PreviewGenerator._GenerateFromPath(asset.path, thumbnailSize, new Callable(this, MethodName.OnPreviewLoaded), forceReload,  perspective);
			}
			catch (AssetPreviewGenerator.PreviewForResourceUnhandledException _)
			{
				if (ResourceLoader.Exists(asset.path))
				{
					EditorResourcePreview resourcePreviewer = _editorInterface.GetResourcePreviewer();
					resourcePreviewer.QueueResourcePreview(asset.path, this,
						MethodName.OnPreviewLoaded, new Variant());
				}
				else
				{
					_assetPlacerUi.SetAssetBroken(asset.path, true);
				}
			}
		}
	}

	public void OnPreviewLoaded(string path, Texture2D preview, Texture2D thumbnailPreview, Variant userdata)
	{
		OnPreviewLoaded(path, preview);
	}
	
	public void OnPreviewLoaded(string assetPath, Variant preview)
	{
		if (preview.Obj is Texture2D previewTexture)
		{
			_assetPlacerUi.UpdateAssetPreview(assetPath, previewTexture, previewTexture, new Variant());
		}
		_assetPlacerUi.SetAssetBroken(assetPath, preview.Obj is not Texture2D);
	}

	private Node3D CreateHologram()
	{
		if (!ResourceLoader.Exists(_selectedAsset.path))
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Cannot find resource at {_selectedAsset.path}: File might have been deleted or moved.");
			_assetPlacerUi.SetAssetBroken(_selectedAsset.path, true);
			return null;
		}
		
		var asset = ResourceLoader.Load<Resource>(_selectedAsset.path); // Expensive operation
		Node3D hologram = null;
		
		var updateButton = _selectedAsset.isMesh != asset is Mesh;
		_selectedAsset.isMesh = asset is Mesh;
		if (updateButton)
		{
			_assetPlacerUi.UpdateAssetButton(_selectedAsset);
			CurrentLibraryData.dirty = true;
			UpdateSaveDisabled();
		}
		
		if (asset is PackedScene scene)
		{
			hologram = scene.Instantiate<Node3D>();
		}
		else if (asset is Mesh mesh)
		{
			var meshInstance = new MeshInstance3D();
			meshInstance.Mesh = mesh;
			hologram = meshInstance;
		}
		
		if(hologram == null)
		{
			GD.PrintErr($"{nameof(AssetPlacerPlugin)}: Cannot instantiate asset at {_selectedAsset.path}: Resource type is not supported (should be a Scene or a Mesh).");
			_assetPlacerUi.SetAssetBroken(_selectedAsset.path, true);
		}
		else
		{
			_assetPlacerUi.SetAssetBroken(_selectedAsset.path, false);
			ReloadAssetPreview(_selectedAsset.path);
		}
		
		return hologram;
	}
	
	public void ClearHologram()
	{
		Hologram?.QueueFree();
		Hologram = null;
	}

	public void SetAssetTransformDataFromHologram() // when added to tree
	{
		if (_selectedAsset == null || Hologram == null || !Hologram.IsInsideTree()) return;
		if (!_selectedAsset.hologramInstantiated)
		{
			_selectedAsset.hologramInstantiated = true;
			_selectedAsset.defaultTransform = Hologram.GlobalTransform;
			SaveTransform();
		}
		else
		{
			var holoTransform = Hologram.GlobalTransform;
			holoTransform.Basis = _selectedAsset.lastTransform.Basis;
			Hologram.GlobalTransform = holoTransform;
		}
	}
	
	public void DeselectAsset()
	{
		_selectedAsset = null;
		SelectedAssetName = null;
		ClearHologram();
		_assetPlacerUi.MarkButtonAsDeselected();
	}

	public bool IsAssetSelected()
	{
		return _selectedAsset != null;
	}

	public void TrySelectPreviousAsset()
	{
		_assetPlacerUi.SelectAsset(_lastSelectedAssetPath);
	}

	public void SaveTransform()
	{
		_selectedAsset.lastTransform = Hologram.GlobalTransform;
		UpdateResetTransformButton(_selectedAsset);
	}

	public void UpdateResetTransformButton(Asset3DData data)
	{
		_assetPlacerUi.SetResetTransformButtonVisible(data.path, data.lastTransform != data.defaultTransform);
	}

	public Node3D CreateInstance()
	{
		Debug.Assert(Hologram != null, "Trying to create an instance without a hologram");
		LastPlacedAsset = Hologram.Duplicate() as Node3D;
		Hologram.GetParent().AddChild(LastPlacedAsset);
		return LastPlacedAsset;
	}

	private string GetAvailableLibraryName(string desiredName)
	{
		if (desiredName == AssetPlacerUi.EmptyTabTitle) desiredName = "Empty";
		var name = desiredName;
		var i = 1;
		while (_libraryDataDict.Keys.Any(x => x == name))
		{
			name = $"{desiredName} ({i})";
			i++;
		}
		return name;
	}

	private string GetFolderPathFromFilePath(string path)
	{
		var idx = path.LastIndexOf('/');

		var folder = idx >=0 ? path.Substring(0, idx) : "";
		if (path.Substring(0, idx+1).EndsWith("//")) return path.Substring(0, idx+1); //root folder
		return folder;
	}
	
	private string GetFileNameFromFilePath(string path)
	{
		var fileNameFull = path.GetFile();
		return fileNameFull.Substring(0, fileNameFull.Length - 5);
	}

	public void ResetHologramTransform()
	{
		if (_selectedAsset != null && Hologram != null)
		{
			var holoTransform = Hologram.GlobalTransform;
			holoTransform.Basis = _selectedAsset.defaultTransform.Basis;
			Hologram.GlobalTransform = holoTransform;
			SaveTransform();
		}
	}
}

#endif
