using UnityEngine;
using System.Collections;


namespace Suimono.Core
{

	public class fx_buoyancy : MonoBehaviour
	{


		//PUBLIC VARIABLES
		public bool applyToParent = false;
		public bool engageBuoyancy = false;
		public float activationRange = 5000.0f;
		public bool inheritForce = false;
		public bool keepAtSurface = false;
		public float buoyancyOffset = 0.0f;
		public float buoyancyStrength = 1.0f;
		public float forceAmount = 1.0f;
		public float forceHeightFactor = 0.0f;

		// PRIVATE VARIABLES
		private float maxVerticalSpeed = 5.0f;
		private float surfaceRange = 0.2f;
		private float buoyancy = 0.0f;
		private float surfaceLevel = 0.0f;
		private float underwaterLevel = 0.0f;
		private bool isUnderwater = false;
		private Transform physTarget;
		private SuimonoModule moduleObject;
		private float waveHeight = 0.0f;
		private float modTime = 0.0f;
		private float splitFac = 1.0f;
		private Rigidbody rigidbodyComponent;
		private float isOver = 0.0f;
		private Vector2 forceAngles = new Vector2(0.0f, 0.0f);
		private float forceSpeed = 0.0f;
		private float waveHt = 0.0f;
		//private float displace;

		private int randSeed;
		private Random buyRand;

		//collect for GC
		private Vector3 gizPos;
		private float testObjectHeight;
		private float buoyancyFactor;
		private float forceMod;
		private float waveFac;
		private float[] heightValues;
		private bool isEnabled = true;
		private bool performHeight = false;
		private float currRange = -1.0f;
		//private float camRange = -1.0f;
		//private Vector3 currCamPos = new Vector3(-1f,-1f,-1f);
		private Vector3 physPosition;
		private bool saveRigidbodyState;
		private float lerpSurfacePosTime = 0f;
		private float targetYPosition;
		private float startYPosition;
		private bool saveKeepAtSurface;

		private void OnDrawGizmos()
		{
			this.gizPos = this.transform.position;
			this.gizPos.y += 0.03f;
			Gizmos.DrawIcon(this.gizPos, "gui_icon_buoy.psd", true);
			this.gizPos.y -= 0.03f;
		}

		private void Start()
		{

			if (GameObject.Find("SUIMONO_Module") != null)
			{
				this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
			}

			//set random
			this.randSeed = System.Environment.TickCount;
			this.buyRand = new Random(this.randSeed);

			//get number of buoyant objects
			if (this.applyToParent)
			{
				fx_buoyancy[] buoyancyObjects = this.transform.parent.gameObject.GetComponentsInChildren<fx_buoyancy>();
				if (buoyancyObjects != null)
				{
					this.splitFac = 1f / buoyancyObjects.Length;
				}
			}

			//set physics target
			if (this.applyToParent)
			{
				this.physTarget = this.transform.parent.transform;
				if (this.physTarget != null)
				{
					if (this.rigidbodyComponent == null)
					{
						this.rigidbodyComponent = this.physTarget.GetComponent<Rigidbody>();
					}
				}
			}
			else
			{
				this.physTarget = this.transform;
				if (this.physTarget != null)
				{
					if (this.rigidbodyComponent == null)
					{
						this.rigidbodyComponent = this.GetComponent<Rigidbody>();
					}
				}
			}
		}

		private void FixedUpdate()
		{
			this.SetUpdate();

			//Determine Vertical Speed
			if (this.isUnderwater == false)
			{
				this.maxVerticalSpeed = 0.25f;

			}
			else if (this.isUnderwater)
			{
				this.maxVerticalSpeed = Mathf.Clamp(this.surfaceLevel - (this.transform.position.y + this.buoyancyOffset - 0.5f), 0.0f, 5.0f);
				if (this.maxVerticalSpeed > 4.0f)
					this.maxVerticalSpeed = 4.0f;
			}

			//Set Buoyancy
			this.buoyancy = 1 + (this.maxVerticalSpeed * this.buoyancyStrength);

		}

