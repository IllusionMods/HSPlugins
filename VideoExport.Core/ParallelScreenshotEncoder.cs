using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace VideoExport.Core
{
    class ParallelScreenshotEncoder
    {
        private Dictionary<int, Texture2D> keepAlive;
        private Mutex keepAliveMutex;

        private int currentKey;

        public ParallelScreenshotEncoder()
        {
            Init();
        }

        public void Init()
        {
            currentKey = 0;
            keepAlive = new Dictionary<int, Texture2D>();
            keepAliveMutex = new Mutex();
        }

        /// <summary>
        /// WARNING: Texture2D is not thread-safe
        /// </summary>
        public void QueueScreenshot(Texture2D texture, VideoExport.ImgFormat format, string savePath)
        {
            int key = currentKey;
            lock (keepAliveMutex)
            {
                keepAlive.Add(key, texture);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (keepAliveMutex)
                {
                    byte[] frame = TextureEncoder.EncodeTexture(texture, format);
                    File.WriteAllBytes(savePath, frame);
                    keepAlive.Remove(key);
                }
            });

            currentKey++;
        }

        public bool WaitForAll()
        {
            const int GIVEUP_THRESHOLD = 100;
            int prevRemaining = keepAlive.Count;
            int giveup = 0;

            while (keepAlive.Count > 0)
            {
                if (giveup >= GIVEUP_THRESHOLD)
                {
                    keepAlive.Clear();
                    return false;
                }

                if (prevRemaining == keepAlive.Count)
                {
                    giveup++;
                }
                else
                {
                    prevRemaining = keepAlive.Count;
                    giveup = 0;
                }

                Thread.Sleep(100);
            }
            return true;
        }
    }
}
