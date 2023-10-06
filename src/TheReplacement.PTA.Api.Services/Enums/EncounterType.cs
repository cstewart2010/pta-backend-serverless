using TheReplacement.PTA.Api.Services.Models;

namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Container for all possible <see cref="SettingModel"/> types
    /// </summary>
    public enum SettingType
    {
        /// <summary>
        /// Represents a Hostile Setting 
        /// </summary>
        Hostile = 1,

        /// <summary>
        /// Represents a NonHostile Setting 
        /// </summary>
        NonHostile = 2,

        /// <summary>
        /// Represents an Setting with both Wild and Trainer participants
        /// </summary>
        Hybrid = 3
    }
}
