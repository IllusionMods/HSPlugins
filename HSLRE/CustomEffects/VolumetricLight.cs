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

namespace HSLRE.CustomEffects
{

	public class VolumetricLight : MonoBehaviour
	{
		public event Action<VolumetricLightRenderer, VolumetricLight, CommandBuffer, Matrix4x4> CustomRenderEvent;

		private Light _light;
		private Material _material;
		private CommandBuffer _commandBuffer;
		private CommandBuffer _cascadeShadowCommandBuffer;

		[Range(1, 64)] public int SampleCount = 8;
		[Range(0.0f, 1.0f)] public float ScatteringCoef = 0.5f;
		[Range(0.0f, 0.1f)] public float ExtinctionCoef = 0.01f;
		[Range(0.0f, 1.0f)] public float SkyboxExtinctionCoef = 0.9f;
		[Range(0.0f, 0.999f)] public float MieG = 0.1f;
		public bool HeightFog = false;
		[Range(0, 0.5f)] public float HeightScale = 0.10f;
		public float GroundLevel = 0;
		public bool Noise = false;
		public float NoiseScale = 0.015f;
		public float NoiseIntensity = 1.0f;
		public float NoiseIntensityOffset = 0.3f;
		public Vector2 NoiseVelocity = new Vector2(3.0f, 3.0f);

		[Tooltip("")] public float MaxRayLength = 400.0f;

		public Light Light { get { return this._light; } }
		public Material VolumetricMaterial { get { return this._material; } }

		private readonly Vector4[] _frustumCorners = new Vector4[4];

		private bool _reversedZ = false;

		private void Awake()
		{
			this._light = this.GetComponent<Light>();
			this._commandBuffer = new CommandBuffer();
			this._commandBuffer.name = "Light Command Buffer";

			this._cascadeShadowCommandBuffer = new CommandBuffer();
			this._cascadeShadowCommandBuffer.name = "Dir Light Command Buffer";
			this._cascadeShadowCommandBuffer.SetGlobalTexture("_CascadeShadowMapTexture", new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive));
		}

		/// <summary>
		/// 
		/// </summary>
		private void Start()
		{ 
			//Shader shader = HSLRE.self._resources.LoadAsset<Shader>("VolumetricLight");
			//if (shader == null)
			//	throw new Exception("Critical Error: \"Sandbox/VolumetricLight\" shader is missing. Make sure it is included in \"Always Included Shaders\" in ProjectSettings/Graphics.");
			this._material = new Material(VolumetricLightRenderer.GetLightMaterial());
			this.enabled = HSLRE.self.effectsDictionary[HSLRE.self.volumetricLights].enabled;
		}

		/// <summary>
		/// 
		/// </summary>
		void OnEnable()
		{
			VolumetricLightRenderer.PreRenderEvent += this.VolumetricLightRenderer_PreRenderEvent;

			if (this._light.type == LightType.Directional)
			{
				this._light.AddCommandBuffer(LightEvent.BeforeScreenspaceMask, this._commandBuffer);
				this._light.AddCommandBuffer(LightEvent.AfterShadowMap, this._cascadeShadowCommandBuffer);

			}
			else
				this._light.AddCommandBuffer(LightEvent.AfterShadowMap, this._commandBuffer);
		}

