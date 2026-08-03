using System.Collections.Generic;
using System.Linq;
using claims.src.part.structure;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.renderer
{
    /// <summary>
    /// Hands the tesselator one texture: a coat of arms, flattened into a single atlas entry.
    ///
    /// The layers are not drawn one over another at render time - the engine does that when baking,
    /// through <see cref="CompositeTexture.BlendedOverlays"/>: the first layer is the base and every
    /// following one is blended onto it, and the result is inserted into the atlas as one texture.
    /// That is why no external library is needed for layered heraldry.
    ///
    /// The atlas is passed in rather than assumed: a mesh is drawn with whichever atlas its renderer
    /// binds, and the capture flag is rendered against the item atlas, not the block one. Repeated
    /// use of the same arms is not a problem - GetOrInsertTexture returns the existing entry.
    /// </summary>
    public class EmblemTexSource : ITexPositionSource
    {
        private readonly ITextureAtlasAPI atlas;
        private readonly TextureAtlasPosition texPos;

        /// <summary>Whether the arms resolved to a real texture. False means the caller should keep its old look.</summary>
        public bool Resolved { get; }

        public EmblemTexSource(ICoreClientAPI capi, string emblem, ITextureAtlasAPI targetAtlas)
        {
            this.atlas = targetAtlas;

            List<string> layers = EmblemHandler.Parse(emblem);
            if (layers.Count == 0)
            {
                texPos = atlas.UnknownTexturePosition;
                Resolved = false;
                return;
            }

            var ctex = new CompositeTexture(new AssetLocation("claims", EmblemHandler.TexturePath(layers[0])));
            if (layers.Count > 1)
            {
                ctex.BlendedOverlays = layers.Skip(1).Select(layer => new BlendedOverlayTexture
                {
                    Base = new AssetLocation("claims", EmblemHandler.TexturePath(layer)),
                    BlendMode = EnumColorBlendMode.Normal
                }).ToArray();
            }
            ctex.Bake(capi.Assets);

            Resolved = atlas.GetOrInsertTexture(ctex, out _, out texPos) && texPos != null;
            if (!Resolved) texPos = atlas.UnknownTexturePosition;
        }

        /// <summary>The banner shape asks for "#color"; every face of it gets the same arms.</summary>
        public TextureAtlasPosition this[string textureCode] => texPos;

        public Size2i AtlasSize => atlas.Size;
    }
}
