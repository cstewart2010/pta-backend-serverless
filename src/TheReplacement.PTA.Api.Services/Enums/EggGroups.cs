namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Container for all possible egg group combinations
    /// </summary>
    [Flags]
    public enum EggGroups
    {
        /// <summary>
        /// Represents the Undiscovered
        /// </summary>
        Undiscovered = 0,

        /// <summary>
        /// Represents the Amorphus
        /// </summary>
        Amorphus = 1,

        /// <summary>
        /// Represents the Bug
        /// </summary>
        Bug = 2,

        /// <summary>
        /// Represents the Dragon
        /// </summary>
        Dragon = 4,

        /// <summary>
        /// Represents the Fairy
        /// </summary>
        Fairy = 8,

        /// <summary>
        /// Represents the Field
        /// </summary>
        Field = 16,

        /// <summary>
        /// Represents the Flying
        /// </summary>
        Flying = 32,

        /// <summary>
        /// Represents the Grass
        /// </summary>
        Grass = 64,

        /// <summary>
        /// Represents the Humanshape
        /// </summary>
        Humanshape = 128,

        /// <summary>
        /// Represents the Mineral
        /// </summary>
        Mineral = 256,

        /// <summary>
        /// Represents the Monster
        /// </summary>
        Monster = 512,

        /// <summary>
        /// Represents the Water 1
        /// </summary>
        Water1 = 1024,

        /// <summary>
        /// Represents the Water 2
        /// </summary>
        Water2 = 2048,

        /// <summary>
        /// Represents the Water 3
        /// </summary>
        Water3 = 4096,

        /// <summary>
        /// Represents the Ditto Egg Group
        /// </summary>
        Ditto = 8191
    }
}
