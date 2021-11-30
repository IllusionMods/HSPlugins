using System;
using UnityEngine;

namespace HSLRE
{
    public class CameraEventsDispatcher : MonoBehaviour
    {
        private Camera _camera;
        private void Awake()
        {
            this._camera = this.GetComponent<Camera>();
        }

        public event Action<Camera> onPreCull;
        private void OnPreCull()
        {
            if (this.onPreCull != null)
                this.onPreCull(this._camera);
        }

        public event Action<Camera> onPreRender;
        private void OnPreRender()
        {
            if (this.onPreRender != null)
                this.onPreRender(this._camera);
        }

        public event Action<Camera> onPostRender;
        private void OnPostRender()
        {
            if (this.onPostRender != null)
                this.onPostRender(this._camera);
        }
    }
}
