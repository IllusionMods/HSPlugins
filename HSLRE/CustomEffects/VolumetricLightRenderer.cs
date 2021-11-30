//  Copyright(c) 2016, Michal Skalsky
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification,
//  are permitted provided that the following conditions are met:
//
//  1. Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//
//  2. Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//
//  3. Neither the name of the copyright holder nor the names of its contributors
//     may be used to endorse or promote products derived from this software without
//     specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//  EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT
//  SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
//  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
//  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
//  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
//  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.



using UnityEngine;
using UnityEngine.Rendering;
using System;
using Harmony;
using Studio;
using Object = UnityEngine.Object;

namespace HSLRE.CustomEffects
{
	public class VolumetricLightRenderer : MonoBehaviour
	{
		public enum VolumetricResolution
		{
			Full,
			Half,
			Quarter
		};

		public static event Action<VolumetricLightRenderer, Matrix4x4> PreRenderEvent;

		private static Mesh _pointLightMesh;
		private static Mesh _spotLightMesh;
		private static Material _lightMaterial;

		private Camera _camera;
		private CommandBuffer _preLightPass;

		private Matrix4x4 _viewProj;
		private Material _blitAddMaterial;
		private Material _bilateralBlurMaterial;

		private RenderTexture _volumeLightTexture;
		private RenderTexture _halfVolumeLightTexture;
		private RenderTexture _quarterVolumeLightTexture;
		private static Texture _defaultSpotCookie;

		private RenderTexture _halfDepthBuffer;
		private RenderTexture _quarterDepthBuffer;
		private VolumetricResolution _currentResolution = VolumetricResolution.Half;
		private Texture2D _ditheringTexture;
		private Texture3D _noiseTexture;

		public bool VisualizeLightTexture = false;
		public VolumetricResolution Resolution = VolumetricResolution.Half;

