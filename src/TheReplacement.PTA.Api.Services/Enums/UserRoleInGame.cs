namespace TheReplacement.PTA.Api.Services.Enums
{
    /// <summary>
    /// Represents a User's Role within a PTA Game Session
    /// </summary>
    [Flags]
    public enum UserRoleInGame
    {
        /// <summary>
        /// Represents a user that is not part of the game session
        /// </summary>
        NotInGame = 0,

        /// <summary>
        /// Represents a user that is a player character within the game session
        /// </summary>
        Trainer = 1,

        /// <summary>
        /// Represents a user with GameMaster privileges within the game session
        /// </summary>
        GameMaster = 2,

        /// <summary>
        /// Represents the current owner and main GM of the game session
        /// </summary>
        Owner = 6
    }
}
