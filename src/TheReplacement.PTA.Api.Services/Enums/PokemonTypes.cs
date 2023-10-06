using System;

namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a container for Pokemon typings
    /// </summary>
    [Flags]
    public enum PokemonTypes
    {
        /// <summary>
        /// Represent no valid Type
        /// </summary>
        None = 0,

        /// <summary>
        /// Represent a Normal Type
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Represent a Fire Type
        /// </summary>
        Fire = 2,

        /// <summary>
        /// Represent a Water Type
        /// </summary>
        Water = 4,

        /// <summary>
        /// Represent a Grass Type
        /// </summary>
        Grass = 8,

        /// <summary>
        /// Represent a Electric Type
        /// </summary>
        Electric = 16,

        /// <summary>
        /// Represent a Ice Type
        /// </summary>
        Ice = 32,

        /// <summary>
        /// Represent a Fighting Type
        /// </summary>
        Fighting = 64,

        /// <summary>
        /// Represent a Poison Type
        /// </summary>
        Poison = 128,

        /// <summary>
        /// Represent a Ground Type
        /// </summary>
        Ground = 256,

        /// <summary>
        /// Represent a Flying Type
        /// </summary>
        Flying = 512,

        /// <summary>
        /// Represent a Psychic Type
        /// </summary>
        Psychic = 1024,

        /// <summary>
        /// Represent a Bug Type
        /// </summary>
        Bug = 2048,

        /// <summary>
        /// Represent a Rock Type
        /// </summary>
        Rock = 4096,

        /// <summary>
        /// Represent a Ghost Type
        /// </summary>
        Ghost = 8192,

        /// <summary>
        /// Represent a Dark Type
        /// </summary>
        Dark = 16384,

        /// <summary>
        /// Represent a Dragon Type
        /// </summary>
        Dragon = 32768,

        /// <summary>
        /// Represent a Steel Type
        /// </summary>
        Steel = 65536,

        /// <summary>
        /// Represent a Fairy Type
        /// </summary>
        Fairy = 131072,
    }
}
