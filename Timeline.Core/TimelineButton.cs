using System.Collections;
using System.ComponentModel;
using System.Linq;
using BepInEx.Configuration;
using KKAPI.Studio.UI;
using KKAPI.Utilities;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Timeline
{
    internal static class TimelineButton
    {
        private const float BlinkTime = 0.7f;
        private static float _blinkTime;

        private static ConfigEntry<bool> _nagShown;

        private static ToolbarButton _button;
        private static Image _buttImage;
        private static bool _currentIsPlaying;
        private static bool _currentShown;

        private static ToolbarButton AddButton()
        {
            var buttTex = ResourceUtils.GetEmbeddedResource("timeline_button.png").LoadTexture();
            _button = CustomToolbarButtons.AddLeftToolbarButton(buttTex, () =>
            {
                if (!AnyInterpolables())
                {
                    Timeline.Logger.LogMessage("Nothing to play. Right click to open the Timeline window.");
                    _nagShown.Value = true;
                    return;
                }

                if (!_nagShown.Value)
                {
                    Timeline.Logger.LogMessage("Right click to open the Timeline window.");
                    _nagShown.Value = true;
                }

                TogglePlaying();
            });
            return _button;
        }

        private static void TogglePlaying()
        {
            Timeline.Play();
        }

        private static bool AnyInterpolables()
        {
            return Timeline.GetAllInterpolables(false).Any();
        }

        internal static void OnUpdate()
        {
            if (_currentIsPlaying)
            {
                _blinkTime += Time.deltaTime;

                if (_blinkTime > BlinkTime)
                {
                    UpdateButtonColor();
                    _blinkTime = 0;
                }
            }
        }

        private static void UpdateButtonColor()
        {
            if (!_buttImage) return;

            if (_currentIsPlaying && _blinkTime > BlinkTime && _buttImage.color != Color.yellow)
                _buttImage.color = Color.yellow;
            else if (_currentShown)
                _buttImage.color = Color.green;
            else
                _buttImage.color = !_currentIsPlaying && AnyInterpolables() ? Color.yellow : Color.white;
        }

        internal static void UpdateButton()
        {
            _currentShown = Timeline.InterfaceVisible;

            var isPlaying = Timeline.isPlaying;
            if (_currentIsPlaying != isPlaying)
            {
                _currentIsPlaying = isPlaying;
                _blinkTime = 0;
            }

            UpdateButtonColor();
        }

        internal static IEnumerator Init()
        {
            var butt = AddButton();

            yield return new WaitUntil(() => butt.ControlObject != null);

            _buttImage = _button.ControlObject.GetComponent<Button>().image;
            _buttImage.OnPointerClickAsObservable().Subscribe(data =>
            {
                if (data.button != PointerEventData.InputButton.Left)
                    Timeline.InterfaceVisible = !Timeline.InterfaceVisible;
            });

            _nagShown = Timeline._self.Config.Bind("Misc", "Right click nag was shown", false, new ConfigDescription("Nag message shown when first clicking the toolbar button.", null, BrowsableAttribute.No));
        }
    }
}
