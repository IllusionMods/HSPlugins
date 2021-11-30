using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
	using UnityEditor;
#endif


namespace Suimono.Core
{

    [ExecuteInEditMode]
    public class Suimono_ShorelineObject : MonoBehaviour
    {

        //PUBLIC VARIABLES
        public int lodIndex;
        public int shorelineModeIndex;
        public List<string> shorelineModeOptions = new List<string>() { "Auto-Generate", "Custom Texture" };
        public int shorelineRunIndex;
        public List<string> shorelineRunOptions = new List<string>() { "At Start", "Continuous" };
        public Transform attachToSurface;
        public bool autoPosition = true;
        public float maxDepth = 25.0f;
        public float sceneDepth = 14.5f;
        public float shoreDepth = 27.7f;
        public bool debug = false;
        public string suimonoVersionNumber;
        public int depthLayer = 2;
        public List<string> suiLayerMasks = new List<string>();
        public Texture2D customDepthTex;
        public int useResolution = 512;

        //PRIVATE VARIABLES
        private Material useMat;
        private Texture reflTex;
        private Texture envTex;
        private Matrix4x4 MV;
        private Camera CamInfo;
        private cameraTools CamTools;
        private SuimonoCamera_depth CamDepth;

        private float curr_sceneDepth;
        private float curr_shoreDepth;
        private float curr_foamDepth;
        private float curr_edgeDepth;

        private Vector3 currPos;
        private Vector3 currScale;
        private Quaternion currRot;

        private Vector4 camCoords = new Vector4(1f, 1f, 0f, 0f);
        private Material localMaterial;
        private Renderer renderObject;
        private MeshFilter meshObject;
        private Material matObject;
        public SuimonoModule moduleObject;
        private float maxScale;

        private int i;
        private string layerName;
        private bool hasRendered = false;
        private bool renderPass = true;
        private int saveMode = -1;
        private Vector3 gizPos;

        private void OnDrawGizmos()
        {
            this.gizPos = this.transform.position;
            this.gizPos.y += 0.03f;
            Gizmos.DrawIcon(this.gizPos, "gui_icon_shore.psd", true);
            this.gizPos.y -= 0.03f;
        }

        private void Start()
        {

            //DISCONNECT FROM PREFAB
#if UNITY_EDITOR
				PrefabUtility.DisconnectPrefabInstance(this.gameObject);
#endif

            //turn off debig at start
            if (Application.isPlaying)
            {
                this.debug = false;
            }

            //get main object
            if (GameObject.Find("SUIMONO_Module") != null)
            {
                this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
                this.suimonoVersionNumber = this.moduleObject.suimonoVersionNumber;
            }

            //setup camera
            this.CamInfo = this.transform.Find("cam_LocalShore").gameObject.GetComponent<Camera>();
            this.CamInfo.depthTextureMode |= DepthTextureMode.DepthNormals;
            this.CamTools = this.transform.Find("cam_LocalShore").gameObject.GetComponent<cameraTools>() as cameraTools;
            this.CamDepth = this.transform.Find("cam_LocalShore").gameObject.GetComponent<SuimonoCamera_depth>() as SuimonoCamera_depth;

            //setup renderer
            this.renderObject = this.gameObject.GetComponent<Renderer>() as Renderer;
            this.meshObject = this.gameObject.GetComponent<MeshFilter>() as MeshFilter;

            //find parent surface
            if (this.transform.parent)
            {
                if (this.transform.parent.gameObject.GetComponent<SuimonoObject>() != null)
                {
                    this.attachToSurface = this.transform.parent;
                }
            }

            //turn on surface
            if (this.attachToSurface != null)
            {
                this.attachToSurface.Find("Suimono_Object").gameObject.GetComponent<Renderer>().enabled = true;
            }

            //setup material
            this.matObject = new Material(HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("Suimono2_FX_ShorelineObject"));
            this.renderObject.material = this.matObject;

            //set rendering flag
            this.hasRendered = false;

        }

