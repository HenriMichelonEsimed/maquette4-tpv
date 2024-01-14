// AssetPlacerPersistence.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

public class AssetPlacerPersistence
{
    public const string SaveFolder = ".assetPlacer";
    public const string SaveFileName = "data";

    private static AssetPlacerPersistence _instance;
    
    public static AssetPlacerPersistence Instance
    {
        get
        {
            if (_instance == null)
            {
                Init();
                return _instance;
            }
            return _instance;
        }
    }

    private string _currentScenePath;
    private AssetPlacerPluginData _pluginData;
    private bool _dirty = false;

    public static void Init()
    {
        _instance = new AssetPlacerPersistence();
        _instance.LoadPluginData();
    }

    public static void Cleanup()
    {
        // free the instance, so it can be garbage collected.
        // Collection has been tested and confirmed with WeakReference and GC.Collect()
        _instance = null;
    }
    
    public void SetSceneRoot(Node root)
    {
        _currentScenePath = root?.SceneFilePath;
    }
    
    public static void StoreGlobalData(string key, Variant value)
    {
        CreatePluginDataIfNotExists();
        Instance._dirty = !(Instance._pluginData.globalData.ContainsKey(key) && Instance._pluginData.globalData[key].Equals(value));
        Instance._pluginData.globalData[key] = value;
    }

    public static Variant LoadGlobalData(string key, Variant defaultValue, Variant.Type variantType)
    {
        if (Instance._pluginData.globalData.ContainsKey(key))
        {
            var variant = Instance._pluginData.globalData[key];
            if (variant.VariantType == variantType)
            {
              return variant;
            }
        }

        return Variant.From(defaultValue);
    }

    public static Variant LoadSceneData(string key, Variant defaultValue, Variant.Type variantType)
    {
        CreatePluginDataIfNotExists(); // regenerate corrupt save file.
        if (Instance._pluginData.sceneData.ContainsKey(Instance._currentScenePath))
        {
            var sceneData = Instance._pluginData.sceneData[Instance._currentScenePath];
            if (sceneData.data.ContainsKey(key))
            {
                var variant = sceneData.data[key];
                //GD.Print($"scene data exists {variant}; of type {variant.VariantType}");
                if (variant.VariantType == variantType)
                {
                    return variant;
                }
                
            }
        }

        return Variant.From(defaultValue);
    }


    public static void StoreSceneData(string key, Variant value)
    {
        var sceneKey = Instance._currentScenePath;
        if (sceneKey == null)
        {
            //GD.Print("Can't store data, no scene selected.");
            return;
        }

        CreatePluginDataIfNotExists();
        if (!Instance._pluginData.sceneData.ContainsKey(sceneKey)) // todo: either pluginData or sceneData null
        {
            Instance._pluginData.sceneData.Add(sceneKey, new SceneData());
        }
        var sceneData = Instance._pluginData.sceneData[sceneKey];

        Instance._dirty = !(sceneData.data.ContainsKey(key) && sceneData.data[key].Equals(value));
        sceneData.data[key] = value;
        //GD.Print($"storing scene data {key} {value} for {sceneKey}");
    }

    private static void CreatePluginDataIfNotExists()
    {
        if (Instance._pluginData == null || Instance._pluginData.sceneData == null || Instance._pluginData.globalData == null)
        {
            Instance._pluginData = new AssetPlacerPluginData();
        }
    }

    private string SaveFolderPath => $"user://{SaveFolder}";
    private string DataPath => $"user://{SaveFolder}/{SaveFileName}.tres";
    private void LoadPluginData()
    {
        CheckSaveFolderExists();
        bool success = false;
        if(ResourceLoader.Exists(DataPath, nameof(AssetPlacerPluginData)))
        {
            _pluginData = ResourceLoader.Load<AssetPlacerPluginData>(DataPath);
            success = _pluginData != null;
        }
        if(!success)
        {
            _pluginData = new AssetPlacerPluginData();
            var e = ResourceSaver.Save(_pluginData, DataPath);
            if(e != Error.Ok) GD.PrintErr($"{e} error on saving resource data at {DataPath}");
        }
        _dirty = false;
    }

    public void SavePluginData()
    {
        if (_dirty)
        {
            // Hopefully this stays efficient enough, even if we save after almost every action.
            var err = ResourceSaver.Save(_pluginData, DataPath);
            _dirty = false;
            if(err != Error.Ok) GD.PrintErr($"Error saving file at {DataPath}: {err}");
        }
    }

    public bool GetShowLicenseAndSetFalse()
    {
        var show = _pluginData.showLicenseOnStart;
        _pluginData.showLicenseOnStart = false;
        return show;
    }

    private void CheckSaveFolderExists()
    {
        if (!DirAccess.DirExistsAbsolute(SaveFolderPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveFolderPath);
        }
    }

    // Only safe the plugin data if the instance exists.
    // Because, when the instance does not exist, saving will not save anything that isnt already saved
    public static void TrySavePluginData()
    {
        _instance?.SavePluginData();
    }
}

#endif