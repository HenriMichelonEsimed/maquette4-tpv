// PlacementPlane.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable
using Godot;

namespace AssetPlacer;

[Tool]
public partial class PlacementPlane : MeshInstance3D
{
    private bool _tweening;
    private StandardMaterial3D _material;
    private float _maxAlpha;
    private Tween _alphaTweener;

    private const float FadeDuration = 0.8f;
    public bool Tweening => _alphaTweener != null;

    public override void _Ready()
    {
        if (!Engine.IsEditorHint()) return;
        _material = (StandardMaterial3D)((QuadMesh)Mesh).Material;
        _material.AlbedoColor = new Color("e65c00a0");
        _maxAlpha = _material.AlbedoColor.A;
        Visible = false;
    }

    public void SetVisible(bool visible)
    {
        if (_alphaTweener == null)
        {
            Visible = visible;
            SetAlpha(_maxAlpha);
        }
        else if (visible)
        {
            SetAlpha(_maxAlpha);
            Visible = true;
            _alphaTweener.Kill();
            _alphaTweener = null;
        }
    }

    public void ShowTemporarily()
    {
        if (!IsInsideTree()) return;
        Visible = true;
        _material.AlbedoColor = new Color("e65c00a0");
        _alphaTweener?.Kill();
        _alphaTweener = CreateTween();
        _alphaTweener.TweenMethod(new Callable(this, nameof(SetAlpha)), _maxAlpha, 0.0f, FadeDuration);
        _alphaTweener.Finished += () =>
        {
            Visible = false;
            _alphaTweener.Kill();
            _alphaTweener = null;
        };
    }

    public void SetAlpha(float val)
    {
        var col = _material.AlbedoColor;
        col.A = val;
        _material.AlbedoColor = col;
    }
}
#endif