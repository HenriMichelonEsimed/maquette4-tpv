// SnappingUi.cs
// Copyright (c) 2023 CookieBadger. All Rights Reserved.

#if TOOLS
#nullable disable

using System.Globalization;
using Godot;

namespace AssetPlacer;
[Tool]
public partial class SnappingUi : Control
{
    private const string TranslateSnapStepSaveKey = "translate_snap_step";
    private const string TranslateShiftSnapStepSaveKey = "translate_shift_snap_step";
    private const string SnapOffsetSaveKey = "snap_offset";
    private const string SnapEnabledSaveKey = "snapping_enabled";
    
    [Export] public NodePath translateSnapCheckbox;
    [Export] public NodePath translateSnapStepEdit;
    [Export] public NodePath translateShiftSnapStepEdit;
    [Export] public NodePath translateSnapOffsetEditX;
    [Export] public NodePath translateSnapOffsetEditY;
    [Export] public NodePath offsetFromSelectedButton;
    [Export] public NodePath resetOffsetButton;
    private CheckBox _translateSnapCheckbox;
    private LineEdit _translateSnapStepEdit;
    private LineEdit _translateShiftSnapStepEdit;
    private LineEdit _translateSnapOffsetEdit1;
    private LineEdit _translateSnapOffsetEdit2;
    public Button _offsetFromSelectedButton;
    public Button _resetOffsetButton;
    
    [Signal]
    public  delegate void TranslateSnapStepChangedEventHandler();
    
    [Signal]
    public  delegate void TranslateOffsetChangedEventHandler();
    
    [Signal]
    public  delegate void TranslateShiftSnapStepChangedEventHandler();

    public float TranslateSnapStep { get; private set; } = 1f;
    public float TranslateShiftSnapStep { get; private set; } = 0.1f;
    public Vector2 TranslateSnapOffset { get; private set; } = Vector2.Zero;

    public void Init()
    {
        _translateSnapStepEdit = GetNode<LineEdit>(translateSnapStepEdit);
        _translateShiftSnapStepEdit = GetNode<LineEdit>(translateShiftSnapStepEdit);
        _translateSnapCheckbox = GetNode<CheckBox>(translateSnapCheckbox);
        _offsetFromSelectedButton = GetNode<Button>(offsetFromSelectedButton);
        _translateSnapOffsetEdit1 = GetNode<LineEdit>(translateSnapOffsetEditX);
        _translateSnapOffsetEdit2 = GetNode<LineEdit>(translateSnapOffsetEditY);
        _resetOffsetButton = GetNode<Button>(resetOffsetButton);
        _resetOffsetButton.Pressed += () => SetTranslateOffset(Vector2.Zero);

        _translateSnapCheckbox.Toggled += OnTranslateSnapToggled;
        _translateSnapStepEdit.TextSubmitted += OnTranslateSnapStepSubmit;
        _translateShiftSnapStepEdit.TextSubmitted += OnTranslateShiftSnapStepSubmit;
        _translateSnapOffsetEdit1.TextSubmitted += (txt) => OnTranslateSnapOffsetSubmit(txt,_translateSnapOffsetEdit1);
        _translateSnapOffsetEdit2.TextSubmitted += (txt) => OnTranslateSnapOffsetSubmit(txt,_translateSnapOffsetEdit2);
        _translateSnapStepEdit.Text = TranslateSnapStep.ToString(CultureInfo.InvariantCulture);
        _translateShiftSnapStepEdit.Text = TranslateShiftSnapStep.ToString(CultureInfo.InvariantCulture);
        _translateSnapOffsetEdit1.Text = TranslateSnapOffset.X.ToString(CultureInfo.InvariantCulture);
        _translateSnapOffsetEdit2.Text = TranslateSnapOffset.Y.ToString(CultureInfo.InvariantCulture);
        
        _offsetFromSelectedButton.Disabled = true;
        _offsetFromSelectedButton.TooltipText = "Set offset such that the selected object is on the grid";
    }

    public void ApplyTheme(Control baseControl)
    {
        var selectIcon = baseControl.GetThemeIcon("EditorPositionUnselected", "EditorIcons");
        _offsetFromSelectedButton.Icon = selectIcon;
        _offsetFromSelectedButton.Text = "";
        var resetIcon = baseControl.GetThemeIcon("Reload", "EditorIcons");
        _resetOffsetButton.Icon = resetIcon;
        _resetOffsetButton.Text = "";
    }
    
    private void OnTranslateSnapStepSubmit(string newtext)
    {
        float val;
        if (AssetPlacerUi.TryParseFloat(newtext, out val) && val != 0 && val != TranslateSnapStep)
        {
            SetTranslateSnapStep(val);
            AssetPlacerPersistence.StoreSceneData(TranslateSnapStepSaveKey, TranslateSnapStep);
            AssetPlacerPersistence.StoreSceneData(TranslateShiftSnapStepSaveKey, TranslateShiftSnapStep);
            EmitSignal(SignalName.TranslateSnapStepChanged);
        }
        _translateSnapStepEdit.Text = TranslateSnapStep.ToString(CultureInfo.InvariantCulture);
        _translateSnapStepEdit.ReleaseFocus();
    }

