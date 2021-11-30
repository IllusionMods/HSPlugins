using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public enum Sui_FX_ClampType
{
    none, atSurface, belowSurface, aboveSurface
}



namespace Suimono.Core
{

    [ExecuteInEditMode]
    public class SuimonoModuleFX : MonoBehaviour
    {



        //PUBLIC VARIABLES
        public string[] effectsLabels;
        public Transform[] effectsSystems;
        public Sui_FX_ClampType systemClampType;
        public Transform[] fxObjects;
        public ParticleSystem[] fxParticles;
        public int[] clampIndex;
        public List<string> clampOptions = new List<string>() { "No Clamp", "Clamp to Surface", "Keep Below Surface", "Keep Above Surface" };
        public List<ParticleSystem.Particle> particleReserve = new List<ParticleSystem.Particle>();

        //PRIVATE VARIABLES
        private Transform fxParentObject;
        private SuimonoModule moduleObject;
        private int fx;
        private int px;
        private float currPXWaterPos;
        private ParticleSystem useParticleComponent;
        private ParticleSystem.Particle[] setParticles;
        private Transform[] tempSystems;
        private int[] tempClamp;
        private int aR;
        private int efx;
        private int epx;
        private int sx;
        private int endLP;
        private int setInt;
        public List<string> sysNames = new List<string>();
        public int sN;
        public int s;
        public string setName;


        // create a simple cheap "smear" effect on the InvokeRepeating processor load.
        // by shifting the work into rough groups via a simple static int, that we loop over.

        // our global counter
        private static int staggerOffset = 0;

        // our loop, we chose groups of roughly 20
        private static int staggerModulus = 20;

        // our actual stagger value 
        private float stagger;

        private void Start()
        {

            //set objects
            this.fxParentObject = this.transform.Find("_particle_effects");
            this.moduleObject = (SuimonoModule)FindObjectOfType(typeof(SuimonoModule));

            //instantiate systems
            if (Application.isPlaying)
            {
                if (this.effectsSystems.Length > 0 && this.fxParentObject != null)
                {
                    Vector3 instPos = new Vector3(this.transform.position.x, -10000.0f, this.transform.position.z);
                    this.fxObjects = new Transform[this.effectsSystems.Length];
                    this.fxParticles = new ParticleSystem[this.effectsSystems.Length];

                    for (int fx = 0; fx < (this.effectsSystems.Length); fx++)
                    {
                        Transform fxObjectPrefab = Instantiate(this.effectsSystems[fx], instPos, this.transform.rotation) as Transform;
                        fxObjectPrefab.transform.parent = this.fxParentObject.transform;
                        this.fxObjects[fx] = (fxObjectPrefab);
                        this.fxParticles[fx] = fxObjectPrefab.gameObject.GetComponent<ParticleSystem>();
                    }
                }
            }

            //do clamp checks at 6fps
            staggerOffset++;
            this.stagger = (staggerOffset + 0f) * 0.05f;
            staggerOffset = staggerOffset % staggerModulus;

            float clampSpeed = 1.0f / 4.0f;
            this.InvokeRepeating("ClampSystems", 0.15f + this.stagger, clampSpeed);
            this.InvokeRepeating("UpdateSystems", 0.2f + this.stagger, 1.0f);
        }

        private void LateUpdate()
        {

            //get objects while in editor mode
#if UNITY_EDITOR
			if (!Application.isPlaying){
				if (moduleObject == null){
					if (GameObject.Find("SUIMONO_Module")){
						moduleObject = GameObject.Find("SUIMONO_Module").GetComponent<Suimono.Core.SuimonoModule>() as Suimono.Core.SuimonoModule;
					}
				}
			}
#endif


            if (!Application.isPlaying)
            {
                this.sysNames = new List<string>();
                this.sysNames.Add("None");
                for (this.sN = 0; this.sN < this.effectsSystems.Length; this.sN++)
                {
                    this.setName = "---";
                    if (this.effectsSystems[this.sN] != null)
                        this.setName = this.effectsSystems[this.sN].transform.name;
                    for (this.s = 0; this.s < this.sN; this.s++)
                    {
                        this.setName += " ";
                    }
                    this.sysNames.Add(this.setName);
                }
            }
        }

        private void UpdateSystems()
        {

            if (Application.isPlaying)
            {
                this.sysNames = new List<string>();
                this.sysNames.Add("None");
                for (this.sN = 0; this.sN < this.effectsSystems.Length; this.sN++)
                {
                    this.setName = "---";
                    if (this.effectsSystems[this.sN] != null)
                        this.setName = this.effectsSystems[this.sN].transform.name;
                    for (this.s = 0; this.s < this.sN; this.s++)
                    {
                        this.setName += " ";
                    }
                    this.sysNames.Add(this.setName);
                }
            }
        }

