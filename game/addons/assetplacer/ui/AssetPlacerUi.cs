// AssetPlacerUi.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace AssetPlacer;

[Tool]
public partial class AssetPlacerUi : Control
{
	[Export] public NodePath dropPanel;
	[Export] public NodePath assetGrid;
	[Export] public NodePath assetButtonRightClickPopup;
	[Export] public NodePath brokenAssetButtonRightClickPopup;
	[Export] public NodePath tabRightClickPopup;
	[Export] public NodePath snappingUiPath;
	[Export] public NodePath spawnParentSelectionUiPath;
	[Export] public NodePath assetButtonSizeSlider;
	[Export] public NodePath placementUiPath;
	[Export] public NodePath saveButtonPath;
	[Export] public NodePath libraryTabBarPath;
	[Export] public NodePath addLibraryButton;
	[Export] public NodePath loadButtonPath;
	[Export] public NodePath loadAssetLibraryDialogPath;
	[Export] public NodePath saveAssetLibraryDialogPath;
	[Export] public NodePath helpDialog;
	[Export] public NodePath helpButton;
	[Export] public NodePath aboutDialog;
	[Export] public NodePath aboutButton;
	[Export] public NodePath toExternalWindowButton;
	[Export] public NodePath matchSelectedButton;
	[Export] public Theme buttonTheme;
	[Export] public Theme selectionTheme;
	[Export] public PackedScene assetPlacerButton;
	[Export] public NodePath assetPaletteScrollContainer;
	[Export] public NodePath assetPaletteFilterLineEdit;

	private AssetDropPanel _dropPanel;
	private HelpDialog _helpDialog;
	private AcceptDialog _aboutDialog;
	private TabBar _tabBar;
	private Slider _assetButtonSizeSlider;
	private Control _assetGrid;
	private RightClickPopup _assetButtonRightClickPopup;
	private RightClickPopup _brokenAssetButtonRightClickPopup;
	private RightClickPopup _tabRightClickPopup;
	public SnappingUi snappingUi;
	public PlacementUi placementUi;
	private FileDialog _loadAssetLibraryDialog;
	private SaveAssetLibraryDialog _saveAssetLibraryDialog;
	private Godot.Collections.Dictionary<string, AssetPlacerButton> _assetButtons = new();
	public NodePathSelectorUi _spawnParentSelectionUi;
	private Button _saveButton;
	private Button _loadButton;
	private Button _addLibraryButton;
	private Button _toExternalWindowButton;
	private Button _matchSelectedButton;
	private Vector2 _defaultAssetButtonSize = new Vector2(100,100);
	private int _defaultAssetButtonFontSize = 18;
	private ScrollContainer _assetPaletteScrollContainer;
	private Godot.Collections.Dictionary<string, int> _libraryScrollPositions = new();
	private Texture2D _brokenIcon;
	private string _currentTabTitle = null;
	private const string PreviewPerspectiveContextMenuLabel = "Preview Perspective";
	private Texture2D _meshIcon;
	private LineEdit _assetPaletteFilterLineEdit;
	private bool _tabAdded = false;
	
	#region Signals
	[Signal]
	public delegate void AssetsAddedEventHandler(string[] assetPaths);
	
	[Signal]
	public delegate void AssetsRemovedEventHandler(string[] assetPaths);
	
	[Signal]
	public delegate void AssetsOpenedEventHandler(string assetPath);
	
	[Signal]
	public delegate void AssetSelectedEventHandler(string assetPath, string assetName);
	
	[Signal]
	public delegate void AssetTransformResetEventHandler(string assetPath);
	
	[Signal]
	public delegate void AssetShownInFileSystemEventHandler(string assetPath);
	
	[Signal]
	public delegate void AssetLibrarySavedEventHandler(string libraryName, string libraryPath, bool changeName);
	[Signal]
	public delegate void AssetButtonRightClickedEventHandler(string assetPath, Vector2 pos);
	[Signal]
	public delegate void TabRightClickedEventHandler(string libraryName, Vector2 pos);
	
	[Signal]
	public delegate void TabsRearrangedEventHandler();
	
	[Signal]
	public delegate void SaveButtonPressedEventHandler();
	