    private void SetTranslateSnapStep(float val)
    {
        TranslateSnapStep = val;
        _translateSnapStepEdit.Text = TranslateSnapStep.ToString(CultureInfo.InvariantCulture);
        TranslateShiftSnapStep = 0.1f * TranslateSnapStep;
        _translateShiftSnapStepEdit.Text = (0.1f * TranslateSnapStep).ToString(CultureInfo.InvariantCulture);
        _translateShiftSnapStepEdit.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
    }

    public bool TranslateSnappingActive()
    {
        if(_translateSnapCheckbox == null) return false;
        return _translateSnapCheckbox.ButtonPressed;
    }

    public void OnTranslateSnapToggled(bool enabled)
    {
        AssetPlacerPersistence.StoreSceneData(SnapEnabledSaveKey, enabled);
    }
    
    private void OnTranslateShiftSnapStepSubmit(string newtext)
    {
        float val;
        if (AssetPlacerUi.TryParseFloat(newtext, out val) && val != 0)
        {
            SetTranslateShiftSnapStep(val);
            AssetPlacerPersistence.StoreSceneData(TranslateShiftSnapStepSaveKey, TranslateShiftSnapStep);
            EmitSignal(SignalName.TranslateShiftSnapStepChanged);
        }
        _translateShiftSnapStepEdit.Text = TranslateShiftSnapStep.ToString(CultureInfo.InvariantCulture);
        _translateShiftSnapStepEdit.ReleaseFocus();
    }

    private void SetTranslateShiftSnapStep(float val)
    {
        TranslateShiftSnapStep = val;
        if (Mathf.IsEqualApprox(TranslateShiftSnapStep, TranslateSnapStep * 0.1f))
        {
            _translateShiftSnapStepEdit.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        }
        else {
            _translateShiftSnapStepEdit.RemoveThemeColorOverride("font_color");
        }
        _translateShiftSnapStepEdit.Text = TranslateShiftSnapStep.ToString(CultureInfo.InvariantCulture);
    }

    private void OnTranslateSnapOffsetSubmit(string newtext, Control editedControl)
    {
        float val;
        if (AssetPlacerUi.TryParseFloat(newtext, out val))
        {
            bool editedFirst = editedControl == _translateSnapOffsetEdit1;
            TranslateSnapOffset =
                editedFirst ? new Vector2(val, TranslateSnapOffset.Y) : new Vector2(TranslateSnapOffset.X, val);
            EmitSignal(SignalName.TranslateOffsetChanged); // this event eventually leads to SetTranslateOffset to be called.
            // saving is done there.
        }
        editedControl.ReleaseFocus();
    }

    public void OffsetFromSelectedButtonDisabled(bool value)
    {
        _offsetFromSelectedButton.Disabled = value;
    }

    public void SetTranslateOffset(Vector2 vector)
    {
        TranslateSnapOffset = vector;
        AssetPlacerPersistence.StoreSceneData(SnapOffsetSaveKey, TranslateSnapOffset);
        _translateSnapOffsetEdit1.Text = vector.X.ToString(CultureInfo.InvariantCulture);
        _translateSnapOffsetEdit2.Text = vector.Y.ToString(CultureInfo.InvariantCulture);
    }

    public void SetOffsetLabelTexts(string text1, string text2)
    {
        var label1 = GetNode<Label>("%SnapOffsetLabel1");
        var label2 = GetNode<Label>("%SnapOffsetLabel2");
        label1.Text = text1;
        label2.Text = text2;
    }

    public void OnSceneChanged()
    {
        // enabled
        var snapEnabled = AssetPlacerPersistence.LoadSceneData(SnapEnabledSaveKey, false, Variant.Type.Bool).AsBool();
        _translateSnapCheckbox.SetPressedNoSignal(snapEnabled);
        
        // offset
        var translateSnapOffset =
            AssetPlacerPersistence.LoadSceneData(SnapOffsetSaveKey, Vector2.Zero, Variant.Type.Vector2).AsVector2();
        SetTranslateOffset(translateSnapOffset);
        
        // snap step
        var snapStep= AssetPlacerPersistence.LoadSceneData(TranslateSnapStepSaveKey, 1f, Variant.Type.Float).AsSingle();
        SetTranslateSnapStep((snapStep));
        
        // shift snap step
        var shiftSnapStep = AssetPlacerPersistence.LoadSceneData(TranslateShiftSnapStepSaveKey, TranslateSnapStep*0.1f, Variant.Type.Float).AsSingle();
        SetTranslateShiftSnapStep(shiftSnapStep);
    }

    public void MultiplySnapSteps(float factor)
    {
        var shiftStep = TranslateShiftSnapStep * factor; // calculate first, so TranslateShiftSnapStep is unchanged
        SetTranslateSnapStep(TranslateSnapStep * factor);
        SetTranslateShiftSnapStep(shiftStep);
        EmitSignal(SignalName.TranslateSnapStepChanged);
    }
}
#endif