using UnityEngine;
using System.Collections;


namespace Suimono.Core
{

    public class fx_causticModule : MonoBehaviour
    {

        //PUBLIC VARIABLES
        public bool enableCaustics = true;
        public Light sceneLightObject;
        public bool inheritLightColor = false;
        public bool inheritLightDirection = false;
        public Color causticTint = new Color(1f, 1f, 1f, 1f);
        public float causticIntensity = 2f;
        public float causticScale = 4f;
        public float heightFac = 0f;
        public int causticFPS = 32;
        public Texture2D[] causticFrames;
        public Texture2D useTex;

        //PRIVATE VARIABLES
        private float causticsTime = 0f;
        private SuimonoModule moduleObject;
        private GameObject lightObject;
        private int frameIndex = 0;

        private void Start()
        {
            //get master objects
            this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
            this.lightObject = this.transform.Find("mainCausticObject").gameObject;
        }

        private void LateUpdate()
        {
            if (!this.enabled)
                return;
            this.useTex = this.causticFrames[this.frameIndex];
            this.causticsTime += Time.deltaTime;
            if (this.causticsTime > (1f / (this.causticFPS * 1f)))
            {
                this.causticsTime = 0f;
                this.frameIndex += 1;
            }

            if (this.frameIndex == this.causticFrames.Length)
                this.frameIndex = 0;

            if (this.moduleObject != null)
            {
                if (this.moduleObject.setLight != null)
                {
                    this.sceneLightObject = this.moduleObject.setLight;
                }

                if (this.lightObject != null)
                {
                    this.lightObject.SetActive(this.moduleObject.enableCaustics);
                }
            }
        }

    }
}