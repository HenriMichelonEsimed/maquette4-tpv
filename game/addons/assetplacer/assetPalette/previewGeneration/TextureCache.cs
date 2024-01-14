// TextureCache.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using System.Linq;
using Godot;
using Godot.Collections;

namespace AssetPlacer;

public class TextureCache
{
    private const int MaxCacheSize = 2000;
    private const int CacheRemoveStep = 50;
    private Godot.Collections.Dictionary<string, Texture2D> _cache = new(); // Godot Dictionary preserves order!

    public Texture2D CheckCache(string key)
    {
        if (_cache.ContainsKey(key))
        {
            var texture = _cache[key];
            // Append at end of cache
            _cache.Remove(key);
            _cache.Add(key, texture);
            return texture;
        }

        return null;
    }

    public void AddToCache(string key, ImageTexture texture)
    {
        _cache[key] = texture;
        if (_cache.Count > MaxCacheSize)
        {
            var oldestKeys = _cache.Keys.Take(CacheRemoveStep);
            foreach (var oldKey in oldestKeys)
            {
                _cache.Remove(oldKey);
            }
        }
    }
}
#endif