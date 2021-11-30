using UnityEngine;
using System.Collections;


namespace Suimono.Core
{

    public class Suimono_DistanceBlur : MonoBehaviour
    {

        //public variables
        public float blurAmt = 0.0f;
        public int iterations = 3;
        public float blurSpread = 0.6f;
        public Shader blurShader = null;
        public Material material = null;

        //private variables
        private float offc;
        private float off;
        private int rtW;
        private int rtH;
        private int i;
        private RenderTexture buffer;
        private RenderTexture buffer2;


        [Range(0, 2)]
        public int downsample = 1;

        [Range(0.0f, 10.0f)]
        public float blurSize = 3.0f;

        private void Start()
        {
            //get material reference
            this.CreateMaterial();
        }

        private void CreateMaterial()
        {
            //get material reference
            if (this.material == null)
            {
                this.material = new Material(this.blurShader);
                this.material.hideFlags = HideFlags.DontSave;
            }
        }


        public void OnDisable()
        {
            if (this.material)
                DestroyImmediate(this.material);
        }



        // Called by the camera to apply the image effect
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {

            if (this.material == null)
                this.CreateMaterial();

            this.iterations = Mathf.FloorToInt(Mathf.Lerp(0, 2, this.blurAmt));
            this.downsample = Mathf.FloorToInt(Mathf.Lerp(0, 2, this.blurAmt));
            this.blurSpread = Mathf.Lerp(0.0f, 2.0f, this.blurAmt);


            float widthMod = 1.0f / (1.0f * (1 << this.downsample));

            this.material.SetVector("_Parameter", new Vector4(this.blurSpread * widthMod, -this.blurSpread * widthMod, 0.0f, 0.0f));
            source.filterMode = FilterMode.Bilinear;

            int rtW = source.width >> this.downsample;
            int rtH = source.height >> this.downsample;

            // downsample
            RenderTexture rt = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);

            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, rt, this.material, 0);

            int passOffs = 0;

            for (int i = 0; i < this.iterations; i++)
            {
                float iterationOffs = (i * 1.0f);
                this.material.SetVector("_Parameter", new Vector4(this.blurAmt * widthMod + iterationOffs, -this.blurAmt * widthMod - iterationOffs, 0.0f, 0.0f));

                // vertical blur
                RenderTexture rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt, rt2, this.material, 1 + passOffs);
                RenderTexture.ReleaseTemporary(rt);
                rt = rt2;

                // horizontal blur
                rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format);
                rt2.filterMode = FilterMode.Bilinear;
                Graphics.Blit(rt, rt2, this.material, 2 + passOffs);
                RenderTexture.ReleaseTemporary(rt);
                rt = rt2;
            }

            Graphics.Blit(rt, destination);
            RenderTexture.ReleaseTemporary(rt);


        }




    }
}
