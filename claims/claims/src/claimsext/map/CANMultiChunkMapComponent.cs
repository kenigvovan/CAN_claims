using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace claims.src.claimsext.map
{
    public class CANMultiChunkMapComponent: MapComponent
    {
        public static int ChunkLen = 4;

        public static LoadedTexture tmpTexture;

        public float renderZ = 50f;

        public Vec2i chunkCoord;

        public LoadedTexture Texture;

        private static int[] emptyPixels;

        private Vec3d worldPos;

        private Vec2f viewPos = new Vec2f();

        private bool[,] chunkSet = new bool[ChunkLen, ChunkLen];

        /// <summary>Tiles queued by <see cref="setChunk"/>, painted in one batch by
        /// <see cref="FinishSetChunks"/>. Indexed dz * ChunkLen + dx.</summary>
        private int[][] pixelsToSet;

        private int chunksize;

        public float TTL = MaxTTL;

        public static float MaxTTL = 15f;

        private Vec2i tmpVec = new Vec2i();

        public bool AnyChunkSet
        {
            get
            {
                for (int i = 0; i < ChunkLen; i++)
                {
                    for (int j = 0; j < ChunkLen; j++)
                    {
                        if (chunkSet[i, j])
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool IsChunkSet(int dx, int dz)
        {
            if (dx < 0 || dz < 0)
            {
                return false;
            }

            return chunkSet[dx, dz];
        }

        public CANMultiChunkMapComponent(ICoreClientAPI capi, Vec2i baseChunkCord)
            : base(capi)
        {
            chunkCoord = baseChunkCord;
            chunksize = capi.World.BlockAccessor.ChunkSize;
            worldPos = new Vec3d(baseChunkCord.X * chunksize, 0.0, baseChunkCord.Y * chunksize);
            if (emptyPixels == null)
            {
                int num = ChunkLen * chunksize;
                emptyPixels = new int[num * num];
            }
        }

        /// <summary>Queues one game chunk for painting. The GL work happens in
        /// <see cref="FinishSetChunks"/> so that a batch of tiles costs one framebuffer, not one per
        /// tile.</summary>
        public void setChunk(int dx, int dz, int[] pixels)
        {
            if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + (ChunkLen - 1) + "]");
            }

            if (pixelsToSet == null)
            {
                pixelsToSet = new int[ChunkLen * ChunkLen][];
            }
            pixelsToSet[dz * ChunkLen + dx] = pixels;
            chunkSet[dx, dz] = true;
        }

        /// <summary>Paints every tile queued since the last call into this component's texture.
        /// Creating and destroying the framebuffer is what a chunk paint actually costs, hence one
        /// framebuffer for the whole batch and a single mipmap pass at the end.</summary>
        public void FinishSetChunks()
        {
            if (pixelsToSet == null) return;

            if (tmpTexture == null || tmpTexture.Disposed)
            {
                tmpTexture = new LoadedTexture(capi, 0, chunksize, chunksize);
            }

            if (Texture == null || Texture.Disposed)
            {
                int num = ChunkLen * chunksize;
                Texture = new LoadedTexture(capi, 0, num, num);
                capi.Render.LoadOrUpdateTextureFromRgba(emptyPixels, linearMag: false, 0, ref Texture);
            }

            FrameBufferRef fb = capi.Render.CreateFrameBuffer(Texture);
            for (int i = 0; i < pixelsToSet.Length; i++)
            {
                if (pixelsToSet[i] == null) continue;

                capi.Render.LoadOrUpdateTextureFromRgba(pixelsToSet[i], linearMag: false, 0, ref tmpTexture);
                // alphaTest -1 turns blending off inside the engine, so the tile overwrites what was
                // there - that is what lets a chunk that lost its plots go back to transparent.
                capi.Render.RenderTextureIntoFrameBuffer(0, tmpTexture, 0f, 0f, chunksize, chunksize, fb,
                    chunksize * (i % ChunkLen), chunksize * (i / ChunkLen), -1);
                pixelsToSet[i] = null;
            }
            capi.Render.DestroyFrameBuffer(fb);
            capi.Render.BindTexture2d(Texture.TextureId);
            capi.Render.GlGenerateTex2DMipmaps();
            pixelsToSet = null;
        }

        public void unsetChunk(int dx, int dz)
        {
            if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
            {
                throw new ArgumentOutOfRangeException("dx/dz must be within [0," + (ChunkLen - 1) + "]");
            }
            chunkSet[dx, dz] = false;
        }

        public override void Render(GuiElementMap map, float dt)
        {
            // A component exists from the moment its first tile is queued, the texture only from the
            // first FinishSetChunks.
            if (Texture == null || Texture.Disposed) return;

            map.TranslateWorldPosToViewPos(worldPos, ref viewPos);
            capi.Render.Render2DTexture(Texture.TextureId, (int)(map.Bounds.renderX + (double)viewPos.X), (int)(map.Bounds.renderY + (double)viewPos.Y), (int)((float)Texture.Width * map.ZoomLevel), (int)((float)Texture.Height * map.ZoomLevel), renderZ);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public void ActuallyDispose()
        {
            // tmpTexture is shared by every component - it goes away in DisposeStatic, not here.
            Texture?.Dispose();
        }

        public bool IsVisible(HashSet<Vec2i> curVisibleChunks)
        {
            for (int i = 0; i < ChunkLen; i++)
            {
                for (int j = 0; j < ChunkLen; j++)
                {
                    tmpVec.Set(chunkCoord.X + i, chunkCoord.Y + j);
                    if (curVisibleChunks.Contains(tmpVec))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void DisposeStatic()
        {
            tmpTexture?.Dispose();
            emptyPixels = null;
            tmpTexture = null;
        }
    }
}
