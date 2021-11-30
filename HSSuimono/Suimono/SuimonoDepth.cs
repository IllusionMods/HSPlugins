using UnityEngine;
using System.Collections;


namespace Suimono.Core
{
	public class SuimonoDepth : MonoBehaviour
	{


		//PUBLIC VARIABLES
		public Shader useShader;

		//PRIVATE VARIABLES
		private Material useMat;

		private void Start()
		{
			//setup material
			this.useMat = new Material(this.useShader);
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (this.useMat != null) Graphics.Blit(source, destination, this.useMat);
		}

	}
}