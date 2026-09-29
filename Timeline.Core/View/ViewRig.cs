using System;
using System.Collections.Generic;
using System.IO;
using KKAPI.Utilities;
using Studio;
using Timeline.Compat;
using Timeline.View;
using UnityEngine;
using UnityEngine.UI;

namespace Timeline
{
    public partial class Timeline
    {
        internal sealed partial class View
        {
            private FloatWin _rigWin;

            private FloatWin RigWin
            {
                get { return _rigWin ?? (_rigWin = MakeFloat("Rwin", "sets", "Rigs", 284f, new Vector2(620f, 330f), FillRig)); }
            }

            public void ToggleRigs()
            {
                ToggleFloat(RigWin);
            }

            private static readonly string[] _kindNames = { "Character", "Item", "Light", "Folder", "Route", "Camera" };

            /// <summary>
            /// The Rigs window: save the selected folder or item as a rig, or load one and link what it
            /// reaches outside of itself before it is made.
            /// </summary>
            private void FillRig(RectTransform body)
            {
                if (NodesConstraintsLink.Available == false)
                {
                    Note(body, "Rigs are made of NodesConstraints constraints, and NodesConstraints is not installed.");
                    return;
                }
                ObjectCtrlInfo selected = T._selectedOCI;
                string selectedName = selected == null ? "nothing selected" : selected.treeNodeObject.textName;

                RectTransform line = Line(body);
                Clickable save = Btn(line, "Save " + (CanBeRig(selected) ? "“" + selectedName + "”" : "the selected folder or item") + " as a rig…", true, SaveRigFile, null);
                if (CanBeRig(selected) == false)
                    Disable(save);
                line = Line(body);
                Btn(line, "Load a rig…", true, LoadRigFile, null);

                PendingRig rig = T._rigPending;
                if (rig == null)
                {
                    Note(body, "A rig is a folder or item with everything under it, the constraints that touch it and its picker pages. Rigs saved with ShalltyUtils' Folder Constraints Rig load here too.");
                    return;
                }

                Cap(body, "LOADED: " + rig.name.ToUpperInvariant(), rig.newToOld.Count + " objects, " + rig.constraints.Count + " constraints");
                List<List<RigConstraint>> links = rig.Links();
                if (links.Count == 0)
                    Note(body, "Nothing in it reaches outside the rig.");
                foreach (List<RigConstraint> link in links)
                {
                    RigConstraint first = link[0];
                    RectTransform row = Kit.Node("Link", body);
                    Kit.Col(row.gameObject, 3f);
                    string kind = first.missingKind >= 0 && first.missingKind < _kindNames.Length ? _kindNames[first.missingKind] : "Object";
                    Text what = Kit.Text("What", row, Kit.Escape(first.missingName) + Kit.Dim("  " + kind + " · " + link.Count + " constraint(s)"), 11, Pal.C(0xE4E7EC));
                    what.supportRichText = true;
                    RectTransform pick = Line(row);
                    bool done = first.linked != null;
                    Text to = Kit.Text("To", pick, done ? "→ " + Kit.Escape(first.linked.treeNodeObject.textName) : "→ not linked", 11, done ? Pal.Hex(0x82CC63) : Pal.Hex(0xF08A7E));
                    Kit.Flex(to.gameObject, 22f);
                    List<RigConstraint> captured = link;
                    Clickable use = Btn(pick, "Link to the selected", false, () =>
                    {
                        foreach (RigConstraint c in captured)
                            c.linked = T._selectedOCI;
                        RefreshFloats();
                    }, selected == null ? "Select the object in the scene first" : "Link it to " + selectedName);
                    if (selected == null)
                        Disable(use);
                }

                line = Line(body);
                Cb(line, "Move the linked bones' animation onto the rig", T._rigMoveTracks, () => { T._rigMoveTracks = !T._rigMoveTracks; RefreshFloats(); });
                line = Line(body);
                Clickable create = Btn(line, "Create", true, () =>
                {
                    PendingRig making = T._rigPending;
                    T._rigPending = null;
                    try
                    {
                        T.CreateRig(making);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Could not make the rig: " + e);
                    }
                    Touch();
                    RefreshFloats();
                }, null);
                create.normal = Pal.accent;
                create.hover = Pal.C(0xF0B558);
                create.GetComponentInChildren<Text>().color = Pal.onAccent;
                create.Refresh();
                Btn(line, "Cancel", false, () => { T._rigPending = null; RefreshFloats(); }, null);
                Note(body, "Constraints to an object left unlinked are skipped. Moving the animation takes the tracks that animate each linked bone and puts them on the rig object at the other end.");
            }

            private static void Disable(Clickable button)
            {
                button.disabled = true;
                button.GetComponentInChildren<Text>().color = Pal.C(0x6B6E74);
                button.Refresh();
            }

            private void SaveRigFile()
            {
                ObjectCtrlInfo root = T._selectedOCI;
                if (CanBeRig(root) == false)
                {
                    Toast("Select a folder or an item in the Workspace first.");
                    return;
                }
                string[] file = OpenFileDialog.ShowDialog("Save rig", RigFolder, "Rigs (*.fcr)|*.fcr", "fcr",
                        OpenFileDialog.OpenSaveFileDialgueFlags.OFN_LONGNAMES | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_EXPLORER);
                if (file == null || file.Length == 0 || string.IsNullOrEmpty(file[0]))
                    return;
                try
                {
                    T.SaveRig(root, file[0]);
                }
                catch (Exception e)
                {
                    Logger.LogError("Could not save the rig: " + e);
                }
            }

            private void LoadRigFile()
            {
                string[] file = OpenFileDialog.ShowDialog("Load rig", RigFolder, "Rigs (*.fcr)|*.fcr", "fcr",
                        OpenFileDialog.OpenSaveFileDialgueFlags.OFN_LONGNAMES | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST | OpenFileDialog.OpenSaveFileDialgueFlags.OFN_EXPLORER);
                if (file == null || file.Length == 0 || File.Exists(file[0]) == false)
                    return;
                try
                {
                    T._rigPending = T.ReadRig(file[0]);
                    if (T._rigPending.Links().Count == 0)
                        Toast("“" + T._rigPending.name + "” is ready: press Create.");
                    else
                        Toast("Link what “" + T._rigPending.name + "” reaches outside of itself, then press Create.");
                }
                catch (Exception e)
                {
                    T._rigPending = null;
                    Logger.LogError("Could not read the rig: " + e);
                }
                ShowFloat(RigWin);
                RefreshFloats();
            }
        }
    }
}
