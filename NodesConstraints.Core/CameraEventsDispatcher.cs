using System;
using UnityEngine;

namespace NodesConstraints
{
    public class CameraEventsDispatcher : MonoBehaviour
    {
        public event Action onPreCull;
        private void OnPreCull()
        {
            if (onPreCull != null)
                onPreCull();
        }

        public event Action onPreRender;
        private void OnPreRender()
        {
            if (onPreRender != null)
                onPreRender();
        }
    }
}
