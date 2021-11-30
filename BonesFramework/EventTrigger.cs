using System;
using UnityEngine;

namespace BonesFramework
{
    public class EventTrigger : MonoBehaviour
    {
        public Action<EventTrigger> onStart;

        private void Start()
        {
            if (this.onStart != null)
                this.onStart(this);
        }

        public Action<GameObject> onDestroy;

        private void OnDestroy()
        {
            if (this.onDestroy != null)
                this.onDestroy(this.gameObject);
        }
    }
}
