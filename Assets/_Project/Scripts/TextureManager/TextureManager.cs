using System.Collections.Generic;
using UnityEngine;

namespace SoraTehk {
    public class TextureManager : MonoBehaviour {
        internal record struct TextureDataKey(int Width, int Height, TextureFormat TextureFormat, int MipCount) {
        }

        // TODO: Fix duplicate references (use HashSet, etc..)
        internal readonly Dictionary<TextureDataKey, Queue<Texture2D>> m_TexturePool = new Dictionary<TextureDataKey, Queue<Texture2D>>();

        internal float m_NextCleanupTime;

        private void Update() {
            if (m_NextCleanupTime >= Time.time) return;
            // TODO: Hard-coded 60 secs
            m_NextCleanupTime = Time.time + 60f;

            CleanupUnused();
        }

        private void OnDestroy() {
            CleanupUnused();
        }

        /// <summary>
        /// Gets a texture from the pool or creates a new one.
        /// </summary>
        public Texture2D Acquire(int width, int height, TextureFormat format = TextureFormat.RGBA32, int mipCount = 0) {
            var key = new TextureDataKey(width, height, format, mipCount);

            // Get the pool queue first
            if (!m_TexturePool.TryGetValue(key, out var poolQueue)) {
                poolQueue = new Queue<Texture2D>();
                m_TexturePool.Add(key, poolQueue);
            }

            // Then create or get the texture
            if (!poolQueue.TryDequeue(out var texture)) {
                texture = new Texture2D(width, height, format, mipCount, false);
            }
            else {
                // TODO: Reset settings
            }

            return texture;
        }
        public Texture2D AcquireClone(Texture2D scrTex) {
            var retTex = Acquire(scrTex.width, scrTex.height, scrTex.format, scrTex.mipmapCount);
            Graphics.CopyTexture(scrTex, retTex);
            return retTex;
        }

        /// <summary>
        /// Releases a texture back to the pool for reuse.
        /// </summary>
        public void Release(Texture2D? texture) {
            if (texture == null) return;

            var key = new TextureDataKey(texture.width, texture.height, texture.format, texture.mipmapCount);

            // Get the pool queue first
            if (!m_TexturePool.TryGetValue(key, out var poolQueue)) {
                poolQueue = new Queue<Texture2D>();
                m_TexturePool.Add(key, poolQueue);
            }

            // Then add the texture to the pool
            poolQueue.Enqueue(texture);
        }

        public void CleanupUnused() {
            foreach (var pool in m_TexturePool.Values) {
                while (pool.TryDequeue(out var texture)) {
                    Destroy(texture);
                }
            }

            m_TexturePool.Clear();
        }
    }
}