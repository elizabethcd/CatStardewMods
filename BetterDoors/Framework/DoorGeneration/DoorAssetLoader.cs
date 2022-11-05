using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace BetterDoors.Framework.DoorGeneration
{
    /// <summary>Registers door tile sheets with SMAPI so maps can access them.</summary>
    internal class DoorAssetLoader
    {
        /*********
        ** Fields
        *********/

        /// <summary>Map of currently registered textures by asset key.</summary>
        private readonly IDictionary<string, Texture2D> doorTextures = new Dictionary<string, Texture2D>();

        /// <summary>Provides an API for loading content assets.</summary>
        private readonly IGameContentHelper helper;

        /*********
        ** Public methods
        *********/

        /// <summary>Construct an instance.</summary>
        /// <param name="helper">Provides an API for loading content assets.</param>
        public DoorAssetLoader(IGameContentHelper helper)
        {
            this.helper = helper;
        }

        /// <summary>Add textures to be loaded.</summary>
        /// <param name="textures">The textures to add.</param>
        public void AddTextures(IDictionary<string, Texture2D> textures)
        {
            foreach (KeyValuePair<string, Texture2D> texture in textures)
                this.doorTextures[texture.Key] = texture.Value;

            this.InvalidateCache(textures);
        }

        /// <summary>Get whether this instance can load the initial version of the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanLoad(IAssetName assetName)
        {
            return this.doorTextures.Keys.Any(key => assetName.IsEquivalentTo(key));
        }

        /// <summary>Load a matched asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public Texture2D Load(IAssetName asset)
        {
            return this.doorTextures[asset.Name];
        }

        /// <summary>Reset the asset loader, clearing loaded textures.</summary>
        public void Reset()
        {
            IDictionary<string, Texture2D> tempTextures = new Dictionary<string, Texture2D>(this.doorTextures);
            this.doorTextures.Clear();

            this.InvalidateCache(tempTextures);
        }

        /*********
        ** Private methods
        *********/

        /// <summary>Invalidates the cache.</summary>
        /// <param name="textures">The textures to invalidate.</param>
        private void InvalidateCache(IDictionary<string, Texture2D> textures)
        {
            this.helper.InvalidateCache(asset => asset.DataType == typeof(Texture2D) && textures.Keys.Any(key => asset.Name.IsEquivalentTo(key)));
        }
    }
}
