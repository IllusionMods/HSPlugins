using UnityEngine;
using System.Collections;



namespace Suimono.Core
{

    public enum suiCausToolType
    {
        aboveWater, belowWater
    };


    [ExecuteInEditMode]
    public class cameraCausticsHandler : MonoBehaviour
    {

        public bool isUnderwater = false;
        public Light causticLight;
        public suiCausToolType causticType;

        private bool enableCaustics = true;
        private SuimonoModule moduleObject;

        private void Start()
        {
            //get master object
            this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));

            //get caustic light object
            if (this.moduleObject != null)
            {
                this.causticLight = this.moduleObject.suimonoModuleLibrary.causticObjectLight;
            }
        }

        private void LateUpdate()
        {
            //turn off caustic light when not playing scene
            if (!Application.isPlaying)
                this.causticLight.enabled = false;
        }

        private void OnPreCull()
        {
            if (this.causticLight == null)
                return;
            //enable caustics lighting
            if (this.moduleObject != null)
            {
                this.enableCaustics = this.moduleObject.enableCaustics;

                if (this.moduleObject.setLight != null)
                {
                    if (!this.moduleObject.setLight.enabled || !this.moduleObject.setLight.gameObject.activeSelf)
                    {
                        this.enableCaustics = false;
                    }
                }

            }

            switch (this.causticType)
            {
                //handle light emission
                case suiCausToolType.aboveWater:
                    this.causticLight.enabled = false;
                    break;
                case suiCausToolType.belowWater:
                    this.causticLight.enabled = this.enableCaustics;
                    break;
                default:
                    this.causticLight.enabled = false;
                    break;
            }

            if (this.isUnderwater)
                this.causticLight.enabled = false;
            if (!Application.isPlaying)
                this.causticLight.enabled = false;
        }

        private void OnPostRender()
        {
            if (this.causticLight == null)
                return;
            this.causticLight.enabled = this.isUnderwater;

            if (!Application.isPlaying)
                this.causticLight.enabled = false;
        }



    }
}