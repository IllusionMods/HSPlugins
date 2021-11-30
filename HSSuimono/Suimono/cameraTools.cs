using System;
using UnityEngine;
using System.Collections;

#if UNITY_5_4_OR_NEWER
	using UnityEngine.Rendering;
#endif



public enum suiCamToolType
{
	transparent, transparentCaustic, wakeEffects, normals, depthMask, localReflection,
	underwaterMask, underwater, shorelineObject, shorelineCapture
};


public enum suiCamToolRender
{
	automatic, deferredShading, deferredLighting, forward
};

public enum suiCamHdrMode
{
	off, on, automatic
};

public enum suiCamClearFlags
{
	automatic, skybox, color
};



namespace Suimono.Core
{


	[ExecuteInEditMode]
	public class cameraTools : MonoBehaviour
	{

		//Public Variables
		public suiCamToolType cameraType;
		public suiCamToolRender renderType = suiCamToolRender.automatic;
		public suiCamHdrMode hdrMode = suiCamHdrMode.off;
		public suiCamClearFlags clearMode = suiCamClearFlags.automatic;
		public Color clearFlagColor = Color.black;

		public int resolution = 256;
		public float cameraOffset = 0f;
		public float reflectionOffset = 0f;
		public RenderTexture renderTexDiff;
		public Shader renderShader;
		public bool executeInEditMode = false;
		public bool isUnderwater = false;

		[HideInInspector] public Renderer surfaceRenderer;
		[HideInInspector] public Renderer scaleRenderer;
		[HideInInspector] public float reflectionDistance = 200.0f;
		[HideInInspector] public int setLayers;


		//Private Variables
		private RenderingPath usePath;
		private SuimonoModule suimonoModuleObject;
		private Camera cam;
		private Camera copyCam;
		private int currResolution = 256;

		//Collect variables for reflection
		private float clipPlaneOffset = 0.07f;

		//Collect variables for GC
		private Vector3 pos;
		private Vector3 normal;
		private float d;
		private Vector4 reflectionPlane;
		private Matrix4x4 reflection;
		private Vector3 oldpos;
		private Vector3 newpos;
		private Vector4 clipPlane;
		private Matrix4x4 projection;
		private Vector3 euler;
		private Matrix4x4 scaleOffset;
		private Vector3 scale;
		private Matrix4x4 mtx;
		private Vector3 offsetPos;
		private Matrix4x4 m;
		private Vector3 cpos;
		private Vector3 cnormal;
		private Matrix4x4 proj;
		private Vector4 q;
		private Vector4 c;
		private float hasStarted = 0f;
		private Vector3 cameraPos = Vector3.zero;
		private Suimono_ShorelineObject shoreObject;
		private CameraClearFlags originalCamClearFlags;

		private void Start()
		{

			//Get object references
			this.suimonoModuleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));


			if (this.cameraType != suiCamToolType.localReflection)
			{
				if (this.transform.parent != null)
					this.surfaceRenderer = this.transform.parent.gameObject.GetComponent<Renderer>();
			}
			else
			{
				if (this.transform.parent != null)
				{
					this.surfaceRenderer = this.transform.parent.Find("Suimono_Object").gameObject.GetComponent<Renderer>();
					this.scaleRenderer = this.transform.parent.Find("Suimono_ObjectScale").gameObject.GetComponent<Renderer>();
				}
			}

			this.cam = this.gameObject.GetComponent<Camera>();
			this.originalCamClearFlags = this.cam.clearFlags;
			if (this.suimonoModuleObject != null && this.suimonoModuleObject.setCamera != null)
			{
				this.copyCam = this.suimonoModuleObject.setCamera.GetComponent<Camera>();
			}