		public CommandBuffer GlobalCommandBuffer { get { return this._preLightPass; } }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static Material GetLightMaterial()
		{
			return _lightMaterial;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static Mesh GetPointLightMesh()
		{
			return _pointLightMesh;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static Mesh GetSpotLightMesh()
		{
			return _spotLightMesh;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public RenderTexture GetVolumeLightBuffer()
		{
			if (this.Resolution == VolumetricResolution.Quarter)
				return this._quarterVolumeLightTexture;
			else if (this.Resolution == VolumetricResolution.Half)
				return this._halfVolumeLightTexture;
			else
				return this._volumeLightTexture;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public RenderTexture GetVolumeLightDepthBuffer()
		{
			if (this.Resolution == VolumetricResolution.Quarter)
				return this._quarterDepthBuffer;
			else if (this.Resolution == VolumetricResolution.Half)
				return this._halfDepthBuffer;
			else
				return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static Texture GetDefaultSpotCookie()
		{
			return _defaultSpotCookie;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Awake()
		{
			this._camera = this.GetComponent<Camera>();
			if (this._camera.actualRenderingPath == RenderingPath.Forward)
				this._camera.depthTextureMode = DepthTextureMode.Depth;

			this._currentResolution = this.Resolution;

			Shader shader = HSLRE.self._resources.LoadAsset<Shader>("BlitAdd");
			if (shader == null)
				throw new Exception("Critical Error: \"Hidden/BlitAdd\" shader is missing. Make sure it is included in \"Always Included Shaders\" in ProjectSettings/Graphics.");
			this._blitAddMaterial = new Material(shader);

			shader = HSLRE.self._resources.LoadAsset<Shader>("BilateralBlur");
			if (shader == null)
				throw new Exception("Critical Error: \"Hidden/BilateralBlur\" shader is missing. Make sure it is included in \"Always Included Shaders\" in ProjectSettings/Graphics.");
			this._bilateralBlurMaterial = new Material(shader);

			this._preLightPass = new CommandBuffer();
			this._preLightPass.name = "PreLight";

			this.ChangeResolution();

			if (_pointLightMesh == null)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				_pointLightMesh = go.GetComponent<MeshFilter>().sharedMesh;
				Object.Destroy(go);
			}

			if (_spotLightMesh == null)
			{
				_spotLightMesh = this.CreateSpotLightMesh();
			}

			if (_lightMaterial == null)
			{
				shader = HSLRE.self._resources.LoadAsset<Shader>("VolumetricLight");
				if (shader == null)
					throw new Exception("Critical Error: \"Sandbox/VolumetricLight\" shader is missing. Make sure it is included in \"Always Included Shaders\" in ProjectSettings/Graphics.");
				_lightMaterial = new Material(shader);
			}

			_defaultSpotCookie = HSLRE.self._resources.LoadAsset<Texture2D>("spot");

			this.LoadNoise3dTexture();
			this.GenerateDitherTexture();
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnEnable()
		{
			//_camera.RemoveAllCommandBuffers();
			this._camera.AddCommandBuffer(this._camera.actualRenderingPath == RenderingPath.Forward ? CameraEvent.AfterDepthTexture : CameraEvent.BeforeLighting, this._preLightPass);
			foreach (VolumetricLight volumetricLight in Resources.FindObjectsOfTypeAll<VolumetricLight>())
				volumetricLight.enabled = true;
		}

		/// <summary>
		/// 
		/// </summary>
		private void OnDisable()
		{
			//_camera.RemoveAllCommandBuffers();
			this._camera.RemoveCommandBuffer(this._camera.actualRenderingPath == RenderingPath.Forward ? CameraEvent.AfterDepthTexture : CameraEvent.BeforeLighting, this._preLightPass);
			foreach (VolumetricLight volumetricLight in Resources.FindObjectsOfTypeAll<VolumetricLight>())
				volumetricLight.enabled = false;
		}

		/// <summary>
		/// 
		/// </summary>
		private void ChangeResolution()
		{
			int width = this._camera.pixelWidth;
			int height = this._camera.pixelHeight;

			if (this._volumeLightTexture != null)
				Object.Destroy(this._volumeLightTexture);

			this._volumeLightTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
			this._volumeLightTexture.name = "VolumeLightBuffer";
			this._volumeLightTexture.filterMode = FilterMode.Bilinear;

			if (this._halfDepthBuffer != null)
				Object.Destroy(this._halfDepthBuffer);
			if (this._halfVolumeLightTexture != null)
				Object.Destroy(this._halfVolumeLightTexture);

			if (this.Resolution == VolumetricResolution.Half || this.Resolution == VolumetricResolution.Quarter)
			{
				this._halfVolumeLightTexture = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
				this._halfVolumeLightTexture.name = "VolumeLightBufferHalf";
				this._halfVolumeLightTexture.filterMode = FilterMode.Bilinear;

				this._halfDepthBuffer = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.RFloat);
				this._halfDepthBuffer.name = "VolumeLightHalfDepth";
				this._halfDepthBuffer.Create();
				this._halfDepthBuffer.filterMode = FilterMode.Point;
			}

			if (this._quarterVolumeLightTexture != null)
				Object.Destroy(this._quarterVolumeLightTexture);
			if (this._quarterDepthBuffer != null)
				Object.Destroy(this._quarterDepthBuffer);

			if (this.Resolution == VolumetricResolution.Quarter)
			{
				this._quarterVolumeLightTexture = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.ARGBHalf);
				this._quarterVolumeLightTexture.name = "VolumeLightBufferQuarter";
				this._quarterVolumeLightTexture.filterMode = FilterMode.Bilinear;

				this._quarterDepthBuffer = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.RFloat);
				this._quarterDepthBuffer.name = "VolumeLightQuarterDepth";
				this._quarterDepthBuffer.Create();
				this._quarterDepthBuffer.filterMode = FilterMode.Point;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void OnPreRender()
		{
			// use very low value for near clip plane to simplify cone/frustum intersection
			Matrix4x4 proj = Matrix4x4.Perspective(this._camera.fieldOfView, this._camera.aspect, 0.01f, this._camera.farClipPlane);
			proj = GL.GetGPUProjectionMatrix(proj, true);
			this._viewProj = proj * this._camera.worldToCameraMatrix;

			this._preLightPass.Clear();

			bool dx11 = SystemInfo.graphicsShaderLevel > 40;

			switch (this.Resolution)
			{
				case VolumetricResolution.Quarter:
				{
					Texture nullTexture = null;
					// down sample depth to half res
					this._preLightPass.Blit(nullTexture, this._halfDepthBuffer, this._bilateralBlurMaterial, dx11 ? 4 : 10);
					// down sample depth to quarter res
					this._preLightPass.Blit(nullTexture, this._quarterDepthBuffer, this._bilateralBlurMaterial, dx11 ? 6 : 11);

					this._preLightPass.SetRenderTarget(this._quarterVolumeLightTexture);
					break;
				}
				case VolumetricResolution.Half:
				{
					Texture nullTexture = null;
					// down sample depth to half res
					this._preLightPass.Blit(nullTexture, this._halfDepthBuffer, this._bilateralBlurMaterial, dx11 ? 4 : 10);

					this._preLightPass.SetRenderTarget(this._halfVolumeLightTexture);
					break;
				}
				default:
					this._preLightPass.SetRenderTarget(this._volumeLightTexture);
					break;
			}

			this._preLightPass.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));

			this.UpdateMaterialParameters();

			if (PreRenderEvent != null)
				PreRenderEvent(this, this._viewProj);
		}

		[ImageEffectOpaque]
		public void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			switch (this.Resolution)
			{
				case VolumetricResolution.Quarter:
				{
					RenderTexture temp = RenderTexture.GetTemporary(this._quarterDepthBuffer.width, this._quarterDepthBuffer.height, 0, RenderTextureFormat.ARGBHalf);
					temp.filterMode = FilterMode.Bilinear;

					// horizontal bilateral blur at quarter res
					Graphics.Blit(this._quarterVolumeLightTexture, temp, this._bilateralBlurMaterial, 8);
					// vertical bilateral blur at quarter res
					Graphics.Blit(temp, this._quarterVolumeLightTexture, this._bilateralBlurMaterial, 9);

					// upscale to full res
					Graphics.Blit(this._quarterVolumeLightTexture, this._volumeLightTexture, this._bilateralBlurMaterial, 7);

					RenderTexture.ReleaseTemporary(temp);
					break;
				}
				case VolumetricResolution.Half:
				{
					RenderTexture temp = RenderTexture.GetTemporary(this._halfVolumeLightTexture.width, this._halfVolumeLightTexture.height, 0, RenderTextureFormat.ARGBHalf);
					temp.filterMode = FilterMode.Bilinear;

					// horizontal bilateral blur at half res
					Graphics.Blit(this._halfVolumeLightTexture, temp, this._bilateralBlurMaterial, 2);

					// vertical bilateral blur at half res
					Graphics.Blit(temp, this._halfVolumeLightTexture, this._bilateralBlurMaterial, 3);

					// upscale to full res
					Graphics.Blit(this._halfVolumeLightTexture, this._volumeLightTexture, this._bilateralBlurMaterial, 5);
					RenderTexture.ReleaseTemporary(temp);
					break;
				}
				default:
				{
					RenderTexture temp = RenderTexture.GetTemporary(this._volumeLightTexture.width, this._volumeLightTexture.height, 0, RenderTextureFormat.ARGBHalf);
					temp.filterMode = FilterMode.Bilinear;

					// horizontal bilateral blur at full res
					Graphics.Blit(this._volumeLightTexture, temp, this._bilateralBlurMaterial, 0);
					// vertical bilateral blur at full res
					Graphics.Blit(temp, this._volumeLightTexture, this._bilateralBlurMaterial, 1);
					RenderTexture.ReleaseTemporary(temp);
					break;
				}
			}

			if (this.VisualizeLightTexture)
				Graphics.Blit(this._volumeLightTexture, destination);
			else
			{
				// add volume light buffer to rendered scene
				this._blitAddMaterial.SetTexture("_Source", source);
				Graphics.Blit(this._volumeLightTexture, destination, this._blitAddMaterial, 0);
			}
		}

		private void UpdateMaterialParameters()
		{
			this._bilateralBlurMaterial.SetTexture("_HalfResDepthBuffer", this._halfDepthBuffer);
			this._bilateralBlurMaterial.SetTexture("_HalfResColor", this._halfVolumeLightTexture);
			this._bilateralBlurMaterial.SetTexture("_QuarterResDepthBuffer", this._quarterDepthBuffer);
			this._bilateralBlurMaterial.SetTexture("_QuarterResColor", this._quarterVolumeLightTexture);

			Shader.SetGlobalTexture("_DitherTexture", this._ditheringTexture);
			Shader.SetGlobalTexture("_NoiseTexture", this._noiseTexture);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Update()
		{
			//#if UNITY_EDITOR
			if (this._currentResolution != this.Resolution)
			{
				this._currentResolution = this.Resolution;
				this.ChangeResolution();
			}

			if ((this._volumeLightTexture.width != this._camera.pixelWidth || this._volumeLightTexture.height != this._camera.pixelHeight))
				this.ChangeResolution();
			//#endif
		}

		/// <summary>
		/// 
		/// </summary>
		private void LoadNoise3dTexture()
		{
			// basic dds loader for 3d texture - !not very robust!

			TextAsset data = HSLRE.self._resources.LoadAsset<TextAsset>("NoiseVolume");

			byte[] bytes = data.bytes;

			uint height = BitConverter.ToUInt32(data.bytes, 12);
			uint width = BitConverter.ToUInt32(data.bytes, 16);
			uint pitch = BitConverter.ToUInt32(data.bytes, 20);
			uint depth = BitConverter.ToUInt32(data.bytes, 24);
			uint formatFlags = BitConverter.ToUInt32(data.bytes, 20 * 4);
			//uint fourCC = BitConverter.ToUInt32(data.bytes, 21 * 4);
			uint bitdepth = BitConverter.ToUInt32(data.bytes, 22 * 4);
			if (bitdepth == 0)
				bitdepth = pitch / width * 8;


			// doesn't work with TextureFormat.Alpha8 for some reason
			this._noiseTexture = new Texture3D((int)width, (int)height, (int)depth, TextureFormat.RGBA32, false);
			this._noiseTexture.name = "3D Noise";

			Color[] c = new Color[width * height * depth];

			uint index = 128;
			if (data.bytes[21 * 4] == 'D' && data.bytes[21 * 4 + 1] == 'X' && data.bytes[21 * 4 + 2] == '1' && data.bytes[21 * 4 + 3] == '0' &&
			    (formatFlags & 0x4) != 0)
			{
				uint format = BitConverter.ToUInt32(data.bytes, (int)index);
				if (format >= 60 && format <= 65)
					bitdepth = 8;
				else if (format >= 48 && format <= 52)
					bitdepth = 16;
				else if (format >= 27 && format <= 32)
					bitdepth = 32;

				//Debug.Log("DXGI format: " + format);
				// dx10 format, skip dx10 header
				//Debug.Log("DX10 format");
				index += 20;
			}

			uint byteDepth = bitdepth / 8;
			pitch = (width * bitdepth + 7) / 8;

			for (int d = 0; d < depth; ++d)
			{
				//index = 128;
				for (int h = 0; h < height; ++h)
				{
					for (int w = 0; w < width; ++w)
					{
						float v = (bytes[index + w * byteDepth] / 255.0f);
						c[w + h * width + d * width * height] = new Color(v, v, v, v);
					}

					index += pitch;
				}
			}

			this._noiseTexture.SetPixels(c);
			this._noiseTexture.Apply();
		}

		/// <summary>
		/// 
		/// </summary>
		private void GenerateDitherTexture()
		{
			if (this._ditheringTexture != null)
			{
				return;
			}

			int size = 8;
#if DITHER_4_4
        size = 4;
#endif
			// again, I couldn't make it work with Alpha8
			this._ditheringTexture = new Texture2D(size, size, TextureFormat.Alpha8, false, true);
			this._ditheringTexture.filterMode = FilterMode.Point;
			Color32[] c = new Color32[size * size];

			byte b;
#if DITHER_4_4
        b = (byte)(0.0f / 16.0f * 255); c[0] = new Color32(b, b, b, b);
        b = (byte)(8.0f / 16.0f * 255); c[1] = new Color32(b, b, b, b);
        b = (byte)(2.0f / 16.0f * 255); c[2] = new Color32(b, b, b, b);
        b = (byte)(10.0f / 16.0f * 255); c[3] = new Color32(b, b, b, b);

        b = (byte)(12.0f / 16.0f * 255); c[4] = new Color32(b, b, b, b);
        b = (byte)(4.0f / 16.0f * 255); c[5] = new Color32(b, b, b, b);
        b = (byte)(14.0f / 16.0f * 255); c[6] = new Color32(b, b, b, b);
        b = (byte)(6.0f / 16.0f * 255); c[7] = new Color32(b, b, b, b);

        b = (byte)(3.0f / 16.0f * 255); c[8] = new Color32(b, b, b, b);
        b = (byte)(11.0f / 16.0f * 255); c[9] = new Color32(b, b, b, b);
        b = (byte)(1.0f / 16.0f * 255); c[10] = new Color32(b, b, b, b);
        b = (byte)(9.0f / 16.0f * 255); c[11] = new Color32(b, b, b, b);

        b = (byte)(15.0f / 16.0f * 255); c[12] = new Color32(b, b, b, b);
        b = (byte)(7.0f / 16.0f * 255); c[13] = new Color32(b, b, b, b);
        b = (byte)(13.0f / 16.0f * 255); c[14] = new Color32(b, b, b, b);
        b = (byte)(5.0f / 16.0f * 255); c[15] = new Color32(b, b, b, b);
#else
			int i = 0;
			b = (byte)(1.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(49.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(13.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(61.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(4.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(52.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(16.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(64.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(33.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(17.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(45.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(29.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(36.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(20.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(48.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(32.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(9.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(57.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(5.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(53.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(12.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(60.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(8.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(56.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(41.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(25.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(37.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(21.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(44.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(28.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(40.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(24.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(3.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(51.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(15.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(63.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(2.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(50.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(14.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(62.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(35.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(19.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(47.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(31.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(34.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(18.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(46.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(30.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(11.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(59.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(7.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(55.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(10.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(58.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(6.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(54.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);

			b = (byte)(43.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(27.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(39.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(23.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(42.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(26.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(38.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
			b = (byte)(22.0f / 65.0f * 255);
			c[i++] = new Color32(b, b, b, b);
#endif

			this._ditheringTexture.SetPixels32(c);
			this._ditheringTexture.Apply();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private Mesh CreateSpotLightMesh()
		{
			// copy & pasted from other project, the geometry is too complex, should be simplified
			Mesh mesh = new Mesh();

			const int segmentCount = 16;
			Vector3[] vertices = new Vector3[2 + segmentCount * 3];
			Color32[] colors = new Color32[2 + segmentCount * 3];

			vertices[0] = new Vector3(0, 0, 0);
			vertices[1] = new Vector3(0, 0, 1);

			float angle = 0;
			float step = Mathf.PI * 2.0f / segmentCount;
			float ratio = 0.9f;

			for (int i = 0; i < segmentCount; ++i)
			{
				vertices[i + 2] = new Vector3(-Mathf.Cos(angle) * ratio, Mathf.Sin(angle) * ratio, ratio);
				colors[i + 2] = new Color32(255, 255, 255, 255);
				vertices[i + 2 + segmentCount] = new Vector3(-Mathf.Cos(angle), Mathf.Sin(angle), 1);
				colors[i + 2 + segmentCount] = new Color32(255, 255, 255, 0);
				vertices[i + 2 + segmentCount * 2] = new Vector3(-Mathf.Cos(angle) * ratio, Mathf.Sin(angle) * ratio, 1);
				colors[i + 2 + segmentCount * 2] = new Color32(255, 255, 255, 255);
				angle += step;
			}

			mesh.vertices = vertices;
			mesh.colors32 = colors;

			int[] indices = new int[segmentCount * 3 * 2 + segmentCount * 6 * 2];
			int index = 0;

			for (int i = 2; i < segmentCount + 1; ++i)
			{
				indices[index++] = 0;
				indices[index++] = i;
				indices[index++] = i + 1;
			}

			indices[index++] = 0;
			indices[index++] = segmentCount + 1;
			indices[index++] = 2;

			for (int i = 2; i < segmentCount + 1; ++i)
			{
				indices[index++] = i;
				indices[index++] = i + segmentCount;
				indices[index++] = i + 1;

				indices[index++] = i + 1;
				indices[index++] = i + segmentCount;
				indices[index++] = i + segmentCount + 1;
			}

			indices[index++] = 2;
			indices[index++] = 1 + segmentCount;
			indices[index++] = 2 + segmentCount;

			indices[index++] = 2 + segmentCount;
			indices[index++] = 1 + segmentCount;
			indices[index++] = 1 + segmentCount + segmentCount;

			//------------
			for (int i = 2 + segmentCount; i < segmentCount + 1 + segmentCount; ++i)
			{
				indices[index++] = i;
				indices[index++] = i + segmentCount;
				indices[index++] = i + 1;

				indices[index++] = i + 1;
				indices[index++] = i + segmentCount;
				indices[index++] = i + segmentCount + 1;
			}

			indices[index++] = 2 + segmentCount;
			indices[index++] = 1 + segmentCount * 2;
			indices[index++] = 2 + segmentCount * 2;

			indices[index++] = 2 + segmentCount * 2;
			indices[index++] = 1 + segmentCount * 2;
			indices[index++] = 1 + segmentCount * 3;

			////-------------------------------------
			for (int i = 2 + segmentCount * 2; i < segmentCount * 3 + 1; ++i)
			{
				indices[index++] = 1;
				indices[index++] = i + 1;
				indices[index++] = i;
			}

			indices[index++] = 1;
			indices[index++] = 2 + segmentCount * 2;
			indices[index++] = segmentCount * 3 + 1;

			mesh.triangles = indices;
			mesh.RecalculateBounds();

			return mesh;
		}
	}

	[HarmonyPatch(typeof(AddObjectLight), "Load", typeof(OILightInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int))]
	internal static class OCILight_Ctor_Patches
	{
		private static void Postfix(OCILight __result)
		{
			if (__result != null && __result.light != null)
				__result.light.gameObject.AddComponent<VolumetricLight>();
		}
	}
}