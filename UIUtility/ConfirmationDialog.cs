using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UILib
{
    public class ConfirmationDialog : MonoBehaviour
    {
        #region Private Variables
        private Action<bool> _currentCallback;
        private Text _text;
        #endregion

        #region Static Methods
        public static Action<Action<bool>, string> SpawnUI()
        {
            Component confirmationDialogComponent = null;
            GameObject dialog = null;
            foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (c.gameObject.name.Equals("ConfirmationDialog"))
                {
                    dialog = c.gameObject;
                    break;
                }
            }
            if (dialog == null)
            {
                Canvas c = UIUtility.CreateNewUISystem("ConfirmationDialog");
                c.sortingOrder = 40;
                c.transform.localPosition = Vector3.zero;
                c.transform.localScale = Vector3.one;
                c.transform.SetRect();
                c.transform.SetAsLastSibling();

                Image bg = UIUtility.CreateImage("Background", c.transform);
                bg.rectTransform.SetRect();
                bg.sprite = null;
                bg.color = new Color(0f, 0f, 0f, 0.5f);
                bg.raycastTarget = true;

                Image panel = UIUtility.CreatePanel("Panel", bg.transform);
                panel.rectTransform.SetRect(new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.6f));
                panel.color = Color.gray;

                Text text = UIUtility.CreateText("Text", panel.transform, "");
                text.rectTransform.SetRect(new Vector2(0f, 0.333333f), Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                text.color = Color.white;
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                Button yes = UIUtility.CreateButton("YesButton", panel.transform, "Yes");
                (yes.transform as RectTransform).SetRect(Vector2.zero, new Vector2(0.5f, 0.333333f), new Vector2(10f, 10f), new Vector2(-5f, -10f));
                text = yes.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                Button no = UIUtility.CreateButton("NoButton", panel.transform, "No");
                (no.transform as RectTransform).SetRect(new Vector2(0.5f, 0f), new Vector2(1f, 0.333333f), new Vector2(5f, 10f), new Vector2(-10f, -10f));
                text = no.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                confirmationDialogComponent = c.gameObject.AddComponent<ConfirmationDialog>();
                c.gameObject.SetActive(false);
            }
            else
            {
                Component[] components = dialog.GetComponents<Component>();
                foreach (Component c in components)
                {
                    if (c.GetType().Name == nameof(ConfirmationDialog))
                    {
                        confirmationDialogComponent = c;
                        break;
                    }
                }
            }
            return (Action<Action<bool>, string>)Delegate.CreateDelegate(typeof(Action<Action<bool>, string>), confirmationDialogComponent, confirmationDialogComponent.GetType().GetMethod(nameof(DisplayDialog), BindingFlags.Instance | BindingFlags.Public));
        }
        #endregion

        #region Unity Methods
        private void Awake()
        {
            this.transform.Find("Background/Panel/YesButton").GetComponent<Button>().onClick.AddListener(this.YesPressed);
            this.transform.Find("Background/Panel/NoButton").GetComponent<Button>().onClick.AddListener(this.NoPressed);
            this._text = this.transform.Find("Background/Panel/Text").GetComponent<Text>();
        }
        #endregion

        #region Public Methods
        public void DisplayDialog(Action<bool> callback, string message = "Are you sure?")
        {
            this._currentCallback = callback;
            this._text.text = message;
            this.gameObject.SetActive(true);
        }
        #endregion

        #region Private Methods
        private void NoPressed()
        {
            if (this._currentCallback != null)
                this._currentCallback(false);
            this.gameObject.SetActive(false);
            this._currentCallback = null;
        }

        private void YesPressed()
        {
            if (this._currentCallback != null)
                this._currentCallback(true);
            this.gameObject.SetActive(false);
            this._currentCallback = null;
        }
        #endregion
    }
}
