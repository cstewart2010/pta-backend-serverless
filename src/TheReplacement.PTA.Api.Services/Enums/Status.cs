namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a container for Pokemon Status
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Represents the fainted status, i.e. HP less than or equal to zero
        /// </summary>
        Fainted = -1,

        /// <summary>
        /// Represents the normal status
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Represents the Asleep status
        /// </summary>
        Asleep = 1,

        /// <summary>
        /// Represents the Badly Poisoned status
        /// </summary>
        BadlyPoisoned = 2,

        /// <summary>
        /// Represents the Burned status
        /// </summary>
        Burned = 3,

        /// <summary>
        /// Represent the Frozen status
        /// </summary>
        Frozen = 4,

        /// <summary>
        /// Represents the Paralyzed status
        /// </summary>
        Paralyzed = 5,

        /// <summary>
        /// Represents the Poisoned status
        /// </summary>
        Poisoned = 6
    }
}
