using Studio;
using System;
using System.Xml;
using UnityEngine;

namespace HSPE.AMModules
{
    public abstract class AdvancedModeModule
    {

        #region Constants
        public static readonly Color _redColor = Color.red;
        public static readonly Color _greenColor = Color.green;
        public static readonly Color _blueColor = Color.Lerp(Color.blue, Color.cyan, 0.5f);
        protected float _inc = 1f;
        #endregion

        #region Protected Variables
        internal bool _isEnabled = false;
        protected PoseController _parent;
        #endregion

        #region Private Variables
        internal static float _repeatTimer = 0f;
        internal static bool _repeatCalled = false;
        private const float _repeatBeforeDuration = 0.5f;
        protected int _incIndex = 0;
        #endregion

        #region Abstract Fields
        public abstract AdvancedModeModuleType type { get; }
        public abstract string displayName { get; }
        #endregion

        #region Public Accessors
        public virtual bool isEnabled { get { return _isEnabled; } set { _isEnabled = value; } }
        public virtual bool shouldDisplay { get { return true; } }
        #endregion

        #region Public Methods
        protected AdvancedModeModule(PoseController parent)
        {
            _parent = parent;
            _parent.onDestroy += OnDestroy;
        }

        public virtual void OnDestroy()
        {
            _parent.onDestroy -= OnDestroy;
        }
        public virtual void IKSolverOnPostUpdate() { }
        public virtual void FKCtrlOnPreLateUpdate() { }
        public virtual void IKExecutionOrderOnPostLateUpdate() { }
        //#if HONEYSELECT
        //        public virtual void CharBodyPreLateUpdate(){}
        //        public virtual void CharBodyPostLateUpdate(){}
        //#elif KOIKATSU
        //        public virtual void CharacterPreLateUpdate() { }
        //        public virtual void CharacterPostLateUpdate() { }
        //#endif
        public virtual void OnCharacterReplaced() { }
        public virtual void OnLoadClothesFile() { }
#if HONEYSELECT
        public virtual void OnCoordinateReplaced(CharDefine.CoordinateType coordinateType, bool force){}
#elif KOIKATSU
        public virtual void OnCoordinateReplaced(ChaFileDefine.CoordinateType coordinateType, bool force) { }
#endif
        public virtual void OnParentage(TreeNodeObject parent, TreeNodeObject child) { }
        public virtual void DrawAdvancedModeChanged() { }
        public virtual void UpdateGizmos() { }
        #endregion

        #region Abstract Methods
        public abstract void GUILogic();
        public abstract int SaveXml(XmlTextWriter xmlWriter);
        public abstract bool LoadXml(XmlNode xmlNode);
        #endregion

        #region Protected Methods
        protected bool RepeatControl()
        {
            _repeatCalled = true;
            if (Mathf.Approximately(_repeatTimer, 0f))
                return true;
            return Event.current.type == EventType.Repaint && _repeatTimer > _repeatBeforeDuration;
        }

        protected void IncEditor(int maxHeight = 76, bool label = false)
        {
            IncEditor(ref _incIndex, out _inc, maxHeight, label);
        }