	[Signal]
	public delegate void AssetLibrarySelectedEventHandler(string libraryPath);
	
	[Signal]
	public delegate void AssetTabSelectedEventHandler(string tabTitle, int scrollPos);
	
	[Signal]
	public delegate void NewTabPressedEventHandler();
	
	[Signal]
	public delegate void HelpDialogOpenedEventHandler();
	
	[Signal]
	public delegate void ToExternalWindowEventHandler();
	
	[Signal]
	public delegate void AssetLibraryShownInFileManagerEventHandler(string libraryPath);
	
	[Signal]
	public delegate void AssetLibraryRemovedEventHandler(string library);
	
	[Signal]
	public delegate void ReloadLibraryPreviewsEventHandler(string libraryPath);
	[Signal]
	public delegate void DefaultLibraryPreviewsEventHandler(string libraryPath);
	
	[Signal]
	public delegate void ReloadAssetPreviewEventHandler(string assetPath);
	[Signal]
	public delegate void MatchSelectedPressedEventHandler();
	
	[Signal]
	public delegate void AssetPreviewPerspectiveChangedEventHandler(string assetPath, Asset3DData.PreviewPerspective perspective);
	
	[Signal]
	public delegate void LibraryPreviewPerspectiveChangedEventHandler(string libraryPath, Asset3DData.PreviewPerspective perspective);
	#endregion

	public void Init()
	{
		_dropPanel = GetNode<AssetDropPanel>(dropPanel);
		_assetGrid = GetNode<Control>(assetGrid);
		_spawnParentSelectionUi = GetNode<NodePathSelectorUi>(spawnParentSelectionUiPath);
		_spawnParentSelectionUi.Init();
		_assetButtonRightClickPopup = GetNode<RightClickPopup>(assetButtonRightClickPopup);
		_brokenAssetButtonRightClickPopup = GetNode<RightClickPopup>(brokenAssetButtonRightClickPopup);
		_tabRightClickPopup = GetNode<RightClickPopup>(tabRightClickPopup);
		_assetPaletteScrollContainer = GetNode<ScrollContainer>(assetPaletteScrollContainer);
		_assetPaletteFilterLineEdit = GetNode<LineEdit>(assetPaletteFilterLineEdit);
		_assetPaletteFilterLineEdit.TextSubmitted += _ => _assetPaletteFilterLineEdit.ReleaseFocus();
		_assetPaletteFilterLineEdit.TextChanged += UpdateAssetFilter;
		_matchSelectedButton = GetNode<Button>(matchSelectedButton);
		_matchSelectedButton.Disabled = true;
		_matchSelectedButton.Pressed += () => EmitSignal(SignalName.MatchSelectedPressed);
		
		_dropPanel.AssetsDropped += OnAssetsDropped;
		_dropPanel.GuiInput += OnPanelGui;
		snappingUi = GetNode<SnappingUi>(snappingUiPath);
		snappingUi.Init();
		placementUi = GetNode<PlacementUi>(placementUiPath);
		placementUi.Init();
		_assetButtonSizeSlider = GetNode<Slider>(assetButtonSizeSlider);
		_assetButtonSizeSlider.ValueChanged += OnAssetButtonSizeSliderChanged;
		_aboutDialog = GetNode<AcceptDialog>(aboutDialog);
		_helpDialog = GetNode<HelpDialog>(helpDialog);
		GetNode<Button>(helpButton).Pressed += () =>
		{
			_helpDialog.Position = GetViewport().GetWindow().Position + (Vector2I)GetViewportRect().GetCenter() - _helpDialog.Size/2;
			ClampWindowToScreen(_helpDialog, this);
			EmitSignal(SignalName.HelpDialogOpened);
			_helpDialog.Popup();
		};
		GetNode<Button>(aboutButton).Pressed += OpenAboutDialog;
		_toExternalWindowButton = GetNode<Button>(toExternalWindowButton);
		_toExternalWindowButton.Pressed += () => EmitSignal(SignalName.ToExternalWindow);
		
		//// Asset Library TabBar
		_tabBar = GetNode<TabBar>(libraryTabBarPath);
		_tabBar.TabSelected += i=>OnTabSelected((int) i);
		_tabBar.ActiveTabRearranged += _ => EmitSignal(SignalName.TabsRearranged);
		_addLibraryButton = GetNode<Button>(addLibraryButton);
		_addLibraryButton.Pressed += () => EmitSignal(SignalName.NewTabPressed);
		ResetTabBar();

		//// AssetLibrary Load and Save Dialogs
		_loadAssetLibraryDialog = GetNode<FileDialog>(loadAssetLibraryDialogPath);
		_saveAssetLibraryDialog = GetNode<SaveAssetLibraryDialog>(saveAssetLibraryDialogPath);
		_saveButton = GetNode<Button>(saveButtonPath);
		_loadButton = GetNode<Button>(loadButtonPath);
		
		_loadButton.Pressed += OnLoadButtonPressed;
		_saveButton.Pressed += () => EmitSignal(SignalName.SaveButtonPressed);
		_loadAssetLibraryDialog.FileSelected += OnAssetLibraryLoadFileSelect;
		_saveAssetLibraryDialog.FileSelected += OnAssetLibrarySaveFileSelect;
		
		Settings.RegisterSetting(Settings.DefaultCategory, Settings.LibrarySaveLocation, 
			(long) FileDialog.AccessEnum.Userdata, Variant.Type.Int, PropertyHint.Enum, 
			PropertyUtils.EnumToPropertyHintString<FileDialog.AccessEnum>());
		
		var librarySaveLocation = (FileDialog.AccessEnum) 
			Settings.GetSetting(Settings.DefaultCategory, Settings.LibrarySaveLocation).AsInt32();
		_loadAssetLibraryDialog.Access = librarySaveLocation;
		_saveAssetLibraryDialog.Access = librarySaveLocation;
		if (librarySaveLocation == FileDialog.AccessEnum.Userdata)
		{
			CreateSaveFolderIfNotExists(librarySaveLocation);
			_loadAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
			_saveAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
		}
		
		_tabBar.GuiInput += OnTabBarGuiInput;
	}

