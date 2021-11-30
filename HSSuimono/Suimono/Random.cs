using UnityEngine;
using System.Collections;

namespace Suimono.Core
{
    /// <summary>
    /// Simple and fast random number generator which can reset to a specific iteration
    /// </summary>

    public class Random
    {
        //Initialiser magic values
        private const ulong m_A_Init = 181353;
        private const ulong m_B_Init = 7;

        //Seed
        public int m_seed;

        //State
        public ulong m_stateA, m_stateB;

        /// <summary>
        /// Contruct and initialise the RNG
        /// </summary>
        /// <param name="seed"></param>
        public Random(int seed = 1)
        {
            this.m_seed = seed;
            if (this.m_seed == 0)
            {
                this.m_seed = 1;
            }
            this.Reset();
        }

        /// <summary>
        /// Reset it to its initial state with the existing seed
        /// </summary>
        public void Reset()
        {
            this.m_stateA = m_A_Init * (uint)this.m_seed;
            this.m_stateB = m_B_Init * (uint)this.m_seed;
        }

        /// <summary>
        /// Restet it to a new state with a new seed
        /// </summary>
        /// <param name="seed">New seed</param>
        public void Reset(int seed)
        {
            this.m_seed = seed;
            if (this.m_seed == 0)
            {
                this.m_seed = 1;
            }
            this.Reset();
        }

        /// <summary>
        /// Reset it to the stade defined by the state variables passed in
        /// </summary>
        public void Reset(ulong stateA, ulong stateB)
        {
            Debug.Log("Resetting RNG State " + stateA + " " + stateB);
            this.m_stateA = stateA;
            this.m_stateB = stateB;
        }

        /// <summary>
        /// Return the current state for serialisation
        /// </summary>
        /// <param name="seed">Seed</param>
        /// <param name="stateA">State A</param>
        /// <param name="stateB">State B</param>
        public void GetState(out int seed, out ulong stateA, out ulong stateB)
        {
            seed = this.m_seed;
            stateA = this.m_stateA;
            stateB = this.m_stateB;
        }

        //Check here for wrapper functions
        //https://github.com/tucano/UnityRandom/blob/master/lib/MersenneTwister.cs

        /// <summary>
        /// Get the next value
        /// </summary>
        /// <returns>A value between zero and one inclusive</returns>
        public float Next()
        {
            ulong x = this.m_stateA;
            ulong y = this.m_stateB;
            this.m_stateA = y;
            x ^= x << 23;
            x ^= x >> 17;
            x ^= y ^ (y >> 26);
            this.m_stateB = x;
            return (float)(x + y) / (float)ulong.MaxValue;
        }

        /// <summary>
        /// Return the next int
        /// </summary>
        /// <returns></returns>
        public int NextInt()
        {
            return (int)(this.Next() * int.MaxValue);
        }

        /// <summary>
        /// Get the next value and scale it between the min and max values supplied inclusive
        /// </summary>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Next value scaled beteen the range supplied</returns>
        public float Next(float min, float max)
        {
            return min + (this.Next() * (max - min));
        }

        /// <summary>
        /// Get the next value and scale it between the min and max values supplied inclusive
        /// </summary>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Next value scaled beteen the range supplied</returns>
        public int Next(int min, int max)
        {
            if (min == max)
            {
                return min;
            }
            return (int)this.Next((float)min, (float)max + 0.999f);
        }

        /// <summary>
        /// Get the next value as a vector
        /// </summary>
        /// <returns>Next value as a vector in ranges 0..1</returns>
        public Vector3 NextVector()
        {
            return new Vector3(this.Next(), this.Next(), this.Next());
        }

        /// <summary>
        /// Get the next value as a vector
        /// </summary>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns></returns>
        public Vector3 NextVector(float min, float max)
        {
            return new Vector3(this.Next(min, max), this.Next(min, max), this.Next(min, max));
        }
    }
}