        protected void IncEditor(ref int incIndex, out float inc, int maxHeight = 76, bool label = false)
        {
            GUILayout.BeginVertical();
            if (label)
            {
                GUILayout.Label("10^1", GUI.skin.box, GUILayout.MaxWidth(45));
                if (GUILayout.Button("+", GUILayout.MaxWidth(45)))
                    incIndex = Mathf.Clamp(incIndex + 1, -5, 1);

                GUILayout.BeginHorizontal(GUILayout.MaxWidth(45));
                GUILayout.FlexibleSpace();
                incIndex = Mathf.RoundToInt(GUILayout.VerticalSlider(incIndex, 1f, -5f, GUILayout.MaxHeight(maxHeight)));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (GUILayout.Button("-", GUILayout.MaxWidth(45)))
                    incIndex = Mathf.Clamp(incIndex - 1, -5, 1);
                GUILayout.Label("10^-5", GUI.skin.box, GUILayout.MaxWidth(45));
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(40));

                GUILayout.BeginVertical();
                if (GUILayout.Button("+", GUILayout.MaxWidth(20), GUILayout.Height(37)))
                    incIndex = Mathf.Clamp(incIndex + 1, -5, 1);
                GUILayout.Space(1);
                if (GUILayout.Button("-", GUILayout.MaxWidth(20), GUILayout.Height(37)))
                    incIndex = Mathf.Clamp(incIndex - 1, -5, 1);
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
                incIndex = Mathf.RoundToInt(GUILayout.VerticalSlider(incIndex, 1f, -5f, GUILayout.MaxHeight(maxHeight)));
                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
            }
            inc = Mathf.Pow(10, incIndex);
            GUILayout.EndVertical();
        }

        protected Vector3 Vector3Editor(Vector3 value, string xLabel = "X:\t", string yLabel = "Y:\t", string zLabel = "Z:\t", Action onValueChanged = null, bool scaleEditor = false)
        {
            return Vector3Editor(value, _redColor, _greenColor, _blueColor, _inc, xLabel, yLabel, zLabel, onValueChanged, scaleEditor);
        }

        protected Vector3 Vector3Editor(Vector3 value, float customInc, string xLabel = "X:\t", string yLabel = "Y:\t", string zLabel = "Z:\t", Action onValueChanged = null, bool scaleEditor = false)
        {
            return Vector3Editor(value, _redColor, _greenColor, _blueColor, customInc, xLabel, yLabel, zLabel, onValueChanged, scaleEditor);
        }

        protected Vector3 Vector3Editor(Vector3 value, Color color, string xLabel = "X:\t", string yLabel = "Y:\t", string zLabel = "Z:\t", Action onValueChanged = null, bool scaleEditor = false)
        {
            return Vector3Editor(value, color, color, color, _inc, xLabel, yLabel, zLabel, onValueChanged, scaleEditor);
        }

        protected Vector3 Vector3Editor(Vector3 value, Color color, float customInc, string xLabel = "X:\t", string yLabel = "Y:\t", string zLabel = "Z:\t", Action onValueChanged = null, bool scaleEditor = false)
        {
            return Vector3Editor(value, color, color, color, customInc, xLabel, yLabel, zLabel, onValueChanged, scaleEditor);
        }

