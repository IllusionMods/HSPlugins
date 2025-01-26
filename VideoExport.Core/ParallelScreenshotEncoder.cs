using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BepInEx;
using ToolBox.Extensions;
using UnityEngine;

namespace VideoExport.Core
{
    class ParallelScreenshotEncoder
    {
        private readonly Dictionary<int, Texture2D> keepAlive;
        private int currentKey;

        public ParallelScreenshotEncoder()
        {
            keepAlive = new Dictionary<int, Texture2D>();
            currentKey = 0;
        }

        /// <summary>
        /// WARNING: Texture2D is not thread-safe. Once queued, wait for the encoder to finish before using the texture again.
        /// </summary>
        public void QueueScreenshotUnsafe(Texture2D texture, VideoExport.ImgFormat format, string savePath)
        {
            int key = currentKey;
            lock (keepAlive)
            {
                keepAlive.Add(key, texture);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (keepAlive)
                {
                    byte[] frame = TextureEncoder.EncodeTexture(texture, format);
                    File.WriteAllBytes(savePath, frame);
                    keepAlive.Remove(key);
                }
            });

            currentKey++;
        }

        /// <summary>
        /// WARNING: Texture2D is not thread-safe. This function also schedules the texture for destruction on the main thread.
        /// </summary>
        public void QueueScreenshotDestructive(Texture2D texture, VideoExport.ImgFormat format, string savePath)
        {
            int key = currentKey;
            lock (keepAlive)
            {
                keepAlive.Add(key, texture);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (keepAlive)
                {
                    byte[] frame = TextureEncoder.EncodeTexture(texture, format);
                    File.WriteAllBytes(savePath, frame);

                    Texture2D tex;
                    if (keepAlive.TryGetValue(key, out tex))
                    {
                        keepAlive.Remove(key);
                        ThreadingHelper.Instance.StartSyncInvoke(() => UnityEngine.Object.Destroy(tex));
                    }
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
