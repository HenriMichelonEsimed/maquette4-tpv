// PreviewCamera3D.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable

using Godot;

namespace AssetPlacer;

[Tool]
public partial class PreviewCamera3D : Camera3D
{
	private bool _updated = false;
	
	[Signal]
	public  delegate void UpdatedEventHandler();
	public override void _Process(double delta)
	{
		if (!_updated)
		{
			EmitSignal(SignalName.Updated);
			_updated = true;
		}
	}

	public void SetTransform(Transform3D transform)
	{
		Transform = transform;
		_updated = false;
	}
}

#endif
