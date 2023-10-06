using System;

namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Containers for all berry flavors
    /// </summary>
    [Flags]
    public enum Flavors
    {
        /// <summary>
        /// Represents the Spicy Flavor
        /// </summary>
        Spicy = 1,

        /// <summary>
        /// Represents the Sour Flavor
        /// </summary>
        Sour = 2,

        /// <summary>
        /// Represents the Dry Flavor
        /// </summary>
        Dry = 4,

        /// <summary>
        /// Represents the Bitter Flavor
        /// </summary>
        Bitter = 8,

        /// <summary>
        /// Represents the Sweet Flavor
        /// </summary>
        Sweet = 16
    }
}
