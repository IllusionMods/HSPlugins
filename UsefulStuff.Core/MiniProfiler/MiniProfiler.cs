using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Graphic = kleberswf.tools.util.Graphic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace kleberswf.tools.miniprofiler
{
#if UNITY_5
	[HelpURL("http://kleber-swf.com/docs/mini-profiler/#mini-profiler")]
#endif
    public class MiniProfiler : LayoutElement, IPointerClickHandler
    {
        private const int Count = 40;
        private const int BlockSize = 5;
        private const int TextureWidth = Count * BlockSize;
        private const int TextureHeight = 64;
        public static readonly Vector2 DefaultSize = new Vector2(200, 64);

        private AbstractValueProvider _valueProvider;
        private float[] _history;
        private float _minValue;
        private float _maxValue;
        private float _avgValue;

        private float _nextValue;
        private float _nextMaxValue;
        private float _nextMinValue;
        private float _total;
        private int _samples;

        public Image Image;
        public Text Text;
        private Graphic _graphic;
        private float _elapsed;
        private int _textHeight = 12;

        private RectTransform _imageRectTransform;

        /// <summary>Interval in seconds to wait to read the watched variable. Default: <code>0.1f</code>.</summary>
        [Tooltip("Interval in seconds to wait to read the watched variable.")]
        public float ReadInterval = 0.1f;

        [SerializeField]
        [Tooltip("Whether the panel is collapsed (true) or expanded (false).")]
        private bool _collapsed;

        /// <summary>Whether the panel is collapsed (true) or expanded (false). Default: <code>false</code>.</summary>
        public bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                _collapsed = value;
                if (_imageRectTransform == null) return;
                if (_collapsed)
                {
                    var size = _imageRectTransform.sizeDelta;
                    size.y = _textHeight;
                    _imageRectTransform.sizeDelta = size;
                    if (_graphic != null) _graphic.Clear(BackgroundColor, true);
                }
                else
                {
                    var size = _imageRectTransform.sizeDelta;
                    size.y = _expandedHeight;
                    _imageRectTransform.sizeDelta = size;
                }
            }
        }

        /// <summary>Whether the panel toggle its collapsed state when touched. Default <code>true</code>.</summary>
        [Tooltip("Whether the panel toggles its collapsed state when touched.")]
        public bool TouchToggleCollapsed = true;

        /// <summary>Toggles the <see cref="Collapsed"/> property.</summary>
        public void ToggleCollapsed()
        {
            Collapsed = !_collapsed;
        }

        [SerializeField]
        private float _expandedHeight;

        [SerializeField]
        [Tooltip("Whether the panel should show a text with values. A Text should be set to this work properly.")]
        private bool _showText = true;

        /// <summary>Whether the panel should show a text with values. A <see cref="Text"/> should be set to this work properly.</summary>
        public bool ShowText
        {
            get { return _showText; }
            set
            {
                _showText = value && Text;
                if (Text != null) Text.enabled = _showText;
            }
        }

        public override void OnEnable()
        {
            if (_valueProvider == null)
            {
                _valueProvider = GetComponent<AbstractValueProvider>();
                if (Application.isPlaying && _valueProvider == null)
                {
                    Debug.LogError("There is no value provider for Profile Panel '" + gameObject.name + "'. Disabling it.");
                    gameObject.SetActive(false);
                    return;
                }
            }

            if (_graphic != null) return;
            _graphic = new Graphic(TextureWidth, TextureHeight, BackgroundColor);

            if (Image == null)
            {
                Image = GetComponent<Image>();
                if (Image == null)
                {
                    Image = gameObject.AddComponent<Image>();
                    Image.rectTransform.sizeDelta = DefaultSize;
                }
            }
            _imageRectTransform = Image.rectTransform;
            Image.sprite = Sprite.Create(_graphic.Texture, new Rect(0, 0, TextureWidth, TextureHeight), Vector2.zero);

            if (Text != null)
            {
                Text.color = TitleColor;
                _textHeight = Mathf.FloorToInt(Text.rectTransform.rect.height);
            }
            else _showText = false;

            _elapsed = ReadInterval;
            _history = new float[Count + 1];

            minHeight = _textHeight;
            Collapsed = _collapsed;
        }

        private void Update()
        {
#if UNITY_EDITOR
			if (!_valueProvider) return;
#endif
            _valueProvider.Refresh(ReadInterval);
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < ReadInterval) return;
            _elapsed = 0f;

            var textHeight = _showText && _imageRectTransform.rect.height > 0
                ? Mathf.CeilToInt(_textHeight * TextureHeight / _imageRectTransform.rect.height)
                : 0;

            var ratio = _maxValue != 0f ? (TextureHeight - textHeight - 5) / _maxValue : 0f;
            var m = (int)(_avgValue * ratio);

            _graphic.Clear(BackgroundColor, false);
            for (var i = Count; i > 0; i--)
                SetValue(i, _history[i - 1], ratio, m, textHeight);
            _graphic.Apply();

            _nextValue = _valueProvider.Value;
            AddToHistory(0, _nextValue);

            _minValue = _nextMinValue;
            _maxValue = _nextMaxValue;
            _avgValue = _total / _samples;
            if (_showText) UpdateTextLine();

            _nextMaxValue = 0f;
            _nextMinValue = float.MaxValue;
            _total = 0f;
            _samples = 0;
        }

        private void AddToHistory(int index, float value)
        {
            _history[index] = value;
            if (value < _nextMinValue) _nextMinValue = value;
            if (value > _nextMaxValue) _nextMaxValue = value;
            _total += value;
            _samples++;
        }

        private void SetValue(int index, float value, float ratio, int m, int textHeight)
        {
            AddToHistory(index, value);
            if (_collapsed) return;
#if UNITY_4_7
			_graphic.DrawRect(TextureWidth - index * BlockSize, textHeight, BlockSize, (int)(value * ratio) + textHeight,
				Color32.Lerp(_minValueColor, _maxValueColor, value / _maxValue), m + textHeight, _averageValueColor);
#else
            _graphic.DrawRect(TextureWidth - index * BlockSize, textHeight, BlockSize, (int)(value * ratio) + textHeight,
                Color32.LerpUnclamped(_minValueColor, _maxValueColor, value / _maxValue), m + textHeight, _averageValueColor);
#endif
        }

        #region Text and Colors

        private const string ColorizedStringFormat =
            "{0} <color=#{1}>▼{4}</color> <color=#{2}>■{5}</color> <color=#{3}>▲{6}</color>";

        private const string StringFormat = "{0} ▼{1} ■{2} ▲{3}";
        private string _textLineFormat = ColorizedStringFormat;

        /// <summary>Force a immediate text update (if there is one).</summary>
        public void UpdateTextLine()
        {
            if (!Text) return;
            if (_colorDirty) UpdateColors();

#if UNITY_EDITOR
			if (!_valueProvider) {
				Text.text = string.Format(_textLineFormat, "<empty provider>", 0f, 0f, 0f);
				return;
			}
#endif

            Text.text = string.Format(_textLineFormat,
                _valueProvider.Title,
                _minValue.ToString(_valueProvider.NumberFormat),
                _avgValue.ToString(_valueProvider.NumberFormat),
                _maxValue.ToString(_valueProvider.NumberFormat));
        }

        private bool _colorDirty = true;

        /// <summary>Sets the color dirty to update it in the next frame.</summary>
        public void SetColorDirty()
        {
            _colorDirty = true;
        }

        private void UpdateColors()
        {
            if (Text != null) Text.color = TitleColor;
            _textLineFormat = _colorizeText
                ? string.Format(ColorizedStringFormat, "{0}", ColorToString(_minValueColor), ColorToString(_averageValueColor),
                    ColorToString(_maxValueColor), "{1}", "{2}", "{3}")
                : StringFormat;
            _colorDirty = false;
        }

        [SerializeField]
        [Tooltip("Whether the text should be colorized with graphic colors.")]
        private bool _colorizeText = true;

        /// <summary>Whether the text should be colorized with graphic colors.</summary>
        public bool ColorizeText
        {
            get { return _colorizeText; }
            set
            {
                _colorizeText = value;
                _colorDirty = true;
            }
        }

        /// <summary>Graphic background color.</summary>
        [Tooltip("Graphic background color.")]
        public Color32 BackgroundColor = new Color32(44, 62, 80, 255);

        [SerializeField]
        [Tooltip("Title color. If Colorize Text is false it will be the label color.")]
        private Color32 _titleColor = Color.white;

        /// <summary>Title color. If <see cref="ColorizeText"/> is <code>false</code> it will be the label color.</summary>
        public Color32 TitleColor
        {
            get { return _titleColor; }
            set
            {
                _titleColor = value;
                if (Text != null) Text.color = value;
            }
        }

        [SerializeField]
        [Tooltip("Minimum value color. It will be blended with Max Value Color depending on the value.")]
        private Color32 _minValueColor = new Color32(244, 70, 71, 255);

        /// <summary>Minimum value color. It will be blended with <see cref="MaxValueColor"/> depending on the value.</summary>
        public Color32 MinValueColor
        {
            get { return _minValueColor; }
            set
            {
                _minValueColor = value;
                _colorDirty = true;
            }
        }

        [SerializeField]
        [Tooltip("Average value color.")]
        private Color32 _averageValueColor = new Color32(201, 234, 251, 255);

        /// <summary>Average value color.</summary>
        public Color32 AverageValueColor
        {
            get { return _averageValueColor; }
            set
            {
                _averageValueColor = value;
                _colorDirty = true;
            }
        }

        [SerializeField]
        [Tooltip("Maximum value color. It will be blended with Min Value Color depending on the value.")]
        private Color32 _maxValueColor = new Color32(247, 152, 50, 255);

        /// <summary>Maximum value color. It will be blended with <see cref="MinValueColor"/> depending on the value.</summary>
        public Color32 MaxValueColor
        {
            get { return _maxValueColor; }
            set
            {
                _maxValueColor = value;
                _colorDirty = true;
            }
        }


        private static string ColorToString(Color32 color)
        {
            return string.Format("{0:x2}{1:x2}{2:x2}{3:x2}", color.r, color.g, color.b, color.a);
        }

        #endregion

        public override void OnRectTransformDimensionsChange()
        {
            if (!_imageRectTransform) return;
            if (_collapsed)
            {
                preferredHeight = _textHeight;
                return;
            }

            var height = _imageRectTransform.rect.height;
            if (height <= float.Epsilon)
            {
                var size = _imageRectTransform.sizeDelta;
                size.y = DefaultSize.y;
                _imageRectTransform.sizeDelta = size;
            }
            if (height <= _textHeight) height = DefaultSize.y;
            _expandedHeight = preferredHeight = height;

#if UNITY_EDITOR
			if (transform.parent == null) return;
			EditorUtility.SetDirty(this);
#endif
        }

        public void OnPointerClick(PointerEventData eventData) { if (TouchToggleCollapsed) ToggleCollapsed(); }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_graphic != null) _graphic.Destroy();
        }
    }
}