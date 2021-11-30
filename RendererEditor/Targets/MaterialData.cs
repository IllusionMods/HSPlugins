using System.Collections.Generic;
using ToolBox;
using UnityEngine;

namespace RendererEditor.Targets
{
    public class MaterialData
    {
        public ITarget parent;
        public int index;

        public EditablePair<int> renderQueue; 
        public EditablePair<string> renderType; 
        public readonly Dictionary<string, EditablePair<Color>> dirtyColorProperties = new Dictionary<string, EditablePair<Color>>();
        public readonly Dictionary<string, EditablePair<float>> dirtyFloatProperties = new Dictionary<string, EditablePair<float>>();
        public readonly Dictionary<string, EditablePair<bool>> dirtyBooleanProperties = new Dictionary<string, EditablePair<bool>>();
        public readonly Dictionary<string, EditablePair<int>> dirtyEnumProperties = new Dictionary<string, EditablePair<int>>();
        public readonly Dictionary<string, EditablePair<Vector4>> dirtyVector4Properties = new Dictionary<string, EditablePair<Vector4>>();
        public readonly Dictionary<string, EditablePair<string, Texture>> dirtyTextureProperties = new Dictionary<string, EditablePair<string, Texture>>();
        public readonly Dictionary<string, EditablePair<Vector2>> dirtyTextureOffsetProperties = new Dictionary<string, EditablePair<Vector2>>();
        public readonly Dictionary<string, EditablePair<Vector2>> dirtyTextureScaleProperties = new Dictionary<string, EditablePair<Vector2>>();
        public readonly HashSet<string> disabledKeywords = new HashSet<string>();
        public readonly HashSet<string> enabledKeywords = new HashSet<string>();
    }
}