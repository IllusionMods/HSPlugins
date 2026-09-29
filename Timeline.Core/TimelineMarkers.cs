using System;
using System.Collections.Generic;
using System.Xml;
using Studio;
using UILib;
using UILib.EventHandlers;
using UILib.ContextMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    /// <summary>
    /// Named times along the ruler, Blender's markers.
    ///
    /// They animate nothing. What they are for is the part of animating that happens in your head: this
    /// is where the hand lands, this is where the line is spoken, this is where the cut goes. Without
    /// them those times live in a notepad next to the game, and every edit that shifts the timing makes
    /// the notepad wrong.
    /// </summary>
    public partial class Timeline
    {
        internal sealed class TimelineMarker
        {
            public float time;
            public string name = "";
        }

        /// <summary>Kept sorted by time, so next and previous are a step through the list.</summary>
        private readonly List<TimelineMarker> _markers = new List<TimelineMarker>();

        /// <summary>How close to a marker a shift snapped drag has to land to take it.</summary>
        private const float _markerSnapPixels = 12f;

        #region Editing
        private void AddMarker(float time)
        {
            RecordUndo("Add marker");
            TimelineMarker marker = new TimelineMarker
            {
                time = Mathf.Clamp(time, 0f, _duration),
                name = "Marker " + (_markers.Count + 1)
            };
            _markers.Add(marker);
            SortMarkers();
            UpdateMarkers();
        }

        private void SortMarkers()
        {
            _markers.Sort((a, b) => a.time.CompareTo(b.time));
        }

        /// <summary>
        /// Moves the playhead to the next or previous marker. With none ahead it stays put rather than
        /// wrapping around, because a wrap in a list of times reads as the timeline having jumped.
        /// </summary>
        private void SeekMarker(bool forward)
        {
            if (_markers.Count == 0)
            {
                Logger.LogMessage("No markers. Right click the ruler to add one.");
                return;
            }

            TimelineMarker found = null;
            foreach (TimelineMarker marker in _markers)
            {
                if (forward && marker.time > _playbackTime + 0.0005f)
                {
                    found = marker;
                    break;
                }
                if (forward == false && marker.time < _playbackTime - 0.0005f)
                    found = marker; // the list is sorted, so the last one under the cursor is the nearest
            }

            if (found == null)
            {
                Logger.LogMessage(forward ? "No marker after the cursor." : "No marker before the cursor.");
                return;
            }
            SeekPlaybackTime(found.time);
        }

        /// <summary>
        /// The marker nearest a time, within a grab of it, or null. Used by snapping, which is why the
        /// tolerance is in pixels: what counts as near depends on how far the timeline is zoomed in.
        /// </summary>
        private bool TryNearestMarker(float time, out float markerTime)
        {
            markerTime = time;
            if (_markers.Count == 0)
                return false;

            const float pixelsPerSecond = 60f;
            float best = float.PositiveInfinity;
            foreach (TimelineMarker marker in _markers)
            {
                float distance = Mathf.Abs(marker.time - time) * pixelsPerSecond;
                if (distance >= best || distance > _markerSnapPixels)
                    continue;
                best = distance;
                markerTime = marker.time;
            }
            return best < float.PositiveInfinity;
        }
        #endregion

        #region Display

        #endregion

        #region Scene data
        private void WriteMarkers(XmlTextWriter writer)
        {
            foreach (TimelineMarker marker in _markers)
            {
                writer.WriteStartElement("marker");
                writer.WriteAttributeString("time", XmlConvert.ToString(marker.time));
                writer.WriteAttributeString("name", marker.name);
                writer.WriteEndElement();
            }
        }

        private void ReadMarkers(XmlNode root)
        {
            _markers.Clear();
            if (root != null)
            {
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.Name != "marker" || node.Attributes["time"] == null)
                        continue;
                    try
                    {
                        _markers.Add(new TimelineMarker
                        {
                            time = XmlConvert.ToSingle(node.Attributes["time"].Value),
                            name = node.Attributes["name"] == null ? "" : node.Attributes["name"].Value
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError("Couldn't load a marker:\n" + node.OuterXml + "\n" + e);
                    }
                }
            }
            SortMarkers();
            UpdateMarkers();
        }
        #endregion
    }
}
