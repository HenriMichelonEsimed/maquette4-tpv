// CacheItem.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable

using Godot;

namespace AssetPlacer;

public partial class CacheItem : GodotObject
{
    public Resource resource;
    public Texture2D texture;

    public CacheItem()
    {
    }

    public CacheItem(Resource resource, Texture2D texture)
    {
        this.resource = resource;
        this.texture = texture;
    }
}    
#endif