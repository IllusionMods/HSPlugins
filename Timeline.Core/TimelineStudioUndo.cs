using Studio;
using UnityEngine;
#if BEPINEX
using HarmonyLib;
#endif

namespace Timeline
{
    /// <summary>
    /// Ctrl+Z over the Timeline window is Timeline's undo. Studio listens for the same keys everywhere,
    /// so without this one press undid a Timeline edit and a Studio one at once. Anywhere else, Studio
    /// keeps its undo as before.
    /// </summary>
    public partial class Timeline
    {
        /// <summary>Whether the pointer is over the window or one of its floating windows.</summary>
        internal bool PointerOverTimeline()
        {
            return _view != null && _view.visible && _view.PointerOverAny();
        }

        internal sealed partial class View
        {
            public bool PointerOverAny()
            {
                if (PointerOverWindow())
                    return true;
                foreach (FloatWin w in _floats)
                {
                    if (w.win != null && w.win.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(w.win, Input.mousePosition, null))
                        return true;
                }
                return _twin != null && _twin.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_twin, Input.mousePosition, null);
            }
        }

#if BEPINEX
        [HarmonyPatch(typeof(UndoRedoManager), nameof(UndoRedoManager.Undo))]
        private static class UndoRedoManager_Undo_Patches
        {
            private static bool Prefix()
            {
                return _self == null || _self.PointerOverTimeline() == false;
            }
        }

        [HarmonyPatch(typeof(UndoRedoManager), nameof(UndoRedoManager.Redo))]
        private static class UndoRedoManager_Redo_Patches
        {
            private static bool Prefix()
            {
                return _self == null || _self.PointerOverTimeline() == false;
            }
        }
#endif
    }
}
