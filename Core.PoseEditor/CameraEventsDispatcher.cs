using System;
using UnityEngine;

namespace HSPE
{
    public class CameraEventsDispatcher : MonoBehaviour
    {
        public event Action onPreRender;
        private void OnPreRender()
        {
            if (onPreRender != null)
                onPreRender();
        }
    }
}
