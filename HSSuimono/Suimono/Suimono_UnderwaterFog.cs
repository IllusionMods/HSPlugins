using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Suimono.Core
{

    [AddComponentMenu("Image Effects/Suimono/UnderwaterFX")]
    public class Suimono_UnderwaterFog : MonoBehaviour
    {

        //Public Variables
        public bool showScreenMask = false;
        public bool doTransition = false;
        public bool cancelTransition = false;
        public bool useUnderSurfaceView = false;
        public bool distanceFog = true;
        public bool useRadialDistance = true;
        public bool heightFog = false;
        public float height = 1.0f;
        public float heightDensity = 2.0f;
        public float startDistance = 0.0f;
        public float fogStart = 0.0f;
        public float fogEnd = 20.0f;
        public float refractAmt = 0.005f;
        public float refractSpd = 1.5f;
        public float refractScale = 0.5f;
        public float lightFactor = 1.0f;
        public Color underwaterColor;
        public float dropsTime = 2.0f;
        public float wipeTime = 1.0f;
        public int iterations = 2;
        public float blurSpread = 1.0f;
        public float darkRange = 40.0f;
        public float heightDepth = 1.0f;
        public float hFac = 0.0f;
        public Texture distortTex;
        //public Texture mask1Tex;
        public Texture mask2Tex;
        public Shader fogShader = null;
        public Material fogMaterial = null;


        //Private Variables
        private SuimonoModule moduleObject;
        private SuimonoModuleLib moduleLibrary;
        private float trans1Time = 1.1f;
        private float trans2Time = 1.1f;
        private int randSeed;
        private Random dropRand;
        private Vector2 dropOff;
        private Camera cam;
        private Transform camtr;
        private int pass;
        private int rtW;
        private int rtH;
        private RenderTexture buffer;
        private int i = 0;
        private RenderTexture buffer2;
        private Vector3 camPos;
        private float FdotC;
        private float paramK;
        private float sceneStart;
        private float sceneEnd;
        private Vector4 sceneParams;
        private float diff;
        private float invDiff;
        private Matrix4x4 frustumCorners;
        private float fovWHalf;
        private Vector3 toRight;
        private Vector3 toTop;
        private Vector3 topLeft;
        private float camScale;
        private Vector3 topRight;
        private Vector3 bottomRight;
        private Vector3 bottomLeft;
        private float offc;
        private float off;
        private Transform trackobject;

        private float _deltaTime;

        private void Start()
        {
            this.cam = this.gameObject.GetComponent<Camera>();
            this.camtr = this.cam.transform;

            if (GameObject.Find("SUIMONO_Module") != null)
            {
                this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
                this.moduleLibrary = (SuimonoModuleLib)FindObjectOfType(typeof(SuimonoModuleLib));
                //moduleLibrary = GameObject.Find("SUIMONO_Module").GetComponent<SuimonoModuleLib>();
            }

            if (this.moduleLibrary != null)
            {
                this.distortTex = this.moduleLibrary.texNormalC;
                //mask1Tex = moduleLibrary.texHeightC;
                this.mask2Tex = this.moduleLibrary.texDrops;
            }

            this.randSeed = System.Environment.TickCount;
            this.dropRand = new Random(this.randSeed);

            this.fogShader = HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("SuimonoUnderwaterFog");
            this.fogMaterial = new Material(this.fogShader);
        }

        private void LateUpdate()
        {

            //update random reference
            if (this.dropRand == null)
                this.dropRand = new Random(this.randSeed);

            //update timing
            this._deltaTime = Time.deltaTime;

            //Handle Transition Settings
            if (this.cancelTransition)
            {
                this.doTransition = false;
                this.cancelTransition = false;
                this.trans1Time = 1.1f;
                this.trans2Time = 1.1f;
            }

            if (this.doTransition)
            {
                this.doTransition = false;
                this.trans1Time = 0.0f;
                this.trans2Time = 0.0f;
                this.dropOff = new Vector2(this.dropRand.Next(0.0f, 1.0f), this.dropRand.Next(0.0f, 1.0f));
            }

            this.trans1Time += (this._deltaTime * 0.7f * this.wipeTime);
            this.trans2Time += (this._deltaTime * 0.1f * this.dropsTime);
        }






        //[ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {

            Graphics.Blit(source, destination);

            this.frustumCorners = Matrix4x4.identity;
            this.fovWHalf = this.cam.fieldOfView * 0.5f;
            this.toRight = this.camtr.right * this.cam.nearClipPlane * Mathf.Tan(this.fovWHalf * Mathf.Deg2Rad) * this.cam.aspect;
            this.toTop = this.camtr.up * this.cam.nearClipPlane * Mathf.Tan(this.fovWHalf * Mathf.Deg2Rad);
            this.topLeft = (this.camtr.forward * this.cam.nearClipPlane - this.toRight + this.toTop);
            this.camScale = this.topLeft.magnitude * this.cam.farClipPlane / this.cam.nearClipPlane;

            this.topLeft.Normalize();
            this.topLeft *= this.camScale;

            this.topRight = (this.camtr.forward * this.cam.nearClipPlane + this.toRight + this.toTop);
            this.topRight.Normalize();
            this.topRight *= this.camScale;

            this.bottomRight = (this.camtr.forward * this.cam.nearClipPlane + this.toRight - this.toTop);
            this.bottomRight.Normalize();
            this.bottomRight *= this.camScale;

            this.bottomLeft = (this.camtr.forward * this.cam.nearClipPlane - this.toRight - this.toTop);
            this.bottomLeft.Normalize();
            this.bottomLeft *= this.camScale;

            this.frustumCorners.SetRow(0, this.topLeft);
            this.frustumCorners.SetRow(1, this.topRight);
            this.frustumCorners.SetRow(2, this.bottomRight);
            this.frustumCorners.SetRow(3, this.bottomLeft);


            //set default values based on water surface height
            if (this.heightFog && this.transform.parent != null)
            {
                this.height = this.transform.parent.transform.position.y + 1.0f;
                this.heightDensity = 2.0f;
            }

            this.camPos = this.camtr.position;
            this.FdotC = this.camPos.y - this.height;
            this.paramK = (this.FdotC <= 0.0f ? 1.0f : 0.0f);
            this.sceneStart = this.fogStart;
            this.sceneEnd = this.fogEnd;

            this.diff = this.sceneEnd - this.sceneStart;
            this.invDiff = Mathf.Abs(this.diff) > 0.0001f ? 1.0f / this.diff : 0.0f;
            this.sceneParams.x = 0.0f;
            this.sceneParams.y = 0.0f;
            this.sceneParams.z = -this.invDiff;
            this.sceneParams.w = this.sceneEnd * this.invDiff;


            if (this.fogMaterial != null)
            {
                this.fogMaterial.SetMatrix("_FrustumCornersWS", this.frustumCorners);
                this.fogMaterial.SetVector("_CameraWS", this.camPos);
                this.fogMaterial.SetVector("_HeightParams", new Vector4(this.height, this.FdotC, this.paramK, this.heightDensity * 0.5f));
                this.fogMaterial.SetVector("_DistanceParams", new Vector4(-Mathf.Max(this.startDistance, 0f), 0f, 0f, 0f));

                this.fogMaterial.SetVector("_SceneFogParams", this.sceneParams);
                this.fogMaterial.SetVector("_SceneFogMode", new Vector4(1f, this.useRadialDistance ? 1f : 0f, 0f, 0f));
                this.fogMaterial.SetColor("_underwaterColor", this.underwaterColor);

                if (this.distortTex != null)
                {
                    this.fogMaterial.SetTexture("_underwaterDistort", this.distortTex);
                    this.fogMaterial.SetFloat("_distortAmt", this.refractAmt);
                    this.fogMaterial.SetFloat("_distortSpeed", this.refractSpd);
                    this.fogMaterial.SetFloat("_distortScale", this.refractScale);
                    this.fogMaterial.SetFloat("_lightFactor", this.lightFactor);
                }
                if (this.distortTex != null)
                {
                    this.fogMaterial.SetTexture("_distort1Mask", this.distortTex);
                }
                if (this.mask2Tex != null)
                {
                    this.fogMaterial.SetTexture("_distort2Mask", this.mask2Tex);
                }

                this.fogMaterial.SetFloat("_trans1", this.trans1Time);
                this.fogMaterial.SetFloat("_trans2", this.trans2Time);
                this.fogMaterial.SetFloat("_dropOffx", this.dropOff.x);
                this.fogMaterial.SetFloat("_dropOffy", this.dropOff.y);

                this.fogMaterial.SetFloat("_showScreenMask", this.showScreenMask ? 1f : 0f);

                this.blurSpread = Mathf.Clamp01(this.blurSpread);
                this.fogMaterial.SetFloat("_blur", this.blurSpread);


                //calculate heightDepth for underwater darkening
                if (this.moduleObject != null)
                {
                    if (this.moduleObject.setTrack != null)
                    {
                        this.trackobject = this.moduleObject.setTrack.transform;
                    }
                    else
                    {
                        if (this.moduleObject.setCamera != null)
                        {
                            this.trackobject = this.moduleObject.setCamera.transform;
                        }
                    }

                    if (this.trackobject != null)
                    {
                        this.hFac = Mathf.Clamp((11.5f) - this.trackobject.localPosition.y, 0.0f, 500.0f);
                    }

                    this.heightDepth = this.hFac;
                    this.hFac = Mathf.Clamp01(Mathf.Lerp(-0.2f, 1f, Mathf.Clamp01(this.hFac / this.darkRange)));
                    this.fogMaterial.SetFloat("_hDepth", this.hFac);

                    this.fogMaterial.SetFloat("_enableUnderwater", this.moduleObject.enableUnderwaterFX ? 1f : 0f);
                }


                //blur
                // Copy source to the 4x4 smaller texture.
                this.rtW = source.width / 4;
                this.rtH = source.height / 4;
                this.buffer = RenderTexture.GetTemporary(this.rtW, this.rtH, 0);
                this.DownSample4x(source, this.buffer);

                // Blur the small texture
                for (this.i = 0; this.i < this.iterations; this.i++)
                {
                    this.buffer2 = RenderTexture.GetTemporary(this.rtW, this.rtH, 0);
                    this.FourTapCone(this.buffer, this.buffer2, this.i);
                    RenderTexture.ReleaseTemporary(this.buffer);
                    this.buffer = this.buffer2;
                }

                Graphics.Blit(this.buffer, destination);
                RenderTexture.ReleaseTemporary(this.buffer);

                this.pass = 0;
                if (this.distanceFog && this.heightFog)
                    this.pass = 0; // distance + height
                else if (this.distanceFog)
                    this.pass = 1; // distance only
                else
                    this.pass = 2; // height only

                this.CustomGraphicsBlit(source, destination, this.fogMaterial, this.pass);
            }

        }

        private void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
        {
            RenderTexture.active = dest;
            fxMaterial.SetTexture("_MainTex", source);

            GL.PushMatrix();
            GL.LoadOrtho();
            fxMaterial.SetPass(passNr);

            GL.Begin(GL.QUADS);
            GL.MultiTexCoord2(0, 0.0f, 0.0f);
            GL.Vertex3(0.0f, 0.0f, 3.0f); // BL
            GL.MultiTexCoord2(0, 1.0f, 0.0f);
            GL.Vertex3(1.0f, 0.0f, 2.0f); // BR
            GL.MultiTexCoord2(0, 1.0f, 1.0f);
            GL.Vertex3(1.0f, 1.0f, 1.0f); // TR
            GL.MultiTexCoord2(0, 0.0f, 1.0f);
            GL.Vertex3(0.0f, 1.0f, 0.0f); // TL
            GL.End();
            GL.PopMatrix();
        }





        // Performs one blur iteration.
        private void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
        {
            this.offc = 0.5f + iteration * this.blurSpread * 2;
            Graphics.BlitMultiTap(source, dest, this.fogMaterial,
                                   new Vector2(-this.offc, -this.offc),
                                   new Vector2(-this.offc, this.offc),
                                   new Vector2(this.offc, this.offc),
                                   new Vector2(this.offc, -this.offc)
                );
        }

        // Downsamples the texture to a quarter resolution.
        private void DownSample4x(RenderTexture source, RenderTexture dest)
        {
            this.off = 1.0f;
            Graphics.BlitMultiTap(source, dest, this.fogMaterial,
                                   new Vector2(-this.off, -this.off),
                                   new Vector2(-this.off, this.off),
                                   new Vector2(this.off, this.off),
                                   new Vector2(this.off, -this.off)
                );
        }








    }
}
