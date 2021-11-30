using UnityEngine;

namespace HSIBL
{
    public struct ProceduralSkyboxParams
    {
        internal float exposure;
        internal float sunsize;
        internal float atmospherethickness;
        internal Color skytint;
        internal Color groundcolor;
        public ProceduralSkyboxParams(float a, float b, float c,Color A,Color B)
        {
            this.exposure = a;
            this.sunsize = b;
            this.atmospherethickness = c;
            this.skytint = A;
            this.groundcolor = B;
        }
    };
    class ProceduralSkyboxManager
    {
        public void Init()
        {
            AssetBundle cubemapbundle = AssetBundle.LoadFromFile(Application.dataPath + "/../abdata/plastic/proceduralskybox.unity3d");
            this.proceduralsky = cubemapbundle.LoadAsset<Material>("Procedural Skybox");
            cubemapbundle.Unload(false);
            cubemapbundle = null;
            //proceduralsky = new Material();
        }
        public void ApplySkybox()
        {
            RenderSettings.skybox = this.proceduralsky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        }
        public void ApplySkyboxParams()
        {
            if (this.proceduralsky != null)
            {
                this.proceduralsky.SetFloat("_SunDisk", 2f);
                this.proceduralsky.SetFloat("_Exposure", this.skyboxparams.exposure);
                this.proceduralsky.SetFloat("_SunSize", this.skyboxparams.sunsize);
                this.proceduralsky.SetColor("_SkyTint", this.skyboxparams.skytint);
                this.proceduralsky.SetColor("_GroundColor", this.skyboxparams.groundcolor);
                this.proceduralsky.SetFloat("_AtmosphereThickness", this.skyboxparams.atmospherethickness);
            }
        }
        Material proceduralsky;
        public ProceduralSkyboxParams skyboxparams = new ProceduralSkyboxParams(1f,0.1f,1f,Color.gray,Color.gray);
        public Material Proceduralsky
        {
            get { return this.proceduralsky; }
            set { this.proceduralsky = value; }
        }
    }
    
}