		/// <summary>
		/// 
		/// </summary>
		void OnDisable()
		{
			VolumetricLightRenderer.PreRenderEvent -= this.VolumetricLightRenderer_PreRenderEvent;

			if (this._light.type == LightType.Directional)
			{
				this._light.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, this._commandBuffer);
				this._light.RemoveCommandBuffer(LightEvent.AfterShadowMap, this._cascadeShadowCommandBuffer);

			}
			else
				this._light.RemoveCommandBuffer(LightEvent.AfterShadowMap, this._commandBuffer);
		}

		/// <summary>
		/// 
		/// </summary>
		public void OnDestroy()
		{
			Destroy(this._material);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="viewProj"></param>
		private void VolumetricLightRenderer_PreRenderEvent(VolumetricLightRenderer renderer, Matrix4x4 viewProj)
		{
			// light was destroyed without deregistring, deregister now
			if (this._light == null || this._light.gameObject == null)
			{
				VolumetricLightRenderer.PreRenderEvent -= this.VolumetricLightRenderer_PreRenderEvent;
				return;
			}

			if (!this._light.gameObject.activeInHierarchy || this._light.enabled == false)
				return;

			this._material.SetVector("_CameraForward", Camera.current.transform.forward);

			this._material.SetInt("_SampleCount", this.SampleCount);
			this._material.SetVector("_NoiseVelocity", new Vector4(this.NoiseVelocity.x, this.NoiseVelocity.y) * this.NoiseScale);
			this._material.SetVector("_NoiseData", new Vector4(this.NoiseScale, this.NoiseIntensity, this.NoiseIntensityOffset));
			this._material.SetVector("_MieG", new Vector4(1 - (this.MieG * this.MieG), 1 + (this.MieG * this.MieG), 2 * this.MieG, 1.0f / (4.0f * Mathf.PI)));
			this._material.SetVector("_VolumetricLight", new Vector4(this.ScatteringCoef, this.ExtinctionCoef, this._light.range, 1.0f - this.SkyboxExtinctionCoef));

			this._material.SetTexture("_CameraDepthTexture", renderer.GetVolumeLightDepthBuffer());

			//if (renderer.Resolution == VolumetricLightRenderer.VolumetricResolution.Full)
			{
				//_material.SetFloat("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
				//_material.DisableKeyword("MANUAL_ZTEST");            
			}
			//else
			{
				this._material.SetFloat("_ZTest", (int)CompareFunction.Always);
				// downsampled light buffer can't use native zbuffer for ztest, try to perform ztest in pixel shader to avoid ray marching for occulded geometry 
				//_material.EnableKeyword("MANUAL_ZTEST");
			}

			if (this.HeightFog)
			{
				this._material.EnableKeyword("HEIGHT_FOG");

				this._material.SetVector("_HeightFog", new Vector4(this.GroundLevel, this.HeightScale));
			}
			else
			{
				this._material.DisableKeyword("HEIGHT_FOG");
			}

			switch (this._light.type)
			{
				case LightType.Point:
					this.SetupPointLight(renderer, viewProj);
					break;
				case LightType.Spot:
					this.SetupSpotLight(renderer, viewProj);
					break;
				case LightType.Directional:
					this.SetupDirectionalLight(renderer, viewProj);
					break;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="viewProj"></param>
		private void SetupPointLight(VolumetricLightRenderer renderer, Matrix4x4 viewProj)
		{
			this._commandBuffer.Clear();

			int pass = 0;
			if (!this.IsCameraInPointLightBounds())
				pass = 2;

			this._material.SetPass(pass);

			Mesh mesh = VolumetricLightRenderer.GetPointLightMesh();

			float scale = this._light.range * 2.0f;
			Matrix4x4 world = Matrix4x4.TRS(this.transform.position, this._light.transform.rotation, new Vector3(scale, scale, scale));

			this._material.SetMatrix("_WorldViewProj", viewProj * world);
			this._material.SetMatrix("_WorldView", Camera.current.worldToCameraMatrix * world);

			if (this.Noise)
				this._material.EnableKeyword("NOISE");
			else
				this._material.DisableKeyword("NOISE");

			this._material.SetVector("_LightPos", new Vector4(this._light.transform.position.x, this._light.transform.position.y, this._light.transform.position.z, 1.0f / (this._light.range * this._light.range)));
			this._material.SetColor("_LightColor", this._light.color * this._light.intensity);

			if (this._light.cookie == null)
			{
				this._material.EnableKeyword("POINT");
				this._material.DisableKeyword("POINT_COOKIE");
			}
			else
			{
				Matrix4x4 view = Matrix4x4.TRS(this._light.transform.position, this._light.transform.rotation, Vector3.one).inverse;
				this._material.SetMatrix("_MyLightMatrix0", view);

				this._material.EnableKeyword("POINT_COOKIE");
				this._material.DisableKeyword("POINT");

				this._material.SetTexture("_LightTexture0", this._light.cookie);
			}

			bool forceShadowsOff = (this._light.transform.position - Camera.current.transform.position).magnitude >= QualitySettings.shadowDistance;

			if (this._light.shadows != LightShadows.None && forceShadowsOff == false)
			{
				this._material.EnableKeyword("SHADOWS_CUBE");
				this._commandBuffer.SetGlobalTexture("_ShadowMapTexture", BuiltinRenderTextureType.CurrentActive);
				this._commandBuffer.SetRenderTarget(renderer.GetVolumeLightBuffer());

				this._commandBuffer.DrawMesh(mesh, world, this._material, 0, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, this._commandBuffer, viewProj);
			}
			else
			{
				this._material.DisableKeyword("SHADOWS_CUBE");
				renderer.GlobalCommandBuffer.DrawMesh(mesh, world, this._material, 0, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, renderer.GlobalCommandBuffer, viewProj);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="viewProj"></param>
		private void SetupSpotLight(VolumetricLightRenderer renderer, Matrix4x4 viewProj)
		{
			this._commandBuffer.Clear();

			int pass = 1;
			if (!this.IsCameraInSpotLightBounds())
			{
				pass = 3;
			}

			Mesh mesh = VolumetricLightRenderer.GetSpotLightMesh();

			float scale = this._light.range;
			float angleScale = Mathf.Tan((this._light.spotAngle + 1) * 0.5f * Mathf.Deg2Rad) * this._light.range;

			Matrix4x4 world = Matrix4x4.TRS(this.transform.position, this.transform.rotation, new Vector3(angleScale, angleScale, scale));

			Matrix4x4 view = Matrix4x4.TRS(this._light.transform.position, this._light.transform.rotation, Vector3.one).inverse;

			Matrix4x4 clip = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.0f), Quaternion.identity, new Vector3(-0.5f, -0.5f, 1.0f));
			Matrix4x4 proj = Matrix4x4.Perspective(this._light.spotAngle, 1, 0, 1);

			this._material.SetMatrix("_MyLightMatrix0", clip * proj * view);

			this._material.SetMatrix("_WorldViewProj", viewProj * world);

			this._material.SetVector("_LightPos", new Vector4(this._light.transform.position.x, this._light.transform.position.y, this._light.transform.position.z, 1.0f / (this._light.range * this._light.range)));
			this._material.SetVector("_LightColor", this._light.color * this._light.intensity);


			Vector3 apex = this.transform.position;
			Vector3 axis = this.transform.forward;
			// plane equation ax + by + cz + d = 0; precompute d here to lighten the shader
			Vector3 center = apex + axis * this._light.range;
			float d = -Vector3.Dot(center, axis);

			// update material
			this._material.SetFloat("_PlaneD", d);
			this._material.SetFloat("_CosAngle", Mathf.Cos((this._light.spotAngle + 1) * 0.5f * Mathf.Deg2Rad));

			this._material.SetVector("_ConeApex", new Vector4(apex.x, apex.y, apex.z));
			this._material.SetVector("_ConeAxis", new Vector4(axis.x, axis.y, axis.z));

			this._material.EnableKeyword("SPOT");

			if (this.Noise)
				this._material.EnableKeyword("NOISE");
			else
				this._material.DisableKeyword("NOISE");

			this._material.SetTexture("_LightTexture0", this._light.cookie == null ? VolumetricLightRenderer.GetDefaultSpotCookie() : this._light.cookie);

			bool forceShadowsOff = (this._light.transform.position - Camera.current.transform.position).magnitude >= QualitySettings.shadowDistance;

			if (this._light.shadows != LightShadows.None && forceShadowsOff == false)
			{
				clip = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));

				proj = this._reversedZ ? Matrix4x4.Perspective(this._light.spotAngle, 1, this._light.range, this._light.shadowNearPlane) : Matrix4x4.Perspective(this._light.spotAngle, 1, this._light.shadowNearPlane, this._light.range);

				Matrix4x4 m = clip * proj;
				m[0, 2] *= -1;
				m[1, 2] *= -1;
				m[2, 2] *= -1;
				m[3, 2] *= -1;

				//view = _light.transform.worldToLocalMatrix;
				this._material.SetMatrix("_MyWorld2Shadow", m * view);
				this._material.SetMatrix("_WorldView", m * view);

				this._material.EnableKeyword("SHADOWS_DEPTH");
				this._commandBuffer.SetGlobalTexture("_ShadowMapTexture", BuiltinRenderTextureType.CurrentActive);
				this._commandBuffer.SetRenderTarget(renderer.GetVolumeLightBuffer());

				this._commandBuffer.DrawMesh(mesh, world, this._material, 0, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, this._commandBuffer, viewProj);
			}
			else
			{
				this._material.DisableKeyword("SHADOWS_DEPTH");
				renderer.GlobalCommandBuffer.DrawMesh(mesh, world, this._material, 0, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, renderer.GlobalCommandBuffer, viewProj);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="viewProj"></param>
		private void SetupDirectionalLight(VolumetricLightRenderer renderer, Matrix4x4 viewProj)
		{
			this._commandBuffer.Clear();

			int pass = 4;

			this._material.SetPass(pass);

			if (this.Noise)
				this._material.EnableKeyword("NOISE");
			else
				this._material.DisableKeyword("NOISE");

			this._material.SetVector("_LightDir", new Vector4(this._light.transform.forward.x, this._light.transform.forward.y, this._light.transform.forward.z, 1.0f / (this._light.range * this._light.range)));
			this._material.SetVector("_LightColor", this._light.color * this._light.intensity);
			this._material.SetFloat("_MaxRayLength", this.MaxRayLength);

			if (this._light.cookie == null)
			{
				this._material.EnableKeyword("DIRECTIONAL");
				this._material.DisableKeyword("DIRECTIONAL_COOKIE");
			}
			else
			{
				this._material.EnableKeyword("DIRECTIONAL_COOKIE");
				this._material.DisableKeyword("DIRECTIONAL");

				this._material.SetTexture("_LightTexture0", this._light.cookie);
			}

			// setup frustum corners for world position reconstruction
			// bottom left
			this._frustumCorners[0] = Camera.current.ViewportToWorldPoint(new Vector3(0, 0, Camera.current.farClipPlane));
			// top left
			this._frustumCorners[2] = Camera.current.ViewportToWorldPoint(new Vector3(0, 1, Camera.current.farClipPlane));
			// top right
			this._frustumCorners[3] = Camera.current.ViewportToWorldPoint(new Vector3(1, 1, Camera.current.farClipPlane));
			// bottom right
			this._frustumCorners[1] = Camera.current.ViewportToWorldPoint(new Vector3(1, 0, Camera.current.farClipPlane));

#if UNITY_5_4_OR_NEWER
        _material.SetVectorArray("_FrustumCorners", _frustumCorners);
#else
			this._material.SetVector("_FrustumCorners0", this._frustumCorners[0]);
			this._material.SetVector("_FrustumCorners1", this._frustumCorners[1]);
			this._material.SetVector("_FrustumCorners2", this._frustumCorners[2]);
			this._material.SetVector("_FrustumCorners3", this._frustumCorners[3]);
#endif

			Texture nullTexture = null;
			if (this._light.shadows != LightShadows.None)
			{
				this._material.EnableKeyword("SHADOWS_DEPTH");
				this._commandBuffer.Blit(nullTexture, renderer.GetVolumeLightBuffer(), this._material, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, this._commandBuffer, viewProj);
			}
			else
			{
				this._material.DisableKeyword("SHADOWS_DEPTH");
				renderer.GlobalCommandBuffer.Blit(nullTexture, renderer.GetVolumeLightBuffer(), this._material, pass);

				if (this.CustomRenderEvent != null)
					this.CustomRenderEvent(renderer, this, renderer.GlobalCommandBuffer, viewProj);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private bool IsCameraInPointLightBounds()
		{
			float distanceSqr = (this._light.transform.position - Camera.current.transform.position).sqrMagnitude;
			float extendedRange = this._light.range + 1;
			if (distanceSqr < (extendedRange * extendedRange))
				return true;
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private bool IsCameraInSpotLightBounds()
		{
			// check range
			float distance = Vector3.Dot(this._light.transform.forward, (Camera.current.transform.position - this._light.transform.position));
			float extendedRange = this._light.range + 1;
			if (distance > (extendedRange))
				return false;

			// check angle
			float cosAngle = Vector3.Dot(this.transform.forward, (Camera.current.transform.position - this._light.transform.position).normalized);
			if ((Mathf.Acos(cosAngle) * Mathf.Rad2Deg) > (this._light.spotAngle + 3) * 0.5f)
				return false;

			return true;
		}
	}
}