			this.UpdateRenderTex();
			this.CameraUpdate();
		}

		private void OnPreRender()
		{
			if (Application.isPlaying && this.cameraType == suiCamToolType.localReflection)
			{
				GL.invertCulling = true;
			}
		}

		private void OnPostRender()
		{
			if (Application.isPlaying)
			{
				GL.invertCulling = false;
			}
		}

		private void Update()
		{

			//update shoreline camera during edit mode
			if (!Application.isPlaying && this.executeInEditMode)
			{
				this.CameraUpdate();
			}

			//set layermasks
			if (this.cam != null)
			{
				if (this.cameraType == suiCamToolType.shorelineCapture)
				{
					this.cam.cullingMask = 1 << this.suimonoModuleObject.layerDepthNum;
				}
			}
		}

		private void LateUpdate()
		{
			if (Application.isPlaying)
			{
				if (this.cameraType == suiCamToolType.shorelineObject)
				{
					if (this.hasStarted == 0f && Time.time > 0.2f)
					{
						this.CameraUpdate();
						this.hasStarted = 1f;
					}
				}
				else
				{
					this.CameraUpdate();
				}
			}
		}

		private void CameraRender()
		{
			//IBL Cubemap hide compatibility
			if (!this.isUnderwater && this.clearMode == suiCamClearFlags.automatic && this.copyCam != null && this.originalCamClearFlags == CameraClearFlags.Skybox)
				this.cam.clearFlags = this.copyCam.clearFlags;

			//Setup Camera Matrices
			if (this.cameraType == suiCamToolType.localReflection)
			{
				this.ReflectionPreRender();
			}

			//RENDER CAMERA
			this.cam.targetTexture = this.renderTexDiff;
			if (Application.isPlaying && this.cameraType == suiCamToolType.shorelineObject)
			{
				this.cam.enabled = false;

				this.cameraPos.y = 3.0f;
				this.cam.transform.localPosition = this.cameraPos;

				this.cam.nearClipPlane = 0.01f;
				this.cam.farClipPlane = 50f;

				this.cam.Render();
			}
			else
			{
				this.cam.enabled = true;
			}

			//Reset Camera Properties
			if (this.cameraType == suiCamToolType.localReflection)
				this.ReflectionPostRender();
		}





		public void CameraUpdate()
		{

			if (this.suimonoModuleObject != null)
			{
				if (this.suimonoModuleObject.setCameraComponent != null)
				{
					this.copyCam = this.suimonoModuleObject.setCameraComponent;
				}
			}

			if (this.copyCam != null && this.cam != null)
			{

				//set camera settings
				if (this.cameraType != suiCamToolType.shorelineObject)
				{
					this.cam.transform.position = this.copyCam.transform.position;
					this.cam.transform.rotation = this.copyCam.transform.rotation;
					this.cam.projectionMatrix = this.copyCam.projectionMatrix;
					this.cam.fieldOfView = this.copyCam.fieldOfView;
				}

				//re-project camera for screen-space effects
				if (this.cameraOffset != 0.0f)
				{
					this.cam.transform.Translate(Vector3.forward * this.cameraOffset);
				}

				//select rendering path
				if (this.renderType == suiCamToolRender.automatic)
				{
					this.usePath = this.copyCam.actualRenderingPath;

					//specific settings for transparent camera
					if (this.cameraType == suiCamToolType.transparent)
					{
						if (this.copyCam.renderingPath == RenderingPath.Forward)
						{
							this.usePath = RenderingPath.DeferredLighting;
						}
						else
						{
							this.usePath = this.copyCam.renderingPath;
						}
					}

				}
				else if (this.renderType == suiCamToolRender.deferredShading)
				{
					this.usePath = RenderingPath.DeferredShading;

				}
				else if (this.renderType == suiCamToolRender.deferredLighting)
				{
					this.usePath = RenderingPath.DeferredLighting;

				}
				else if (this.renderType == suiCamToolRender.forward)
				{
					this.usePath = RenderingPath.Forward;
				}

				//set effect rendering path
				this.cam.renderingPath = this.usePath;


				if (this.renderTexDiff != null)
				{

					//update texture resolution
					if (this.resolution != this.currResolution)
					{

						if (this.cameraType == suiCamToolType.shorelineObject)
						{
							this.shoreObject = this.transform.parent.gameObject.GetComponent<Suimono_ShorelineObject>();
							if (this.shoreObject != null)
							{
								this.resolution = this.shoreObject.useResolution;
							}
						}

						this.currResolution = this.resolution;
						this.UpdateRenderTex();
					}

					//render custom normal effects shader
					if (this.cameraType == suiCamToolType.normals)
					{
						if (this.suimonoModuleObject.enableAdvancedDistort)
						{
#if UNITY_5_6_OR_NEWER
								cam.allowHDR = false;
#else
							this.cam.hdr = false;
#endif

							this.cam.SetReplacementShader(this.renderShader, "RenderType");
							this.CameraRender();
						}
						else
						{
							this.renderTexDiff = null;
						}

						//render customwake effects shader
					}
					else if (this.cameraType == suiCamToolType.wakeEffects)
					{
						if (this.suimonoModuleObject.enableAdvancedDistort)
						{
							this.cam.SetReplacementShader(this.renderShader, "RenderType");
							this.CameraRender();
						}
						else
						{
							this.renderTexDiff = null;
						}

						//render transparency effects
					}
					else if (this.cameraType == suiCamToolType.transparent)
					{
						if (this.suimonoModuleObject.enableTransparency)
						{
							this.CameraRender();
						}
						else
						{
							this.renderTexDiff = null;
						}

						//render caustics effects
					}
					else if (this.cameraType == suiCamToolType.transparentCaustic)
					{
						if (this.suimonoModuleObject.enableCaustics)
						{
							this.CameraRender();
						}
						else
						{
							this.renderTexDiff = null;
						}

					}
					else
					{
						this.CameraRender();
					}



					//pass texture to shader
					if (this.cameraType == suiCamToolType.transparent)
					{
						Shader.SetGlobalTexture("_suimono_TransTex", this.renderTexDiff);
						if (!this.suimonoModuleObject.enableCausticsBlending) Shader.SetGlobalTexture("_suimono_CausticTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.transparentCaustic)
					{
						if (this.suimonoModuleObject.enableCausticsBlending) Shader.SetGlobalTexture("_suimono_CausticTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.wakeEffects)
					{
						Shader.SetGlobalTexture("_suimono_WakeTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.normals)
					{
						Shader.SetGlobalTexture("_suimono_NormalsTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.depthMask)
					{
						Shader.SetGlobalTexture("_suimono_depthMaskTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.underwaterMask)
					{
						Shader.SetGlobalTexture("_suimono_underwaterMaskTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.underwater)
					{
						Shader.SetGlobalTexture("_suimono_underwaterTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.localReflection)
					{
						if (this.surfaceRenderer != null)
							this.surfaceRenderer.sharedMaterial.SetTexture("_ReflectionTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.shorelineObject)
					{
						if (this.surfaceRenderer != null)
							this.surfaceRenderer.sharedMaterial.SetTexture("_MainTex", this.renderTexDiff);
					}
					if (this.cameraType == suiCamToolType.shorelineCapture)
					{
						Shader.SetGlobalTexture("_suimono_shorelineTex", this.renderTexDiff);
					}

				}
				else
				{
					this.UpdateRenderTex();
				}

			}

		}

		private void UpdateRenderTex()
		{

			if (this.resolution < 4)
				this.resolution = 4;

			if (this.renderTexDiff != null)
			{
				if (this.cam != null)
					this.cam.targetTexture = null;
				DestroyImmediate(this.renderTexDiff);
			}
			//renderTexDiff = new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
			//renderTexDiff = new RenderTexture(resolution,resolution,24,RenderTextureFormat.DefaultHDR,RenderTextureReadWrite.Linear);
			//renderTexDiff = new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
			this.renderTexDiff = new RenderTexture(this.resolution, this.resolution, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

#if UNITY_5_4_OR_NEWER
				renderTexDiff.dimension = TextureDimension.Tex2D;
#else
			this.renderTexDiff.isCubemap = false;
#endif

#if UNITY_2017_1_OR_NEWER
				renderTexDiff.autoGenerateMips = false;
#else
			this.renderTexDiff.generateMips = false;
#endif
			this.renderTexDiff.anisoLevel = 1;
			this.renderTexDiff.filterMode = FilterMode.Trilinear;
			this.renderTexDiff.wrapMode = TextureWrapMode.Clamp;
		}

		private void ReflectionPreRender()
		{

			// find out the reflection plane: position and normal in world space
			this.pos = this.transform.parent.position;

			if (this.isUnderwater)
			{
				this.normal = -this.transform.parent.transform.up; //underwater
			}
			else
			{
				this.normal = this.transform.parent.transform.up; //above water
			}

			//set camera properties
			this.cam.CopyFrom(this.copyCam);
			this.cam.backgroundColor = this.clearFlagColor;

			//turn hdr off
			if (this.hdrMode == suiCamHdrMode.off)
			{
#if UNITY_5_6_OR_NEWER
					cam.allowHDR = false;
#else
				this.cam.hdr = false;
#endif

			}
			else if (this.hdrMode == suiCamHdrMode.on)
			{
#if UNITY_5_6_OR_NEWER
					cam.allowHDR = true;
#else
				this.cam.hdr = true;
#endif
			}



			if (this.isUnderwater)
			{
				this.cam.farClipPlane = 3;
				this.cam.clearFlags = CameraClearFlags.Color;
				this.cam.depthTextureMode = DepthTextureMode.Depth;
			}
			else
			{
				//cam.farClipPlane = reflectionDistance;
				if (this.clearMode != suiCamClearFlags.automatic)
				{
					if (this.clearMode == suiCamClearFlags.skybox)
						this.cam.clearFlags = CameraClearFlags.Skybox;
					if (this.clearMode == suiCamClearFlags.color)
					{
						this.cam.clearFlags = CameraClearFlags.Color;
						this.cam.backgroundColor = this.clearFlagColor;
					}
				}
				else if (this.copyCam != null && this.originalCamClearFlags == CameraClearFlags.Skybox)
				{
					this.cam.clearFlags = this.copyCam.clearFlags;
				}

			}

			//render transparency effects
			if (this.cameraType == suiCamToolType.localReflection)
			{
				if (this.renderShader != null)
				{
					this.cam.SetReplacementShader(this.renderShader, null);

				}
			}

			this.cam.cullingMask = this.setLayers;

			// Render reflection
			// Reflect camera around reflection plane
			this.d = -Vector3.Dot(this.normal, this.pos) - this.clipPlaneOffset;
			this.reflectionPlane = new Vector4(this.normal.x, this.normal.y - this.reflectionOffset, this.normal.z, this.d);

			this.reflection = Matrix4x4.zero;
			this.reflection = this.Set_CalculateReflectionMatrix(this.reflectionPlane);

			this.oldpos = this.copyCam.transform.position;
			this.newpos = this.reflection.MultiplyPoint(this.oldpos);
			this.cam.worldToCameraMatrix = this.copyCam.worldToCameraMatrix * this.reflection;

			// Setup oblique projection matrix so that near plane is our reflection
			// plane. This way we clip everything below/above it for free.
			this.clipPlane = this.Set_CameraSpacePlane(this.cam, this.pos, this.normal, 1f);
			this.projection = this.copyCam.projectionMatrix;
			this.projection = this.Set_CalculateObliqueMatrix(this.clipPlane);
			this.cam.projectionMatrix = this.projection;

			GL.invertCulling = true;

			this.cam.transform.position = this.newpos;
			this.euler = this.copyCam.transform.eulerAngles;
			this.cam.transform.eulerAngles = new Vector3(0f, this.euler.y, this.euler.z);
		}

		private void ReflectionPostRender()
		{
			this.cam.transform.position = this.oldpos;
			GL.invertCulling = false;
			this.scaleOffset = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
			this.scale = this.transform.lossyScale;
			this.mtx = this.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.0f / this.scale.x, -1.0f / this.scale.y, 1.0f / this.scale.z));
			this.mtx = this.scaleOffset * this.copyCam.projectionMatrix * this.copyCam.worldToCameraMatrix * this.mtx;
		}


		public float Set_sgn(float a)
		{
			if (a > 0.0f) return 1.0f;
			if (a < 0.0f) return -1.0f;
			return 0.0f;
		}


		public Vector4 Set_CameraSpacePlane(Camera cm, Vector3 pos, Vector3 normal, float sideSign)
		{
			this.offsetPos = pos + normal * (this.clipPlaneOffset);
			this.m = cm.worldToCameraMatrix;
			this.cpos = this.m.MultiplyPoint(this.offsetPos);
			this.cnormal = this.m.MultiplyVector(normal).normalized * sideSign;
			return new Vector4(this.cnormal.x, this.cnormal.y, this.cnormal.z, -Vector3.Dot(this.cpos, this.cnormal));
		}


		public Matrix4x4 Set_CalculateObliqueMatrix(Vector4 clipPlane)
		{
			this.proj = this.copyCam.projectionMatrix;
			this.q = this.proj.inverse * new Vector4(this.Set_sgn(clipPlane.x), this.Set_sgn(clipPlane.y), 1f, 1f);
			this.c = clipPlane * (2f / (Vector4.Dot(clipPlane, this.q)));
			this.proj[2] = this.c.x - this.proj[3];
			this.proj[6] = this.c.y - this.proj[7];
			this.proj[10] = this.c.z - this.proj[11];
			this.proj[14] = this.c.w - this.proj[15];
			return this.proj;
		}


		public Matrix4x4 Set_CalculateReflectionMatrix(Vector4 plane)
		{

			Matrix4x4 reflectionMat = Matrix4x4.zero;

			reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
			reflectionMat.m01 = (-2F * plane[0] * plane[1]);
			reflectionMat.m02 = (-2F * plane[0] * plane[2]);
			reflectionMat.m03 = (-2F * plane[3] * plane[0]);

			reflectionMat.m10 = (-2F * plane[1] * plane[0]);
			reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
			reflectionMat.m12 = (-2F * plane[1] * plane[2]);
			reflectionMat.m13 = (-2F * plane[3] * plane[1]);

			reflectionMat.m20 = (-2F * plane[2] * plane[0]);
			reflectionMat.m21 = (-2F * plane[2] * plane[1]);
			reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
			reflectionMat.m23 = (-2F * plane[3] * plane[2]);

			reflectionMat.m30 = 0F;
			reflectionMat.m31 = 0F;
			reflectionMat.m32 = 0F;
			reflectionMat.m33 = 1F;

			return reflectionMat;
		}



	}
}