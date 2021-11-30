using UnityEngine;
using UnityEngine.Rendering;

namespace HSLRE.CustomEffects
{
    public class SEGIHSStandardCustom : MonoBehaviour
    {
        private bool _init = false;
        private Camera _camera;
        private Camera _parentCamera;
        private Shader _replacementShader;
        private Shader _getGBufferShader;
        private Material _getGBufferMaterial;

        public volatile RenderTexture GBuffer0 = null;
        public volatile RenderTexture GBuffer1 = null;
        public volatile RenderTexture GBuffer2 = null;
        public volatile RenderTexture Depth = null;

        private void Awake()
        {
            this.Init();
        }

        private void Init()
        {
            if (this._init)
                return;
            this._camera = this.gameObject.AddComponent<Camera>();
            this._parentCamera = this.transform.parent.GetComponent<Camera>();

            this._camera.renderingPath = RenderingPath.DeferredShading;
            this._camera.clearFlags = CameraClearFlags.Color;
            this._camera.backgroundColor = Color.clear;
            this._camera.depth = this._parentCamera.depth - 10;

            this._replacementShader = HSLRE.self._resources.LoadAsset<Shader>("SEGIHSStandardReplacement");
            this._camera.SetReplacementShader(this._replacementShader, "HSStandard");
            this._getGBufferShader = HSLRE.self._resources.LoadAsset<Shader>("GetGBuffer");
            this._getGBufferMaterial = new Material(this._getGBufferShader);

            this._init = true;
        }

        private void OnPreRender()
        {
            this.Init();
            if (Mathf.Approximately(this._camera.fieldOfView, this._parentCamera.fieldOfView) == false)
                this._camera.fieldOfView = this._parentCamera.fieldOfView;
            if (this._camera.cullingMask != this._parentCamera.cullingMask)
                this._camera.cullingMask = this._parentCamera.cullingMask;
            if (Mathf.Approximately(this._camera.farClipPlane, this._parentCamera.farClipPlane) == false)
                this._camera.farClipPlane = this._parentCamera.farClipPlane;
            if (Mathf.Approximately(this._camera.nearClipPlane, this._parentCamera.nearClipPlane) == false)
                this._camera.nearClipPlane = this._parentCamera.nearClipPlane;
            if (this._camera.rect != this._parentCamera.rect)
                this._camera.rect = this._parentCamera.rect;
            if (this._camera.hdr != this._parentCamera.hdr)
                this._camera.hdr = this._parentCamera.hdr;
            if (this._camera.depthTextureMode != this._parentCamera.depthTextureMode)
                this._camera.depthTextureMode = this._parentCamera.depthTextureMode;

            if (this.GBuffer0 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer0);
            if (this.GBuffer1 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer1);
            if (this.GBuffer2 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer2);
            if (this.Depth != null)
                RenderTexture.ReleaseTemporary(this.Depth);

            int width, height;
            if (this._camera.targetTexture == null)
            {
                width = this._camera.pixelWidth;
                height = this._camera.pixelHeight;
            }
            else
            {
                width = this._camera.targetTexture.width;
                height = this._camera.targetTexture.height;
            }

            this.GBuffer0 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            this.GBuffer0.filterMode = FilterMode.Point;
            this.GBuffer1 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            this.GBuffer1.filterMode = FilterMode.Point;
            this.GBuffer2 = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
            this.GBuffer2.filterMode = FilterMode.Point;
            this.Depth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            this.Depth.filterMode = FilterMode.Point;
        }

        private void OnPostRender()
        {
            this._camera.ResetReplacementShader();

            Graphics.Blit(null, this.GBuffer0, this._getGBufferMaterial, 0);
            Graphics.Blit(null, this.GBuffer1, this._getGBufferMaterial, 1);
            Graphics.Blit(null, this.GBuffer2, this._getGBufferMaterial, 2);
            Graphics.Blit(null, this.Depth, this._getGBufferMaterial, 3);

            this._camera.SetReplacementShader(this._replacementShader, "HSStandard");
        }

        private void OnDestroy()
        {
            this._camera.RemoveAllCommandBuffers();
            if (this.GBuffer0 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer0);
            if (this.GBuffer1 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer1);
            if (this.GBuffer2 != null)
                RenderTexture.ReleaseTemporary(this.GBuffer2);
            if (this.Depth != null)
                RenderTexture.ReleaseTemporary(this.Depth);
            this.GBuffer0 = null;
            this.GBuffer1 = null;
            this.GBuffer2 = null;
            this.Depth = null;
            UnityEngine.Object.Destroy(this._getGBufferMaterial);
        }
    }
}