        private void ClampSystems()
        {

            for (this.fx = 0; this.fx < this.fxObjects.Length; this.fx++)
            {
                if (this.fxObjects[this.fx] != null)
                {
                    if (this.clampIndex[this.fx] != 0)
                    {
                        this.currPXWaterPos = 0.0f;

                        //get particles
                        this.useParticleComponent = this.fxParticles[this.fx];
                        if (this.setParticles == null)
                            this.setParticles = new ParticleSystem.Particle[this.useParticleComponent.particleCount];
                        this.useParticleComponent.GetParticles(this.setParticles);
                        //set particles
                        if (this.useParticleComponent.particleCount > 0.0f)
                        {
                            for (this.px = 0; this.px < this.useParticleComponent.particleCount; this.px++)
                            {
                                this.currPXWaterPos = this.moduleObject.SuimonoGetHeight(this.setParticles[this.px].position, "surfaceLevel");

                                //Clamp to Surface
                                if (this.clampIndex[this.fx] == 1)
                                {
                                    this.setParticles[this.px].position = new Vector3(this.setParticles[this.px].position.x, this.currPXWaterPos + 0.2f, this.setParticles[this.px].position.z);
                                }
                                //Clamp Under Water
                                if (this.clampIndex[this.fx] == 2)
                                {
                                    if (this.setParticles[this.px].position.y > this.currPXWaterPos - 0.2f)
                                    {
                                        this.setParticles[this.px].position = new Vector3(this.setParticles[this.px].position.x, this.currPXWaterPos - 0.2f, this.setParticles[this.px].position.z);
                                    }
                                }
                                //Clamp Above Water
                                if (this.clampIndex[this.fx] == 3)
                                {
                                    if (this.setParticles[this.px].position.y < this.currPXWaterPos + 0.2f)
                                    {
                                        this.setParticles[this.px].position = new Vector3(this.setParticles[this.px].position.x, this.currPXWaterPos + 0.2f, this.setParticles[this.px].position.z);
                                    }
                                }
                            }
                            this.useParticleComponent.SetParticles(this.setParticles, this.setParticles.Length);
                            this.useParticleComponent.Play();
                        }
                    }
                }
            }
        }



        public void AddSystem()
        {
            this.tempSystems = this.effectsSystems;
            this.tempClamp = this.clampIndex;

            this.effectsSystems = new Transform[this.tempSystems.Length + 1];
            this.clampIndex = new int[this.tempClamp.Length + 1];

            for (this.aR = 0; this.aR < this.tempSystems.Length; this.aR++)
            {
                this.effectsSystems[this.aR] = this.tempSystems[this.aR];
                this.clampIndex[this.aR] = this.tempClamp[this.aR];
            }
            this.effectsSystems[this.tempSystems.Length] = null;
            this.clampIndex[this.tempClamp.Length] = 0;
        }




        public void AddParticle(ParticleSystem.Particle particleData)
        {
            this.particleReserve.Add(particleData);
        }

        private IEnumerator updateFX()
        {

            //EMIT New Particles
            for (this.efx = 0; this.efx < this.effectsSystems.Length; this.efx++)
            {
                for (this.epx = 0; this.epx < this.particleReserve.Count; this.epx++)
                {
                    if (Mathf.Floor(this.particleReserve[this.epx].angularVelocity) == this.efx)
                    {
                        this.fxParticles[this.fx].Emit(1);
                    }
                }
            }

            //SET NEW Particle position and behaviors
            for (this.fx = 0; this.fx < this.effectsSystems.Length; this.fx++)
            {
                for (this.px = 0; this.px < this.particleReserve.Count; this.px++)
                {
                    if (Mathf.FloorToInt(this.particleReserve[this.px].angularVelocity) == this.fx)
                    {

                        //get particles
                        this.useParticleComponent = this.fxParticles[this.fx];
                        if (this.setParticles == null)
                            this.setParticles = new ParticleSystem.Particle[this.useParticleComponent.particleCount];
                        this.useParticleComponent.GetParticles(this.setParticles);
                        //set particles
                        for (this.sx = (this.useParticleComponent.particleCount - 1); this.sx < this.useParticleComponent.particleCount; this.sx++)
                        {
                            //set position
                            this.setParticles[this.px].position = this.particleReserve[this.px].position;

                            //set variables
                            this.setParticles[this.px].startSize = this.particleReserve[this.px].startSize;

                            this.setParticles[this.px].rotation = this.particleReserve[this.px].rotation;
                            this.setParticles[this.px].velocity = this.particleReserve[this.px].velocity;
                        }
                        this.useParticleComponent.SetParticles(this.setParticles, this.setParticles.Length);
                    }
                }
            }

            yield return null;
            if (this.particleReserve == null)
                this.particleReserve = new List<ParticleSystem.Particle>();
        }



        public void DeleteSystem(int sysNum)
        {
            this.tempSystems = this.effectsSystems;
            this.tempClamp = this.clampIndex;

            this.endLP = this.tempSystems.Length - 1;
            if (this.endLP <= 0)
            {
                this.endLP = 0;

                if (this.effectsSystems == null)
                    this.effectsSystems = new Transform[this.tempSystems.Length + 1];
                if (this.clampIndex == null)
                    this.clampIndex = new int[this.tempSystems.Length + 1];


            }
            else
            {

                //if (effectsSystems == null) effectsSystems = new Transform[endLP];
                //if (clampIndex == null) clampIndex = new int[endLP];

                this.tempSystems = new Transform[this.endLP];
                this.setInt = 0;

                for (this.aR = 0; this.aR < this.endLP + 1; this.aR++)
                {


                    //if (setInt < tempSystems.Length){
                    //	effectsSystems[aR] = tempSystems[setInt];
                    //	clampIndex[aR] = tempClamp[setInt];
                    //}

                    if (this.aR != sysNum)
                    {
                        this.tempSystems[this.setInt] = this.effectsSystems[this.aR];
                        this.setInt += 1;
                    }

                }

                this.effectsSystems = this.tempSystems;
                //Debug.Log("current:"+tempSystems.Length);

            }


        }

        private void OnApplicationQuit()
        {
            for (this.fx = 0; this.fx < (this.effectsSystems.Length); this.fx++)
            {
                Destroy(this.fxObjects[this.fx].gameObject);
            }
        }




    }
}