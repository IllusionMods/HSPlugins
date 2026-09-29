using System.Collections.Generic;
using System.Globalization;
using Timeline.Graph;
using Timeline.View;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            private float _randomAmount = 0.001f;
            private bool _randomValues = true;
            private bool _randomTimes;

            /// <summary>An amount per component, added to or taken from every selected key.</summary>
            private void OpenOffset(object sample)
            {
                int dim = CurveComponents.Count(sample);
                if (dim == 0 || sample is bool)
                    return;
                RectTransform dialog = OpenDialog("Add to or subtract from the selected keys", winW / 2f - 150f, 80f);
                RectTransform line = Line(dialog);
                var fields = new InputField[dim];
                for (int c = 0; c < dim; ++c)
                    fields[c] = Fld(line, dim > 1 ? CurveComponents.Name(sample, c) : "Amount", dim > 1 ? _axisColors[c] : 0x9A9DA2, "0", null, null);
                Note(dialog, sample is Quaternion ? "In degrees, per axis." : "Each component of every selected key, whichever track it is on.");
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                foreach (bool subtract in new[] { true, false })
                {
                    bool captured = subtract;
                    Btn(buttons, subtract ? "− Subtract" : "+ Add", false, () =>
                    {
                        var amounts = new float[dim];
                        for (int c = 0; c < dim; ++c)
                            float.TryParse(fields[c].text, NumberStyles.Float, CultureInfo.InvariantCulture, out amounts[c]);
                        CloseMenu();
                        T.OffsetSelectedValues(amounts, captured);
                        Touch();
                    }, null);
                }
                PlaceDialog(dialog);
            }

            /// <summary>A little randomness in the selected keys' values or times.</summary>
            private void OpenRandomize()
            {
                RectTransform dialog = OpenDialog("Randomize the selected keys", winW / 2f - 150f, 80f);
                RectTransform line = Line(dialog);
                InputField amount = Fld(line, "Up to", 0x9A9DA2, _randomAmount.ToString("0.#####", CultureInfo.InvariantCulture), null, null);
                line = Line(dialog);
                Cb(line, "Values", _randomValues, () => _randomValues = !_randomValues);
                Cb(line, "Times", _randomTimes, () => _randomTimes = !_randomTimes);
                Note(dialog, "Each key moves by a random amount up to this either way: in the value's own units (degrees for a rotation), or in seconds.");
                RectTransform buttons = DialogButtons(dialog);
                Btn(buttons, "Cancel", false, CloseMenu, null);
                Clickable ok = Btn(buttons, "Randomize", false, () =>
                {
                    float parsed;
                    if (float.TryParse(amount.text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed > 0f)
                        _randomAmount = parsed;
                    CloseMenu();
                    T.RandomizeSelectedKeys(_randomAmount, _randomValues, _randomTimes);
                    Touch();
                }, null);
                ok.normal = Pal.accent;
                ok.hover = Pal.C(0xF0B558);
                ok.GetComponentInChildren<Text>().color = Pal.onAccent;
                ok.Refresh();
                PlaceDialog(dialog);
            }

            /// <summary>The curves saved to files: one to give the selected keys, and saving or deleting them.</summary>
            private List<MenuItem> SavedCurveItems(Keyframe k)
            {
                List<CurvePreset> presets = T.LoadCurvePresets();
                var items = new List<MenuItem> { new MenuItem { head = "SAVED CURVES" } };
                if (presets.Count == 0)
                    items.Add(new MenuItem { label = "None saved yet", disabled = true });
                foreach (CurvePreset preset in presets)
                {
                    CurvePreset captured = preset;
                    items.Add(new MenuItem { label = preset.name, act = () => ApplyCurve(captured.curve, captured.name) });
                }
                items.Add(new MenuItem { sep = true });
                items.Add(new MenuItem
                {
                    label = "Save this key's curve…",
                    act = () => RenameDialog("Save the curve as", "Curve " + (presets.Count + 1), name => T.SaveCurvePreset(name, k.curve))
                });
                if (presets.Count != 0)
                {
                    var delete = new List<MenuItem>();
                    foreach (CurvePreset preset in presets)
                    {
                        CurvePreset captured = preset;
                        delete.Add(new MenuItem
                        {
                            label = preset.name,
                            act = () => Confirm("Delete “" + captured.name + "”", "The saved curve's file is removed. Keys already using it keep their curve.", "Delete", () => DeleteCurvePreset(captured))
                        });
                    }
                    items.Add(new MenuItem { label = "Delete", sub = delete });
                }
                return items;
            }
        }
    }
}
