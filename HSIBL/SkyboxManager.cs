using UnityEngine;

namespace HSIBL
{
    public struct SkyboxParams
    {
        internal float exposure;
        internal float rotation;
        internal Color tint;
        public SkyboxParams(float a, float b, Color A)
        {
            this.exposure = a;
            this.rotation = b;
            this.tint = A;
        }
    };
    class SkyboxManager
    {
        public void ApplySkybox()
        {
            RenderSettings.skybox = this.skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        }

        public void ApplySkyboxParams()
        {
            if (this.skybox != null)
            {
                this.skybox.SetFloat("_Exposure", this.skyboxparams.exposure);
                this.skybox.SetColor("_Tint", this.skyboxparams.tint);
                this.skybox.SetFloat("_Rotation", this.skyboxparams.rotation);
            }
        }
        Material skybox;
        public SkyboxParams skyboxparams = new SkyboxParams(1f,0f,Color.gray);
        public Material Skybox { get { return this.skybox; } set { this.skybox = value; } }
    }

}
