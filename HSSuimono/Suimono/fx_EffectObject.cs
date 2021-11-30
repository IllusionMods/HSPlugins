using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum Sui_FX_Rules
{
	none, isUnderWater, isAboveWater, isAtWaterSurface, speedIsGreater, speedIsLess,
	waterDepthGreater, waterDepthLess
}
public enum Sui_FX_RuleModifiers
{
	and, or
}
public enum Sui_FX_System
{
	none, bubbles, rings, ringfoam, splash, splashdrops
}
//public enum Sui_FX_ActionType{
//		repeat,once
//		}



namespace Suimono.Core
{

	[ExecuteInEditMode]
	public class fx_EffectObject : MonoBehaviour
	{

		//PUBLIC VARIABLES
		public SuimonoModuleFX fxObject;
		public Sui_FX_Rules[] effectRule;
		public float[] effectData;
		public Sui_FX_Rules[] resetRule;
		public string[] effectSystemName;
		public Sui_FX_System[] effectSystem;
		//public Sui_FX_ActionType actionType = Sui_FX_ActionType.repeat;
		public Vector2 effectDelay = new Vector2(1.0f, 1.0f);
		public Vector2 emitTime = new Vector2(1.0f, 1.0f);
		public Vector2 emitNum = new Vector2(1.0f, 1.0f);
		public Vector2 effectSize = new Vector2(1.0f, 1.0f);
		public float emitSpeed;
		public float speedThreshold;
		public float directionMultiplier;
		public bool emitAtWaterLevel = false;
		public float effectDistance = 100.0f;

		//audio
		public AudioClip audioObj;
		public Vector2 audioVol = new Vector2(0.9f, 1.0f);
		public Vector2 audioPit = new Vector2(0.8f, 1.2f);
		public float audioSpeed;

		//events
		public bool enableEvents = false;

		//color
		public Color tintCol = new Color(1f, 1f, 1f, 1f);
		public bool clampRot = false;

		// for custom editor
		public int actionIndex = 1;
		[System.NonSerialized]
		public string[] actionOptions = {
			"Once","Repeat","Specific"
			};
		public int actionNum = 5;
		public float actionReset = 15f;

		public int typeIndex = 0;
		public int[] ruleIndex;
		[System.NonSerialized]
		public string[] ruleOptions = {
			"None","Object Is Underwater","Object Is Above Water","Object Is At Surface",
			"Object Speed Is Greater Than","Object Speed Is Less Than","Water Depth Is Greater Than",
			"Water Depth Is Less Than"
			};

		public int systemIndex = 0;
		public List<string> sysNames = new List<string>();
		public float currentSpeed;


		//PRIVATE VARIABLES
		private int actionCount = 0;
		private float actionTimer = 0f;
		private Vector3 savePos = new Vector3(0f, 0f, 0f);
		private SuimonoModule moduleObject;
		private float emitTimer;
		private bool delayPass = true;
		private bool actionPass = true;
		private float useSpd;
		private float useAudioSpd;
		private float isOverWater;
		private float currentWaterPos;
		private Vector3 emitPos;
		private bool rulepass = false;
		private float timerAudio = 0.0f;
		private float timerParticle = 0.0f;
		private float currentCamDistance = 0.0f;

		//collect for GC
		private Vector3 gizPos;
		private int sN;
		private int s;
		private int rCK;
		private int emitN;
		private float emitS;
		private Vector3 emitV;
		private float emitR;
		private float emitAR;
		private bool rp;
		private float ruleData;
		private float depth;
		private Sui_FX_Rules[] tempRules;
		private int[] tempIndex;
		private float[] tempData;
		private int aR;
		private int endLP;
		private int setInt;
		private float[] heightValues;

		private Transform transf;

		private int randSeed;
		private Random fxRand;

		// create a simple cheap "smear" effect on the InvokeRepeating processor load.
		// by shifting the work into rough groups via a simple static int, that we loop over.

