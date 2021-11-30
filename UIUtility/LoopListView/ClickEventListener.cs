using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperScrollView
{
    public class ClickEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public static ClickEventListener Get(GameObject obj)
        {
            ClickEventListener listener = obj.GetComponent<ClickEventListener>();
            if (listener == null)
                listener = obj.AddComponent<ClickEventListener>();
            return listener;
        }

        System.Action<GameObject> mClickedHandler = null;
        System.Action<GameObject> mDoubleClickedHandler = null;
        System.Action<GameObject> mOnPointerDownHandler = null;
        System.Action<GameObject> mOnPointerUpHandler = null;
        bool mIsPressed = false;

        public bool IsPressd { get { return this.mIsPressed; } }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                if (this.mDoubleClickedHandler != null)
                    this.mDoubleClickedHandler(this.gameObject);
            }
            else
            {
                if (this.mClickedHandler != null)
                    this.mClickedHandler(this.gameObject);
            }

        }

        public void SetClickEventHandler(System.Action<GameObject> handler)
        {
            this.mClickedHandler = handler;
        }

        public void SetDoubleClickEventHandler(System.Action<GameObject> handler)
        {
            this.mDoubleClickedHandler = handler;
        }

        public void SetPointerDownHandler(System.Action<GameObject> handler)
        {
            this.mOnPointerDownHandler = handler;
        }

        public void SetPointerUpHandler(System.Action<GameObject> handler)
        {
            this.mOnPointerUpHandler = handler;
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            this.mIsPressed = true;
            if (this.mOnPointerDownHandler != null)
                this.mOnPointerDownHandler(this.gameObject);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            this.mIsPressed = false;
            if (this.mOnPointerUpHandler != null)
                this.mOnPointerUpHandler(this.gameObject);
        }

    }

}
