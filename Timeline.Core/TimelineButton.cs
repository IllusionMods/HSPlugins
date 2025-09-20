using System.Collections;
using System.ComponentModel;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KKAPI.Studio.UI.Toolbars;
using KKAPI.Utilities;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Timeline
{
    internal class TimelineButton : SimpleToolbarButton
    {
        private const float BlinkTime = 0.7f;
        private static ConfigEntry<bool> _nagShown;
        private Coroutine _currentlyIsPlaying;
        private bool _currentlyShown;

        public TimelineButton(BaseUnityPlugin owner) :
            base(buttonID: "Timeline",
                 hoverText: "Left click to play/pause Timeline.\nRight click to open Timeline window.\nThe button is yellow if there is Timeline data present in the scene.",
                 iconGetter: () => ResourceUtils.GetEmbeddedResource("timeline_button.png").LoadTexture(),
                 owner: owner)
        {
            _nagShown = owner.Config.Bind("Misc", "Right click nag was shown", false, new ConfigDescription("Nag message shown when first clicking the toolbar button.", null, BrowsableAttribute.No));

            OnClicked.Subscribe(DoClick);
        }

        public bool CurrentlyIsPlaying
        {
            get => _currentlyIsPlaying != null;
            set
            {
                var isPlaying = _currentlyIsPlaying != null;
                if (isPlaying != value)
                {
                    if (isPlaying)
                    {
                        ButtonObject.StopCoroutine(_currentlyIsPlaying);
                        _currentlyIsPlaying = null;
                    }

                    if (value)
                        _currentlyIsPlaying = ButtonObject.StartCoroutine(BlinkerCo());
                }
            }
        }

        protected override void CreateControl()
        {
            base.CreateControl();
            UpdateButton();
        }

        private static void DoClick(PointerEventData.InputButton btn)
        {
            if (btn == PointerEventData.InputButton.Left)
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

                Timeline.Play();
            }
            else
            {
                Timeline.InterfaceVisible = !Timeline.InterfaceVisible;
            }
        }

        private static bool AnyInterpolables()
        {
            return Timeline.GetAllInterpolables(false).Any();
        }

        private IEnumerator BlinkerCo()
        {
            var isBlink = true;
            while (true)
            {
                yield return new WaitForSecondsRealtime(BlinkTime);

                UpdateButtonColor(isBlink);
                isBlink = !isBlink;
            }
        }

        private void UpdateButtonColor(bool isBlink)
        {
            if (ButtonObject == null || ButtonObject.image == null) return;
            var buttImage = ButtonObject.image;

            if (isBlink)
                buttImage.color = Color.yellow;
            else if (_currentlyShown)
                buttImage.color = Color.green;
            else
                buttImage.color = !CurrentlyIsPlaying && AnyInterpolables() ? Color.yellow : Color.white;
        }

        public void UpdateButton()
        {
            _currentlyShown = Timeline.InterfaceVisible;
            CurrentlyIsPlaying = Timeline.isPlaying;
            UpdateButtonColor(false);
        }
    }
}