		// our global counter
		private static int staggerOffset = 0;

		// our loop, we chose groups of roughly 20
		private static int staggerModulus = 20;

		// our actual stagger value 
		private float stagger;

		private float _deltaTime;

		private void OnDrawGizmos()
		{
			this.gizPos = this.transform.position;
			this.gizPos.y += 0.03f;
			Gizmos.DrawIcon(this.gizPos, "gui_icon_fxobj.psd", true);
		}

		private void Start()
		{

			// Object References
			this.transf = this.transform;
			if (GameObject.Find("SUIMONO_Module"))
			{
				this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));
				if (this.moduleObject != null)
					this.fxObject = this.moduleObject.suimonoModuleLibrary.fxObject;
			}


			//populate system names
			if (this.fxObject != null)
			{
				this.sysNames = this.fxObject.sysNames;
			}

			//set random
			this.randSeed = System.Environment.TickCount;
			this.fxRand = new Random(this.randSeed);

			//run update loop at set FPS interval
			staggerOffset++;
			this.stagger = (staggerOffset + 0f) * 0.05f;
			staggerOffset = staggerOffset % staggerModulus;

			this.InvokeRepeating("SetUpdate", 0.1f + this.stagger, (1.0f / 30.0f));
		}

		private void SetUpdate()
		{

			if (this.moduleObject != null)
			{

				//Cache Time for performance
				this._deltaTime = Time.deltaTime;

				//check for action timing
				this.actionPass = false;
				if (this.actionIndex == 0 && this.actionCount < 1)
					this.actionPass = true;
				if (this.actionIndex == 2 && this.actionCount < this.actionNum)
					this.actionPass = true;
				if (this.actionIndex == 1)
					this.actionPass = true;

				if (this.actionCount > 0 && (this.actionIndex == 0 || this.actionIndex == 2))
				{
					this.actionTimer += this._deltaTime;
					if (this.actionTimer > this.actionReset && this.actionReset > 0f)
					{
						this.actionCount = 0;
						this.actionTimer = 0f;
					}
				}



				//set Random
				if (this.fxRand == null)
					this.fxRand = new Random(this.randSeed);

				//get objects while in editor mode
#if UNITY_EDITOR
				if (!Application.isPlaying){
					if (moduleObject == null){	
					if (GameObject.Find("SUIMONO_Module")){
						moduleObject = GameObject.Find("SUIMONO_Module").GetComponent<Suimono.Core.SuimonoModule>() as Suimono.Core.SuimonoModule;
						fxObject = moduleObject.suimonoModuleLibrary.fxObject;
					}
					}
				}
#endif

				if (Application.isPlaying)
				{

					//calculate camera distance
					if (this.moduleObject.setTrack != null)
					{
						this.currentCamDistance = Vector3.Distance(this.transf.position, this.moduleObject.setTrack.transform.position);
						if (this.currentCamDistance <= this.effectDistance)
						{

							//track position / speed
							if (this.savePos != this.transf.position)
							{
								this.currentSpeed = Vector3.Distance(this.savePos, new Vector3(this.transf.position.x, this.transf.position.y, this.transf.position.z)) / this._deltaTime;
							}
							this.savePos = this.transf.position;

							// track timers and emit
							this.timerParticle += this._deltaTime;
							this.timerAudio += this._deltaTime;

							this.EmitFX();
							if (this.timerAudio > this.audioSpeed)
							{
								this.timerAudio = 0f;
								this.EmitSoundFX();
							}

						}
					}


					//Event trigger function
					if (this.enableEvents)
					{
						this.timerEvent += this._deltaTime;
						this.BroadcastEvent();
					}

				}
			}
		}

		private void EmitSoundFX()
		{

			if (this.actionPass)
			{
				if (this.audioObj != null && this.moduleObject != null)
				{
					if (this.moduleObject.gameObject.activeInHierarchy)
					{
						if (this.rulepass)
						{
							this.moduleObject.AddSoundFX(this.audioObj, this.emitPos, new Vector3(0f, this.fxRand.Next(this.audioPit.x, this.audioPit.y), this.fxRand.Next(this.audioVol.x, this.audioVol.y)));
						}
					}
				}
			}
		}

		private void EmitFX()
		{
			if (!this.moduleObject.enableInteraction || !Application.isPlaying || this.moduleObject == null || !this.moduleObject.gameObject.activeInHierarchy || !this.actionPass)
				return;
			//######################################
			//##    CALCULATE TIMING and DELAYS   ##
			//######################################
			this.delayPass = false;

			this.emitTimer += this._deltaTime;
			if (this.emitTimer >= this.emitSpeed)
			{
				this.emitTimer = 0f;
				this.delayPass = true;
			}



			//####################################
			//##    CALCULATE WATER RELATION   ##
			//####################################
			this.heightValues = this.moduleObject.SuimonoGetHeightAll(this.transf.position);
			this.currentWaterPos = this.heightValues[3];
			this.isOverWater = this.heightValues[4];


			//##########################
			//##    CALCULATE RULES   ##
			//##########################
			this.rulepass = true;

			bool rp = false;
			for (this.rCK = 0; this.rCK < this.effectRule.Length; this.rCK++)
			{
				this.ruleData = this.speedThreshold;

				//get depth
				this.depth = this.currentWaterPos;

				if (this.rCK < this.effectData.Length)
					this.ruleData = this.effectData[this.rCK];

				//rules.none
				int index = this.ruleIndex[this.rCK];

				if (this.isOverWater == 1f)
				{
					switch (index)
					{
						//rules.ObjectIsUnderwater
						case 1:
							if (!(this.depth > 0f))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.ObjectIsAbovewater
						case 2:
							if (!(this.depth <= 0f))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.ObjectIsAtSurface
						case 3:
							if (!(this.depth < 0.15f) || !(this.depth > -0.15f))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.speedIsGreater
						case 4:
							if (!(this.currentSpeed > this.ruleData))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.speedIsLess
						case 5:
							if (!(this.currentSpeed < this.ruleData))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.WaterDepthGreater
						case 6:
							if (!(this.depth > this.ruleData))
							{
								this.rulepass = false;
								return;
							}
							break;
						//rules.WaterDepthIsLess
						case 7:
							if (!(this.depth < this.ruleData))
							{
								this.rulepass = false;
								return;
							}
							break;
					}
				}
			}

			//######################
			//##    INITIATE FX   ##
			//######################
			if (this.delayPass && this.rulepass)
			{
				this.emitN = Mathf.FloorToInt(this.fxRand.Next(this.emitNum.x, this.emitNum.y));
				this.emitS = this.fxRand.Next(this.effectSize.x, this.effectSize.y);
				this.emitV = new Vector3(0f, 0f, 0f);
				this.emitPos = this.transform.position;
				this.emitR = this.transform.eulerAngles.y - 180f;

				if (!this.clampRot)
				{
					this.emitR = this.fxRand.Next(-30f, 10f);
				}
				this.emitAR = this.fxRand.Next(-360f, 360f);

				//get water level
				if (this.emitAtWaterLevel)
				{
					this.emitPos = new Vector3(this.emitPos.x, (this.transform.position.y + this.currentWaterPos) - 0.35f, this.emitPos.z);
				}

				if (this.directionMultiplier > 0f)
				{
					this.emitV = this.transform.up * (this.directionMultiplier * Mathf.Clamp01((this.currentSpeed / this.speedThreshold)));
				}

				//EMIT PARTICLE SYSTEM
				if (this.timerParticle > this.emitSpeed)
				{
					this.timerParticle = 0f;

					if (this.systemIndex - 1 >= 0)
					{
						this.emitPos.y += (this.emitS * 0.4f);
						this.emitPos.x += this.fxRand.Next(-0.2f, 0.2f);
						this.emitPos.z += this.fxRand.Next(-0.2f, 0.2f);
						this.moduleObject.AddFX(this.systemIndex - 1, this.emitPos, this.emitN, this.fxRand.Next(0.5f, 0.75f) * this.emitS, this.emitR, this.emitAR, this.emitV, this.tintCol);
					}

					this.actionCount++;

				}
			}
		}







		public void AddRule()
		{
			this.tempRules = this.effectRule;

			this.tempIndex = this.ruleIndex;
			this.tempData = this.effectData;

			this.effectRule = new Sui_FX_Rules[this.tempRules.Length + 1];
			this.ruleIndex = new int[this.tempRules.Length + 1];
			this.effectData = new float[this.tempRules.Length + 1];

			for (this.aR = 0; this.aR < this.tempRules.Length; this.aR++)
			{
				this.effectRule[this.aR] = this.tempRules[this.aR];
				this.ruleIndex[this.aR] = this.tempIndex[this.aR];
				this.effectData[this.aR] = this.tempData[this.aR];
			}
			this.effectRule[this.tempRules.Length] = Sui_FX_Rules.none;
			this.ruleIndex[this.tempRules.Length] = 0;
			this.effectData[this.tempRules.Length] = 0;

		}




		public void DeleteRule(int ruleNum)
		{
			this.tempRules = this.effectRule;
			this.tempIndex = this.ruleIndex;
			this.tempData = this.effectData;

			this.endLP = this.tempRules.Length - 1;

			if (this.endLP <= 0)
			{
				this.endLP = 0;
				this.effectRule = new Sui_FX_Rules[0];
				this.ruleIndex = new int[0];
				this.effectData = new float[0];

			}
			else
			{
				this.effectRule = new Sui_FX_Rules[this.endLP];
				this.ruleIndex = new int[this.endLP];
				this.effectData = new float[this.endLP];
				this.setInt = -1;

				for (this.aR = 0; this.aR <= this.endLP; this.aR++)
				{

					if (this.aR != ruleNum)
					{
						this.setInt += 1;
					}
					else
					{
						this.setInt += 2;
					}

					if (this.setInt <= this.endLP)
					{
						this.effectRule[this.aR] = this.tempRules[this.setInt];
						this.ruleIndex[this.aR] = this.tempIndex[this.setInt];
						this.effectData[this.aR] = this.tempData[this.setInt];
					}
				}
			}
		}

		private void OnDisable()
		{
			this.CancelInvoke("SetUpdate");
		}

		private void OnEnable()
		{

			staggerOffset++;
			this.stagger = (staggerOffset + 0f) * 0.05f;
			staggerOffset = staggerOffset % staggerModulus;

			this.CancelInvoke("SetUpdate");
			this.InvokeRepeating("SetUpdate", 0.1f + this.stagger, (1f / 10f));
		}





		/// Used to broadcast location info.
		public delegate void TriggerHandler(Vector3 position, Quaternion rotatoin);

		// Broadcast when Event Trigger fires.
		public event TriggerHandler OnTrigger;

		// Broadcast interval in second.
		public float eventInterval = 1f;

		// Should event returns water level position instead of fx_EffectObject position?
		public bool eventAtSurface = false;

		// simple timer
		private float timerEvent;

		private void BroadcastEvent()
		{
			if (!this.moduleObject.isActiveAndEnabled || !this.rulepass || this.OnTrigger == null || this.timerEvent < this.eventInterval) return;
			this.timerEvent = 0f;
			//Position: if atSurface is true, then return water level position, otherwise return this.transform.position
			this.OnTrigger.Invoke(this.eventAtSurface ? new Vector3(this.emitPos.x, (this.transf.position.y + this.currentWaterPos) - 0.35f, this.emitPos.z) : this.transf.position, this.transf.rotation);
		}





	}
}