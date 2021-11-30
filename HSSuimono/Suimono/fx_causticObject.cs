using UnityEngine;
using System.Collections;


namespace Suimono.Core
{
	public class fx_causticObject : MonoBehaviour
	{

		public bool manualPlacement = false;

		private SuimonoModule moduleObject;
		private fx_causticModule causticObject;
		private Light lightComponent;
		private float heightMult = 1f;

		private void Start()
		{
			//get master objects
			this.moduleObject = GameObject.Find("SUIMONO_Module").GetComponent<SuimonoModule>();
			this.causticObject = GameObject.Find("_caustic_effects").GetComponent<fx_causticModule>();
			this.lightComponent = this.GetComponent<Light>();
		}

		private void LateUpdate()
		{


			if (this.causticObject.enableCaustics)
			{

				//get the current light texture from Module
				this.lightComponent.cookie = this.causticObject.useTex;
				this.lightComponent.cullingMask = this.moduleObject.causticLayer;
				this.lightComponent.color = this.causticObject.causticTint;

				//Set Height (manual objects only)
				this.heightMult = 1f;
				if (this.manualPlacement)
					this.heightMult = (1f - this.causticObject.heightFac);
				this.lightComponent.intensity = this.causticObject.causticIntensity * this.heightMult;

				this.lightComponent.cookieSize = this.causticObject.causticScale;

				//get scene lighting
				if (this.causticObject.sceneLightObject != null)
				{

					//set caustic color based on scene lighting
					if (this.causticObject.inheritLightColor)
					{
						this.lightComponent.color = this.causticObject.sceneLightObject.color * this.causticObject.causticTint;
						this.lightComponent.intensity = this.lightComponent.intensity * this.causticObject.sceneLightObject.intensity;
					}
					else
					{
						this.lightComponent.color = this.causticObject.causticTint;
					}

					//set caustic direction based on scene light direction
					if (this.causticObject.inheritLightDirection)
					{
						this.transform.eulerAngles = this.causticObject.sceneLightObject.transform.eulerAngles;
					}
					else
					{
						this.transform.eulerAngles = new Vector3(90f, 0f, 0f);
					}

				}
			}
		}
	}
}