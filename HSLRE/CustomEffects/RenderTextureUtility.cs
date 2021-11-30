using UnityEngine;
using System.Collections.Generic;

namespace HSLRE.CustomEffects
{
    public class RenderTextureUtility
    {
        // Token: 0x0600003C RID: 60 RVA: 0x00003BD8 File Offset: 0x00001DD8
        public RenderTexture GetTemporaryRenderTexture(int width, int height, int depthBuffer = 0, RenderTextureFormat format = RenderTextureFormat.ARGBHalf, FilterMode filterMode = FilterMode.Bilinear)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, depthBuffer, format);
            temporary.filterMode = filterMode;
            temporary.wrapMode = TextureWrapMode.Clamp;
            temporary.name = "RenderTextureUtilityTempTexture";
            this.m_TemporaryRTs.Add(temporary);
            return temporary;
        }

        // Token: 0x0600003D RID: 61 RVA: 0x00003C18 File Offset: 0x00001E18
        public void ReleaseTemporaryRenderTexture(RenderTexture rt)
        {
            if (rt == null)
            {
                return;
            }
            if (!this.m_TemporaryRTs.Contains(rt))
            {
                UnityEngine.Debug.LogErrorFormat("Attempting to remove texture that was not allocated: {0}", new object[]
                {
                    rt
                });
                return;
            }
            this.m_TemporaryRTs.Remove(rt);
            RenderTexture.ReleaseTemporary(rt);
        }

        // Token: 0x0600003E RID: 62 RVA: 0x00003C68 File Offset: 0x00001E68
        public void ReleaseAllTemporaryRenderTextures()
        {
            for (int i = 0; i < this.m_TemporaryRTs.Count; i++)
            {
                RenderTexture.ReleaseTemporary(this.m_TemporaryRTs[i]);
            }
            this.m_TemporaryRTs.Clear();
        }

        // Token: 0x0400008F RID: 143
        private List<RenderTexture> m_TemporaryRTs = new List<RenderTexture>();
    }
}