		private void SetUpdate()
		{


			if (this.moduleObject != null)
			{

				//set Random
				if (this.buyRand == null)
					this.buyRand = new Random(this.randSeed);

				//check activations
				this.performHeight = true;
				if (this.physTarget != null && this.moduleObject.setCamera != null)
				{

					//check for range activation
					if (this.activationRange > 0f)
					{
						this.currRange = Vector3.Distance(this.moduleObject.setCamera.transform.position, this.physTarget.transform.position);
						if (this.currRange >= this.activationRange)
						{
							this.performHeight = false;
						}
					}

					if (this.activationRange <= 0f)
						this.performHeight = true;

					/*
					//check for frustrum activation
					camRange = 0.2f;
					if (moduleObject != null && performHeight){
					if (moduleObject.setCameraComponent != null){
						currCamPos = moduleObject.setCameraComponent.WorldToViewportPoint(physTarget.transform.position);
						if (currCamPos.x > (1f+camRange) || currCamPos.y > (1f+camRange)){
							performHeight = false;
						}
						if (currCamPos.x < (0f-camRange) || currCamPos.y < (0f-camRange)){
							performHeight = false;
						}
					}
					}
					*/

					//check for enable activation
					if (!this.isEnabled)
					{
						this.performHeight = false;
					}
				}


				//perform height check
				if (this.performHeight)
				{
					// Get all height variables from Suimono Module object
					this.heightValues = this.moduleObject.SuimonoGetHeightAll(this.transform.position);
					this.isOver = this.heightValues[4];
					this.waveHt = this.heightValues[8];
					this.surfaceLevel = this.heightValues[0];
					this.forceAngles = this.moduleObject.SuimonoConvertAngleToVector(this.heightValues[6]);
					this.forceSpeed = this.heightValues[7] * 0.1f;
				}

				//clamp variables
				this.forceHeightFactor = Mathf.Clamp01(this.forceHeightFactor);

				//Reset values
				this.isUnderwater = false;
				this.underwaterLevel = 0f;

				//calculate scaling
				this.testObjectHeight = (this.transform.position.y + this.buoyancyOffset - 0.5f);

				this.waveHeight = this.surfaceLevel;
				if (this.testObjectHeight < this.waveHeight)
				{
					this.isUnderwater = true;
				}
				this.underwaterLevel = this.waveHeight - this.testObjectHeight;


				//set buoyancy
				if (!this.keepAtSurface && this.rigidbodyComponent)
					this.rigidbodyComponent.isKinematic = this.saveRigidbodyState;

				if (!this.keepAtSurface && this.engageBuoyancy && this.isOver == 1f)
				{
					if (this.rigidbodyComponent && !this.rigidbodyComponent.isKinematic)
					{

						//reset rigidbody if turned off
						if (this.rigidbodyComponent.isKinematic)
						{
							this.rigidbodyComponent.isKinematic = this.saveRigidbodyState;
						}

						this.buoyancyFactor = 10.0f;

						if (this.isUnderwater)
						{

							if (this.transform.position.y + this.buoyancyOffset - 0.5f < this.waveHeight - this.surfaceRange)
							{

								// add vertical force to buoyancy while underwater
								this.forceMod = (this.buoyancyFactor * (this.buoyancy * this.rigidbodyComponent.mass) * (this.underwaterLevel) * this.splitFac * (this.isUnderwater ? 1f : 0f));
								if (this.rigidbodyComponent.velocity.y < this.maxVerticalSpeed)
								{
									this.rigidbodyComponent.AddForceAtPosition(new Vector3(0f, 1f, 0f) * this.forceMod, this.transform.position);
								}

								this.modTime = 0f;

							}
							else
							{

								// slow down vertical velocity as it reaches water surface or wave zenith
								this.modTime = (this.transform.position.y + this.buoyancyOffset - 0.5f) / (this.waveHeight + this.buyRand.Next(0f, 0.25f) * (this.isUnderwater ? 1f : 0f));
								if (this.rigidbodyComponent.velocity.y > 0f)
								{
									this.rigidbodyComponent.velocity = new Vector3(this.rigidbodyComponent.velocity.x,
										Mathf.SmoothStep(this.rigidbodyComponent.velocity.y, 0f, this.modTime), this.rigidbodyComponent.velocity.z);
								}
							}


							//Add Water Force / Direction to Buoyancy Object
							if (this.inheritForce)
							{
								if (this.transform.position.y + this.buoyancyOffset - 0.5f <= this.waveHeight)
								{
									this.waveFac = Mathf.Lerp(0f, this.forceHeightFactor, this.waveHt);
									if (this.forceHeightFactor == 0f)
										this.waveFac = 1f;
									this.rigidbodyComponent.AddForceAtPosition(new Vector3(this.forceAngles.x, 0f, this.forceAngles.y) * (this.buoyancyFactor * 2f) * this.forceSpeed * this.waveFac * this.splitFac * this.forceAmount, this.transform.position);
								}
							}

						}

					}
				}



				//Keep At Surface Option
				if (this.keepAtSurface && this.isOver == 1f)
				{
					this.saveKeepAtSurface = this.keepAtSurface;
					float testPos = (this.surfaceLevel - this.physTarget.position.y - this.buoyancyOffset);
					if (testPos >= -0.25f)
					{

						//remove rigidbody
						if (this.rigidbodyComponent != null)
						{
							//rigidbodyComponent.velocity = Vector3.zero;
							if (!this.rigidbodyComponent.isKinematic)
							{
								this.saveRigidbodyState = false;
								this.rigidbodyComponent.isKinematic = true;
							}
						}

						//set Y position
						this.physPosition = this.physTarget.position;
						this.physPosition.y = Mathf.Lerp(this.startYPosition, this.targetYPosition, this.lerpSurfacePosTime);
						this.physTarget.position = this.physPosition;

					}
					else
					{
						this.rigidbodyComponent.isKinematic = this.saveRigidbodyState;
					}


					//set timer for smooth blend
					this.lerpSurfacePosTime += Time.deltaTime * 4f;
					if (this.lerpSurfacePosTime > 1f || this.keepAtSurface != this.saveKeepAtSurface)
					{
						this.lerpSurfacePosTime = 0f;
						this.startYPosition = this.physTarget.position.y;
						this.targetYPosition = this.surfaceLevel - this.buoyancyOffset;//physTarget.position.y;
					}

				}

			}
		}
	}
}