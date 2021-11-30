
using UnityEngine;
using System.Collections;


namespace Suimono.Core
{
    [ExecuteInEditMode]
    public class SuimonoCamera_depth : MonoBehaviour
    {

        //PUBLIC VARIABLES
        [HideInInspector] public float _sceneDepth = 20.0f;
        [HideInInspector] public float _shoreDepth = 45.0f;

        //PRIVATE VARIABLES
        private Material useMat;

        private void Start()
        {
            //setup material
            this.useMat = new Material(HSSuimono.HSSuimono._self._resources.LoadAsset<Shader>("Suimono2_FX_Depth"));
        }

        private void LateUpdate()
        {

            //clamp values
            this._sceneDepth = Mathf.Clamp(this._sceneDepth, 0.0f, 100.0f);
            this._shoreDepth = Mathf.Clamp(this._shoreDepth, 0.0f, 100.0f);

            //set material properties
            this.useMat.SetFloat("_sceneDepth", this._sceneDepth);
            this.useMat.SetFloat("_shoreDepth", this._shoreDepth);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, this.useMat);
        }

    }
}



