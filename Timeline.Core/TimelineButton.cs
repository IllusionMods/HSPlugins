using System;
using System.Collections;
using System.ComponentModel;
using BepInEx.Configuration;
using BepInEx.Logging;
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
        private static Func<bool> _anyInterpolables;

        private static ManualLogSource Logger;
        private static Action _toggleUiShown;
        private static Func<bool> _uiVisible;

        private static ToolbarButton AddButton()
        {
            var buttTex = ResourceUtils.GetEmbeddedResource("timeline_button.png").LoadTexture();
            _button = CustomToolbarButtons.AddLeftToolbarButton(buttTex, () =>
            {
                if (!_anyInterpolables())
                {
                    Logger.LogMessage("Nothing to play. Right click to open the Timeline window.");
                    _nagShown.Value = true;
                    return;
                }

                if (!_nagShown.Value)
                {
                    Logger.LogMessage("Right click to open the Timeline window.");
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

        private static void ToggleWindowShown()
        {
            _toggleUiShown();
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
            if (_currentIsPlaying && _blinkTime > BlinkTime && _buttImage.color != Color.yellow)
                _buttImage.color = Color.yellow;
            else if (_currentShown)
                _buttImage.color = Color.green;
            else
                _buttImage.color = !_currentIsPlaying && _anyInterpolables() ? Color.yellow : Color.white;
        }

        internal static void UpdateButton()
        {
            if (_uiVisible == null) return;

            _currentShown = _uiVisible();

            var isPlaying = Timeline.isPlaying;
            if (_currentIsPlaying != isPlaying)
            {
                _currentIsPlaying = isPlaying;
                _blinkTime = 0;
                // Save some overhead
                //Instance.enabled = _currentIsPlaying;
            }

            UpdateButtonColor();
        }

        internal static IEnumerator Init(Func<bool> anyInterporables, Func<bool> uiVisible, Action toggleUiShown, ManualLogSource logger)
        {
            Logger = logger;

            var butt = AddButton();
            //NodesConstraintsButton.AddButton();

            yield return new WaitUntil(() => butt.ControlObject != null);

            _buttImage = _button.ControlObject.GetComponent<Button>().image;
            _buttImage.OnPointerClickAsObservable().Subscribe(data =>
            {
                if (data.button != PointerEventData.InputButton.Left)
                    ToggleWindowShown();
            });

            _anyInterpolables = anyInterporables;
            _toggleUiShown = toggleUiShown;
            _uiVisible = uiVisible;

            //Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

            _nagShown = Timeline._self.Config.Bind("Misc", "Right click nag was shown", false, new ConfigDescription("Nag message shown when first clicking the toolbar button.", null, BrowsableAttribute.No));
        }
    }
}