        protected Vector3 Vector3Editor(Vector3 value, Color xColor, Color yColor, Color zColor, float customInc, string xLabel = "X:\t", string yLabel = "Y:\t", string zLabel = "Z:\t", Action onValueChanged = null, bool scaleEditor = false)
        {
            string customIncString = customInc.ToString("+0.#####;-0.#####");
            string minusCustomIncString = (-customInc).ToString("+0.#####;-0.#####");

            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = xColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(xLabel, GUILayout.ExpandWidth(false));

            string oldValue = value.x.ToString("0.00000");
            string newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value.x = res;
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));

            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value.x = (scaleEditor ? 1 : 0);
                onValueChanged?.Invoke();
            }

            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value -= customInc * Vector3.right;
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value += customInc * Vector3.right;
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUI.color = yColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(yLabel, GUILayout.ExpandWidth(false));

            oldValue = value.y.ToString("0.00000");
            newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value.y = res;
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));

            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value.y = (scaleEditor ? 1 : 0);
                onValueChanged?.Invoke();
            }

            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value -= customInc * Vector3.up;
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value += customInc * Vector3.up;
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUI.color = zColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(zLabel, GUILayout.ExpandWidth(false));

            oldValue = value.z.ToString("0.00000");
            newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value.z = res;
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));

            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value.z = (scaleEditor ? 1 : 0);
                onValueChanged?.Invoke();
            }

            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value -= customInc * Vector3.forward;
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value += customInc * Vector3.forward;
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }


        protected Quaternion QuaternionEditor(Quaternion value, float customInc, string xLabel = "X (Pitch):\t", string yLabel = "Y (Yaw):\t", string zLabel = "Z (Roll):\t", Action onValueChanged = null)
        {
            return QuaternionEditor(value, _redColor, _greenColor, _blueColor, customInc, xLabel, yLabel, zLabel, onValueChanged);
        }

        protected Quaternion QuaternionEditor(Quaternion value, Color xColor, Color yColor, Color zColor, float customInc, string xLabel = "X (Pitch):\t", string yLabel = "Y (Yaw):\t", string zLabel = "Z (Roll):\t", Action onValueChanged = null)
        {
            string customIncString = customInc.ToString("+0.#####;-0.#####");
            string minusCustomIncString = (-customInc).ToString("+0.#####;-0.#####");

            GUILayout.BeginVertical();
            Color c = GUI.color;
            GUI.color = xColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(xLabel, GUILayout.ExpandWidth(false));

            string oldValue = value.eulerAngles.x.ToString("0.00000");
            string newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value = Quaternion.Euler(res, value.eulerAngles.y, value.eulerAngles.z);
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value = Quaternion.Euler(0f, value.eulerAngles.y, value.eulerAngles.z);
                onValueChanged?.Invoke();
            }
            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(-customInc, Vector3.right);
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(customInc, Vector3.right);
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUI.color = yColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(yLabel, GUILayout.ExpandWidth(false));

            oldValue = value.eulerAngles.y.ToString("0.00000");
            newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value = Quaternion.Euler(value.eulerAngles.x, res, value.eulerAngles.z);
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value = Quaternion.Euler(value.eulerAngles.x, 0f, value.eulerAngles.z);
                onValueChanged?.Invoke();
            }
            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(-customInc, Vector3.up);
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(customInc, Vector3.up);
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            GUI.color = zColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(zLabel, GUILayout.ExpandWidth(false));

            oldValue = value.eulerAngles.z.ToString("0.00000");
            newValue = GUILayout.TextField(oldValue, GUILayout.MaxWidth(60));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                {
                    value.z = res;
                    value = Quaternion.Euler(value.eulerAngles.x, value.eulerAngles.y, res);
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
            if (GUILayout.Button("0", GUILayout.Width(20f)))
            {
                value.z = 0f;
                value = Quaternion.Euler(value.eulerAngles.x, value.eulerAngles.y, 0f);
                onValueChanged?.Invoke();
            }
            if (GUILayout.RepeatButton(minusCustomIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(-customInc, Vector3.forward);
                if (onValueChanged != null)
                    onValueChanged();
            }
            if (GUILayout.RepeatButton(customIncString) && RepeatControl())
            {
                value *= Quaternion.AngleAxis(customInc, Vector3.forward);
                if (onValueChanged != null)
                    onValueChanged();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }

        protected float FloatEditor(float value, float min, float max, string label = "Label\t", string format = "0.000", float inputWidth = 40f, Func<float, float> onReset = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            value = GUILayout.HorizontalSlider(value, min, max);
            string oldValue = value.ToString("0.000");
            string newValue = GUILayout.TextField(oldValue, GUILayout.Width(inputWidth));
            if (oldValue != newValue)
            {
                float res;
                if (float.TryParse(newValue, out res))
                    value = res;
            }

            Color c = GUI.color;
            GUI.color = Color.red;
            if (onReset != null && GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
                value = onReset(value);
            GUI.color = c;
            GUILayout.EndHorizontal();
            return value;
        }
        #endregion
    }

    public enum AdvancedModeModuleType
    {
        BonesEditor = 0,
        CollidersEditor,
        BoobsEditor,
        DynamicBonesEditor,
        BlendShapes,
        IK,
        ClothesTransformEditor,
    }
}
