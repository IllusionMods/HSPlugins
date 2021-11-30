using UnityEngine;
using System.Collections;


namespace Suimono.Core
{

    public class SuimonoModuleLib : MonoBehaviour
    {

        public Texture2D texNormalC;
        public Texture2D texNormalT;
        public Texture2D texNormalR;

        public Texture2D texFoam;
        public Texture2D texRampWave;
        public Texture2D texRampDepth;
        public Texture2D texRampBlur;
        public Texture2D texRampFoam;
        public Texture2D texWave;
        public Cubemap texCube1;
        public Texture2D texBlank;
        public Texture2D texMask;
        public Texture2D texDrops;

        [Space(10)]
        public Material materialSurface;
        public Material materialSurfaceScale;
        public Material materialSurfaceShadow;

        [Space(10)]
        public GameObject surfaceObject;
        public GameObject moduleObject;

        [Space(10)]
        public fx_causticModule causticObject;
        public Light causticObjectLight;
        public cameraTools transToolsObject;
        public Camera transCamObject;
        public cameraCausticsHandler causticHandlerObjectTrans;
        public cameraTools causticToolsObject;
        public Camera causticCamObject;
        public cameraCausticsHandler causticHandlerObject;
        public GameObject wakeObject;
        public Camera wakeCamObject;
        public GameObject normalsObject;
        public Camera normalsCamObject;
        public SuimonoModuleFX fxObject;
        public fx_soundModule sndparentobj;
        public ParticleSystem underwaterDebris;
        public Transform underwaterDebrisTransform;
        public Renderer underwaterDebrisRendererComponent;

        [Space(10)]
        public Mesh[] meshLevel;
        public Shader[] shaderRepository;
        public TextAsset[] presetRepository;

    }
}