	private void OnAssetLibrarySaveFileSelect(string path)
	{
		EmitSignal(SignalName.AssetLibrarySaved, _saveAssetLibraryDialog.AssetLibraryName, path, _saveAssetLibraryDialog.ChangeName);
		_saveAssetLibraryDialog.CurrentFile = null;
		_saveAssetLibraryDialog.DeselectAll();
	}

	private void OnAssetLibraryLoadFileSelect(string path)
	{
		EmitSignal(SignalName.AssetLibrarySelected, path);
	}

	private void UpdateAssetFilter(string text)
	{
		foreach (var button in _assetButtons.Values)
		{
			button.Visible = button.assetName.ToLower().Contains(text.ToLower());
		}
	}

	private void OnTabBarGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			var tab = _tabBar.GetTabIdxAtPoint(mouseButton.Position);
			if (tab == -1) return;
			var title = _tabBar.GetTabTitle(tab);
			if (string.IsNullOrEmpty(title) || title is EmptyTabTitle) return;
			if (mouseButton.ButtonMask == MouseButtonMask.Right)
				EmitSignal(SignalName.TabRightClicked, title, _tabBar.GetScreenPosition() + mouseButton.Position);
			else if (mouseButton.ButtonMask == MouseButtonMask.Middle && title != EmptyTabTitle)  EmitSignal(SignalName.AssetLibraryRemoved, title);
		}
	}

	private void OnLoadButtonPressed()
	{
		_loadAssetLibraryDialog.Position = GetViewport().GetWindow().Position + (Vector2I)GetViewportRect().GetCenter() - _loadAssetLibraryDialog.Size/2;
		ClampWindowToScreen(_loadAssetLibraryDialog, this);
		
		var access = (FileDialog.AccessEnum) 
			Settings.GetSetting(Settings.DefaultCategory, Settings.LibrarySaveLocation).AsInt32();
		
		if (access != _loadAssetLibraryDialog.Access) // Access changed
		{
			_loadAssetLibraryDialog.Access = access;
			if (access == FileDialog.AccessEnum.Userdata)
			{
				CreateSaveFolderIfNotExists(access);
				_loadAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
			}
			else
				_loadAssetLibraryDialog.CurrentDir = "";
		}

		_loadAssetLibraryDialog.Popup();
	}
	private void CreateSaveFolderIfNotExists(FileDialog.AccessEnum access)
	{
		var libraryDirPath = AssetPalette.GetAssetLibraryDirPath(access);
		if (!DirAccess.DirExistsAbsolute(libraryDirPath))
		{
			DirAccess.MakeDirRecursiveAbsolute(libraryDirPath);
			_loadAssetLibraryDialog.Access = FileDialog.AccessEnum.Userdata;
			_loadAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
			_saveAssetLibraryDialog.Access = FileDialog.AccessEnum.Userdata;
			_saveAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
		}
	}
	
	public void ApplyTheme(Control baseControl)
	{
		_spawnParentSelectionUi.ApplyTheme(baseControl);
		snappingUi.ApplyTheme(baseControl);
		placementUi.ApplyTheme(baseControl);
		_brokenIcon = baseControl.GetThemeIcon("FileBrokenBigThumb", "EditorIcons");

		var addIcon = baseControl.GetThemeIcon("Add", "EditorIcons");
		_addLibraryButton.Text = "";
		_addLibraryButton.Icon = addIcon;
		
		#if GODOT4_1_OR_GREATER
		var externalWindowIcon = baseControl.GetThemeIcon("MakeFloating", "EditorIcons");
		#else
		var externalWindowIcon = baseControl.GetThemeIcon("Window", "EditorIcons");
		#endif
		_toExternalWindowButton.Icon = externalWindowIcon;
		_toExternalWindowButton.Text = "";

		var searchIcon = baseControl.GetThemeIcon("Search", "EditorIcons");
		_assetPaletteFilterLineEdit.RightIcon = searchIcon;
		
		var removeIcon = baseControl.GetThemeIcon("Remove", "EditorIcons");
		var sceneIcon = baseControl.GetThemeIcon("PackedScene", "EditorIcons");
		var filesystemIcon = baseControl.GetThemeIcon("Filesystem", "EditorIcons");
		var saveIcon = baseControl.GetThemeIcon("Save", "EditorIcons");
		var reloadIcon = baseControl.GetThemeIcon("Reload", "EditorIcons");
		_meshIcon = baseControl.GetThemeIcon("Mesh", "EditorIcons");
		
		_assetButtonRightClickPopup.Clear();
		_assetButtonRightClickPopup.AddEntry("Remove", removeIcon, new Callable(this, MethodName.RemoveAsset));
		_assetButtonRightClickPopup.AddEntry("Open Scene", sceneIcon, new Callable(this, MethodName.OpenAssetScene), new Callable(this, MethodName.IsNotAssetResFile));
		_assetButtonRightClickPopup.AddEntry("Show in FileSystem", null, new Callable(this, MethodName.ShowAssetInFileSystem));
		_assetButtonRightClickPopup.AddEntry("Reload Preview", reloadIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.ReloadAssetPreview);
		_assetButtonRightClickPopup.AddEnumEntry(PreviewPerspectiveContextMenuLabel, new Callable(this, MethodName.SetPreviewPerspective), PropertyUtils.EnumToStrings<Asset3DData.PreviewPerspective>());
		
		_brokenAssetButtonRightClickPopup.Clear();
		_brokenAssetButtonRightClickPopup.AddEntry("Try Reload", reloadIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.ReloadAssetPreview);
		_brokenAssetButtonRightClickPopup.AddEntry("Remove", removeIcon, new Callable(this, MethodName.RemoveAsset));

		_tabRightClickPopup.Clear();
		_tabRightClickPopup.AddEntry("Close Tab", removeIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.AssetLibraryRemoved);
		_tabRightClickPopup.AddEntry("Show in File Manager", filesystemIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.AssetLibraryShownInFileManager);
		_tabRightClickPopup.AddEntry("Save as", saveIcon, new Callable(this, MethodName.ShowSaveAsDialog));
		_tabRightClickPopup.AddEntry("Reload all Previews", reloadIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.ReloadLibraryPreviews);
		_tabRightClickPopup.AddEnumEntry(PreviewPerspectiveContextMenuLabel, new Callable(this, MethodName.SetLibraryPreviewPerspective), PropertyUtils.EnumToStrings<Asset3DData.PreviewPerspective>());
		_tabRightClickPopup.AddEntry("Reset all Preview Perspectives", reloadIcon, new Callable(this, MethodName.EmitPathSignal), SignalName.DefaultLibraryPreviews);

		foreach (var button in _assetButtons.Values)
		{
			button.UpdateButtonIcon(_meshIcon);
		}
	}

	public void EmitPathSignal(StringName signal, string path)
	{
		EmitSignal(signal, path);
	}

	public void SetPreviewPerspective(string path, int previewPerspective)
	{
		EmitSignal(SignalName.AssetPreviewPerspectiveChanged, path, previewPerspective);
	}

	public void SetLibraryPreviewPerspective(string libraryPath, int previewPerspective)
	{
		EmitSignal(SignalName.LibraryPreviewPerspectiveChanged, libraryPath, previewPerspective);
	}
	
	public void OpenAboutDialog()
	{
		_aboutDialog.Position = GetViewport().GetWindow().Position + (Vector2I)GetViewportRect().GetCenter() - _aboutDialog.Size/2;
		ClampWindowToScreen(_aboutDialog, this);
		#if GODOT4_1_OR_GREATER
		_aboutDialog.GetParentOrNull<Node>()?.RemoveChild(_aboutDialog);
		_aboutDialog.PopupExclusive(this);
		#else
		_aboutDialog.Popup();
		#endif
	}

	public void InitHelpDialog(System.Collections.Generic.Dictionary<string, string> shortcutDictionary)
	{
		_helpDialog.InitShortcutTable(shortcutDictionary);
	}

	private void ResetTabBar()
	{
		_tabBar.ClearTabs();
		_tabBar.AddTab(EmptyTabTitle);
		_tabBar.CurrentTab = 0;
	}

	public void ShowSaveAsDialog(string libraryName)
	{
		ShowSaveDialog(libraryName, true);
	}
	
	public void ShowSaveDialog(string libraryName, bool changeName)
	{
		_saveAssetLibraryDialog.Position = GetViewport().GetWindow().Position + (Vector2I) GetViewportRect().GetCenter() - _saveAssetLibraryDialog.Size/2;
		ClampWindowToScreen(_saveAssetLibraryDialog, this);
		_saveAssetLibraryDialog.AssetLibraryName = libraryName;
		
		var access = (FileDialog.AccessEnum) 
			Settings.GetSetting(Settings.DefaultCategory, Settings.LibrarySaveLocation).AsInt32();
		
		if (access != _saveAssetLibraryDialog.Access)  // access changed
		{
			_saveAssetLibraryDialog.Access = access;
			if (access == FileDialog.AccessEnum.Userdata)
			{
				CreateSaveFolderIfNotExists(access);
				_saveAssetLibraryDialog.CurrentDir = AssetPalette.AssetLibrarySaveFolder;
			}
			else
				_saveAssetLibraryDialog.CurrentDir = "";
		}
		_saveAssetLibraryDialog.ChangeName = changeName;
		_saveAssetLibraryDialog.Popup();
	}
	
	public void SetAssetLibrarySaveDisabled(bool disabled)
	{
		_saveButton.Disabled = disabled;
	}
	
	public void ChangeTabTitle(string oldName, string newName)
	{
		if(oldName == _currentTabTitle) _currentTabTitle = newName;
		if (_libraryScrollPositions.ContainsKey(oldName) && oldName != newName)
		{
			_libraryScrollPositions.Add(newName, _libraryScrollPositions[oldName]);
			_libraryScrollPositions.Remove(oldName);
		}
		var tab = GetTabIdx(oldName);
		if(tab != -1) _tabBar.SetTabTitle(tab, newName);
	}

	public const string EmptyTabTitle = "[Empty]";
	private void OnTabSelected(int index)
	{
		if (_tabAdded) return;
		var tabTitle = _tabBar.GetTabTitle(index);
		if(_currentTabTitle != null) _libraryScrollPositions[_currentTabTitle] = _assetPaletteScrollContainer.ScrollVertical;
		_currentTabTitle = tabTitle;
		_assetPaletteFilterLineEdit.Clear();
		if(_assetPaletteFilterLineEdit.IsInsideTree()) _assetPaletteFilterLineEdit.ReleaseFocus();
		EmitSignal(SignalName.AssetTabSelected, tabTitle, GetScrollPos(tabTitle));
	}

	private void OnPanelGui(InputEvent @event)
	{
		if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left && button.Pressed)
		{
			DeselectAsset();
		}
	}

	private void OnAssetsDropped(string[] obj)
	{
		 EmitSignal(SignalName.AssetsAdded, obj);
	}

	public void UpdateAllAssets(IEnumerable<Asset3DData> assetData, int scrollPosition = 0)
	{
		_assetButtons.Clear();
		foreach (var child in _assetGrid.GetChildren())
		{
			_assetGrid.RemoveChild(child);
			child.QueueFree();
		}

		AddAssets(assetData, scrollPosition);
	}

	public void AddAssets(IEnumerable<Asset3DData> assetData, int scrollPosition = -1)
	{
		var assetList = assetData.ToList();
		assetList.ForEach(AddAsset);
		if(assetList.Count != 0 && scrollPosition >= 0) SetPaletteScrollPos(scrollPosition); // after last asset
	}
	public void RemoveAssets(IEnumerable<string> assetPaths)
	{
		foreach (var assetPath in _assetButtons.Keys.Where(assetPaths.Contains))
		{
			if(_assetButtons[assetPath] == _selectedAsset) DeselectAsset();
			_assetGrid.RemoveChild(_assetButtons[assetPath]);
			_assetButtons[assetPath].QueueFree();
			_assetButtons.Remove(assetPath);
		}
	}

	private void OnAssetButtonSizeSliderChanged(double val)
	{
		foreach (var button in _assetButtons.Values)
		{
			button.CustomMinimumSize = _defaultAssetButtonSize * (float) val;
			button.GetNode<Label>("%Label").AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((float) (_defaultAssetButtonFontSize * val)));
		}
	}
	
	public void UpdateAssetPreview(string path, Texture2D preview, Texture2D thumbnailPreview, Variant userdata)
	{
		// when the view is switched during loading, or when several libraries are being loaded at the same time,
		// we cannot update the texture. The textures are stored in a cache to not waste the computing
		if (preview == null || !_assetButtons.ContainsKey(path)) return;
		
		
		var textureRect = _assetButtons[path].GetNode<TextureRect>("%TextureRect");
		textureRect.Texture = preview;
	}

	public void AddAndSelectAssetTab(string tabTitle)
	{
		AddAssetTab(tabTitle);
		_tabAdded = true; // prevent signal and call manually, to prevent regressions should call routine ever change
		_tabBar.CurrentTab = _tabBar.TabCount - 1; // select the tab
		_tabAdded = false;
		OnTabSelected(_tabBar.CurrentTab);
	}

	public void AddAssetTab(string tabTitle)
	{
		if(_tabBar.GetTabTitle(0) == EmptyTabTitle)
		{
			_tabBar.RemoveTab(0);
		}
		_tabAdded = true; // prevent tab select signal from being triggered
		_tabBar.AddTab(tabTitle); // add the tab
		_tabAdded = false;
	}

	public void SelectAssetTab(string tabTitle)
	{
		var tab = GetTabIdx(tabTitle);
		if (tab != -1)
		{
			_tabBar.CurrentTab = tab;
		}
	}

	public int GetTabIdx(string title)
	{
		// iterate over all tabs and check if the name matches
		for (int i = 0; i < _tabBar.TabCount; i++)
		{
			if (_tabBar.GetTabTitle(i) == title)
			{
				return i;
			}
		}
		return -1;
	}

	public void RemoveAssetLibrary(string title)
	{
		var tab = GetTabIdx(title);
		var currentTab = _tabBar.CurrentTab;
		Debug.Assert(tab != -1);
		if (_tabBar.TabCount <= 1) ResetTabBar();
		else
		{
			_tabBar.RemoveTab(tab);
			if (tab == currentTab) // we were removing the current tab 
			{
				var lowerTabTitle = _tabBar.GetTabTitle(Mathf.Max(tab-1, 0));
				 EmitSignal(SignalName.AssetTabSelected, lowerTabTitle, GetScrollPos(lowerTabTitle));
			}
		}

		if (_libraryScrollPositions.ContainsKey(title)) _libraryScrollPositions.Remove(title);
	}

	private Button _selectedAsset;

	private void AddAsset(Asset3DData data)
	{
		string assetPath = data.path;
		var button = assetPlacerButton.Instantiate<AssetPlacerButton>();
		var label = button.GetNode<Label>("%Label");
		var sliderVal = (float) _assetButtonSizeSlider.Value;
		
		_defaultAssetButtonSize = button.CustomMinimumSize;
		
		button.CustomMinimumSize = _defaultAssetButtonSize * sliderVal;
		button.Theme = buttonTheme;
		var assetName = GetAssetName(assetPath);
		const int maxDisplayNameLength = 14;
		label.Text = assetName.Length <= maxDisplayNameLength ? assetName : assetName.Substring(0, maxDisplayNameLength-2)+"..";
		label.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((float) (_defaultAssetButtonFontSize * sliderVal)));
		button.TooltipText = assetName;
		_assetGrid.AddChild(button);
		_assetButtons[assetPath]= button;
		
		button.SetData(assetPath, assetName);
		button.SetChildButtonTheme(buttonTheme);
		button.ButtonWasPressed += AssetButtonPressed;
		button.RightClicked += (path, pos) => EmitSignal(SignalName.AssetButtonRightClicked, path, pos);
		button.ResetTransformPressed += AssetResetTransformPressed;
		button.SetResetTransformButtonVisible(data.lastTransform != data.defaultTransform);
		UpdateAssetButton(data);
	}

	public void UpdateAssetButton(Asset3DData asset)
	{
		_assetButtons[asset.path].SetButtonType(asset.isMesh ? AssetPlacerButton.ButtonType.Mesh : AssetPlacerButton.ButtonType.Normal);
		_assetButtons[asset.path].UpdateButtonIcon(_meshIcon);
	}
	
	public void DisplayAssetRightClickPopup(string assetPath, int perspectiveIdx, Vector2 position)
	{
		if (_assetButtons[assetPath].isBroken)
		{
			RightClicked(assetPath, position, _brokenAssetButtonRightClickPopup);
		} else {
			RightClicked(assetPath, position, _assetButtonRightClickPopup);
			_assetButtonRightClickPopup.SetEnumEntryChecked(PreviewPerspectiveContextMenuLabel, perspectiveIdx);
		}
	}
	public void DisplayTabRightClickPopup(string library, int perspectiveIdx, Vector2 position)
	{
		RightClicked(library, position, _tabRightClickPopup);
		_tabRightClickPopup.SetEnumEntryChecked(PreviewPerspectiveContextMenuLabel, perspectiveIdx);
	}

	public void SelectAssetButtonFromPath(string assetPath)
	{
		AssetButtonPressed(_assetButtons[assetPath]);
		SetPaletteScrollPosOnButton(_assetButtons[assetPath]);
	}

	private void AssetButtonPressed(AssetPlacerButton button)
	{
		var prevSelection = _selectedAsset;
		DeselectAsset();
		if (prevSelection != button)
		{
			_selectedAsset = button;
			_selectedAsset.Theme = selectionTheme;
			 EmitSignal(SignalName.AssetSelected, button.assetPath, button.assetName);
		}
	}

	private void AssetResetTransformPressed(AssetPlacerButton button, string assetPath)
	{
		EmitSignal(SignalName.AssetTransformReset, assetPath);
	}

	private async void SetPaletteScrollPos(int pos)
	{
		await ToSignal(GetTree(), "process_frame");
		_assetPaletteScrollContainer.ScrollVertical = pos;
	}

	public async void SetPaletteScrollPosOnButton(Button button)
	{
		// Await position of potentially added button to be updated
		await ToSignal(GetTree(), "process_frame");
		
		var maxScroll = Mathf.FloorToInt(Mathf.Max(_assetGrid.Size.Y - _assetPaletteScrollContainer.Size.Y, 0));
		var buttonPos = Mathf.RoundToInt(button.Position.Y);
		var scrollPos = Mathf.Clamp(buttonPos, 0, maxScroll);
		_assetPaletteScrollContainer.ScrollVertical = scrollPos;
	}

	private static string GetAssetName(string path)
	{
		var assetNameWithEnding = path.Split('/')[^1];
		//var endingIndex = assetNameWithEnding.IndexOf(".tscn", StringComparison.Ordinal);
		//var assetName = assetNameWithEnding.Substring(0, endingIndex > 0 ? endingIndex : assetNameWithEnding.Length);
		return assetNameWithEnding;
	}

	private void RightClicked(String itemPath, Vector2 position, RightClickPopup popup)
	{
		popup.Position = (Vector2I) position;
		popup.ResetSize();
		popup.Popup();
		popup.itemPath = itemPath;
		popup.UpdateConditions();
	}

	public void DeselectAsset()
	{
		if (_selectedAsset != null)
		{
			EmitSignal(SignalName.AssetSelected, "", "");
			MarkButtonAsDeselected();
		}
	}

	public void MarkButtonAsDeselected()
	{
		if (_selectedAsset != null)
		{
			_selectedAsset.Theme = buttonTheme;
			_selectedAsset = null;
		}
	}

	public void RemoveAsset(string assetPath)
	{
		if (assetPath != null)
		{
			 EmitSignal(SignalName.AssetsRemoved, new[]{assetPath});
		}
	}

	private void OpenAssetScene(string assetPath)
	{
		if (assetPath != null)
		{
			 EmitSignal(SignalName.AssetsOpened, assetPath);
		}
	}
	
	private bool IsNotAssetResFile(string assetPath)
	{
		if (assetPath != null)
		{
			string[] validEndings =
			{
				AssetPalette.resFileEnding, AssetPalette.tresFileEnding
			};

			// Check File Ending
			return !(validEndings.Any(assetPath.EndsWith));
		}
		return false;
	}

	private void ShowAssetInFileSystem(string assetPath)
	{
		if (assetPath != null)
		{
			 EmitSignal(SignalName.AssetShownInFileSystem, assetPath);
		}
	}
	public static bool TryParseFloat(string text, out float val)
	{
		if (text.StartsWith('.')) text = "0" + text;
		return float.TryParse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out val);
	}

	public void SelectAsset(string path)
	{
		if (path != null && _assetButtons.ContainsKey(path))
		{
			_assetButtons[path].EmitSignal("pressed");
		}
	}

	public void OnSceneChanged()
	{
		placementUi.OnSceneChanged();
		snappingUi.OnSceneChanged();
	}

	private int GetScrollPos(string tabName)
	{
		return _libraryScrollPositions.ContainsKey(tabName) ? _libraryScrollPositions[tabName] : 0;
	}

	public void SetAssetBroken(string selectedAssetPath, bool broken)
	{
		bool valid = _assetButtons.TryGetValue(selectedAssetPath, out var button);
		if (!valid) return;
		if (broken)
		{
			UpdateAssetPreview(selectedAssetPath, _brokenIcon, _brokenIcon, new Variant());
			button.TooltipText = $"Asset file not found. Was at: {selectedAssetPath}\nRight click for options.";
		}
		else if(button.isBroken) // button no longer broken
		{
			button.TooltipText = button.assetName;
		}
		button.isBroken = broken;
	}

	public void OnAttachmentChanged(bool attached)
	{
		_toExternalWindowButton.Visible = attached;
		if(attached) _aboutDialog.GetParentOrNull<Node>()?.RemoveChild(_aboutDialog); // prevent disposal
	}

	public void SetResetTransformButtonVisible(string assetPath, bool visible)
	{
		_assetButtons[assetPath].SetResetTransformButtonVisible(visible);
	}
	
	public void MatchSelectedButtonDisabled(bool value)
	{
		_matchSelectedButton.Disabled = value;
	}

	public static void ClampWindowToScreen(Window w, Control controlOnScreen)
	{
		var screenPos = DisplayServer.ScreenGetPosition(controlOnScreen.GetWindow().CurrentScreen); // position of screen on a multi-monitor setup
		var min = screenPos + Vector2I.Down * 30;
		var max = screenPos + DisplayServer.ScreenGetSize(controlOnScreen.GetWindow().CurrentScreen) - w.Size -
				  Vector2I.Down * 30;
		
		if (min < max)
		{
			w.Position = w.Position.Clamp(min, max); // clamp such that entire window is on screen
		}
		else // clamp such that window's TitleBar is on the screen
		{
			w.Position = min;
		}
	}
}

#endif
