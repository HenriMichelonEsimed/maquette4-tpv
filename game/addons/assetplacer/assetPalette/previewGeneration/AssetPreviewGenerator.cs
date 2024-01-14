// AssetPreviewGenerator.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

public partial class AssetPreviewGenerator : Node
{
    public const string PerspectiveSetting = "Preview_Perspective";
    private const string CachePerspectiveSeparator = "//<>"; // some illegal characters to avoid cache collision
    public enum Perspective
    {
        Top, Bottom, Front, Back, Left, Right
    }
    
    private Array<PreviewRenderingViewport> _renderingViewports;

    private Array<LoadData> _pendingLoadActions = new();
    private TextureCache _textureCache = new();
    private Godot.Collections.Dictionary<LoadData, PreviewRenderingViewport> _assignedLoadVps = new();

    public AssetPreviewGenerator() {}
    
    public void Init(Array<PreviewRenderingViewport> renderingViewports)
    {
        Debug.Assert(renderingViewports.Count != 0);
        _renderingViewports = renderingViewports;
        Settings.RegisterSetting(Settings.DefaultCategory, PerspectiveSetting, (int) Perspective.Front, Variant.Type.Int, PropertyHint.Enum, PropertyUtils.EnumToPropertyHintString<Perspective>());
    }
    
    public void _Generate(Resource resource, Vector2I size, Callable onPreviewLoaded, bool ignoreCache, Asset3DData.PreviewPerspective perspective)
    {
        _GenerateFromPath(resource?.ResourcePath, size, onPreviewLoaded, ignoreCache, perspective);
    }

    public void _GenerateFromPath(string path, Vector2I size, Callable onPreviewLoaded, bool ignoreCache, Asset3DData.PreviewPerspective pperspective)
    {
        var perspective = GetPerspective(pperspective);
        var texture = _textureCache.CheckCache(GetCacheKey(path, perspective));
        if (!ignoreCache && texture != null)
        {
            // cache hit
            onPreviewLoaded.Call(path, texture);
            return;
        }
        
        // no cache hit or ignore
        var resource = ResourceLoader.Exists(path) ? ResourceLoader.Load(path) : null;
        if (resource is PackedScene or Mesh)
        {
            _pendingLoadActions.Add(new LoadData(onPreviewLoaded, resource, size, perspective));
        }
        else
        {
            throw new PreviewForResourceUnhandledException();
        }
    }

    private static string GetCacheKey(string resourcePath, Perspective perspective)
    {
        return resourcePath + CachePerspectiveSeparator + perspective;
    }


    public void Process()
    {
        if (_pendingLoadActions.Count == 0) return;
        List<LoadData> completedLoads = new();
        var vpMaybeAvailable = true; // optimization to skip a loop
        foreach (var loadAction in _pendingLoadActions)
        {
            if (vpMaybeAvailable && loadAction.waiting)
            {
                var availableViewport = _renderingViewports.FirstOrDefault(vp => _assignedLoadVps.Keys.All(l => _assignedLoadVps[l] != vp));
                if (availableViewport != null)
                {
                    loadAction.waiting = false;
                    availableViewport.SetPreviewNode(loadAction.assetResource, loadAction.previewPerspective);
                    loadAction.viewport = availableViewport;
                    const int renderSizeFactor = 2;
                    availableViewport.Size = loadAction.size * renderSizeFactor;
                    // Set the viewport to always render, such that the image gets updated (will be disabled once finished)
                    availableViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
                    _assignedLoadVps.Add(loadAction, availableViewport);
                }
                else
                {
                    vpMaybeAvailable = false;
                }
            }
            
            if (loadAction.viewport != null)
            {
                loadAction.loadsteps++;
                if (loadAction.viewport.PreviewReady)
                {
                    OnLoadFinished(loadAction);
                    completedLoads.Add(loadAction);
                } 
            }
        }
        foreach (var load in completedLoads)
        {
            _pendingLoadActions.Remove(load);
            load.Dispose();
        }
    }

    private void OnLoadFinished(LoadData loadData)
    {
        var image = loadData.viewport.GetTexture().GetImage();
        var texture = ImageTexture.CreateFromImage(image);
        loadData.action.Call(loadData.assetResource.ResourcePath, texture);
        loadData.viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        loadData.viewport = null;
        loadData.action = new Callable();
        _assignedLoadVps.Remove(loadData);
        _textureCache.AddToCache(GetCacheKey(loadData.assetResource.ResourcePath, loadData.previewPerspective), texture);
    }

    public bool HandlesFile(string filePath)
    {
        const string sceneFileEnding = ".tscn";
        const string compressedSceneFileEnding = ".scn";
        const string objFileEnding = ".obj";
        const string gltfFileEnding = ".gltf";
        const string glbFileEnding = ".glb";
        const string fbxFileEnding = ".fbx";
        const string colladaFileEnding = ".dae";
        const string blendFileEnding = ".blend";
        const string resFileEnding = ".res";
        const string tresFileEnding = ".tres";

        string[] validEndings =
        {
            sceneFileEnding, compressedSceneFileEnding, objFileEnding, gltfFileEnding, glbFileEnding, fbxFileEnding, colladaFileEnding, blendFileEnding, resFileEnding, tresFileEnding
        };

        // Check File Ending
        if (validEndings.Any(filePath.EndsWith))
        {
            return true;
        }

        return false;
    }

    private Texture2D GenerateDebugTexture(Vector2I size)
    {
        var b = new byte[size.X*size.Y*3];
        for (int i = 0; i < size.X*size.Y; i++)
        {
            b[i * 3] = 0;
            b[i * 3+1] = 0;
            b[i * 3+2] = 255;
        }

        var image = Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgb8, b);
        var t = ImageTexture.CreateFromImage(image);
        return t;
    }
    
    private static Perspective GetPerspective(Asset3DData.PreviewPerspective previewPerspective)
    {
        return previewPerspective switch
        {
            Asset3DData.PreviewPerspective.Default => (Perspective)Settings.GetSetting(Settings.DefaultCategory, PerspectiveSetting).AsInt32(),
            Asset3DData.PreviewPerspective.Front => Perspective.Front,
            Asset3DData.PreviewPerspective.Top => Perspective.Top,
            Asset3DData.PreviewPerspective.Back => Perspective.Back,
            Asset3DData.PreviewPerspective.Bottom => Perspective.Bottom,
            Asset3DData.PreviewPerspective.Left => Perspective.Left,
            Asset3DData.PreviewPerspective.Right => Perspective.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(Asset3DData.PreviewPerspective))
        };
    }

    public class PreviewForResourceUnhandledException : Exception
    {
    }
}

#endif