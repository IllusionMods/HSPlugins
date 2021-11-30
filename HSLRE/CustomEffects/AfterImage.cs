using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IllusionInjector;
using IllusionPlugin;
using UnityEngine;

namespace HSLRE.CustomEffects
{
	public class AfterImage
	{
		public const int maxFrames = 128;
		private readonly Material _motionBlurMat;
		private readonly LinkedList<RenderTexture> _frames = new LinkedList<RenderTexture>();
		private readonly float[] _weights = new float[maxFrames];
		private int _framesToMix = 8;
		private float _falloffPower = 10f / 7f;
		private readonly Func<bool> _videoExportIsRecording;

		public int framesToMix
		{
			get { return this._framesToMix; }
			set
			{
				this._framesToMix = Mathf.Clamp(value, 2, maxFrames);
				this.UpdateWeights();
			}
		}

		public float falloffPower
		{
			get { return this._falloffPower; }
			set
			{
				this._falloffPower = value;
				this.UpdateWeights();
			}
		}

		public AfterImage()
		{
			Shader motionBlurShader = HSLRE.self._resources.LoadAsset<Shader>("AfterImage");
			this._motionBlurMat = new Material(motionBlurShader);
			this.UpdateWeights();

			Type t = Type.GetType("VideoExport.VideoExport,VideoExport");
			if (t != null)
			{
				IPlugin videoExport = PluginManager.Plugins.FirstOrDefault(p => p.GetType() == t);
				if (videoExport != null)
				{
					PropertyInfo pi = t.GetProperty("isRecording", BindingFlags.Public | BindingFlags.Instance);
					if (pi != null)
						this._videoExportIsRecording = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), videoExport, pi.GetGetMethod());
				}
			}
		}

		private void OnDisable()
		{
			while (this._frames.Count != 0)
			{
				RenderTexture.ReleaseTemporary(this._frames.Last.Value);
				this._frames.RemoveLast();
			}
		}

		private void UpdateWeights()
		{
			float total = 0f;
			for (int i = 0; i < this._framesToMix; i++)
			{
				this._weights[i] = 1f / Mathf.Pow(Mathf.Pow(128, this._falloffPower), i / ((float)this._framesToMix));
				total += this._weights[i];
			}
			for (int i = 0; i < this._framesToMix; i++)
				this._weights[i] = this._weights[i] / total;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (this._videoExportIsRecording != null && this._videoExportIsRecording() && Camera.main.targetTexture == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			RenderTexture newFrame = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
			Graphics.Blit(source, newFrame);
			this._frames.AddFirst(newFrame);

			while (this._frames.Count > this._framesToMix)
			{
				RenderTexture.ReleaseTemporary(this._frames.Last.Value);
				this._frames.RemoveLast();
			}

			RenderTexture mixTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
			RenderTexture mixTex2 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);

			Graphics.Blit(Texture2D.blackTexture, mixTex);
			int i = 0;
			foreach (RenderTexture renderTexture in this._frames)
			{
				this._motionBlurMat.SetTexture("_MixTex", mixTex);
				this._motionBlurMat.SetFloat("_Weight", this._weights[i]);
				Graphics.Blit(renderTexture, mixTex2, this._motionBlurMat, 0);

				RenderTexture temp = mixTex;
				mixTex = mixTex2;
				mixTex2 = temp;
				++i;
			}
			Graphics.Blit(mixTex, destination);

			RenderTexture.ReleaseTemporary(mixTex);
			RenderTexture.ReleaseTemporary(mixTex2);
		}
	}
}