        private void LateUpdate()
        {


            if (this.moduleObject != null)
            {

                //version number
                this.suimonoVersionNumber = this.moduleObject.suimonoVersionNumber;

                //set layers and tags
                this.gameObject.layer = this.moduleObject.layerDepthNum;
                this.CamInfo.gameObject.layer = this.moduleObject.layerDepthNum;
                this.CamInfo.farClipPlane = this.maxDepth;

                this.gameObject.tag = "Untagged";
                this.CamInfo.gameObject.tag = "Untagged";



                //---------
                //set layer mask array
                this.suiLayerMasks = new List<string>();
                for (this.i = 0; this.i < 32; this.i++)
                {
                    this.layerName = LayerMask.LayerToName(this.i);
                    this.suiLayerMasks.Add(this.layerName);
                }


                if (!Application.isPlaying)
                {
                    if (this.attachToSurface != null)
                    {
                        this.attachToSurface.Find("Suimono_Object").gameObject.GetComponent<Renderer>().enabled = !this.debug;
                    }
                }


                if (this.shorelineModeIndex == 0)
                {
                    // set camera culling
                    if (this.CamInfo != null)
                    {
                        this.CamInfo.enabled = true;
                        this.CamInfo.cullingMask = this.depthLayer;
                    }
                }
                else
                {
                    if (this.CamInfo != null)
                        this.CamInfo.enabled = false;
                }


                //Handle Debug Mode
                if (this.debug)
                {
                    if (this.renderObject != null)
                        this.renderObject.hideFlags = HideFlags.None;
                    if (this.meshObject != null)
                        this.meshObject.hideFlags = HideFlags.None;
                    if (this.matObject != null)
                        this.matObject.hideFlags = HideFlags.None;
                    if (this.shorelineModeIndex == 0)
                    {
                        if (this.CamInfo != null)
                            this.CamInfo.gameObject.hideFlags = HideFlags.None;
                        if (this.CamTools != null)
                        {
                            this.CamTools.executeInEditMode = true;
                            this.CamTools.CameraUpdate();
                        }
                    }
                    if (this.renderObject != null)
                        this.renderObject.enabled = true;
                }
                else
                {
                    if (this.renderObject != null)
                        this.renderObject.hideFlags = HideFlags.HideInInspector;
                    if (this.meshObject != null)
                        this.meshObject.hideFlags = HideFlags.HideInInspector;
                    if (this.matObject != null)
                        this.matObject.hideFlags = HideFlags.HideInInspector;
                    if (this.shorelineModeIndex == 0)
                    {
                        if (this.CamInfo != null)
                            this.CamInfo.gameObject.hideFlags = HideFlags.HideInHierarchy;
                        if (this.CamTools != null)
                            this.CamTools.executeInEditMode = false;
                    }
                    this.renderObject.enabled = Application.isPlaying || this.renderObject == null;
                }
                //---------



                //flag mode setting
                if (this.saveMode != this.shorelineModeIndex)
                {
                    this.saveMode = this.shorelineModeIndex;
                    this.hasRendered = false;
                }


                //CALCULATE RENDER PASS FLAG
                this.renderPass = true;
                if (this.shorelineModeIndex == 0)
                {
                    if (this.shorelineRunIndex == 0 && this.hasRendered && Application.isPlaying)
                        this.renderPass = false;
                    if (this.shorelineRunIndex == 1)
                        this.renderPass = true;
                }
                if (this.shorelineModeIndex == 1 && this.hasRendered && Application.isPlaying)
                    this.renderPass = false;




                //RENDER
                if (!this.renderPass)
                {

                    if (this.CamInfo != null)
                        this.CamInfo.enabled = false;
                    if (this.CamTools != null)
                        this.CamTools.enabled = false;

                }
                else
                {

                    if (this.CamInfo != null)
                        this.CamInfo.enabled = true;
                    if (this.CamTools != null)
                        this.CamTools.enabled = true;
                    if (this.CamDepth != null)
                        this.CamDepth.enabled = true;

                    //set Depth Thresholds
                    if (this.shorelineModeIndex == 0)
                    {
                        this.CamDepth._sceneDepth = this.sceneDepth;
                        this.CamDepth._shoreDepth = this.shoreDepth;
                    }

                    if (this.attachToSurface != null)
                    {

                        //force y height
                        this.transform.localScale = new Vector3(this.transform.localScale.x, 1f, this.transform.localScale.z);

                        //force y position based on surface attachment
                        if (this.attachToSurface != null && this.autoPosition)
                        {
                            this.transform.position = new Vector3(this.transform.position.x, this.attachToSurface.position.y, this.transform.position.z);
                        }


                        //AUTO GENERATION MODE --------------------------------------------------
                        if (this.shorelineModeIndex == 0)
                        {

                            //Set object and camera Projection Size
                            this.maxScale = Mathf.Max(this.transform.localScale.x, this.transform.localScale.z);
                            this.CamInfo.orthographicSize = this.maxScale * 20f;
                            if (this.transform.localScale.x < this.transform.localScale.z)
                            {
                                this.camCoords = new Vector4(this.transform.localScale.x / this.transform.localScale.z,
                                    1.0f,
                                    0.5f - ((this.transform.localScale.x / this.transform.localScale.z) * 0.5f),
                                    0.0f);
                            }
                            else if (this.transform.localScale.x > this.transform.localScale.z)
                            {
                                this.camCoords = new Vector4(1f, this.transform.localScale.z / this.transform.localScale.x,
                                    0.0f,
                                    0.5f - ((this.transform.localScale.z / this.transform.localScale.x) * 0.5f));
                            }
                            this.CamTools.surfaceRenderer.sharedMaterial.SetColor("_Mult", this.camCoords);

                            //Update when moved,rotated, or scaled, or edited
                            if (this.CamTools != null)
                            {
                                if (this.currPos != this.transform.position)
                                {
                                    this.currPos = this.transform.position;
                                    this.CamTools.CameraUpdate();
                                }
                                if (this.currScale != this.transform.localScale)
                                {
                                    this.currScale = this.transform.localScale;
                                    this.CamTools.CameraUpdate();
                                }
                                if (this.currRot != this.transform.rotation)
                                {
                                    this.currRot = this.transform.rotation;
                                    this.CamTools.CameraUpdate();
                                }

                                if (this.curr_sceneDepth != this.sceneDepth)
                                {
                                    this.curr_sceneDepth = this.sceneDepth;
                                    this.CamTools.CameraUpdate();
                                }
                                if (this.curr_shoreDepth != this.shoreDepth)
                                {
                                    this.curr_shoreDepth = this.shoreDepth;
                                    this.CamTools.CameraUpdate();
                                }

                                if (Application.isPlaying)
                                    this.CamTools.CameraUpdate();

                            }
                        }


                        //CUSTOM TEXTURE MODE --------------------------------------------------
                        if (this.shorelineModeIndex == 1)
                        {
                            if (this.customDepthTex != null)
                            {
                                if (this.renderObject != null)
                                {
                                    this.renderObject.sharedMaterial.SetColor("_Mult", new Vector4(1f, 1f, 0f, 0f));
                                    this.renderObject.sharedMaterial.SetTexture("_MainTex", this.customDepthTex);
                                }
                            }

                        }


                        if (Application.isPlaying && Time.time > 1f)
                            this.hasRendered = true;

                    }
                }
            }


        }
    }
}